/// Unit tests for RuleResult.leaves/failingLeaves and RunResult's own
/// forwarders.
///
/// `failingLeaves` is an independent recursion, not a filter over `leaves`
/// -- see RuleResult.failingLeaves's own doc comment for the exact formula
/// this file pins down case by case. A Dart-idiom sibling of Python's own
/// test_result.py.
library;

import 'package:test/test.dart';
import 'package:verdict_rules/verdict_rules.dart';

RuleResult _leaf(String name, bool passed, {String detail = ''}) =>
    RuleResult(ruleName: name, passed: passed, detail: detail);

RuleResult _composite(String name, bool passed, List<RuleResult> subResults) =>
    RuleResult(ruleName: name, passed: passed, subResults: subResults);

FunctionRule<Context> _pass(String name) =>
    FunctionRule(name, (ctx) async => const PredicateOutcome(true));

FunctionRule<Context> _fail(String name) =>
    FunctionRule(name, (ctx) async => const PredicateOutcome(false));

void main() {
  group('leaves', () {
    test('a leaf result is its own single leaf', () {
      final leaf = _leaf('a', true);
      expect(leaf.leaves, [leaf]);
    });

    test('a composite flattens its direct children', () {
      final a = _leaf('a', true);
      final b = _leaf('b', false);
      final composite = _composite('and1', false, [a, b]);
      expect(composite.leaves, [a, b]);
    });

    test('nested composites flatten all the way down', () {
      final a = _leaf('a', true);
      final b = _leaf('b', true);
      final c = _leaf('c', false);
      final inner = _composite('inner_and', true, [a, b]);
      final outer = _composite('outer_and', false, [inner, c]);
      expect(outer.leaves, [a, b, c]);
    });

    test('an empty composite has no leaves at all', () {
      // A degenerate composite (e.g. AndRule([])) carries no subResults,
      // which collapses to the leaf case -- not a composite with zero
      // children; absence of subResults *is* the leaf signal.
      final empty = _composite('and1', true, const []);
      expect(empty.leaves, [empty]);
    });
  });

  group('failingLeaves', () {
    test('a passed leaf has no failing leaves', () {
      expect(_leaf('a', true).failingLeaves, isEmpty);
    });

    test('a failed leaf is its own failing leaf', () {
      final leaf = _leaf('a', false);
      expect(leaf.failingLeaves, [leaf]);
    });

    test('AndRule failure surfaces exactly the failing child', () {
      final a = _leaf('a', true);
      final b = _leaf('b', false, detail: 'bad');
      final composite = _composite('and1', false, [a, b]);
      expect(composite.failingLeaves, [b]);
    });

    test('OrRule all-fail surfaces every failing child', () {
      final a = _leaf('a', false);
      final b = _leaf('b', false);
      final composite = _composite('or1', false, [a, b]);
      expect(composite.failingLeaves, [a, b]);
    });

    test(
        'a passed result has no failing leaves even after an earlier failed branch',
        () {
      // The case that makes this an independent recursion, not a filter
      // over `leaves`: an OrRule whose first sub-rule failed before its
      // second one passed. The whole thing passed -- `failingLeaves` must
      // report nothing, even though `leaves` still shows the earlier
      // failure.
      final failedFirst = _leaf('a', false);
      final passedSecond = _leaf('b', true);
      final orResult = _composite('or1', true, [failedFirst, passedSecond]);

      expect(orResult.leaves, [failedFirst, passedSecond]);
      expect(orResult.failingLeaves, isEmpty);
    });

    test('a failed result with no failing children is its own leaf', () {
      // The negation case: a failed NotRule wraps an inner rule that
      // itself *passed* -- recursing into subResults finds no failures at
      // all, so the composite's own failed result has to be the leaf,
      // instead of returning an empty list that would misrepresent why
      // anything failed at all.
      final passedInner = _leaf('inner', true);
      final notResult = _composite('not1', false, [passedInner]);

      expect(notResult.failingLeaves, [notResult]);
    });

    test('negation nested in a passing sibling is still the whole failure', () {
      // A nested negation pins the recursion more precisely than a
      // top-level one: all(a, not(b)) with both a and b passing -- not(b)
      // fails (b passed), the outer composite's failingLeaves has to be
      // exactly [not(b)], not empty (every actual leaf below it passed)
      // and not wrong because not(b)'s own self-as-leaf result was the
      // only failure among siblings rather than the sole child.
      final a = _leaf('a', true);
      final notB = _composite('not1', false, [_leaf('b', true)]);
      final outerAnd = _composite('and1', false, [a, notB]);

      expect(outerAnd.failingLeaves, [notB]);
    });

    test('nested failure flattens to the deepest actual failures', () {
      final a = _leaf('a', true);
      final b = _leaf('b', false);
      final innerOr =
          _composite('inner_or', false, [b]); // only 'b' ran and failed
      final outerAnd = _composite('outer_and', false, [a, innerOr]);
      expect(outerAnd.failingLeaves, [b]);
    });
  });

  group('RuleResult.leaf', () {
    test('is a compile-time constant and canonicalized', () {
      const a = RuleResult.leaf(ruleName: 'a', passed: false, detail: 'nope');
      const b = RuleResult.leaf(ruleName: 'a', passed: false, detail: 'nope');
      expect(identical(a, b), isTrue);
    });

    test('agrees with the general constructor on every field', () {
      const viaLeaf =
          RuleResult.leaf(ruleName: 'a', passed: false, detail: 'x');
      final viaGeneral = RuleResult(ruleName: 'a', passed: false, detail: 'x');

      expect(viaLeaf.ruleName, viaGeneral.ruleName);
      expect(viaLeaf.passed, viaGeneral.passed);
      expect(viaLeaf.detail, viaGeneral.detail);
      expect(viaLeaf.data, viaGeneral.data);
      expect(viaLeaf.subResults, viaGeneral.subResults);
      expect(viaLeaf.decidedByIndices, viaGeneral.decidedByIndices);
    });

    test('is a leaf for every derived view', () {
      const leaf = RuleResult.leaf(ruleName: 'a', passed: false);
      expect(leaf.leaves, [leaf]);
      expect(leaf.failingLeaves, [leaf]);
      expect(leaf.decidedBy, isEmpty);
    });

    test('its fixed collections still reject mutation', () {
      const leaf = RuleResult.leaf(ruleName: 'a', passed: true);
      expect(() => leaf.subResults.add(leaf), throwsUnsupportedError);
      expect(() => leaf.decidedByIndices.add(0), throwsUnsupportedError);
    });
  });

  group('RunResult forwarders', () {
    test('leaves flattens across every result', () {
      final a = _leaf('a', true);
      final b = _leaf('b', false);
      final composite = _composite('and1', false, [a, b]);
      final standalone = _leaf('standalone', true);
      final run = RunResult(passed: false, results: [standalone, composite]);
      expect(run.leaves, [standalone, a, b]);
    });

    test('failingLeaves forwards to each result rather than filtering', () {
      // RunResult.failingLeaves delegates to each result's own
      // failingLeaves. It is *not* a filter over leaves -- the two tests
      // below cover the cases where filtering gives a different, wrong
      // answer.
      final a = _leaf('a', true);
      final b = _leaf('b', false);
      final composite = _composite('and1', false, [a, b]);
      final standalone = _leaf('standalone', true);
      final run = RunResult(passed: false, results: [standalone, composite]);
      expect(run.failingLeaves, [b]);
    });

    test('a failed run never reports no failures', () async {
      // A failed NotRule wraps a child that passed, so it is its own failing
      // leaf. Filtering leaves by !passed finds only the passing child and
      // reports nothing -- a failed run with no failures.
      final engine = RulesEngine<Context>(
          [NotRule<Context>('not_positive', _pass('positive'))]);
      final run = await engine.runAll({});

      expect(run.passed, isFalse);
      expect(run.failingLeaves.map((l) => l.ruleName), ['not_positive']);
      expect(run.failingLeaves, run.results.single.failingLeaves);
    });

    test('a passed run never reports a failure', () async {
      // A passed OrRule can hold a branch that failed before a later one
      // recovered. Filtering leaves surfaces that branch -- a passing run
      // reporting a failure.
      final engine = RulesEngine<Context>([
        OrRule<Context>('either', [_fail('negative'), _pass('positive')])
      ]);
      final run = await engine.runAll({});

      expect(run.passed, isTrue);
      expect(run.failingLeaves, isEmpty);
      expect(run.leaves.map((l) => l.ruleName), ['negative', 'positive']);
    });

    test('an empty RunResult has no leaves', () {
      final run = RunResult(passed: true, results: []);
      expect(run.leaves, isEmpty);
      expect(run.failingLeaves, isEmpty);
    });
  });

  group('mixed composite tree', () {
    // A genuinely wide, deep tree mixing every rule kind -- AndRule,
    // OrRule, NotRule, and a bare FunctionRule -- at multiple levels on
    // multiple branches, built through real evaluation (not hand-built
    // RuleResult literals), to prove leaves/failingLeaves report
    // correctly at a scale none of the other tests here exercise.
    //
    //   root = AndRule("root", [a, b, c])
    //     a = AndRule("a", [a1, a2, a3])
    //       a1 = FunctionRule (leaf)
    //       a2 = OrRule("a2", [a2x (fails), a2y (passes)])
    //       a3 = NotRule("a3", a3Inner)
    //     b = FunctionRule (leaf)
    //     c = OrRule("c", [c1, c2])
    //       c1 = AndRule("c1", [c1x (fails), c1y])
    //       c2 = NotRule("c2", c2Inner)

    test('leaves flatten across every rule kind even when everything passes',
        () async {
      final a1 = _pass('a1');
      final a2 = OrRule('a2', [_fail('a2x'), _pass('a2y')]);
      final a3 = NotRule('a3', _fail('a3-inner')); // inner fails -> passes
      final a = AndRule('a', [a1, a2, a3]);

      final b = _pass('b');

      final c1 =
          AndRule('c1', [_fail('c1x'), _pass('c1y')]); // short-circuits, fails
      final c2 = NotRule('c2', _fail('c2-inner')); // inner fails -> passes
      final c = OrRule('c', [c1, c2]);

      final root = AndRule('root', [a, b, c]);
      final result = await root.evaluate(const {});

      expect(result.passed, isTrue);
      // c1y never ran at all (c1 short-circuited on c1x) -- absent, not
      // present-and-passing.
      expect(result.leaves.map((l) => l.ruleName).toList(),
          ['a1', 'a2x', 'a2y', 'a3-inner', 'b', 'c1x', 'c2-inner']);
      // A passing root has no failing leaves, full stop -- even though
      // c1 failed internally three branches deep, on the way to c's own
      // pass via c2.
      expect(result.failingLeaves, isEmpty);

      // decidedBy, traced at multiple levels. root/a both fully pass with
      // every top-level member evaluated (no early stop ever triggered),
      // so decidedBy names all of them, same as subResults.
      final aResult = result.subResults[0];
      final cResult = result.subResults[2];
      expect(result.decidedBy, [aResult, result.subResults[1], cResult]);
      expect(aResult.decidedBy, aResult.subResults); // [a1, a2, a3]

      // a2 (OrRule) passes via its second/last sub-rule -- decidedBy is
      // just that one, not both, even though it's the last item evaluated.
      final a2Result = aResult.subResults[1];
      expect(a2Result.decidedBy.map((r) => r.ruleName), ['a2y']);

      // a3 (NotRule) -- decidedBy is always [inner], here the failing inner
      // that made the NotRule pass.
      final a3Result = aResult.subResults[2];
      expect(a3Result.decidedBy.map((r) => r.ruleName), ['a3-inner']);

      // c (OrRule) short-circuits on c2 (its second/last item) -- decidedBy
      // is just c2, not both c1 and c2.
      expect(cResult.decidedBy, [cResult.subResults[1]]);

      // c1 (AndRule) fails on its first item -- decidedBy is just that one.
      final c1Result = cResult.subResults[0];
      expect(c1Result.decidedBy.map((r) => r.ruleName), ['c1x']);
    });

    test('failingLeaves pinpoints the exact failure through multiple levels',
        () async {
      final a1 = _pass('a1');
      final a2 = OrRule('a2', [_fail('a2x'), _pass('a2y')]);
      final a3 = NotRule('a3', _pass('a3-inner')); // inner passes -> fails
      final a = AndRule('a', [a1, a2, a3]);

      // b/c are never evaluated at all -- root short-circuits on 'a'.
      final b = _pass('b');
      final c = OrRule('c', [
        AndRule('c1', [_fail('c1x'), _pass('c1y')]),
        NotRule('c2', _fail('c2-inner')),
      ]);

      final root = AndRule('root', [a, b, c]);
      final result = await root.evaluate(const {});

      expect(result.passed, isFalse);
      expect(result.subResults.map((r) => r.ruleName).toList(),
          ['a']); // b, c never ran
      final a3Result = result.subResults[0].subResults[2];
      expect(a3Result.ruleName, 'a3');
      // The one true failure, three levels deep (root -> a -> a3), with
      // a1/a2 (passing siblings of a3) contributing nothing and b/c
      // (never evaluated) not appearing at all.
      expect(result.failingLeaves, [a3Result]);

      // decidedBy -- this tree hits the exact case a first implementation
      // pass got wrong: `a` is a 3-item AndRule that fails on its *last*
      // evaluated sub-rule (a3), so subResults.length == total here exactly
      // like a genuine full pass would. decidedBy must still be just [a3],
      // never all three -- position in the list is irrelevant to blame.
      final aResult = result.subResults[0];
      expect(aResult.decidedBy, [a3Result]);
      // a3 (NotRule) -- decidedBy is always [inner], here the passing inner
      // that made the NotRule fail.
      expect(a3Result.decidedBy.map((r) => r.ruleName), ['a3-inner']);
      // root itself fails early (on its first item, 'a') -- decidedBy names
      // just that one; b/c never evaluated at all.
      expect(result.decidedBy, [aResult]);
    });

    test(
        'NotRule wrapping a short-circuited composite with an earlier '
        'passing sibling', () async {
      // Stacks everything that could plausibly go wrong at once: an
      // earlier AndRule sibling that passes despite an internal failure
      // (ordering independence -- the real failure comes later), NotRule
      // wrapping a genuine OrRule rather than a bare leaf, that wrapped
      // OrRule short-circuiting internally, and the outer AndRule *also*
      // short-circuiting -- two independent prunings at different depths
      // in the same tree.
      final innerOr = OrRule('inner_or', [_fail('w'), _pass('x')]); // passes

      final innerOrForNot = OrRule(
          'inner_or_for_not', [_pass('p'), _pass('q')]); // short-circuits
      final notResult = NotRule('not1', innerOrForNot); // passed -> fails

      final z = _pass('z'); // never reached

      final root = AndRule('root', [innerOr, notResult, z]);
      final result = await root.evaluate(const {});

      expect(result.passed, isFalse);
      expect(result.leaves.map((l) => l.ruleName).toList(), ['w', 'x', 'p']);
      expect(result.failingLeaves.map((l) => l.ruleName).toList(), ['not1']);

      // decidedBy, traced through both short-circuits. root (AndRule)
      // fails early at its second item ('not1') -- 'z' (the third) never
      // ran, and decidedBy names just the decisive failure, not 'z' too.
      final innerOrResult = result.subResults[0];
      final notRuleResult = result.subResults[1];
      expect(result.decidedBy, [notRuleResult]);
      // innerOr (OrRule) passes via its second/last item ('x').
      expect(innerOrResult.decidedBy.map((r) => r.ruleName), ['x']);
      // notRuleResult (NotRule) -- decidedBy is always [inner], here the
      // passing inner_or_for_not that made it fail.
      final innerOrForNotResult = notRuleResult.subResults[0];
      expect(notRuleResult.decidedBy, [innerOrForNotResult]);
      // inner_or_for_not (OrRule) short-circuits on its very first item
      // ('p') -- 'q' never ran, decidedBy is just ['p'].
      expect(innerOrForNotResult.decidedBy.map((r) => r.ruleName), ['p']);
    });

    test('OrRule all-fail interleaves real leaves and Not fallbacks in order',
        () async {
      final rule = OrRule('root', [
        _fail('a'),
        NotRule('notB', _pass('b')),
        _fail('c'),
        NotRule('notD', _pass('d')),
      ]);
      final result = await rule.evaluate(const {});

      expect(result.passed, isFalse);
      final failing = result.failingLeaves;
      expect(
          failing.map((l) => l.ruleName).toList(), ['a', 'notB', 'c', 'notD']);
      expect(identical(failing[0], result.subResults[0]), isTrue);
      expect(identical(failing[1], result.subResults[1]), isTrue);

      // decidedBy -- every sub-rule failed (an OrRule exhausted without
      // ever finding its stopOn trigger), so decidedBy names all four,
      // mirroring the AndRule last-item case with the opposite polarity:
      // genuinely collective, not attributable to the last one alone.
      expect(result.decidedBy, result.subResults);
      // Each NotRule still reports [inner] regardless of its own verdict.
      expect(result.subResults[1].decidedBy.map((r) => r.ruleName), ['b']);
      expect(result.subResults[3].decidedBy.map((r) => r.ruleName), ['d']);
    });

    test('a genuinely vacuous composite nested inside a larger failing tree',
        () async {
      final innerOr = OrRule('inner_or', [_fail('a3'), _pass('b3')]);
      final emptyOr = OrRule('empty_or', <Rule<Context>>[]);
      final z = _pass('z');

      final root = AndRule('root', [innerOr, emptyOr, z]);
      final result = await root.evaluate(const {});

      expect(result.passed, isFalse);
      expect(result.leaves.map((l) => l.ruleName).toList(),
          ['a3', 'b3', 'empty_or']);
      expect(
          result.failingLeaves.map((l) => l.ruleName).toList(), ['empty_or']);

      // decidedBy -- root (AndRule) fails early at its second item
      // ('empty_or'); 'z' (the third) never ran. The vacuous empty_or is
      // both the sole failing leaf and the sole decider here.
      final emptyOrResult = result.subResults[1];
      expect(emptyOrResult.ruleName, 'empty_or');
      expect(result.decidedBy, [emptyOrResult]);
      // A vacuous composite has no sub-results to decide it by.
      expect(emptyOrResult.decidedBy, isEmpty);
    });
  });
}
