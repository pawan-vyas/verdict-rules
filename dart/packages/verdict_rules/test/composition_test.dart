/// Unit tests for the composition primitives behind AndRule/OrRule, and the
/// new shipped NotRule: SequentialEvaluator, ShortCircuitEvaluator, and the
/// AndRule.failed/passing, OrRule.passed/failing, NotRule.negated
/// accessors.
///
/// See .agents/plans/composite-rule-and-leaves-redesign/README.md §3-§3e
/// for the design this pins down. A Dart-idiom sibling of Python's own
/// test_composition.py -- not a strict 1:1 port (this package's test
/// suite doesn't maintain that discipline the way rule_test.dart/
/// engine_test.dart do), but the same cases, case for case.
library;

import 'package:test/test.dart';
import 'package:verdict_rules/verdict_rules.dart';

FunctionRule<Context> _pass(String name) =>
    FunctionRule(name, (ctx) async => const PredicateOutcome(true));

FunctionRule<Context> _fail(String name) =>
    FunctionRule(name, (ctx) async => const PredicateOutcome(false));

FunctionRule<Context> _trackedPass(String name, List<String> calls) =>
    FunctionRule(name, (ctx) async {
      calls.add(name);
      return const PredicateOutcome(true);
    });

void main() {
  group('SequentialEvaluator', () {
    // The general-purpose piece ShortCircuitEvaluator (and a custom
    // composite like AtLeastNRule) is built on.

    test('an empty rule list returns the vacuous result directly', () async {
      // Checked before the loop runs at all -- there's no last element to
      // fall back on when the list started empty.
      final evaluator = SequentialEvaluator<Context>(
        decider: (latest, soFar, total) => null,
        vacuousResult: true,
      );
      final result = await evaluator.evaluate('e', const [], {});
      expect(result.passed, isTrue);
      expect(result.subResults, isEmpty);
    });

    test('a decider returning a value stops evaluation immediately', () async {
      final calls = <String>[];
      final rules = [
        _trackedPass('a', calls),
        _trackedPass('b', calls),
        _trackedPass('c', calls),
      ];
      final evaluator = SequentialEvaluator<Context>(
        decider: (latest, soFar, total) => soFar.length == 1 ? false : null,
        vacuousResult: true,
      );
      final result = await evaluator.evaluate('e', rules, {});

      expect(result.passed, isFalse);
      expect(calls, ['a'],
          reason: 'evaluation must stop after the first rule, never reach b/c');
      expect(result.subResults.map((r) => r.ruleName), ['a']);
    });

    test('a decider returning null throughout falls back to the vacuous result',
        () async {
      final rules = [_pass('a'), _pass('b')];
      final evaluator = SequentialEvaluator<Context>(
        decider: (latest, soFar, total) => null,
        vacuousResult: false,
      );
      final result = await evaluator.evaluate('e', rules, {});
      expect(result.passed,
          isFalse); // the vacuousResult, not a verdict from the rules
      expect(result.subResults.map((r) => r.ruleName), ['a', 'b']);
    });

    test('subResults always carries every rule actually evaluated', () async {
      final rules = [_pass('a'), _pass('b'), _pass('c')];
      final evaluator = SequentialEvaluator<Context>(
        decider: (latest, soFar, total) => null,
        vacuousResult: true,
      );
      final result = await evaluator.evaluate('e', rules, {});
      expect(result.subResults.map((r) => r.ruleName), ['a', 'b', 'c']);
    });
  });

  group('ShortCircuitEvaluator', () {
    // The exact shape AndRule (stopOn: false)/OrRule (stopOn: true) compose.

    test('stopOn: false behaves like AndRule', () async {
      final calls = <String>[];
      final evaluator = ShortCircuitEvaluator<Context>(stopOn: false);
      final rules = [_pass('a'), _fail('b'), _trackedPass('c', calls)];
      final result = await evaluator.evaluate('e', rules, {});
      expect(result.passed, isFalse);
      expect(calls, isEmpty,
          reason: "stops at the first failure -- 'c' never runs");
      expect(result.subResults.map((r) => r.ruleName), ['a', 'b']);
    });

    test('stopOn: true behaves like OrRule', () async {
      final calls = <String>[];
      final evaluator = ShortCircuitEvaluator<Context>(stopOn: true);
      final rules = [_fail('a'), _pass('b'), _trackedPass('c', calls)];
      final result = await evaluator.evaluate('e', rules, {});
      expect(result.passed, isTrue);
      expect(calls, isEmpty,
          reason: "stops at the first pass -- 'c' never runs");
      expect(result.subResults.map((r) => r.ruleName), ['a', 'b']);
    });

    test('an empty rule list derives its vacuous result from stopOn', () async {
      final andShaped = ShortCircuitEvaluator<Context>(stopOn: false);
      final orShaped = ShortCircuitEvaluator<Context>(stopOn: true);
      expect((await andShaped.evaluate('e', const [], {})).passed, isTrue);
      expect((await orShaped.evaluate('e', const [], {})).passed, isFalse);
    });

    test('all pass with stopOn: false runs every rule', () async {
      final calls = <String>[];
      final evaluator = ShortCircuitEvaluator<Context>(stopOn: false);
      final rules = [_trackedPass('a', calls), _trackedPass('b', calls)];
      final result = await evaluator.evaluate('e', rules, {});
      expect(result.passed, isTrue);
      expect(calls, ['a', 'b']);
    });

    test('all fail with stopOn: true runs every rule', () async {
      final evaluator = ShortCircuitEvaluator<Context>(stopOn: true);
      final rules = [_fail('a'), _fail('b')];
      final result = await evaluator.evaluate('e', rules, {});
      expect(result.passed, isFalse);
      expect(result.subResults.map((r) => r.ruleName), ['a', 'b']);
    });
  });

  group('AndRule accessors', () {
    test('failed returns the sole decisive failure', () async {
      final rule = AndRule<Context>('and1', [_pass('a'), _fail('b')]);
      final result = await rule.evaluate({});
      final failed = AndRule.failed(result);
      expect(failed, isNotNull);
      expect(failed!.ruleName, 'b');
    });

    test('failed returns null when the AndRule passed', () async {
      final rule = AndRule<Context>('and1', [_pass('a'), _pass('b')]);
      final result = await rule.evaluate({});
      expect(AndRule.failed(result), isNull);
    });

    test('failed returns null for an empty AndRule', () async {
      final rule = AndRule<Context>('and1', const []);
      final result = await rule.evaluate({});
      expect(AndRule.failed(result), isNull);
    });

    test('passing returns every sub-result when the AndRule passed', () async {
      final rule = AndRule<Context>('and1', [_pass('a'), _pass('b')]);
      final result = await rule.evaluate({});
      expect(AndRule.passing(result).map((r) => r.ruleName), ['a', 'b']);
    });

    test('passing excludes the decisive failure', () async {
      final rule =
          AndRule<Context>('and1', [_pass('a'), _pass('b'), _fail('c')]);
      final result = await rule.evaluate({});
      expect(AndRule.passing(result).map((r) => r.ruleName), ['a', 'b']);
    });
  });

  group('OrRule accessors', () {
    test('passed returns the sole decisive pass', () async {
      final rule = OrRule<Context>('or1', [_fail('a'), _pass('b')]);
      final result = await rule.evaluate({});
      final passed = OrRule.passed(result);
      expect(passed, isNotNull);
      expect(passed!.ruleName, 'b');
    });

    test('passed returns null when the OrRule failed', () async {
      final rule = OrRule<Context>('or1', [_fail('a'), _fail('b')]);
      final result = await rule.evaluate({});
      expect(OrRule.passed(result), isNull);
    });

    test('passed returns null for an empty OrRule', () async {
      final rule = OrRule<Context>('or1', const []);
      final result = await rule.evaluate({});
      expect(OrRule.passed(result), isNull);
    });

    test('failing returns every sub-result when the OrRule failed', () async {
      final rule = OrRule<Context>('or1', [_fail('a'), _fail('b')]);
      final result = await rule.evaluate({});
      expect(OrRule.failing(result).map((r) => r.ruleName), ['a', 'b']);
    });

    test('failing excludes the decisive pass', () async {
      final rule = OrRule<Context>('or1', [_fail('a'), _fail('b'), _pass('c')]);
      final result = await rule.evaluate({});
      expect(OrRule.failing(result).map((r) => r.ruleName), ['a', 'b']);
    });
  });

  group('NotRule', () {
    test('passes when the inner rule fails', () async {
      final rule = NotRule<Context>('not1', _fail('inner'));
      final result = await rule.evaluate({});
      expect(result.passed, isTrue);
    });

    test('fails when the inner rule passes', () async {
      final rule = NotRule<Context>('not1', _pass('inner'));
      final result = await rule.evaluate({});
      expect(result.passed, isFalse);
    });

    test('subResults truthfully carries the one inner result', () async {
      final rule = NotRule<Context>('not1', _pass('inner'));
      final result = await rule.evaluate({});
      expect(result.subResults, hasLength(1));
      expect(result.subResults[0].ruleName, 'inner');
    });

    test('negated returns the inner result', () async {
      final rule = NotRule<Context>('not1', _fail('inner'));
      final result = await rule.evaluate({});
      final inner = NotRule.negated(result);
      expect(inner.ruleName, 'inner');
      expect(inner.passed, isFalse);
    });

    test('a failed NotRule has itself as its own failing leaf', () async {
      // The negation case failingLeaves exists to handle correctly -- the
      // inner rule passed, so there's no failing descendant to recurse
      // into; the failed NotRule result has to be the leaf itself.
      final rule = NotRule<Context>('not1', _pass('inner'));
      final result = await rule.evaluate({});
      expect(result.failingLeaves, [result]);
    });

    test('toString shows the name', () {
      final rule = NotRule<Context>('not1', _pass('inner'));
      expect(rule.toString(), 'NotRule "not1"');
    });

    test('toString shows the group when present', () {
      final rule = NotRule<Context>('not1', _pass('inner'), group: 'g1');
      expect(rule.toString(), 'NotRule "not1" (g1)');
    });

    test("does not catch the inner rule's exception", () async {
      final flaky = FunctionRule<Context>('flaky', (ctx) async {
        throw StateError('boom');
      });
      final rule = NotRule<Context>('not1', flaky);
      expect(() => rule.evaluate({}), throwsStateError);
    });
  });
}
