<!-- Title: Extending — A Genuinely New Rule Shape (Dart) -->
# A genuinely new rule shape: Dart

> The concept, the diagram, and the trade-off are in
> [`README.md`](README.md) — read that first. This page is the concrete
> Dart code.

```dart
import 'package:verdict_rules/verdict_rules.dart';

/// Passes if at least [minimum] of the given sub-rules pass.
///
/// Not part of verdict itself -- a consumer-defined combinator, exactly
/// as free to exist as AndRule/OrRule are, with no changes needed on
/// verdict's side to support it. `implements Rule` is required here --
/// Dart has no free structural typing for a multi-member interface the
/// way Python's Protocol or TypeScript's structural interface do.
class ThresholdRule implements Rule {
  @override
  final String name;

  @override
  final String? group;

  final List<Rule> _rules;
  final int _minimum;

  ThresholdRule(this.name, List<Rule> rules, int minimum, {this.group})
      : _rules = rules,
        _minimum = minimum;

  @override
  Future<RuleResult> evaluate(Map<String, Object?> context) async {
    final subResults = <RuleResult>[];
    for (final rule in _rules) {
      subResults.add(await rule.evaluate(context));
    }
    final passedCount = subResults.where((r) => r.passed).length;
    return RuleResult(
      ruleName: name,
      passed: passedCount >= _minimum,
      detail: '$passedCount of ${_rules.length} passed, needed $_minimum',
      data: subResults,
    );
  }
}
```

`ThresholdRule` can now be handed to a `RulesEngine`, nested inside an
`AndRule`, or hold an `AndRule` as one of its own sub-rules — every
existing piece of this package already knows how to run it, because
nothing anywhere checks the runtime type of a `Rule`; `implements Rule`
is the only contract that matters.

The same case the spec's own diagram shows — 2 of 3 needed, the third
sub-rule fails:

```dart
Future<RuleResult> alwaysPass(String name) async =>
    RuleResult(ruleName: name, passed: true);
Future<RuleResult> alwaysFail(String name) async =>
    RuleResult(ruleName: name, passed: false);

Future<void> main() async {
  final atLeastTwo = ThresholdRule(
    'at_least_two',
    [
      FunctionRule('rule_1', (ctx) => alwaysPass('rule_1')),
      FunctionRule('rule_2', (ctx) => alwaysPass('rule_2')),
      FunctionRule('rule_3', (ctx) => alwaysFail('rule_3')),
    ],
    2,
  );

  final result = await atLeastTwo.evaluate({});
  print('${result.passed}, ${result.detail}');
  // true, 2 of 3 passed, needed 2
}
```

## Related

- [`README.md`](README.md) — the language-agnostic scenario this page
  implements.
- [`../../samples/graduation-requirement-verdict/`](../../samples/graduation-requirement-verdict/README.md) —
  `AtLeastNRule`, the design this exact pattern would back, once this
  SDK has its own tested instance.
