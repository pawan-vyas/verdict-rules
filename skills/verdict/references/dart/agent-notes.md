# Dart — agent notes

What is specific to the Dart SDK, and to writing Dart that uses it.
The engine's own general guarantees are in `SKILL.md`, not repeated here.

## Install and import

```sh
dart pub add verdict_rules
```

Works unchanged in a Flutter project too -- `flutter pub add verdict_rules` there.

```dart
import 'package:verdict_rules/verdict_rules.dart';
```

## The API, in one screen

```dart
typedef Context = Map<String, Object?>;    // the dict-context spelling of TContext

abstract interface class Rule<TContext> { // nominal -- must `implements Rule<TContext>` explicitly
  String get name;
  String? get group;
  Future<RuleResult> evaluate(TContext context);
}
typedef RulePredicate<TContext> = Future<RuleResult> Function(TContext context);

FunctionRule(name, predicate, {group})     // wraps a plain async predicate (TContext inferred)
AndRule(name, rules, {group})              // passes only if every sub-rule passes
OrRule(name, rules, {group})               // passes as soon as one does

final engine = RulesEngine(rules);
await engine.runAll(context);              // every rule, never short-circuits
await engine.runNamed(name, context);      // one rule; throws ArgumentError if absent
await engine.runGroup(group, context);     // one group;  throws ArgumentError if absent
await engine.tryRunNamed(name, context);   // -> RuleResult?
await engine.tryRunGroup(group, context);  // -> RunResult?
engine.ruleNames, engine.groupNames        // Iterable<String> of what exists
```

`RuleResult` and `RunResult` are plain immutable classes with `const`
constructors, not interfaces to satisfy — construct them directly:
`RuleResult(ruleName: name, passed: true, detail: '', data: null)` and
`RunResult(passed: true, results: [...])`.

**A typed, non-dict context** reaches for the same `FunctionRule`/
`AndRule`/`OrRule`/`RulesEngine` — `TContext` is a type parameter on
each, not a separate name. `TContext` is usually inferred from the
predicate's own parameter type at a constructor call site
(`FunctionRule('x', predicate)` needs no type argument as long as
`predicate` is typed), so most call sites are unaffected by which
context a rule reads from.

## Which run mode

| Need | Reach for |
| --- | --- |
| One fast pass/fail verdict | A composite's own `evaluate()` |
| Every rule's own outcome (a status page, an audit trail) | `engine.runAll()` |
| One named rule/group; absence would be a bug | The strict lookup — throws |
| One named rule/group; absence is expected, your domain decides what it means | The non-raising lookup |
