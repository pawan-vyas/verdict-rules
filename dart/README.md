# verdict_rules

> A small, zero-dependency, async-native rule-evaluation engine for Dart.
> Compose independently-changing conditions into one explainable pass/fail
> verdict.

The Dart SDK of [verdict](https://github.com/pawan-vyas/verdict-rules), which
exists in more than one language with identical execution-model guarantees.

**This is `0.0.1` — correct, but minimal.** The type set and its guarantees are
complete and tested. The worked example, the shared cross-language fixture, and
the full documentation set arrive before `0.1.0`.

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

  final verdict = await eligible.evaluate({'age': 21, 'score': 55});
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
owning its own name and group must say `implements Rule`, where Python's
`Protocol` and TypeScript's structural interfaces would accept it as-is. That
narrow difference is why `FunctionRule` carries more weight in this SDK.

When you do declare one, note that `Rule` is an `abstract interface class`:
consumers **implement** it, never **extend** it. Forbidding extension means an
instance method calling another method on `this` always reaches a known
implementation, rather than landing in a consumer's override.

## Licence

MIT.
