import 'package:test/test.dart';
import 'package:verdict_rules/verdict_rules.dart';

/// A plain top-level function, declared as nothing in particular.
Future<RuleResult> hasQuorum(Map<String, Object?> ctx) async =>
    RuleResult(ruleName: 'quorum', passed: ctx.length >= 3);

/// Coverage for Dart-specific idioms, not universal contracts.
/// `rule_test.dart` and `engine_test.dart` together are this package's 1:1
/// port of Python's own core contract suite; this file proves things true
/// only because of how Dart itself works, so it stays separate from that
/// mirror rather than diluting it.
void main() {
  test('a top-level function is a rule with nothing declared', () async {
    // Dart's structural typing for function types: hasQuorum was never
    // declared to be anything rule-shaped, and matching the RulePredicate
    // signature is enough. Passed as a tear-off, not a closure.
    final result = await AndRule('composed', [
      FunctionRule('quorum', hasQuorum),
    ]).evaluate({'a': 1, 'b': 2, 'c': 3});
    expect(result.passed, isTrue);
  });

  test(
    'a plain class satisfying the contract works without subclassing',
    () async {
      // Dart has no structural typing for a multi-member interface like
      // Rule -- an object carrying name/group/evaluate is not thereby a
      // Rule the way Python's Protocol or TypeScript's structural typing
      // would accept it. `implements Rule` is the explicit opt-in.
      final result = await AndRule('composed', [_Custom()]).evaluate({});
      expect(result.passed, isTrue);
    },
  );
}

class _Custom implements Rule {
  @override
  String get name => 'custom';

  @override
  String? get group => null;

  @override
  Future<RuleResult> evaluate(Map<String, Object?> context) async =>
      RuleResult(ruleName: name, passed: true);
}
