import 'package:verdict_rules/verdict_rules.dart';

/// Passes if at least [minimum] of the given sub-rules pass.
///
/// Not part of verdict_rules itself; see
/// docs/extending/new-rule-shape/. Evaluates every sub-rule
/// unconditionally, unlike [AndRule]/[OrRule].
class AtLeastNRule implements Rule<Context> {
  @override
  final String name;

  @override
  final String? group;

  final List<Rule<Context>> _rules;
  final int _minimum;

  AtLeastNRule(this.name, List<Rule<Context>> rules, int minimum, {this.group})
      : _rules = rules,
        _minimum = minimum;

  @override
  Future<RuleResult> evaluate(Context context) async {
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
