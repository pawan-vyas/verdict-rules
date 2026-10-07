/// A rule or result owns its collections -- a caller that keeps the list it
/// passed in cannot change one afterwards, and a consumer handed a result
/// cannot mutate its children either.
///
/// Every other suite here builds a rule or a result and asserts on what
/// evaluation computed. None asks whether the inputs can move underneath it,
/// which is the gap these cover: the collections used to be aliased, so
/// retaining the list handed to a composite let a caller change that
/// composite's sub-rules -- and its verdict -- after construction, and let an
/// early-define/late-init pattern build a result containing itself, which
/// every traversal here recurses through.
library;

import 'package:test/test.dart';
import 'package:verdict_rules/verdict_rules.dart';

Future<PredicateOutcome> _passes(Map<String, Object?> context) async =>
    const PredicateOutcome(true);

Future<PredicateOutcome> _fails(Map<String, Object?> context) async =>
    const PredicateOutcome(false);

FunctionRule<Map<String, Object?>> _rule(String name, {bool passing = true}) =>
    FunctionRule(name, passing ? _passes : _fails);

void main() {
  group('composites own their sub-rules', () {
    test('appending to the caller\'s list adds no sub-rule', () {
      final held = [_rule('a')];
      final and = AndRule('c', held);
      final or = OrRule('c', held);
      held.add(_rule('injected'));
      expect(and.toString(), contains('1 sub-rule(s)'));
      expect(or.toString(), contains('1 sub-rule(s)'));
    });

    test('clearing the caller\'s list does not empty the composite', () {
      final held = [_rule('a'), _rule('b')];
      final and = AndRule('c', held);
      held.clear();
      expect(and.toString(), contains('2 sub-rule(s)'));
    });

    test('the verdict cannot change after construction', () async {
      final held = [_rule('a')];
      final and = AndRule('and1', held);
      expect((await and.evaluate({})).passed, isTrue);

      held.add(_rule('sabotage', passing: false));
      expect((await and.evaluate({})).passed, isTrue);
    });
  });

  group('results own their children', () {
    test('subResults is copied', () {
      final held = [RuleResult(ruleName: 'child', passed: true)];
      final result =
          RuleResult(ruleName: 'parent', passed: true, subResults: held);
      held.add(RuleResult(ruleName: 'injected', passed: false));
      expect(result.subResults.map((r) => r.ruleName), ['child']);
    });

    test('decidedByIndices is copied', () {
      final child = RuleResult(ruleName: 'child', passed: false);
      final held = [0];
      final result = RuleResult(
        ruleName: 'parent',
        passed: false,
        subResults: [child],
        decidedByIndices: held,
      );
      held.clear();
      expect(result.decidedBy.map((r) => r.ruleName), ['child']);
    });

    test('an index naming a child that does not exist is rejected', () {
      // The one way the indices form can be wrong, caught at construction
      // rather than when something later reads decidedBy. The objects form
      // admitted no equivalent check -- nothing stopped it naming a result
      // that was never a child of this one.
      final child = RuleResult(ruleName: 'child', passed: false);

      expect(
        () => RuleResult(
          ruleName: 'parent',
          passed: false,
          subResults: [child],
          decidedByIndices: [1],
        ),
        throwsArgumentError,
      );
    });

    test('decidedBy cannot disagree with subResults', () {
      // decidedBy is derived, so there is no second stored list that could
      // drift out of step with the children it names.
      final a = RuleResult(ruleName: 'a', passed: true);
      final b = RuleResult(ruleName: 'b', passed: false);
      final result = RuleResult(
        ruleName: 'parent',
        passed: false,
        subResults: [a, b],
        decidedByIndices: [1],
      );

      expect(result.decidedBy.first, same(result.subResults[1]));
    });

    test('RunResult.results is copied', () {
      final held = [RuleResult(ruleName: 'a', passed: true)];
      final run = RunResult(passed: true, results: held);
      held.add(RuleResult(ruleName: 'injected', passed: false));
      expect(run.results.map((r) => r.ruleName), ['a']);
    });

    test('a result cannot be made to contain itself', () {
      // Early-define/late-init: add the result to the very list it was
      // constructed from. Copying severs it, so the traversals terminate.
      final kids = <RuleResult>[];
      final result = RuleResult(ruleName: 'p', passed: false, subResults: kids);
      kids.add(result);

      expect(result.subResults, isEmpty);
      expect(result.leaves.map((l) => l.ruleName), ['p']);
      expect(result.failingLeaves.map((l) => l.ruleName), ['p']);
    });

    test('the exposed lists reject mutation by a consumer too', () {
      // List.unmodifiable gives this on top of the copy: a consumer handed a
      // result cannot add to its children either.
      final result = RuleResult(
        ruleName: 'parent',
        passed: true,
        subResults: [RuleResult(ruleName: 'child', passed: true)],
      );
      expect(
        () => result.subResults.add(RuleResult(ruleName: 'x', passed: true)),
        throwsUnsupportedError,
      );
      expect(
        () => result.decidedByIndices.add(0),
        throwsUnsupportedError,
      );
      expect(
        () => RunResult(passed: true, results: [])
            .results
            .add(RuleResult(ruleName: 'x', passed: true)),
        throwsUnsupportedError,
      );
    });

    test('mutating a derived list changes nothing on the result', () {
      // decidedBy, leaves and failingLeaves each build a fresh list per call,
      // so they are growable -- but writing to one is writing to a copy. The
      // stored lists above are what must reject mutation.
      final result = RuleResult(
        ruleName: 'parent',
        passed: false,
        subResults: [RuleResult(ruleName: 'child', passed: false)],
        decidedByIndices: [0],
      );

      result.decidedBy.add(RuleResult(ruleName: 'x', passed: true));
      result.leaves.clear();

      expect(result.decidedBy.map((r) => r.ruleName), ['child']);
      expect(result.leaves.map((l) => l.ruleName), ['child']);
    });
  });

  group('the engine owns its rules', () {
    test('appending to the caller\'s list registers no rule', () async {
      final held = [_rule('a')];
      final engine = RulesEngine(held);
      held.add(_rule('injected'));
      expect(engine.ruleNames, ['a']);
      expect((await engine.runAll({})).results, hasLength(1));
    });
  });

  group('NotRule owns its child', () {
    test('holds one rule, so there is no collection to copy', () async {
      final result = await NotRule('not_a', _rule('a')).evaluate({});
      expect(result.passed, isFalse);
      expect(result.subResults.map((r) => r.ruleName), ['a']);
    });
  });
}
