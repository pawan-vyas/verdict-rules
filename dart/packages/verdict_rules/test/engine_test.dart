import 'dart:async';

import 'package:test/test.dart';
import 'package:verdict_rules/verdict_rules.dart';

import 'test_helpers.dart';

/// A strict, 1:1 port of Python's `test_engine.py` -- RulesEngine. Every
/// test here has an exact Python counterpart. Dart-specific coverage lives
/// in `dart_idioms_test.dart` instead, so this file stays auditable against
/// Python's own suite test-for-test.
void main() {
  group('RunAll', () {
    // Mirrors test_all_passing_rules_yields_passed_true.
    test('all passing rules yields passed true', () async {
      final engine = RulesEngine([pass('a'), pass('b')]);
      final result = await engine.runAll({});
      expect(result.passed, isTrue);
      expect(result.results.map((r) => r.ruleName), ['a', 'b']);
    });

    // Mirrors test_one_failing_rule_yields_passed_false.
    test('one failing rule yields passed false', () async {
      final engine = RulesEngine([pass('a'), failing('b')]);
      final result = await engine.runAll({});
      expect(result.passed, isFalse);
    });

    // Mirrors test_does_not_short_circuit_unlike_and_rule.
    test('does not short-circuit unlike AndRule', () async {
      final engine = RulesEngine([failing('a'), pass('b')]);
      final result = await engine.runAll({});
      expect(result.results.map((r) => r.ruleName), ['a', 'b']);
    });

    // Mirrors test_empty_engine_run_all_vacuously_passes.
    test('empty engine run-all vacuously passes', () async {
      final engine = RulesEngine([]);
      final result = await engine.runAll({});
      expect(result.passed, isTrue);
      expect(result.results, isEmpty);
    });
  });

  group('RunNamed', () {
    // Mirrors test_returns_that_rule_s_own_result.
    test("returns that rule's own result", () async {
      final engine = RulesEngine([pass('a'), failing('b')]);
      final result = await engine.runNamed('b', {});
      expect(result.ruleName, 'b');
      expect(result.passed, isFalse);
    });

    // Mirrors test_unknown_name_raises_key_error.
    test('unknown name raises ArgumentError', () async {
      final engine = RulesEngine([pass('a')]);
      expect(() => engine.runNamed('missing', {}), throwsArgumentError);
    });
  });

  group('RunGroup', () {
    // Mirrors test_runs_only_matching_group.
    test('runs only the matching group', () async {
      final engine = RulesEngine([
        pass('a', group: 'g1'),
        pass('b', group: 'g2'),
        failing('c', group: 'g1'),
      ]);
      final result = await engine.runGroup('g1', {});
      expect(result.results.map((r) => r.ruleName), ['a', 'c']);
      expect(result.passed, isFalse);
    });

    // Mirrors test_unknown_group_raises.
    test('unknown group raises ArgumentError', () async {
      final engine = RulesEngine([pass('a', group: 'g1')]);
      expect(
        () => engine.runGroup('no-such-group', {}),
        throwsArgumentError,
      );
    });

    // Mirrors test_ungrouped_rules_are_never_matched.
    test('ungrouped rules are never matched', () async {
      final engine = RulesEngine([pass('a')]); // no group
      expect(() => engine.runGroup('g1', {}), throwsArgumentError);
    });

    // Mirrors test_empty_composite_still_passes_vacuously.
    test('empty composite still passes/fails vacuously', () async {
      expect((await AndRule('none', []).evaluate({})).passed, isTrue);
      expect((await OrRule('none', []).evaluate({})).passed, isFalse);
    });
  });

  // Mirrors Python's TestTryLookups -- the non-throwing primitives, and
  // that the strict ones sit on top of them. These exist because the
  // engine cannot know what an absent group means: for one consumer it's
  // "no constraint applies, pass", for another "skip this and don't count
  // it", for a third "the configuration is wrong, fail loudly". A library
  // default would be right for one of them and wrong for the rest.
  group('try lookups', () {
    // Mirrors test_try_run_group_returns_the_result_when_present.
    test('tryRunGroup returns the result when present', () async {
      final engine =
          RulesEngine([pass('a', group: 'g1'), failing('b', group: 'g1')]);
      final result = await engine.tryRunGroup('g1', {});
      expect(result, isNotNull);
      expect(result!.results.map((r) => r.ruleName), ['a', 'b']);
      expect(result.passed, isFalse);
    });

    // Mirrors test_try_run_group_returns_none_when_absent.
    test('tryRunGroup returns null when absent', () async {
      final engine = RulesEngine([pass('a', group: 'g1')]);
      expect(await engine.tryRunGroup('no-such-group', {}), isNull);
    });

    // Mirrors test_try_run_named_returns_the_result_when_present.
    test('tryRunNamed returns the result when present', () async {
      final engine = RulesEngine([pass('a')]);
      final result = await engine.tryRunNamed('a', {});
      expect(result, isNotNull);
      expect(result!.ruleName, 'a');
    });

    // Mirrors test_try_run_named_returns_none_when_absent.
    test('tryRunNamed returns null when absent', () async {
      final engine = RulesEngine([pass('a')]);
      expect(await engine.tryRunNamed('nope', {}), isNull);
    });

    // Mirrors test_none_means_absent_never_failed.
    test('null means absent, never failed', () async {
      final engine = RulesEngine([failing('present', group: 'g1')]);

      final failed = await engine.tryRunNamed('present', {});
      expect(failed, isNotNull);
      expect(failed!.passed, isFalse);

      expect(await engine.tryRunNamed('absent', {}), isNull);
    });

    // Mirrors test_strict_forms_are_the_try_forms_plus_an_assertion.
    test('strict forms are the try forms plus an assertion', () async {
      final engine = RulesEngine([pass('a', group: 'g1')]);

      final namedStrict = await engine.runNamed('a', {});
      final namedTry = await engine.tryRunNamed('a', {});
      expect(namedStrict.ruleName, namedTry!.ruleName);

      final groupStrict = await engine.runGroup('g1', {});
      final groupTry = await engine.tryRunGroup('g1', {});
      expect(groupTry, isNotNull);
      expect(groupStrict.passed, groupTry!.passed);
      expect(groupStrict.results.length, groupTry.results.length);
    });

    // Mirrors test_fallback_matrix -- the full matrix a caller faces:
    // three states a lookup can be in, against the three things a caller
    // can decide absence means. The interesting rows are the ones where
    // the default must NOT fire -- a fallback firing on a
    // present-but-failing group turns a real rejection into a silent
    // approval, which is the whole failure this API exists to let
    // callers avoid.
    //
    //   group state       | ?? true | ?? false | strict
    //   ------------------+---------+----------+---------------
    //   present, passing  | true    | true     | passed = true
    //   present, failing  | false   | false    | passed = false  <- must not fire
    //   absent            | true    | false    | throws
    for (final (group, defaultTrue, defaultFalse, strictThrows) in [
      ('passing', true, true, false),
      ('failing', false, false, false),
      ('absent', true, false, true),
    ]) {
      test('fallback matrix: $group', () async {
        final engine = RulesEngine([
          pass('p', group: 'passing'),
          failing('f', group: 'failing'),
        ]);

        final result = await engine.tryRunGroup(group, {});

        expect(result?.passed ?? true, defaultTrue);
        expect(result?.passed ?? false, defaultFalse);

        if (strictThrows) {
          expect(() => engine.runGroup(group, {}), throwsArgumentError);
        } else {
          final strict = await engine.runGroup(group, {});
          expect(strict.passed, defaultTrue);
          expect(strict.passed, defaultFalse);
        }
      });
    }

    // Mirrors test_skipping_counts_only_what_exists.
    test('skipping counts only what exists', () async {
      final engine = RulesEngine([failing('f', group: 'failing')]);

      final evaluated = [
        await engine.tryRunGroup('failing', {}),
        await engine.tryRunGroup('absent', {}),
      ].whereType<RunResult>().toList();

      expect(evaluated, hasLength(1));
      expect(evaluated.single.passed, isFalse);
    });
  });

  // Mirrors Python's TestIntrospection -- ruleNames/groupNames let a
  // caller check instead of catching.
  group('introspection', () {
    // Mirrors test_reports_registered_names_in_order.
    test('reports registered names in order', () {
      final engine = RulesEngine([
        pass('a', group: 'g1'),
        pass('b', group: 'g2'),
        pass('c'),
      ]);
      expect(engine.ruleNames, ['a', 'b', 'c']);
      expect(engine.groupNames, ['g1', 'g2']);
    });

    // Mirrors test_group_names_is_exactly_what_run_group_accepts.
    test('groupNames is exactly what runGroup accepts', () async {
      final engine = RulesEngine([pass('a', group: 'g1'), pass('b')]);

      for (final group in engine.groupNames) {
        await engine.runGroup(group, {}); // must not throw
      }

      expect(engine.groupNames, isNot(contains('g2')));
      expect(() => engine.runGroup('g2', {}), throwsArgumentError);
    });

    // Mirrors test_empty_engine_reports_nothing.
    test('empty engine reports nothing', () {
      final engine = RulesEngine([]);
      expect(engine.ruleNames, isEmpty);
      expect(engine.groupNames, isEmpty);
    });
  });

  group('construction', () {
    // Mirrors test_duplicate_names_last_one_wins_in_by_name_lookup. Python's
    // version reaches into the engine's own private `_by_name` dict
    // directly; Dart's `_byName` is a genuinely private library member, so
    // this asserts the same fact through the public API instead -- which
    // rule `runNamed` actually returns for a duplicated name.
    test('duplicate names -- last one wins in by-name lookup', () async {
      final engine = RulesEngine([pass('a'), failing('a')]);
      final result = await engine.runNamed('a', {});
      expect(
          result.passed, isFalse); // the second registration ("a", failing) won
    });
  });

  // Mirrors Python's TestExceptionPropagation in test_engine.py -- a
  // predicate's own exception is never caught anywhere in the engine, and
  // propagates exactly as if the caller had invoked the predicate
  // directly.
  group('engine exception propagation', () {
    // Mirrors test_run_all_does_not_catch_a_predicate_s_exception.
    test("runAll does not catch a predicate's exception", () async {
      final engine = RulesEngine([
        pass('a'),
        FunctionRule('flaky', (ctx) async {
          throw TimeoutException('external check unreachable');
        }),
        pass('c'),
      ]);
      expect(() => engine.runAll({}), throwsA(isA<TimeoutException>()));
    });

    // Mirrors test_run_group_does_not_catch_a_predicate_s_exception. Uses
    // StateError rather than ArgumentError so the assertion cannot be
    // satisfied by the engine's own "no such group" ArgumentError instead
    // of the predicate's -- the group below genuinely exists, but a type
    // shared with the engine's own error would leave that unproven.
    test("runGroup does not catch a predicate's exception", () async {
      final engine = RulesEngine([
        FunctionRule('flaky', (ctx) async {
          throw StateError('bad input');
        }, group: 'g'),
      ]);
      expect(() => engine.runGroup('g', {}), throwsStateError);
    });
  });
}
