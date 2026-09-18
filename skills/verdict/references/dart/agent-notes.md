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

RuleResult(ruleName: name, passed: true, detail: '', data: null)  // immutable, const constructors
RunResult(passed: true, results: [...])
```

## Which run mode

| Need | Reach for |
| --- | --- |
| One fast pass/fail verdict | A composite's own `evaluate()` |
| Every rule's own outcome (a status page, an audit trail) | `engine.runAll()` |
| One named rule/group; absence would be a bug | The strict lookup — throws |
| One named rule/group; absence is expected, your domain decides what it means | The non-raising lookup |
