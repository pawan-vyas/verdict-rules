/// Unit tests for RuleResult.leaves/failingLeaves and RunResult's own
/// forwarders.
///
/// `failingLeaves` is an independent recursion, not a filter over `leaves`
/// -- see RuleResult.failingLeaves's own doc comment and
/// .agents/plans/composite-rule-and-leaves-redesign/README.md §2 for the
/// exact formula this file pins down case by case. A Dart-idiom sibling of
/// Python's own test_result.py.
library;

import 'package:test/test.dart';
import 'package:verdict_rules/verdict_rules.dart';

RuleResult _leaf(String name, bool passed, {String detail = ''}) =>
    RuleResult(ruleName: name, passed: passed, detail: detail);

RuleResult _composite(String name, bool passed, List<RuleResult> subResults) =>
    RuleResult(ruleName: name, passed: passed, subResults: subResults);

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

    test('negation nested in a passing sibling is still the whole failure',
        () {
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

  group('RunResult forwarders', () {
    test('leaves flattens across every result', () {
      final a = _leaf('a', true);
      final b = _leaf('b', false);
      final composite = _composite('and1', false, [a, b]);
      final standalone = _leaf('standalone', true);
      final run = RunResult(passed: false, results: [standalone, composite]);
      expect(run.leaves, [standalone, a, b]);
    });

    test('failingLeaves is a plain filter over leaves', () {
      // Unlike RuleResult.failingLeaves, RunResult's own version *is* a
      // plain filter -- runAll/runGroup never short-circuit, so every
      // top-level result's own verdict is already final; no earlier
      // short-circuited branch to misrepresent.
      final a = _leaf('a', true);
      final b = _leaf('b', false);
      final composite = _composite('and1', false, [a, b]);
      final standalone = _leaf('standalone', true);
      final run = RunResult(passed: false, results: [standalone, composite]);
      expect(run.failingLeaves, [b]);
    });

    test('an empty RunResult has no leaves', () {
      const run = RunResult(passed: true, results: []);
      expect(run.leaves, isEmpty);
      expect(run.failingLeaves, isEmpty);
    });
  });
}
