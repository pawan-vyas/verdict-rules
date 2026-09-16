<!-- Title: Extending — Isolating A Flaky Predicate (Dart) -->
# Isolating a flaky predicate: Dart

> The concept and the reasoning are in [`README.md`](README.md) — read
> that first. This page is the concrete Dart code.

```dart
import 'package:verdict_rules/verdict_rules.dart';

/// Turn a predicate's own exception into a failing RuleResult, instead
/// of letting it propagate out of the run that contains it.
FunctionRule defensive(String name, RulePredicate predicate) {
  Future<RuleResult> wrapped(Map<String, Object?> context) async {
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
  Map<String, Object?> context,
) async {
  if (context['simulateTimeout'] == true) {
    throw Exception('promo-validation service did not respond');
  }
  return RuleResult(
    ruleName: 'promo_code_valid',
    passed: context['promoCode'] == 'SAVE10',
  );
}

final rule = defensive('promo_code_valid', checkPromoCodeAgainstExternalService);
```

```dart
await rule.evaluate({'promoCode': 'SAVE10'});
// RuleResult(ruleName: promo_code_valid, passed: true, detail: )

await rule.evaluate({'promoCode': 'SAVE10', 'simulateTimeout': true});
// RuleResult(ruleName: promo_code_valid, passed: false,
//   detail: Exception: promo-validation service did not respond)
```

The same timeout against the **unwrapped** predicate propagates instead
of returning a result — this is what every other rule shares a
`runAll`/`runGroup` with, unless it's wrapped too:

```dart
final unwrapped = FunctionRule(
  'promo_code_valid',
  checkPromoCodeAgainstExternalService,
);
await unwrapped.evaluate({'promoCode': 'SAVE10', 'simulateTimeout': true});
// throws Exception: promo-validation service did not respond
```

Now a timeout in the wrapped check reports as `passed: false, detail:
"..."` — one entry in `RunResult.results`, same as any other failing
rule — and every other rule in that `runAll`/`runGroup` still runs and
still reports.

## Related

- [`README.md`](README.md) — the language-agnostic scenario this page
  implements.
