/// Marketplace eligibility -- the flagship generic-context example, as real code.
///
/// See docs/samples/marketplace-eligibility/README.md for the full design
/// and fixtures/marketplace_eligibility/README.md for the shared,
/// cross-language data contract this module reproduces.
library;

import 'dart:convert';
import 'dart:io';

import 'package:verdict_rules/verdict_rules.dart';

import 'contexts.dart';
import 'projecting_rule.dart';

const priceFloorCents = 100;
const allowedCategories = ['books', 'electronics', 'home'];
const purchaseLimitCents = 100000;
const highValueThresholdCents = 50000;
const blockedCountries = ['ir', 'nk'];
const newSellerThresholdDays = 30;

Future<RuleResult> _isVerifiedIdentity(IdentityFlag context) async =>
    RuleResult(ruleName: 'is_verified_identity', passed: context.verified);

ProjectingRule<SellerListingContext, IdentityFlag> _sellerIdentityRule() =>
    ProjectingRule(
      FunctionRule('is_verified_identity', _isVerifiedIdentity),
      (ctx) => IdentityFlag(verified: ctx.sellerVerified),
    );

ProjectingRule<BuyerPurchaseContext, IdentityFlag> _buyerIdentityRule() =>
    ProjectingRule(
      FunctionRule('is_verified_identity', _isVerifiedIdentity),
      (ctx) => IdentityFlag(verified: ctx.buyerVerified),
    );

Future<RuleResult> _priceFloorMet(SellerListingContext context) async =>
    RuleResult(
      ruleName: 'price_floor_met',
      passed: context.listingPriceCents >= priceFloorCents,
    );

Future<RuleResult> _categoryAllowed(SellerListingContext context) async =>
    RuleResult(
      ruleName: 'category_allowed',
      passed: allowedCategories.contains(context.category),
    );

Future<RuleResult> _sufficientBalance(BuyerPurchaseContext context) async =>
    RuleResult(
      ruleName: 'sufficient_balance',
      passed: context.buyerBalanceCents >= context.purchaseAmountCents,
    );

Future<RuleResult> _purchaseLimitNotExceeded(
        BuyerPurchaseContext context) async =>
    RuleResult(
      ruleName: 'purchase_limit_not_exceeded',
      passed: context.purchaseAmountCents <= purchaseLimitCents,
    );

/// Build the typed listing-eligibility composite for one seller.
///
/// Returns a `(listingEligible, sellerVerified)` pair -- the full
/// composite, and the identity sub-rule alone, so a caller (and the test
/// suite) can distinguish "which check failed" without re-running
/// anything.
(AndRule<SellerListingContext>, Rule<SellerListingContext>) buildSellerCheck() {
  final sellerVerified = _sellerIdentityRule();
  final listingEligible = AndRule<SellerListingContext>('listing_eligible', [
    sellerVerified,
    FunctionRule('price_floor_met', _priceFloorMet),
    FunctionRule('category_allowed', _categoryAllowed),
  ]);
  return (listingEligible, sellerVerified);
}

/// Build the typed purchase-eligibility composite for one buyer.
///
/// Returns the same shape as [buildSellerCheck]'s own return value.
(AndRule<BuyerPurchaseContext>, Rule<BuyerPurchaseContext>) buildBuyerCheck() {
  final buyerVerified = _buyerIdentityRule();
  final purchaseEligible = AndRule<BuyerPurchaseContext>('purchase_eligible', [
    buyerVerified,
    FunctionRule('sufficient_balance', _sufficientBalance),
    FunctionRule('purchase_limit_not_exceeded', _purchaseLimitNotExceeded),
  ]);
  return (purchaseEligible, buyerVerified);
}

Future<RuleResult> _highValueFlag(Context context) async => RuleResult(
      ruleName: 'high_value_flag',
      passed: (context['amount_cents'] as int) > highValueThresholdCents,
    );

Future<RuleResult> _blockedCountryFlag(Context context) async => RuleResult(
      ruleName: 'blocked_country_flag',
      passed: blockedCountries.contains(context['country'] as String),
    );

Future<RuleResult> _newSellerFlag(Context context) async => RuleResult(
      ruleName: 'new_seller_flag',
      passed: (context['seller_age_days'] as int) < newSellerThresholdDays,
    );

/// Build the dict-context compliance catalog.
///
/// Unlike the seller/buyer sides, this is deliberately untyped: a
/// compliance team adds a new flag by registering one more rule here,
/// never by agreeing on a shared typed context every existing flag would
/// otherwise need to accommodate too. See
/// docs/architecture/README.md#generic-context for the full reasoning.
RulesEngine<Context> buildComplianceCatalog() => RulesEngine<Context>([
      FunctionRule('high_value_flag', _highValueFlag),
      FunctionRule('blocked_country_flag', _blockedCountryFlag),
      FunctionRule('new_seller_flag', _newSellerFlag),
    ]);

Map<String, Object?> loadSellers(String path) =>
    jsonDecode(File(path).readAsStringSync()) as Map<String, Object?>;

Map<String, Object?> loadBuyers(String path) =>
    jsonDecode(File(path).readAsStringSync()) as Map<String, Object?>;

Map<String, Object?> loadComplianceEvents(String path) =>
    jsonDecode(File(path).readAsStringSync()) as Map<String, Object?>;

SellerListingContext sellerContext(String sellerId, Map<String, Object?> row) =>
    SellerListingContext(
      sellerId: sellerId,
      sellerVerified: row['seller_verified'] as bool,
      listingPriceCents: row['listing_price_cents'] as int,
      category: row['category'] as String,
    );

BuyerPurchaseContext buyerContext(String buyerId, Map<String, Object?> row) =>
    BuyerPurchaseContext(
      buyerId: buyerId,
      buyerVerified: row['buyer_verified'] as bool,
      purchaseAmountCents: row['purchase_amount_cents'] as int,
      buyerBalanceCents: row['buyer_balance_cents'] as int,
    );
