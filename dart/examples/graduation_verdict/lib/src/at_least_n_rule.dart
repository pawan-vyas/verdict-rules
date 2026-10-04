import 'package:verdict_rules/verdict_rules.dart';

/// Passes if at least [minimum] of the given sub-rules pass.
///
/// Not part of verdict_rules itself; see docs/extending/new-rule-shape/.
/// Composes [SequentialEvaluator] directly (not [ShortCircuitEvaluator],
/// whose own decider hardcodes "the trigger value is the result" and
/// doesn't fit this rule's own policy): the decider short-circuits as soon
/// as [minimum] is mathematically decided either way -- once enough
/// sub-rules have passed to guarantee it's met, or once too many have
/// failed for it to still be reachable even if every remaining sub-rule
/// passed -- so a sub-rule after that point never runs.
class AtLeastNRule implements Rule<Context> {
  @override
  final String name;

  @override
  final String? group;

  final List<Rule<Context>> _rules;
  final int _minimum;
  final SequentialEvaluator<Context> _evaluator;

  AtLeastNRule(this.name, List<Rule<Context>> rules, int minimum, {this.group})
      // Copied, not aliased, the same way the library's own composites do
      // it: a caller that kept the list it passed could otherwise change
      // this rule's sub-rules -- and its verdict -- after construction.
      : _rules = List.unmodifiable(rules),
        _minimum = minimum,
        _evaluator = SequentialEvaluator<Context>(
          decider: (latest, soFar, total) {
            final passed = soFar.where((r) => r.passed).length;
            if (passed >= minimum) return true;
            final remaining = total - soFar.length;
            if (passed + remaining < minimum) return false;
            return soFar.length == total ? false : null;
          },
          vacuousResult: minimum <= 0,
        );

  @override
  Future<RuleResult> evaluate(Context context) async {
    final result = await _evaluator.evaluate(name, _rules, context);
    final passedCount = result.subResults.where((r) => r.passed).length;
    return RuleResult(
      ruleName: result.ruleName,
      passed: result.passed,
      detail: '$passedCount of ${_rules.length} passed, needed $_minimum',
      subResults: result.subResults,
      // Forwarded as positions, which is also what the evaluator stored: the
      // sub-results carry over unchanged, so the positions still name the
      // same children.
      decidedByIndices: result.decidedByIndices,
    );
  }
}
