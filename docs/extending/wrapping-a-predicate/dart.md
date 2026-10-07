<!-- Title: Extending — Wrapping A Predicate (Dart) -->
# Wrapping a predicate: Dart

> The concept and why it's the common case are in
> [`README.md`](README.md) — read that first. This page is the concrete
> Dart code.

```dart
import 'package:verdict_rules/verdict_rules.dart';

Future<PredicateOutcome> cartMeetsMinimum(Map<String, Object?> context) async {
  final total = context['cartTotal']! as num;
  final minimum = context['minimumForOffer']! as num;
  return PredicateOutcome(
    total >= minimum,
    detail: '$total vs minimum $minimum',
  );
}

final rule = FunctionRule('cart_meets_minimum', cartMeetsMinimum);
```

The wrap itself doesn't care what the predicate's own context looks
like — a predicate already written against a typed context wraps
exactly the same way:

```dart
class CartContext {
  final num cartTotal;
  final num minimumForOffer;
  const CartContext({required this.cartTotal, required this.minimumForOffer});
}

Future<PredicateOutcome> cartMeetsMinimumTyped(CartContext context) async =>
    PredicateOutcome(
      context.cartTotal >= context.minimumForOffer,
      detail: '${context.cartTotal} vs minimum ${context.minimumForOffer}',
    );

final typedRule = FunctionRule<CartContext>(
  'cart_meets_minimum',
  cartMeetsMinimumTyped,
);
```

## Related

- [`README.md`](README.md) — the language-agnostic scenario this page
  implements.
- [`../new-rule-shape/dart.md`](../new-rule-shape/dart.md) — the
  next step up, for combination logic `FunctionRule` alone can't express.
