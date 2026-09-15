<!-- Title: Extending — Wrapping A Predicate (Dart) -->
# Wrapping a predicate: Dart

> The concept and why it's the common case are in
> [`README.md`](README.md) — read that first. This page is the concrete
> Dart code.

```dart
import 'package:verdict_rules/verdict_rules.dart';

Future<RuleResult> cartMeetsMinimum(Map<String, Object?> context) async {
  final total = context['cartTotal']! as num;
  final minimum = context['minimumForOffer']! as num;
  return RuleResult(
    ruleName: 'cart_meets_minimum',
    passed: total >= minimum,
    detail: '$total vs minimum $minimum',
  );
}

final rule = FunctionRule('cart_meets_minimum', cartMeetsMinimum);
```

## Related

- [`README.md`](README.md) — the language-agnostic scenario this page
  implements.
- [`../new-rule-shape/dart.md`](../new-rule-shape/dart.md) — the
  next step up, for combination logic `FunctionRule` alone can't express.
