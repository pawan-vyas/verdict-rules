/// Marketplace eligibility, implemented with verdict_rules.
///
/// See fixtures/marketplace_eligibility/README.md for the design and the fixture
/// contract.
library;

import 'dart:convert';
import 'dart:io';

import 'package:verdict_rules/verdict_rules.dart';

import 'contexts.dart';
import 'projecting_rule.dart';

/// Read the shared policy numbers this example evaluates against.
///
/// Read from the fixture rather than written as literals here, so the four
/// ports cannot drift from each other or from the data their suites assert
/// against -- changing a number in one place changes every port at once.
Map<String, Object?> loadThresholds(String path) =>
    jsonDecode(File(path).readAsStringSync()) as Map<String, Object?>;

/// Locate the shared fixture directory by walking up from the working
/// directory, so this resolves whether the caller is `dart test` (cwd is the
/// package) or `dart run` from somewhere else entirely.
String _findFixtures() {
  for (var dir = Directory.current.absolute;
      dir.parent.path != dir.path;
      dir = dir.parent) {
    final candidate = '${dir.path}/fixtures/marketplace_eligibility';
    if (File('$candidate/thresholds.json').existsSync()) return candidate;
  }
  throw StateError(
      'fixtures/marketplace_eligibility not found above ${Directory.current.path}');
}

final _thresholds = loadThresholds('${_findFixtures()}/thresholds.json');

final priceFloorCents = _thresholds['price_floor_cents']! as int;
final allowedCategories =
    (_thresholds['allowed_categories']! as List).cast<String>();
final purchaseLimitCents = _thresholds['purchase_limit_cents']! as int;
final highValueThresholdCents =
    _thresholds['high_value_threshold_cents']! as int;
final blockedCountries =
    (_thresholds['blocked_countries']! as List).cast<String>();
final newSellerThresholdDays = _thresholds['new_seller_threshold_days']! as int;

Future<PredicateOutcome> _isVerifiedIdentity(IdentityFlag context) async =>
    PredicateOutcome(context.verified);

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

Future<PredicateOutcome> _priceFloorMet(SellerListingContext context) async =>
    PredicateOutcome(context.listingPriceCents >= priceFloorCents);

Future<PredicateOutcome> _categoryAllowed(SellerListingContext context) async =>
    PredicateOutcome(allowedCategories.contains(context.category));

Future<PredicateOutcome> _sufficientBalance(
        BuyerPurchaseContext context) async =>
    PredicateOutcome(context.buyerBalanceCents >= context.purchaseAmountCents);

Future<PredicateOutcome> _purchaseLimitNotExceeded(
        BuyerPurchaseContext context) async =>
    PredicateOutcome(context.purchaseAmountCents <= purchaseLimitCents);

/// Build the typed listing-eligibility composite for one seller.
///
/// Returns a `(listingEligible, sellerVerified)` pair -- the full
/// composite, and the identity sub-rule alone.
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

Future<PredicateOutcome> _highValueFlag(Context context) async =>
    PredicateOutcome(
        (context['amount_cents'] as int) > highValueThresholdCents);

Future<PredicateOutcome> _blockedCountryFlag(Context context) async =>
    PredicateOutcome(blockedCountries.contains(context['country'] as String));

Future<PredicateOutcome> _newSellerFlag(Context context) async =>
    PredicateOutcome(
        (context['seller_age_days'] as int) < newSellerThresholdDays);

/// Build the dict-context compliance catalog.
///
/// Unlike the seller/buyer sides, this is deliberately untyped. See
/// docs/architecture/README.md#generic-context.
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
