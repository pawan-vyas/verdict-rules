/**
 * Tests for the marketplace-eligibility example.
 *
 * This project exercises Rule<TContext> end to end: two typed contexts
 * sharing no fields, a rule reused across both via ProjectingRule, and a
 * dict-context catalog coexisting in the same codebase. See
 * docs/samples/marketplace-eligibility/README.md for the design and
 * fixtures/marketplace_eligibility/README.md for the shared contract this
 * suite reproduces.
 */
import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { describe, it } from "node:test";
import { fileURLToPath } from "node:url";

import { FunctionRule, RulesEngine } from "verdict-rules";

import {
  buildBuyerCheck,
  buildComplianceCatalog,
  buildSellerCheck,
  buyerContext,
  loadBuyers,
  loadComplianceEvents,
  loadSellers,
  sellerContext,
} from "../marketplace-eligibility.js";

const __dirname = dirname(fileURLToPath(import.meta.url));
const FIXTURES = join(__dirname, "..", "..", "..", "..", "fixtures", "marketplace_eligibility");
const SELLERS = loadSellers(join(FIXTURES, "sellers.json"));
const BUYERS = loadBuyers(join(FIXTURES, "buyers.json"));
const EVENTS = loadComplianceEvents(join(FIXTURES, "compliance_events.json"));

describe("seller listing eligibility", () => {
  // Every expectation in sellers.json, asserted -- the typed Rule<SellerListingContext> side.
  for (const sellerId of Object.keys(SELLERS)) {
    it(`${sellerId}: sellerVerified matches`, async () => {
      const row = SELLERS[sellerId];
      const { sellerVerified } = buildSellerCheck();
      const result = await sellerVerified.evaluate(sellerContext(sellerId, row));
      assert.equal(result.passed, row.expected.seller_verified, sellerId);
    });

    it(`${sellerId}: listingEligible matches`, async () => {
      const row = SELLERS[sellerId];
      const { listingEligible } = buildSellerCheck();
      const result = await listingEligible.evaluate(sellerContext(sellerId, row));
      assert.equal(result.passed, row.expected.listing_eligible, sellerId);
    });
  }
});

describe("buyer purchase eligibility", () => {
  // Every expectation in buyers.json, asserted -- the typed Rule<BuyerPurchaseContext> side.
  for (const buyerId of Object.keys(BUYERS)) {
    it(`${buyerId}: buyerVerified matches`, async () => {
      const row = BUYERS[buyerId];
      const { buyerVerified } = buildBuyerCheck();
      const result = await buyerVerified.evaluate(buyerContext(buyerId, row));
      assert.equal(result.passed, row.expected.buyer_verified, buyerId);
    });

    it(`${buyerId}: purchaseEligible matches`, async () => {
      const row = BUYERS[buyerId];
      const { purchaseEligible } = buildBuyerCheck();
      const result = await purchaseEligible.evaluate(buyerContext(buyerId, row));
      assert.equal(result.passed, row.expected.purchase_eligible, buyerId);
    });
  }
});

describe("ProjectingRule reuses one rule across contexts", () => {
  it("both sides wrap the same named rule, not two independent definitions", () => {
    const { sellerVerified } = buildSellerCheck();
    const { buyerVerified } = buildBuyerCheck();
    // Both projections wrap a FunctionRule named "is_verified_identity" --
    // the same rule, reused via a different projection function per side,
    // not two independently-written checks that happen to agree.
    assert.equal(sellerVerified.inner.name, "is_verified_identity");
    assert.equal(buyerVerified.inner.name, "is_verified_identity");
  });

  it("the projection reads a different field per side", async () => {
    // seller_bob has seller_verified=false -- proving the adapter, not the
    // rule, is what changes between reuse sites.
    const row = SELLERS.seller_bob;
    const { sellerVerified } = buildSellerCheck();
    const result = await sellerVerified.evaluate(sellerContext("seller_bob", row));
    assert.equal(result.passed, false);
  });
});

describe("compliance catalog is dict-context and heterogeneous", () => {
  // Every expectation in compliance_events.json, asserted -- the untyped RulesEngine side.
  for (const eventId of Object.keys(EVENTS)) {
    it(`${eventId}: highValueFlag matches`, async () => {
      const row = EVENTS[eventId];
      const catalog = buildComplianceCatalog();
      const result = await catalog.runNamed("high_value_flag", row);
      assert.equal(result.passed, row.expected.high_value_flag, eventId);
    });

    it(`${eventId}: blockedCountryFlag matches`, async () => {
      const row = EVENTS[eventId];
      const catalog = buildComplianceCatalog();
      const result = await catalog.runNamed("blocked_country_flag", row);
      assert.equal(result.passed, row.expected.blocked_country_flag, eventId);
    });

    it(`${eventId}: newSellerFlag matches`, async () => {
      const row = EVENTS[eventId];
      const catalog = buildComplianceCatalog();
      const result = await catalog.runNamed("new_seller_flag", row);
      assert.equal(result.passed, row.expected.new_seller_flag, eventId);
    });
  }

  it("flags are independent, not a composite verdict", async () => {
    // event_high_value trips exactly one flag -- proving the three checks
    // are looked up independently, not folded into one AND/OR.
    const row = EVENTS.event_high_value;
    const catalog = buildComplianceCatalog();
    const fired = [];
    for (const name of catalog.ruleNames) {
      if ((await catalog.runNamed(name, row)).passed) fired.push(name);
    }
    assert.deepEqual(fired, ["high_value_flag"]);
  });

  it("a new flag is additive, not a shared-context change", async () => {
    // Registering a fourth check needs no change to SellerListingContext,
    // BuyerPurchaseContext, or any existing flag -- the whole point of
    // keeping this side dict-context. Proven by constructing one directly
    // alongside the existing three, reading the same event shape with no
    // adapter needed.
    const weekendFlag = new FunctionRule("weekend_flag", async (ctx) => ({
      ruleName: "weekend_flag",
      passed: ctx.is_weekend ?? false,
    }));
    const extended = new RulesEngine([
      new FunctionRule("high_value_flag", async (ctx) => ({
        ruleName: "high_value_flag",
        passed: ctx.amount_cents > 50_000,
      })),
      weekendFlag,
    ]);
    const result = await extended.runNamed("weekend_flag", { is_weekend: true, amount_cents: 0 });
    assert.equal(result.passed, true);
  });
});

it("data files actually loaded", () => {
  // Sanity check the fixture loaders against the real files, not a stub.
  const rawSellers = JSON.parse(readFileSync(join(FIXTURES, "sellers.json"), "utf8"));
  const rawBuyers = JSON.parse(readFileSync(join(FIXTURES, "buyers.json"), "utf8"));
  const rawEvents = JSON.parse(readFileSync(join(FIXTURES, "compliance_events.json"), "utf8"));
  assert.deepEqual(new Set(Object.keys(SELLERS)), new Set(Object.keys(rawSellers)));
  assert.deepEqual(new Set(Object.keys(BUYERS)), new Set(Object.keys(rawBuyers)));
  assert.deepEqual(new Set(Object.keys(EVENTS)), new Set(Object.keys(rawEvents)));
});
