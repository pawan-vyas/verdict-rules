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
/// verdict's side to support it. `implements Rule<TContext>` is
/// required here -- Dart has no free structural typing for a
/// multi-member interface the way Python's Protocol or TypeScript's
/// structural interface do.
class ThresholdRule<TContext> implements Rule<TContext> {
  @override
  final String name;

  @override
  final String? group;

  final List<Rule<TContext>> _rules;
  final int _minimum;

  ThresholdRule(this.name, List<Rule<TContext>> rules, int minimum, {this.group})
      : _rules = rules,
        _minimum = minimum;

  @override
  Future<RuleResult> evaluate(TContext context) async {
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

`ThresholdRule<TContext>` can now be handed to a `RulesEngine`, nested
inside an `AndRule`, or hold an `AndRule` as one of its own sub-rules —
every existing piece of this package already knows how to run it,
because nothing anywhere checks the runtime type of a `Rule`;
`implements Rule<TContext>` is the only contract that matters.

The same case the spec's own diagram shows — 2 of 3 needed, the third
sub-rule fails:

```dart
Future<RuleResult> alwaysPass(String name) async =>
    RuleResult(ruleName: name, passed: true);
Future<RuleResult> alwaysFail(String name) async =>
    RuleResult(ruleName: name, passed: false);

Future<void> main() async {
  final atLeastTwo = ThresholdRule<Map<String, Object?>>(
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

`ThresholdRule<TContext>` binds every direct sub-rule to the same
`TContext` — but a sub-rule can be a
[`ProjectingRule`](../reusing-a-rule-across-contexts/dart.md), which
itself satisfies `Rule<TContext>` while its wrapped rule reads a
narrower, different type internally. The combinator stays bound to one
context; what its sub-rules actually read does not have to match:

```dart
final verifiedRule = FunctionRule('is_verified_user', isVerifiedUser); // reads UserFlag
final checkoutVerified = ProjectingRule<OrderContext, UserFlag>(
  verifiedRule,
  (ctx) => UserFlag(isVerified: ctx.isVerified),
);

final qualifies = ThresholdRule<OrderContext>(
  'qualifies',
  [
    checkoutVerified, // reads UserFlag internally, via the projection
    FunctionRule('has_promo_code', hasPromoCode), // reads OrderContext directly
  ],
  2,
);
```

## Related

- [`README.md`](README.md) — the language-agnostic scenario this page
  implements.
- [`../../samples/graduation-requirement-verdict/`](../../samples/graduation-requirement-verdict/README.md) —
  `AtLeastNRule`, the design this exact pattern would back, once this
  SDK has its own tested instance.
- [`../reusing-a-rule-across-contexts/dart.md`](../reusing-a-rule-across-contexts/dart.md) —
  `ProjectingRule` itself, used above to mix a sub-rule reading a
  narrower context into a `ThresholdRule<TContext>` bound to a wider
  one.
