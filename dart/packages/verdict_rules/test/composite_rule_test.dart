/// Unit tests for walking a rule *tree* before it is evaluated.
///
/// [CompositeRule] is what makes that possible without naming concrete
/// classes. The result surface answers what ran; these pin down what was built
/// -- and in particular that one walk reaches a composite this package never
/// saw, which the three built-ins alone cannot demonstrate.
library;

import 'package:test/test.dart';
import 'package:verdict_rules/verdict_rules.dart';

FunctionRule<Context> _pass(String name) =>
    FunctionRule(name, (ctx) async => const PredicateOutcome(true));

FunctionRule<Context> _fail(String name) =>
    FunctionRule(name, (ctx) async => const PredicateOutcome(false));

/// A composite defined outside the package, implementing the interface.
///
/// It implements [CompositeRule] directly and extends nothing verdict owns --
/// that is the point.
class AtLeastOneOf implements CompositeRule<Context> {
  AtLeastOneOf(this.name, List<Rule<Context>> parts)
      : subRules = List.unmodifiable(parts);

  @override
  final String name;

  @override
  String? get group => null;

  @override
  final List<Rule<Context>> subRules;

  @override
  Future<RuleResult> evaluate(Context context) async {
    final subResults = <RuleResult>[];
    for (final part in subRules) {
      subResults.add(await part.evaluate(context));
      if (subResults.last.passed) {
        return RuleResult(
          ruleName: name,
          passed: true,
          subResults: subResults,
          decidedByIndices: [subResults.length - 1],
        );
      }
    }
    return RuleResult(ruleName: name, passed: false, subResults: subResults);
  }
}

/// One walk, with no knowledge of which composite it is looking at.
List<String> _leafNames(Rule<Context> rule) => rule is CompositeRule<Context>
    ? rule.subRules.expand(_leafNames).toList()
    : [rule.name];

void main() {
  group('subRules', () {
    test('AndRule exposes its sub-rules in order', () {
      final first = _pass('a');
      final second = _fail('b');
      expect(AndRule<Context>('gate', [first, second]).subRules,
          [same(first), same(second)]);
    });

    test('OrRule exposes its sub-rules in order', () {
      final first = _fail('a');
      final second = _pass('b');
      expect(OrRule<Context>('gate', [first, second]).subRules,
          [same(first), same(second)]);
    });

    test('NotRule reports one sub-rule under the shared name', () {
      final inner = _pass('inner');
      // `subRules`, not `rule`: a walk must not need to know this is a
      // negation to find its one part.
      expect(NotRule<Context>('negated', inner).subRules, [same(inner)]);
    });

    test('a vacuous composite reports no sub-rules rather than refusing', () {
      expect(AndRule<Context>('empty-and', []).subRules, isEmpty);
      expect(OrRule<Context>('empty-or', []).subRules, isEmpty);
    });

    test('the sub-rule list is a copy taken at construction', () {
      final parts = <Rule<Context>>[_pass('a')];
      final composite = AndRule<Context>('gate', parts);
      parts.clear();
      expect(composite.subRules, hasLength(1));
    });

    test('the exposed list is unmodifiable', () {
      final composite = AndRule<Context>('gate', [_pass('a')]);
      expect(() => composite.subRules.add(_pass('b')),
          throwsUnsupportedError);
    });

    test("a negation's one-element list is unmodifiable too", () {
      final composite = NotRule<Context>('negated', _pass('inner'));
      expect(() => composite.subRules.clear(), throwsUnsupportedError);
    });
  });

  group('CompositeRule', () {
    test('every built-in composite implements it', () {
      final leaf = _pass('leaf');
      expect(AndRule<Context>('and', [leaf]), isA<CompositeRule<Context>>());
      expect(OrRule<Context>('or', [leaf]), isA<CompositeRule<Context>>());
      expect(NotRule<Context>('not', leaf), isA<CompositeRule<Context>>());
    });

    test('a leaf rule does not', () {
      // What makes "structure or terminal check" answerable without naming
      // concrete classes.
      expect(_pass('leaf'), isNot(isA<CompositeRule<Context>>()));
    });

    test('a consumer-defined composite does', () {
      expect(AtLeastOneOf('custom', [_pass('a')]),
          isA<CompositeRule<Context>>());
    });
  });

  group('walking a tree', () {
    test('reaches every leaf through nested built-in composites', () {
      final tree = AndRule<Context>('top', [
        _pass('a'),
        OrRule<Context>('either', [_fail('b'), _pass('c')]),
        NotRule<Context>('not-d', _fail('d')),
      ]);
      expect(_leafNames(tree), ['a', 'b', 'c', 'd']);
    });

    test('reaches into a consumer-defined composite with the same walk', () {
      // The capability the built-ins alone cannot prove, and the reason this
      // is a published interface rather than three getters.
      final tree = AndRule<Context>('top', [
        _pass('a'),
        AtLeastOneOf('custom', [
          _fail('b'),
          NotRule<Context>('not-c', _pass('c')),
        ]),
      ]);
      expect(_leafNames(tree), ['a', 'b', 'c']);
    });

    test('leaves a consumer-defined composite evaluating normally', () async {
      // Satisfying the inspection interface costs nothing on the evaluation
      // side: it is still just a rule.
      final custom = AtLeastOneOf('custom', [_fail('b'), _pass('c')]);
      final verdict = await custom.evaluate(const {});
      expect(verdict.passed, isTrue);
      expect(verdict.decidedBy.map((r) => r.ruleName), ['c']);
    });
  });
}
