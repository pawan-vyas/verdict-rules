<!-- Title: Extending — Isolating A Flaky Predicate (Dart) -->
# Isolating a flaky predicate: Dart

> The concept and the reasoning are in [`README.md`](README.md) — read
> that first. This page is the concrete Dart code.

```dart
import 'package:verdict_rules/verdict_rules.dart';

class PromoContext {
  final String promoCode;
  final bool simulateTimeout;

  PromoContext({required this.promoCode, this.simulateTimeout = false});
}

/// Turn a predicate's own exception into a failing RuleResult, instead
/// of letting it propagate out of the run that contains it.
FunctionRule<TContext> defensive<TContext>(
  String name,
  RulePredicate<TContext> predicate,
) {
  Future<RuleResult> wrapped(TContext context) async {
    try {
      return await predicate(context);
    } catch (exc) {
      return RuleResult(ruleName: name, passed: false, detail: '$exc');
    }
  }

  return FunctionRule(name, wrapped);
}

/// Stands in for a real network call that can time out.
Future<RuleResult> checkPromoCodeAgainstExternalService(
  PromoContext context,
) async {
  if (context.simulateTimeout) {
    throw Exception('promo-validation service did not respond');
  }
  return RuleResult(
    ruleName: 'promo_code_valid',
    passed: context.promoCode == 'SAVE10',
  );
}

final rule = defensive<PromoContext>(
  'promo_code_valid',
  checkPromoCodeAgainstExternalService,
);
```

```dart
await rule.evaluate(PromoContext(promoCode: 'SAVE10'));
// RuleResult(ruleName: promo_code_valid, passed: true, detail: )

await rule.evaluate(PromoContext(promoCode: 'SAVE10', simulateTimeout: true));
// RuleResult(ruleName: promo_code_valid, passed: false,
//   detail: Exception: promo-validation service did not respond)
```

The same timeout against the **unwrapped** predicate propagates instead
of returning a result — this is what every other rule shares a
`runAll`/`runGroup` with, unless it's wrapped too:

```dart
final unwrapped = FunctionRule<PromoContext>(
  'promo_code_valid',
  checkPromoCodeAgainstExternalService,
);
await unwrapped.evaluate(
  PromoContext(promoCode: 'SAVE10', simulateTimeout: true),
);
// throws Exception: promo-validation service did not respond
```

Now a timeout in the wrapped check reports as `passed: false, detail:
"..."` — one entry in `RunResult.results`, same as any other failing
rule — and every other rule in that `runAll`/`runGroup` still runs and
still reports.

## Related

- [`README.md`](README.md) — the language-agnostic scenario this page
  implements.
