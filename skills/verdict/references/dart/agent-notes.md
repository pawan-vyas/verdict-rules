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
            subResults: const [], decidedByIndices: const [])
const RuleResult.leaf(ruleName: n, passed: p, detail: '', data: null)  // no children, so const works
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
rule the run evaluated. Both forward to each result's own view -- in
particular `failingLeaves` is **not** a filter over `leaves`, because a
result's verdict is not a function of its leaves' verdicts (a failed
`NotRule` is its own failing leaf; a passed `OrRule` may hold a failed
branch it recovered from).

**Only `subResults` and `decidedByIndices` are stored**, and both are
unmodifiable -- `subResults.add(...)` throws. The other three are getters
that build a fresh growable list per call, so writing to one of those is
writing to a copy. Build a result by passing positions, not children:

```dart
RuleResult(ruleName: 'pair', passed: false, subResults: [a, b],
    decidedByIndices: [1])
```

An index naming a child the result does not have throws `ArgumentError` at
construction.

**Serializing needs `toJson()`, which both result types have** -- unlike
the other three SDKs, `jsonEncode` cannot encode an arbitrary object:

```dart
jsonEncode(result);  // uses RuleResult.toJson()
```

Key an audit trail on a leaf's own `ruleName`, never a composite's:

```dart
final verdict = await graduates.evaluate(student);
if (!verdict.passed) {
  final refusals = verdict.failingLeaves.map((leaf) => leaf.ruleName);
  log.warning("refused by ${refusals.join(', ')}");
}
```

Composites short-circuit, so these hold only what was evaluated: a passing
`OrRule` has no failing leaves even when an earlier branch failed on the
way to that pass. **A failed `AndRule` reports the failing leaves of the
one sub-rule that stopped it** -- a single leaf only when that sub-rule is
itself a leaf, and several when it is a composite that failed on more than
one of its own. Read the whole list; `.first` names one of several causes
without saying so, and throws outright on an empty list.

## Which run mode

| Need | Reach for |
| --- | --- |
| One fast pass/fail verdict | A composite's own `evaluate()` |
| Every rule's own outcome (a status page, an audit trail) | `engine.runAll()` |
| One named rule/group; absence would be a bug | The strict lookup — throws |
| One named rule/group; absence is expected, your domain decides what it means | The non-raising lookup |
