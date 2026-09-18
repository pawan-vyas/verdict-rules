/**
 * Marketplace eligibility -- the flagship generic-context example, as real code.
 *
 * See docs/samples/marketplace-eligibility/README.md for the full design
 * and fixtures/marketplace_eligibility/README.md for the shared,
 * cross-language data contract this module reproduces.
 *
 * Nothing here is illustrative pseudocode: every function is imported and
 * exercised by test/marketplace-eligibility.test.js.
 */
import { readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

import { AndRule, FunctionRule, RulesEngine } from "verdict-rules";

const __dirname = dirname(fileURLToPath(import.meta.url));
// Fixture data lives at the repo root, shared by every language's own port of
// this example -- see fixtures/marketplace_eligibility/README.md for the contract.
const FIXTURES = join(__dirname, "..", "..", "..", "fixtures", "marketplace_eligibility");

export const PRICE_FLOOR_CENTS = 100;
export const ALLOWED_CATEGORIES = ["books", "electronics", "home"];
export const PURCHASE_LIMIT_CENTS = 100_000;
export const HIGH_VALUE_THRESHOLD_CENTS = 50_000;
export const BLOCKED_COUNTRIES = ["ir", "nk"];
export const NEW_SELLER_THRESHOLD_DAYS = 30;

/** @typedef {{ verified: boolean }} IdentityFlag */
/** @typedef {{ sellerId: string, sellerVerified: boolean, listingPriceCents: number, category: string }} SellerListingContext */
/** @typedef {{ buyerId: string, buyerVerified: boolean, purchaseAmountCents: number, buyerBalanceCents: number }} BuyerPurchaseContext */

/**
 * Adapts a rule built against TInner to run inside a composite built on TOuter.
 *
 * Not part of verdict-rules itself -- a consumer-defined adapter, exactly
 * as free to exist as a new rule shape is, with no changes needed on
 * verdict-rules' side to support it. See
 * docs/extending/reusing-a-rule-across-contexts/js.md for the standalone
 * version of this same pattern.
 */
export class ProjectingRule {
  /**
   * @param {import("verdict-rules").Rule<any>} inner
   * @param {(outer: any) => any} project
   */
  constructor(inner, project) {
    this.name = inner.name;
    this.group = inner.group;
    this.inner = inner;
    this.project = project;
  }

  evaluate(context) {
    return this.inner.evaluate(this.project(context));
  }
}

/** Written once, against IdentityFlag alone -- reused on both sides via ProjectingRule. */
async function isVerifiedIdentity(context) {
  return { ruleName: "is_verified_identity", passed: context.verified };
}

function sellerIdentityRule() {
  const inner = new FunctionRule("is_verified_identity", isVerifiedIdentity);
  return new ProjectingRule(inner, (ctx) => ({ verified: ctx.sellerVerified }));
}

function buyerIdentityRule() {
  const inner = new FunctionRule("is_verified_identity", isVerifiedIdentity);
  return new ProjectingRule(inner, (ctx) => ({ verified: ctx.buyerVerified }));
}

async function priceFloorMet(context) {
  return {
    ruleName: "price_floor_met",
    passed: context.listingPriceCents >= PRICE_FLOOR_CENTS,
    detail: `${context.listingPriceCents} vs ${PRICE_FLOOR_CENTS}`,
  };
}

async function categoryAllowed(context) {
  return {
    ruleName: "category_allowed",
    passed: ALLOWED_CATEGORIES.includes(context.category),
  };
}

async function sufficientBalance(context) {
  return {
    ruleName: "sufficient_balance",
    passed: context.buyerBalanceCents >= context.purchaseAmountCents,
  };
}

async function purchaseLimitNotExceeded(context) {
  return {
    ruleName: "purchase_limit_not_exceeded",
    passed: context.purchaseAmountCents <= PURCHASE_LIMIT_CENTS,
  };
}

/**
 * Build the typed listing-eligibility composite for one seller.
 *
 * @returns {{ listingEligible: AndRule<SellerListingContext>, sellerVerified: ProjectingRule }}
 *   The full composite, and the identity sub-rule alone, so a caller (and
 *   the test suite) can distinguish "which check failed" without
 *   re-running anything.
 */
export function buildSellerCheck() {
  const sellerVerified = sellerIdentityRule();
  const listingEligible = new AndRule("listing_eligible", [
    sellerVerified,
    new FunctionRule("price_floor_met", priceFloorMet),
    new FunctionRule("category_allowed", categoryAllowed),
  ]);
  return { listingEligible, sellerVerified };
}

/**
 * Build the typed purchase-eligibility composite for one buyer.
 *
 * @returns {{ purchaseEligible: AndRule<BuyerPurchaseContext>, buyerVerified: ProjectingRule }}
 */
export function buildBuyerCheck() {
  const buyerVerified = buyerIdentityRule();
  const purchaseEligible = new AndRule("purchase_eligible", [
    buyerVerified,
    new FunctionRule("sufficient_balance", sufficientBalance),
    new FunctionRule("purchase_limit_not_exceeded", purchaseLimitNotExceeded),
  ]);
  return { purchaseEligible, buyerVerified };
}

async function highValueFlag(context) {
  return { ruleName: "high_value_flag", passed: context.amount_cents > HIGH_VALUE_THRESHOLD_CENTS };
}

async function blockedCountryFlag(context) {
  return { ruleName: "blocked_country_flag", passed: BLOCKED_COUNTRIES.includes(context.country) };
}

async function newSellerFlag(context) {
  return { ruleName: "new_seller_flag", passed: context.seller_age_days < NEW_SELLER_THRESHOLD_DAYS };
}

/**
 * Build the dict-context compliance catalog.
 *
 * Unlike the seller/buyer sides, this is deliberately untyped: a
 * compliance team adds a new flag by registering one more rule here,
 * never by agreeing on a shared typed context every existing flag would
 * otherwise need to accommodate too. See
 * docs/architecture/README.md#generic-context for the full reasoning.
 *
 * @returns {RulesEngine<import("verdict-rules").Context>}
 */
export function buildComplianceCatalog() {
  return new RulesEngine([
    new FunctionRule("high_value_flag", highValueFlag),
    new FunctionRule("blocked_country_flag", blockedCountryFlag),
    new FunctionRule("new_seller_flag", newSellerFlag),
  ]);
}

export function loadSellers(path) {
  return JSON.parse(readFileSync(path, "utf8"));
}

export function loadBuyers(path) {
  return JSON.parse(readFileSync(path, "utf8"));
}

export function loadComplianceEvents(path) {
  return JSON.parse(readFileSync(path, "utf8"));
}

/** @returns {SellerListingContext} */
export function sellerContext(sellerId, row) {
  return {
    sellerId,
    sellerVerified: row.seller_verified,
    listingPriceCents: row.listing_price_cents,
    category: row.category,
  };
}

/** @returns {BuyerPurchaseContext} */
export function buyerContext(buyerId, row) {
  return {
    buyerId,
    buyerVerified: row.buyer_verified,
    purchaseAmountCents: row.purchase_amount_cents,
    buyerBalanceCents: row.buyer_balance_cents,
  };
}

async function demo() {
  const sellers = loadSellers(join(FIXTURES, "sellers.json"));
  const buyers = loadBuyers(join(FIXTURES, "buyers.json"));
  const events = loadComplianceEvents(join(FIXTURES, "compliance_events.json"));
  const catalog = buildComplianceCatalog();

  console.log("=== Marketplace Eligibility -- Demo ===\n");
  console.log("--- Sellers ---");
  for (const [sellerId, row] of Object.entries(sellers)) {
    const { listingEligible } = buildSellerCheck();
    const result = await listingEligible.evaluate(sellerContext(sellerId, row));
    console.log(`${sellerId.padEnd(14)}: ${result.passed ? "ELIGIBLE" : "NOT ELIGIBLE"}`);
  }

  console.log("\n--- Buyers ---");
  for (const [buyerId, row] of Object.entries(buyers)) {
    const { purchaseEligible } = buildBuyerCheck();
    const result = await purchaseEligible.evaluate(buyerContext(buyerId, row));
    console.log(`${buyerId.padEnd(14)}: ${result.passed ? "ELIGIBLE" : "NOT ELIGIBLE"}`);
  }

  console.log("\n--- Compliance events (one shared, untyped engine) ---");
  for (const [eventId, row] of Object.entries(events)) {
    const fired = [];
    for (const name of catalog.ruleNames) {
      if ((await catalog.runNamed(name, row)).passed) fired.push(name);
    }
    console.log(`${eventId.padEnd(22)}: ${fired.length ? JSON.stringify(fired) : "clean"}`);
  }
}

if (import.meta.url === `file://${process.argv[1]}`) {
  await demo();
}
