import 'package:test/test.dart';
import 'package:verdict_rules/verdict_rules.dart';

/// A plain top-level function, declared as nothing in particular.
Future<RuleResult> hasQuorum(Context ctx) async =>
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
    // signature is enough. Passed as a tear-off, not a closure. TContext is
    // inferred as Context from hasQuorum's own parameter type -- no
    // explicit type argument needed here.
    final result = await AndRule<Context>('composed', [
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
      // would accept it. `implements Rule<Context>` is the explicit opt-in
      // -- this package's one real breaking migration from its pre-generic
      // form (`implements Rule` alone no longer compiles; see the test
      // below for that fact made explicit).
      final result =
          await AndRule<Context>('composed', [_Custom()]).evaluate({});
      expect(result.passed, isTrue);
    },
  );

  test(
    'an explicit Rule implementation must now name its context',
    () async {
      // The migration this package's generics work introduces: an
      // implementation that would have said `implements Rule` before
      // generics existed must now say `implements Rule<Context>` (or
      // `implements Rule<SomeTypedContext>`) explicitly -- there is no
      // default type parameter to fall back on. _TypedCustom below is the
      // same shape as _Custom, but against a typed, non-dict context,
      // proving the migration isn't dict-context-only.
      final result = await AndRule<_OrderContext>('composed', [_TypedCustom()])
          .evaluate(_OrderContext(total: 75));
      expect(result.passed, isTrue);
    },
  );
}

class _Custom implements Rule<Context> {
  @override
  String get name => 'custom';

  @override
  String? get group => null;

  @override
  Future<RuleResult> evaluate(Context context) async =>
      RuleResult(ruleName: name, passed: true);
}

class _OrderContext {
  final double total;
  const _OrderContext({required this.total});
}

class _TypedCustom implements Rule<_OrderContext> {
  @override
  String get name => 'order_total_met';

  @override
  String? get group => null;

  @override
  Future<RuleResult> evaluate(_OrderContext context) async =>
      RuleResult(ruleName: name, passed: context.total >= 50);
}
