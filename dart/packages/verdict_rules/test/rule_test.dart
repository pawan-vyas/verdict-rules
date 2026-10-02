import 'package:test/test.dart';
import 'package:verdict_rules/verdict_rules.dart';

import 'test_helpers.dart';

/// A strict, 1:1 port of Python's `test_rule.py` -- FunctionRule, AndRule,
/// OrRule. Every test here has an exact Python counterpart. Dart-specific
/// coverage (structural typing for function types, an explicit `Rule`
/// implementation, the generic context migration) lives in
/// `dart_idioms_test.dart`/`generics_test.dart` instead, so this file stays
/// auditable against Python's own suite test-for-test.
void main() {
  group('FunctionRule', () {
    // Mirrors test_wraps_a_passing_predicate.
    test('wraps a passing predicate', () async {
      final rule = pass('r1');
      final result = await rule.evaluate({});
      expect(result.passed, isTrue);
      expect(result.ruleName, 'r1');
    });

    // Mirrors test_wraps_a_failing_predicate.
    test('wraps a failing predicate', () async {
      final rule = failing('r1', detail: 'nope');
      final result = await rule.evaluate({});
      expect(result.passed, isFalse);
      expect(result.detail, 'nope');
    });

    // Mirrors test_a_leaf_result_names_nothing_in_decided_by. A bare leaf
    // result -- FunctionRule.evaluate's own output, never built by
    // SequentialEvaluator/ShortCircuitEvaluator/NotRule -- has no subResults
    // to point into, so decidedBy must be empty for both a passing and a
    // failing outcome. Nothing previously read decidedBy on a leaf at all;
    // every other decidedBy test in this suite exercises a composite built
    // on top of one.
    test('a leaf result names nothing in decidedBy', () async {
      final passingResult = await pass('r1').evaluate({});
      final failingResult = await failing('r1').evaluate({});
      expect(passingResult.decidedBy, isEmpty);
      expect(failingResult.decidedBy, isEmpty);
    });

    // Mirrors test_predicate_receives_the_context.
    test('predicate receives the context', () async {
      Context? seen;
      final rule = FunctionRule<Context>('r1', (ctx) async {
        seen = ctx;
        return PredicateOutcome(true);
      });
      await rule.evaluate({'user_id': 42});
      expect(seen, {'user_id': 42});
    });

    // Mirrors test_carries_name_and_group.
    test('carries name and group', () {
      final rule = pass('r1', group: 'g1');
      expect(rule.name, 'r1');
      expect(rule.group, 'g1');
    });

    // Mirrors test_group_defaults_to_none.
    test('group defaults to null', () {
      final rule = pass('r1');
      expect(rule.group, isNull);
    });
  });

  group('AndRule', () {
    // Mirrors test_all_pass_yields_pass.
    test('all pass yields pass', () async {
      final rule = AndRule<Context>('and1', [pass('a'), pass('b')]);
      final result = await rule.evaluate({});
      expect(result.passed, isTrue);
      expect(result.ruleName, 'and1');
    });

    // Mirrors test_one_failure_yields_fail. AndRule's own `detail` is
    // always empty now (composing ShortCircuitEvaluator leaves no channel
    // to build one from) -- `failingLeaves` is the replacement for "which
    // sub-rule, and why".
    test('one failure yields fail', () async {
      final rule =
          AndRule<Context>('and1', [pass('a'), failing('b', detail: 'bad')]);
      final result = await rule.evaluate({});
      expect(result.passed, isFalse);
      expect(result.detail, isEmpty);
      expect(result.failingLeaves.map((l) => l.ruleName), ['b']);
      expect(result.failingLeaves.single.detail, 'bad');
    });

    // Mirrors test_short_circuits_after_first_failure.
    test('short-circuits after first failure', () async {
      final log = <String>[];
      final rule = AndRule<Context>('and1', [
        failing('a'),
        FunctionRule<Context>('c', (ctx) async {
          log.add('c');
          return PredicateOutcome(true);
        }),
      ]);
      await rule.evaluate({});
      expect(log, isEmpty); // never reached -- 'a' already failed
    });

    // Mirrors test_data_carries_sub_results_up_to_failure -- renamed from
    // `data` to `subResults`: `AndRule` stopped writing its sub-results
    // into `RuleResult.data` as part of this redesign (see
    // `.agents/plans/composite-rule-and-leaves-redesign/README.md`); `data`
    // itself is genuinely opaque now.
    test('subResults carries sub-results up to failure', () async {
      final rule =
          AndRule<Context>('and1', [pass('a'), failing('b'), pass('c')]);
      final result = await rule.evaluate({});
      expect(result.subResults.map((r) => r.ruleName), ['a', 'b']);
    });

    // Mirrors test_empty_rule_list_vacuously_passes.
    test('empty rule list vacuously passes', () async {
      final result = await AndRule<Context>('and1', []).evaluate({});
      expect(result.passed, isTrue);
    });
  });

  group('OrRule', () {
    // Mirrors test_any_pass_yields_pass.
    test('any pass yields pass', () async {
      final rule = OrRule<Context>('or1', [failing('a'), pass('b')]);
      final result = await rule.evaluate({});
      expect(result.passed, isTrue);
    });

    // Mirrors test_all_fail_yields_fail. OrRule's own `detail` is always
    // empty now, same reason as AndRule's above -- `failingLeaves` reports
    // every sub-rule that failed instead of one hand-built sentence.
    test('all fail yields fail', () async {
      final rule = OrRule<Context>('or1', [failing('a'), failing('b')]);
      final result = await rule.evaluate({});
      expect(result.passed, isFalse);
      expect(result.detail, isEmpty);
      expect(result.failingLeaves.map((l) => l.ruleName), ['a', 'b']);
    });

    // Mirrors test_short_circuits_after_first_pass.
    test('short-circuits after first pass', () async {
      final log = <String>[];
      final rule = OrRule<Context>('or1', [
        pass('a'),
        FunctionRule<Context>('c', (ctx) async {
          log.add('c');
          return PredicateOutcome(false);
        }),
      ]);
      await rule.evaluate({});
      expect(log, isEmpty); // never reached -- 'a' already passed
    });

    // Mirrors test_empty_rule_list_vacuously_fails.
    test('empty rule list vacuously fails', () async {
      final result = await OrRule<Context>('or1', []).evaluate({});
      expect(result.passed, isFalse);
    });

    // No Python counterpart yet (found via this package's own mutation
    // testing run, which flagged the sibling `AndRule` test above as the
    // only one of the pair actually proving `subResults` is populated).
    // Proves the same thing `AndRule`'s "subResults carries sub-results up
    // to failure" does, mirrored for the success path: `subResults` must
    // carry every sub-result seen before -- and including -- the one that
    // ended the loop, not just an empty list a dropped `soFar.add` would
    // also satisfy for `passed`/`detail` alone.
    test('subResults carries sub-results up to success', () async {
      final rule = OrRule<Context>('or1', [failing('a'), pass('b'), pass('c')]);
      final result = await rule.evaluate({});
      expect(result.subResults.map((r) => r.ruleName), ['a', 'b']);
    });
  });

  // Mirrors Python's TestExceptionPropagation in test_rule.py -- AndRule/
  // OrRule catch nothing either; a sub-rule's own exception propagates
  // straight out of evaluate(). See docs/extending/isolating-flaky-predicates/
  // for the wrapper a consumer opts into if the opposite is wanted.
  group('rule-level exception propagation', () {
    // Mirrors test_and_rule_does_not_catch_a_sub_rule_s_exception.
    test("AndRule does not catch a sub-rule's exception", () async {
      final flaky = FunctionRule<Context>('flaky', (ctx) async {
        throw StateError('boom');
      });
      expect(
        () => AndRule<Context>('and1', [pass('a'), flaky]).evaluate({}),
        throwsStateError,
      );
    });

    // Mirrors test_or_rule_does_not_catch_a_sub_rule_s_exception.
    test("OrRule does not catch a sub-rule's exception", () async {
      final flaky = FunctionRule<Context>('flaky', (ctx) async {
        throw StateError('boom');
      });
      expect(
        () => OrRule<Context>('or1', [failing('a'), flaky]).evaluate({}),
        throwsStateError,
      );
    });
  });
}
