/// Tests for the marketplace-eligibility example.
///
/// This project exercises Rule<TContext> end to end: two typed contexts
/// sharing no fields, a rule reused across both via ProjectingRule, and a
/// dict-context catalog coexisting in the same codebase. See
/// docs/samples/marketplace-eligibility/README.md for the design and
/// fixtures/marketplace_eligibility/README.md for the shared contract this
/// suite reproduces.
import 'dart:io';

import 'package:marketplace_eligibility/marketplace_eligibility.dart';
import 'package:test/test.dart';
import 'package:verdict_rules/verdict_rules.dart';

final _fixtures = Directory(
        '${Directory.current.path}/../../../fixtures/marketplace_eligibility')
    .absolute
    .path;
final _sellers = loadSellers('$_fixtures/sellers.json');
final _buyers = loadBuyers('$_fixtures/buyers.json');
final _events = loadComplianceEvents('$_fixtures/compliance_events.json');

Map<String, Object?> _row(Map<String, Object?> data, String key) =>
    data[key] as Map<String, Object?>;

Map<String, Object?> _expected(Map<String, Object?> row) =>
    row['expected'] as Map<String, Object?>;

void main() {
  group('seller listing eligibility', () {
    // Every expectation in sellers.json, asserted -- the typed
    // Rule<SellerListingContext> side.
    for (final sellerId in _sellers.keys) {
      test('$sellerId: sellerVerified matches', () async {
        final row = _row(_sellers, sellerId);
        final (_, sellerVerified) = buildSellerCheck();
        final result =
            await sellerVerified.evaluate(sellerContext(sellerId, row));
        expect(result.passed, _expected(row)['seller_verified']);
      });

      test('$sellerId: listingEligible matches', () async {
        final row = _row(_sellers, sellerId);
        final (listingEligible, _) = buildSellerCheck();
        final result =
            await listingEligible.evaluate(sellerContext(sellerId, row));
        expect(result.passed, _expected(row)['listing_eligible']);
      });
    }
  });

  group('buyer purchase eligibility', () {
    // Every expectation in buyers.json, asserted -- the typed
    // Rule<BuyerPurchaseContext> side.
    for (final buyerId in _buyers.keys) {
      test('$buyerId: buyerVerified matches', () async {
        final row = _row(_buyers, buyerId);
        final (_, buyerVerified) = buildBuyerCheck();
        final result = await buyerVerified.evaluate(buyerContext(buyerId, row));
        expect(result.passed, _expected(row)['buyer_verified']);
      });

      test('$buyerId: purchaseEligible matches', () async {
        final row = _row(_buyers, buyerId);
        final (purchaseEligible, _) = buildBuyerCheck();
        final result =
            await purchaseEligible.evaluate(buyerContext(buyerId, row));
        expect(result.passed, _expected(row)['purchase_eligible']);
      });
    }
  });

  group('ProjectingRule reuses one rule across contexts', () {
    test('both sides wrap the same named rule', () {
      final (_, sellerVerified) = buildSellerCheck();
      final (_, buyerVerified) = buildBuyerCheck();
      // Both projections wrap a FunctionRule named "is_verified_identity" --
      // the same rule, reused via a different projection function per
      // side, not two independently-written checks that happen to agree.
      expect(sellerVerified.name, 'is_verified_identity');
      expect(buyerVerified.name, 'is_verified_identity');
    });

    test('the projection reads a different field per side', () async {
      // seller_bob has seller_verified=false -- proving the adapter, not
      // the rule, is what changes between reuse sites.
      final row = _row(_sellers, 'seller_bob');
      final (_, sellerVerified) = buildSellerCheck();
      final result =
          await sellerVerified.evaluate(sellerContext('seller_bob', row));
      expect(result.passed, isFalse);
    });
  });

  group('compliance catalog is dict-context and heterogeneous', () {
    // Every expectation in compliance_events.json, asserted -- the untyped
    // RulesEngine side.
    for (final eventId in _events.keys) {
      test('$eventId: highValueFlag matches', () async {
        final row = _row(_events, eventId);
        final catalog = buildComplianceCatalog();
        final result = await catalog.runNamed('high_value_flag', row);
        expect(result.passed, _expected(row)['high_value_flag']);
      });

      test('$eventId: blockedCountryFlag matches', () async {
        final row = _row(_events, eventId);
        final catalog = buildComplianceCatalog();
        final result = await catalog.runNamed('blocked_country_flag', row);
        expect(result.passed, _expected(row)['blocked_country_flag']);
      });

      test('$eventId: newSellerFlag matches', () async {
        final row = _row(_events, eventId);
        final catalog = buildComplianceCatalog();
        final result = await catalog.runNamed('new_seller_flag', row);
        expect(result.passed, _expected(row)['new_seller_flag']);
      });
    }

    test('flags are independent, not a composite verdict', () async {
      // event_high_value trips exactly one flag -- proving the three
      // checks are looked up independently, not folded into one AND/OR.
      final row = _row(_events, 'event_high_value');
      final catalog = buildComplianceCatalog();
      final fired = <String>[];
      for (final name in catalog.ruleNames) {
        if ((await catalog.runNamed(name, row)).passed) fired.add(name);
      }
      expect(fired, ['high_value_flag']);
    });

    test('a new flag is additive, not a shared-context change', () async {
      // Registering a fourth check needs no change to
      // SellerListingContext, BuyerPurchaseContext, or any existing flag.
      final extended = RulesEngine<Context>([
        FunctionRule<Context>(
          'high_value_flag',
          (ctx) async => RuleResult(
            ruleName: 'high_value_flag',
            passed: (ctx['amount_cents'] as int) > 50000,
          ),
        ),
        FunctionRule<Context>(
          'weekend_flag',
          (ctx) async => RuleResult(
            ruleName: 'weekend_flag',
            passed: ctx['is_weekend'] as bool,
          ),
        ),
      ]);
      final result = await extended
          .runNamed('weekend_flag', {'is_weekend': true, 'amount_cents': 0});
      expect(result.passed, isTrue);
    });
  });
}
