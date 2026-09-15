<!-- Title: Sample — Data-Driven Rule Sets (Dart) -->
# Sample: Data-Driven Rule Sets

> The problem, the design, and what a solution must demonstrate are
> language-agnostic and live in [`README.md`](README.md) — read that
> first. This page is the Dart implementation of it.

## The naive way (and why it breaks down)

The obvious first implementation compiles every configured condition
straight into an `if`/`else if` ladder in application code:

```dart
bool matchesConditionGrant(String? category, List<String> groups) {
  if (category == 'Manager' && groups.contains('Headquarters')) {
    return true;
  }
  if (category == 'Analyst' &&
      (groups.contains('Support') || groups.contains('Headquarters'))) {
    return true;
  }
  return false;
}
```

This is, functionally, a two-row configuration table hand-transcribed
into source code — every new condition an admin wants is a developer
task, a PR, and a deploy. See the spec for the rest of what this shape
gets wrong.

## The `verdict` way

```dart
import 'package:verdict_rules/verdict_rules.dart';

enum RowOperator { gte, eq, in_ }

/// One stored row describing a single, independently-editable rule.
class ConfiguredRow {
  final int id;
  final String field;
  final RowOperator operator;
  final Object? value;

  ConfiguredRow({
    required this.id,
    required this.field,
    required this.operator,
    required this.value,
  });
}

/// Turn one stored row into a Rule -- the only place that knows how.
/// `implements Rule` is required here -- Dart has no free structural
/// typing for a multi-member interface the way TypeScript's structural
/// `interface` does, so this can't be a bare object literal the way
/// the JS/TS version is.
class RowRule implements Rule {
  final ConfiguredRow _row;

  RowRule(this._row);

  @override
  String get name => 'row:${_row.id}';

  @override
  String? get group => null;

  @override
  Future<RuleResult> evaluate(Map<String, Object?> context) async {
    final actual = context[_row.field];
    final checks = <RowOperator, bool Function()>{
      RowOperator.gte: () => actual != null && (actual as num) >= (_row.value! as num),
      RowOperator.eq: () => actual == _row.value,
      RowOperator.in_: () => (_row.value! as Set<Object?>).contains(actual),
    };
    final passed = checks[_row.operator]!();
    return RuleResult(ruleName: name, passed: passed);
  }
}

Rule ruleForRow(ConfiguredRow row) => RowRule(row);

/// Stand-in for a real DB read -- the only I/O in this whole pattern.
/// A real implementation queries storage instead of returning a literal.
Future<List<ConfiguredRow>> fetchCurrentRows() async => [
      ConfiguredRow(id: 1, field: 'category', operator: RowOperator.eq, value: 'Manager'),
      ConfiguredRow(
        id: 2,
        field: 'region',
        operator: RowOperator.in_,
        value: {'US', 'CA', 'UK'},
      ),
    ];

/// Combine values: `all` (AndRule -- every row must pass; empty config
/// passes) or `any` (OrRule -- at least one row must pass; empty config
/// fails). Which default is correct is a domain decision, made
/// explicitly here, not inferred -- see the spec's "reading the
/// diagram" note on this.
enum Combine { all, any }

/// Build the current rule set fresh and evaluate it -- nothing retained
/// between calls.
Future<bool> evaluateAgainstCurrentConfig(
  Map<String, Object?> context,
  Combine combine,
) async {
  final rows = await fetchCurrentRows();
  final rules = rows.map(ruleForRow).toList();
  final combined = combine == Combine.all
      ? AndRule('combined', rules)
      : OrRule('combined', rules);
  final result = await combined.evaluate(context);
  return result.passed;
}
```

Against the two rows above — a category grant and a region grant, both
required:

```dart
await evaluateAgainstCurrentConfig({'category': 'Manager', 'region': 'US'}, Combine.all);
// true -- matches both rows

await evaluateAgainstCurrentConfig({'category': 'Manager', 'region': 'DE'}, Combine.all);
// false -- row 2 fails; the DE region isn't in the configured set

await evaluateAgainstCurrentConfig({'category': 'Manager', 'region': 'DE'}, Combine.any);
// true -- combine=any only needs one row to pass
```

Editing row 2's `value` to add `'DE'` — a data change, in whatever
storage `fetchCurrentRows` reads from — changes the second call's
result with no edit to this function, `RowRule`, or the combinator.

## Related

- [`README.md`](README.md) —
  the language-agnostic spec this page implements.
- [`../../extending/data-driven-rule-construction/`](../../extending/data-driven-rule-construction/README.md) —
  the general scenario this sample is a fuller version of, including how
  two unrelated domains share one engine without coupling to each
  other.
- [`dynamic-discounts/dart.md`](../dynamic-discounts/dart.md) — a
  smaller, single-`AndRule` instance of the same idea.
