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
typedef RulePredicate<TContext> = Future<PredicateOutcome> Function(TContext context);

class PredicateOutcome {                   // what a predicate returns; it has no ruleName
  final bool passed;                       // positional: PredicateOutcome(true, detail: '...')
  final String detail;
  final Object? data;
}

FunctionRule(name, predicate, {group})     // wraps a plain async predicate (TContext inferred)
AndRule(name, rules, {group})              // passes only if every sub-rule passes
OrRule(name, rules, {group})               // passes as soon as one does
NotRule(name, rule, {group})               // passes exactly when the one wrapped rule fails

final engine = RulesEngine(rules);
await engine.runAll(context);              // every rule, never short-circuits
await engine.runNamed(name, context);      // one rule; throws ArgumentError if absent
await engine.runGroup(group, context);     // one group;  throws ArgumentError if absent
await engine.tryRunNamed(name, context);   // -> RuleResult?
await engine.tryRunGroup(group, context);  // -> RunResult?
engine.ruleNames, engine.groupNames        // Iterable<String> of what exists

RuleResult(ruleName: name, passed: true, detail: '', data: null,
            subResults: const [], decidedBy: const [])
RunResult(passed: true, results: [...])
```

A predicate reports a `PredicateOutcome`; the `FunctionRule` wrapping it
owns the name and builds the `RuleResult`:

```dart
Future<PredicateOutcome> overEighteen(Context ctx) async =>
    PredicateOutcome((ctx['age'] as int) >= 18, detail: 'age ${ctx['age']}');

final rule = FunctionRule('over_18', overEighteen);
```

## Reading a result

```dart
result.subResults     // this result's own children, exactly what it evaluated
result.decidedBy      // which of those explain this result's own verdict
result.leaves         // every leaf reachable from here, flattened
result.failingLeaves  // the leaves explaining a failure
```

`RunResult` exposes `leaves`/`failingLeaves` too, flattened across every
rule the run evaluated. The lists a result hands back are unmodifiable --
`subResults.add(...)` throws.

Key an audit trail on a leaf's own `ruleName`, never a composite's:

```dart
final verdict = await graduates.evaluate(student);
if (!verdict.passed) {
  log.warning('refused by ${verdict.failingLeaves.first.ruleName}');
}
```

Composites short-circuit, so these hold only what was evaluated: a failed
`AndRule` has exactly one failing leaf, and a passing `OrRule` has none
even when an earlier branch failed on the way to that pass.

## Which run mode

| Need | Reach for |
| --- | --- |
| One fast pass/fail verdict | A composite's own `evaluate()` |
| Every rule's own outcome (a status page, an audit trail) | `engine.runAll()` |
| One named rule/group; absence would be a bug | The strict lookup — throws |
| One named rule/group; absence is expected, your domain decides what it means | The non-raising lookup |
