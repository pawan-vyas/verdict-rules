<!-- Title: Extending — Deciding What A Missing Rule Set Means (Dart) -->
# Deciding what a missing rule set means: Dart

> The concept, the table, and the diagram are in
> [`README.md`](README.md) — read that first. This page is the concrete
> Dart code.

```dart
import 'package:verdict_rules/verdict_rules.dart';

Future<RuleResult> isBetaTester(Map<String, Object?> context) async =>
    RuleResult(
      ruleName: 'is_beta_tester',
      passed: (context['betaTester'] as bool?) ?? false,
    );

final engine = RulesEngine([
  FunctionRule('is_beta_tester', isBetaTester, group: 'beta_checks'),
]);

final result = await engine.tryRunGroup('beta_checks', {'betaTester': true});
// RunResult(passed: true, results: [RuleResult(ruleName: is_beta_tester, passed: true)])

await engine.tryRunGroup('no_such_group', {});
// null -- the group was never registered
```

The four situations from the spec, as four different ways to consume
that same `null`:

```dart
// 1. Absence means "no constraint applies"
final result1 = await engine.tryRunGroup(group, context);
final allowed1 = result1 != null ? result1.passed : true;

// 2. Absence means "the configuration is wrong"
final result2 = await engine.tryRunGroup(group, context);
final allowed2 = result2 != null ? result2.passed : false;

// 3. Absence means "skip it" -- contributes nothing either way
final maybeResult = await engine.tryRunGroup(group, context);
final checks = [if (maybeResult != null) maybeResult];

// 4. Absence is genuinely unexpected -- say so immediately
final result4 = await engine.runGroup(group, context); // throws ArgumentError
```

`tryRunGroup` is the primitive `runGroup` is built on, not the other way
around:

```dart
Future<RunResult> runGroup(String group, Map<String, Object?> context) async {
  final result = await tryRunGroup(group, context);
  if (result == null) {
    throw ArgumentError.value(group, 'group', 'No rules in this group');
  }
  return result;
}
```

There is one lookup path. The strict form is a two-line assertion on top
of the lenient one, rather than a second implementation that could drift
from it.

If you only need to enumerate what exists, `engine.ruleNames` and
`engine.groupNames` report exactly the lookups that will not throw.

## Related

- [`README.md`](README.md) — the language-agnostic scenario this page
  implements.
