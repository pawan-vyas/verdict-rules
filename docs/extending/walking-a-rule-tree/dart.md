<!-- Title: Extending — Walking a Rule Tree (Dart) -->
# Walking a rule tree: Dart

> The concept, the diagram, and the trap are in
> [`README.md`](README.md) — read that first. This page is the concrete
> Dart code.

`CompositeRule<TContext>` is an `abstract interface class`, like `Rule` — you
`implements` it and never extend it. Dart's `is` promotes the variable inside
the branch, so `subRules` needs no cast.

```dart
import 'package:verdict_rules/verdict_rules.dart';

/// A consumer-defined composite that also carries its own threshold.
class AtLeastNRule implements CompositeRule<Context> {
  AtLeastNRule(this.name, List<Rule<Context>> rules, this._minimum)
      : subRules = List.unmodifiable(rules);

  @override
  final String name;

  @override
  String? get group => null;

  @override
  final List<Rule<Context>> subRules;

  final int _minimum;

  @override
  Future<RuleResult> evaluate(Context context) async {
    final subResults = <RuleResult>[];
    for (final rule in subRules) {
      subResults.add(await rule.evaluate(context));
    }
    final passed = subResults.where((r) => r.passed).length;
    return RuleResult(
      ruleName: name,
      passed: passed >= _minimum,
      subResults: subResults,
      decidedByIndices: [
        for (var i = 0; i < subResults.length; i++)
          if (subResults[i].passed) i,
      ],
    );
  }
}

/// Every rule in the tree, parents before children, depth-first.
Iterable<Rule<Context>> walk(Rule<Context> rule) sync* {
  yield rule;
  if (rule is CompositeRule<Context>) {
    for (final part in rule.subRules) {
      yield* walk(part);
    }
  }
}

List<String> leafNames(Rule<Context> rule) => walk(rule)
    .where((r) => r is! CompositeRule<Context>)
    .map((r) => r.name)
    .toList();

/// Names used by two *different* rule objects -- a build problem.
Set<String> duplicateNames(Rule<Context> rule) {
  final seen = <String, Rule<Context>>{};
  final duplicates = <String>{};
  for (final found in walk(rule)) {
    final first = seen.putIfAbsent(found.name, () => found);
    if (!identical(first, found)) duplicates.add(found.name);
  }
  return duplicates;
}

Rule<Context> leaf(String name, {bool passed = true}) =>
    FunctionRule<Context>(name, (ctx) async => PredicateOutcome(passed));

Future<void> main() async {
  final tree = AndRule<Context>('eligible', [
    leaf('verified'),
    OrRule<Context>('either_path', [leaf('auto', passed: false), leaf('manual')]),
    NotRule<Context>('not_blocked', leaf('blocked', passed: false)),
    AtLeastNRule('two_of_three', [leaf('a'), leaf('b'), leaf('c', passed: false)], 2),
  ]);

  print(leafNames(tree));
  // [verified, auto, manual, blocked, a, b, c]

  print(duplicateNames(tree));
  // {}

  // The custom composite's own parts are reached, so its three leaves are
  // listed. An `is`-chain over AndRule/OrRule/NotRule would have stopped at
  // `two_of_three` and reported four leaves instead of seven.
  final verdict = await tree.evaluate(const {});
  print('${verdict.passed} ${verdict.subResults.length}');
  // true 4
}
```

`sync*` plus `yield*` gives a lazy depth-first walk, so a caller that only
needs the first match does not traverse the whole tree.

## Related

- [`README.md`](README.md) — the concept and the trap this page implements.
- [`../new-rule-shape/dart.md`](../new-rule-shape/dart.md) — where
  `AtLeastNRule` comes from, written out in full.
