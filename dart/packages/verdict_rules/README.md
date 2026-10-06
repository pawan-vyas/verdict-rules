# Verdict — Dart

> The Dart implementation of Verdict — a small, zero-dependency,
> async-native rule-evaluation engine.

## Install

```sh
dart pub add verdict_rules
```

Works unchanged in a Flutter project too — `flutter pub add verdict_rules` there.

```dart
import 'package:verdict_rules/verdict_rules.dart';
```

## A first rule

```dart
import 'package:verdict_rules/verdict_rules.dart';

FunctionRule atLeast(String name, String field, num floor) => FunctionRule(
      name,
      (ctx) async {
        final value = ctx[field]! as num;
        return PredicateOutcome(value >= floor, detail: '$value vs $floor');
      },
    );

Future<void> main() async {
  final eligible = AndRule('eligible', [
    atLeast('age_ok', 'age', 18),
    atLeast('score_ok', 'score', 60),
  ]);

  final engine = RulesEngine([eligible]);
  final verdict = await engine.runNamed('eligible', {'age': 21, 'score': 55});
  print(verdict.passed); // false
  // `eligible` is an AndRule -- its own `detail` is always empty (composing
  // ShortCircuitEvaluator leaves no channel to build one from). The actual
  // reason lives in `failingLeaves`, the flattened leaf results that
  // explain the failure.
  print(verdict.failingLeaves.map((l) => '${l.ruleName}: ${l.detail}')); // (score_ok: 55 vs 60)
}
```

## Shape-based rules, within what Dart allows

Dart *does* have structural typing — for function types. Any function matching
the predicate signature is a rule through `FunctionRule`, with nothing declared
and no type to name. A tear-off works directly:

```dart
Future<PredicateOutcome> isBusinessHours(Map<String, Object?> ctx) async =>
    PredicateOutcome((ctx['hour']! as int) >= 9 && (ctx['hour']! as int) < 17);

final rule = FunctionRule('is_business_hours', isBusinessHours);
```

What Dart lacks is structural typing for a *multi-member* interface. An object
carrying `name`, `group` and `evaluate` is not thereby a `Rule` — a rule shape
owning its own name and group must say `implements Rule` explicitly. That's
why `FunctionRule` carries more weight in this SDK: it is the escape hatch
back to shape-based rules, and most rules should use it rather than declaring
a type.

When you do declare one, note that `Rule` is an `abstract interface class`:
consumers **implement** it, never **extend** it. Forbidding extension means an
instance method calling another method on `this` always reaches a known
implementation, rather than landing in a consumer's override.

## Absence returns null, not a thrown error

`runNamed`/`runGroup` throw on an unknown name or group. When absence *is*
expected, `tryRunNamed`/`tryRunGroup` return null instead:

```dart
final result = await engine.tryRunGroup('beta_checks', ctx);
final allowed = result?.passed ?? true; // absent means "no constraint here"
```

Null means **absent, never failed** — a rule that exists and fails still
returns a `RuleResult` with `passed` false. These are the primitives; the
throwing forms are assertions on top of them.

**The fallback only applies to absence.** A group that exists always reports
its real verdict, so `?? true` does not mean "sometimes true" — a failing group
is still a failure whatever default you choose. If you test code using this,
the case worth covering is a *present, failing* group rather than the absent
one everybody thinks of first.

## What it guarantees

- **Sequential evaluation, never concurrent.** Composites use a plain loop with
  `await`, never `Future.wait`. Short-circuiting only means something if later
  work never *starts* — and because the returned boolean is identical either
  way, getting this wrong is silent.
- **Vacuous truth has a polarity.** `AndRule([])` passes, `OrRule([])` fails.
  Deliberately asymmetric.
- **Emptiness is not absence.** An empty composite folds to its identity; an
  unknown rule name or group **throws**. A group exists only because some rule
  declared it, so a lookup matching nothing can only be a mistake — and a
  misspelled group silently approving is the worst failure an eligibility check
  can have. Use `ruleNames` / `groupNames` to check membership, or
  `tryRunNamed` / `tryRunGroup` where your own domain has an answer for
  absence — both return null instead of throwing.
- **`RuleResult.data` is opaque** — never read or written by this package.
  `AndRule`/`OrRule`/`NotRule` carry their own children in
  `RuleResult.subResults` instead — only what actually ran, never padded,
  never flattened. `RuleResult.leaves` / `RuleResult.failingLeaves` give the
  flattened view across any depth.
- **Zero runtime dependencies.**

## Where to go next

| Doc | For |
| --- | --- |
| [`doc/quickstart.md`](https://github.com/pawan-vyas/verdict-rules/blob/dart-v0.4.0/dart/packages/verdict_rules/doc/quickstart.md) | The quickstart — core concepts and a full worked example |
| [`docs/architecture/`](https://github.com/pawan-vyas/verdict-rules/blob/dart-v0.4.0/docs/architecture/README.md) | Why it's shaped this way, in depth — type structure, the execution model |
| [`docs/extending/`](https://github.com/pawan-vyas/verdict-rules/blob/dart-v0.4.0/docs/extending/README.md) | Building on top of it from your own code, with no changes here |
| [`docs/maintenance/`](https://github.com/pawan-vyas/verdict-rules/blob/dart-v0.4.0/docs/maintenance/README.md) | Changing this package itself |
| [`docs/testing/`](https://github.com/pawan-vyas/verdict-rules/blob/dart-v0.4.0/docs/testing/README.md) | How the test suite is organized, and what a change needs to prove |
| [`fixtures/`](https://github.com/pawan-vyas/verdict-rules/blob/dart-v0.4.0/fixtures/README.md) | The two worked scenarios — each one's problem, design, and the cross-language data contract every port reproduces |
| [`examples/`](https://github.com/pawan-vyas/verdict-rules/blob/dart-v0.4.0/dart/examples/README.md) | Full, tested mini-projects behind the fixtures — real code, real tests, real docs |
