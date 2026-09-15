# Verdict — Dart

> The Dart implementation of Verdict — a small, zero-dependency,
> async-native rule-evaluation engine.

## Install

```yaml
dependencies:
  verdict_rules: ^0.0.1
```

## Use

```dart
import 'package:verdict_rules/verdict_rules.dart';

FunctionRule atLeast(String name, String field, num floor) => FunctionRule(
      name,
      (ctx) async {
        final value = ctx[field]! as num;
        return RuleResult(
          ruleName: name,
          passed: value >= floor,
          detail: '$value vs $floor',
        );
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
  print(verdict.detail); // 'score_ok' failed: 55 vs 60
}
```

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
  can have.

  When absence *is* expected, `tryRunNamed`/`tryRunGroup` return null instead
  of throwing:

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
- **`RuleResult.data` is opaque** — only what actually ran, never padded, never
  flattened.
- **Zero runtime dependencies.**

## Shape-based rules, within what Dart allows

Dart *does* have structural typing — for function types. Any function matching
the predicate signature is a rule through `FunctionRule`, with nothing declared
and no type to name. A tear-off works directly:

```dart
Future<RuleResult> hasQuorum(Map<String, Object?> ctx) async =>
    RuleResult(ruleName: 'quorum', passed: ctx.length >= 3);

final rule = FunctionRule('quorum', hasQuorum);
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

## Where to go next

| Doc | For |
| --- | --- |
| [Top-level `README.md`](https://github.com/pawan-vyas/verdict-rules#readme) | What Verdict is and why it's shaped this way, in narrative form |
| [`docs/quickstart.md`](https://github.com/pawan-vyas/verdict-rules/blob/dart-v0.0.1/dart/packages/verdict_rules/docs/quickstart.md) | The quickstart — core concepts and a full worked example |
| [`docs/architecture/`](https://github.com/pawan-vyas/verdict-rules/blob/dart-v0.0.1/docs/architecture/README.md) | Why it's shaped this way, in depth — type structure, the execution model |
| [`docs/extending/`](https://github.com/pawan-vyas/verdict-rules/blob/dart-v0.0.1/docs/extending/README.md) | Building on top of it from your own code, with no changes here |
| [`docs/maintenance/`](https://github.com/pawan-vyas/verdict-rules/blob/dart-v0.0.1/docs/maintenance/README.md) | Changing this package itself |
| [`docs/testing/`](https://github.com/pawan-vyas/verdict-rules/blob/dart-v0.0.1/docs/testing/README.md) | How the test suite is organized, and what a change needs to prove |
| [`docs/samples/`](https://github.com/pawan-vyas/verdict-rules/blob/dart-v0.0.1/docs/samples/README.md) | Worked examples — dynamic discounts, fee waivers, tier promotions, moderation routing, data-driven rule sets |
