<!-- Title: Sample — Dynamic Discount Eligibility (Dart) -->
# Sample: Dynamic Discount Eligibility

> The problem, the design, and what a solution must demonstrate are
> language-agnostic and live in [`README.md`](README.md) — read that
> first. This page is the Dart implementation of it.

## The naive way (and why it breaks down)

The obvious first implementation is a nested `if` chain, checked
straight against the cart, with the current campaign's numbers baked
directly into the code:

```dart
class Cart {
  final num total;
  final String region;
  final bool isFirstPurchase;

  Cart({required this.total, required this.region, required this.isFirstPurchase});
}

bool qualifiesForPromo(Cart cart) {
  if (cart.total >= 100) {
    if (['US', 'CA'].contains(cart.region)) {
      if (cart.isFirstPurchase) {
        return true;
      }
    }
  }
  return false;
}
```

This is fine for exactly as long as the campaign's numbers never
change — see the spec for the three specific ways it stops being fine.

## The `verdict` way

```dart
import 'package:verdict_rules/verdict_rules.dart';

Future<RuleResult> cartMeetsMinimum(Map<String, Object?> context) async {
  final total = context['cartTotal']! as num;
  final minimum = context['promoMinimum']! as num;
  return RuleResult(
    ruleName: 'cart_meets_minimum',
    passed: total >= minimum,
    detail: 'cartTotal=$total, needs >= $minimum',
  );
}

Future<RuleResult> isEligibleRegion(Map<String, Object?> context) async {
  final region = context['region']! as String;
  final eligible = context['eligibleRegions']! as Set<String>;
  return RuleResult(
    ruleName: 'is_eligible_region',
    passed: eligible.contains(region),
    detail: 'region=$region not in ${eligible.toList()..sort()}',
  );
}

Future<RuleResult> isFirstPurchase(Map<String, Object?> context) async =>
    RuleResult(
      ruleName: 'is_first_purchase',
      passed: context['isFirstPurchase']! as bool,
    );

// Built once. The AndRule and the engine below both hold these same three
// objects -- nothing is declared twice, and nothing here needs to know in
// advance which of the two call sites below will use it.
final minimumRule = FunctionRule('cart_meets_minimum', cartMeetsMinimum);
final regionRule = FunctionRule('is_eligible_region', isEligibleRegion);
final firstPurchaseRule = FunctionRule('is_first_purchase', isFirstPurchase);

final qualifiesForPromo = AndRule(
  'qualifies_for_promo',
  [minimumRule, regionRule, firstPurchaseRule],
);
final engine = RulesEngine([minimumRule, regionRule, firstPurchaseRule]);

/// The checkout gate: the cheapest possible yes/no, stopping at the
/// first failing condition. Nothing past this point needs to know
/// *which* condition failed -- only whether the discount applies.
Future<bool> checkCartFast(
  Map<String, Object?> cart,
  Map<String, Object?> campaign,
) async {
  final context = {...cart, ...campaign};
  return (await qualifiesForPromo.evaluate(context)).passed;
}

/// The support-facing view: every condition, always -- even if more
/// than one failed at once. Never the fast path's job, so it doesn't
/// share the fast path's short-circuiting.
///
/// [campaign] (e.g. `{'promoMinimum': 100, 'eligibleRegions': {'US', 'CA'}}`)
/// is whatever the checkout code already reads the active campaign's
/// configured values from -- nothing here cares where it came from.
Future<RunResult> whyNot(
  Map<String, Object?> cart,
  Map<String, Object?> campaign,
) async {
  final context = {...cart, ...campaign};
  return engine.runAll(context);
  // result.passed is true only if every condition passed; result.results
  // is one entry per condition, always all three, regardless of how many
  // failed -- this is what a "why not?" screen actually needs.
}
```

Run against a qualifying cart, then one that fails on two conditions at
once:

```dart
final cart = {'cartTotal': 120, 'region': 'US', 'isFirstPurchase': true};
final campaign = {'promoMinimum': 100, 'eligibleRegions': {'US', 'CA'}};

await checkCartFast(cart, campaign);
// true

var result = await whyNot(cart, campaign);
result.passed;
// true -- every condition passed

final badCart = {'cartTotal': 50, 'region': 'MX', 'isFirstPurchase': true};
await checkCartFast(badCart, campaign);
// false -- stops at the first failure

result = await whyNot(badCart, campaign);
result.passed;
// false
result.results.map((r) => r.passed).toList();
// [false, false, true] -- both failures visible, not just the first
```

Swapping the campaign's minimum from `100` to `75`, or adding `'MX'` to
`eligibleRegions`, is now a data change passed into either function —
no edit to `cartMeetsMinimum`/`isEligibleRegion`/`isFirstPurchase`,
the `AndRule`, or the engine.

## Related

- [`README.md`](README.md) —
  the language-agnostic spec this page implements.
- [`shipping-fee-waiver/dart.md`](../shipping-fee-waiver/dart.md) — the
  `OrRule` mirror image of this same idea (any one qualifying path, not
  all).
- [`data-driven-rule-sets/dart.md`](../data-driven-rule-sets/dart.md) —
  building the rules themselves from stored config, not just their
  values.
