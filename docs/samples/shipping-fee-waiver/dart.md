<!-- Title: Sample — Shipping Fee Waiver (Dart) -->
# Sample: Shipping Fee Waiver

> The problem, the design, and what a solution must demonstrate are
> language-agnostic and live in [`README.md`](README.md) — read that
> first. This page is the Dart implementation of it.

## The naive way (and why it breaks down)

The obvious first implementation is a conditional ladder, one `if` per
qualifying path:

```dart
class Order {
  final String? promoCode;
  final bool isPremiumMember;
  final num total;
  final num freeShippingThreshold;

  Order({
    this.promoCode,
    required this.isPremiumMember,
    required this.total,
    required this.freeShippingThreshold,
  });
}

Future<bool> shipsFree(Order order) async {
  if (await validatePromoCode(order.promoCode)) {
    return true;
  }
  if (order.isPremiumMember) {
    return true;
  }
  if (order.total >= order.freeShippingThreshold) {
    return true;
  }
  return false;
}
```

Adding a fourth path means opening this function and inserting another
`if` among the ones already there, at risk to the branches already
correct and already shipped. Here that cost is concrete: the
promo-code check is an external service call, and it happens to run
first, so it runs on **every single order** — including a $500 order
from a non-member with no promo code, which qualifies on total alone
and never needed the network round trip at all. See the spec for the
rest of what this shape gets wrong.

## The `verdict` way

A typed context, not a `Map` — and unlike the other typed samples, one
of its fields isn't order data at all: `promoCodeService` is an
injected dependency, typed as an interface rather than `Object?`, so
every rule reading it gets the same compile-time checking as the plain
fields:

```dart
import 'package:verdict_rules/verdict_rules.dart';

abstract interface class PromoCodeService {
  Future<bool> validate(String? code);
}

class ShippingContext {
  final num orderTotal;
  final num freeShippingThreshold;
  final bool isPremiumMember;
  final String? promoCode;
  final PromoCodeService promoCodeService;

  ShippingContext({
    required this.orderTotal,
    required this.freeShippingThreshold,
    required this.isPremiumMember,
    this.promoCode,
    required this.promoCodeService,
  });
}

Future<RuleResult> orderTotalOverThreshold(ShippingContext context) async {
  final passed = context.orderTotal >= context.freeShippingThreshold;
  return RuleResult(ruleName: 'order_total_over_threshold', passed: passed);
}

Future<RuleResult> hasPremiumMembership(ShippingContext context) async =>
    RuleResult(
      ruleName: 'has_premium_membership',
      passed: context.isPremiumMember,
    );

Future<RuleResult> hasValidPromoCode(ShippingContext context) async {
  // The expensive path: only reached if both cheaper checks above failed.
  final isValid = await context.promoCodeService.validate(context.promoCode);
  return RuleResult(ruleName: 'has_valid_promo_code', passed: isValid);
}

final shipsFree = OrRule<ShippingContext>('ships_free', [
  FunctionRule('order_total_over_threshold', orderTotalOverThreshold),
  FunctionRule('has_premium_membership', hasPremiumMembership),
  FunctionRule('has_valid_promo_code', hasValidPromoCode), // cheapest-last, on purpose
]);
```

The comment on the last rule is the whole fix, made visible: cost order
is now a stated decision at the point it matters, not something the
next person to edit this file has to reconstruct from scratch.

Proving the skip, not just asserting it — a call-counting fake stands
in for the real service, and satisfies `PromoCodeService` the same way
the real implementation would:

```dart
class FakePromoService implements PromoCodeService {
  int calls = 0;

  @override
  Future<bool> validate(String? code) async {
    calls += 1;
    return code == 'SAVE10';
  }
}

Future<void> main() async {
  final promoService = FakePromoService();
  final order = ShippingContext(
    orderTotal: 120,
    freeShippingThreshold: 50,
    isPremiumMember: false,
    promoCode: null,
    promoCodeService: promoService,
  );

  final result = await shipsFree.evaluate(order);
  print('${result.passed}, ${promoService.calls}');
  // true, 0 -- order_total alone already qualifies; the promo service is never called
}
```

## Related

- [`README.md`](README.md) —
  the language-agnostic spec this page implements.
- [`dynamic-discounts/dart.md`](../dynamic-discounts/dart.md) — the
  `AndRule` mirror image (every condition must pass, not just one).
- [`../../architecture/README.md`](../../architecture/README.md#execution-model-sequential-not-concurrent) —
  why short-circuiting only means something because evaluation is
  sequential, never concurrent.
