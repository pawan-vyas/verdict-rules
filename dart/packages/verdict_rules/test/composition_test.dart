/// Unit tests for the composition primitives behind AndRule/OrRule, and the
/// new shipped NotRule: SequentialEvaluator, ShortCircuitEvaluator, and
/// RuleResult.decidedBy.
///
/// Each type's own doc comment in `lib/src/rule.dart` states the contract
/// these pin down case by case. A Dart-idiom sibling of Python's own
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

    // AndRule/OrRule always route through ShortCircuitEvaluator, which
    // unconditionally discards and recomputes decidedBy from its own stopOn
    // after delegating here -- so no AndRule/OrRule test, however thorough,
    // can ever exercise SequentialEvaluator's own generic decidedBy rule
    // directly. Only a test instantiating SequentialEvaluator itself, the
    // way a custom composite would, can reach it. Found by this package's
    // own mutation testing run, which survived a `soFar.length ==
    // rules.length` -> `!=` mutant at the line computing this.
    test(
        'decided in-loop with items left unevaluated names only the deciding sub-result',
        () async {
      final rules = [_pass('a'), _pass('b'), _pass('c')];
      final evaluator = SequentialEvaluator<Context>(
        // Resolves on the second of three rules -- soFar has two evaluated
        // sub-results at that point, not just one, so this genuinely
        // distinguishes "every sub-result seen so far" from "just the one
        // that decided it" (deciding on the very first rule can't: a
        // one-element soFar looks identical either way).
        decider: (latest, soFar, total) => soFar.length == 2 ? true : null,
        vacuousResult: true,
      );
      final result = await evaluator.evaluate('e', rules, {});
      expect(result.subResults.map((r) => r.ruleName), ['a', 'b']);
      expect(result.decidedBy.map((r) => r.ruleName), ['b']);
    });

    test(
        'decided in-loop on exactly the last sub-rule names every evaluated sub-result',
        () async {
      final rules = [_pass('a'), _pass('b'), _pass('c')];
      final evaluator = SequentialEvaluator<Context>(
        // Resolves only once every rule has been evaluated -- soFar.length
        // == total holds here exactly like it would for the post-loop
        // fallback below, but this decision is reached through the in-loop
        // `early != null` branch, on the loop's own final iteration, never
        // through the fallback. decidedBy must still be every evaluated
        // sub-result, not just the one that happened to decide it.
        decider: (latest, soFar, total) => soFar.length == total ? true : null,
        vacuousResult: false,
      );
      final result = await evaluator.evaluate('e', rules, {});
      expect(result.subResults.map((r) => r.ruleName), ['a', 'b', 'c']);
      expect(result.decidedBy.map((r) => r.ruleName), ['a', 'b', 'c']);
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

  group('RuleResult.decidedBy', () {
    // The one-level, non-recursive explanation for a composite's own
    // verdict. Supersedes AndRule.failed/passing and OrRule.passed/failing
    // (removed): those were static methods a caller could apply to the
    // wrong family's result and get a plausible, silently wrong answer --
    // confirmed with concrete cases from a real adopter review, not
    // hypothetical. decidedBy closes that structurally: there is no second
    // method to reach for, every result carries its own correctly-populated
    // field.

    test('AndRule failing early names just the decisive failure', () async {
      final rule = AndRule<Context>(
          'and1', [_pass('a'), _fail('b'), _pass('c'), _pass('d')]);
      final result = await rule.evaluate({});
      expect(result.decidedBy.map((r) => r.ruleName), ['b']);
    });

    test('AndRule failing on its last item still names just that one',
        () async {
      // The case a first attempt at this got wrong: subResults.length ==
      // total holds here exactly like it does for a genuine full pass, so
      // a rule based on count alone can't tell them apart -- position in
      // the list is irrelevant to blame; ShortCircuitEvaluator's own
      // stopOn is what actually distinguishes them.
      final rule =
          AndRule<Context>('and1', [_pass('a'), _pass('b'), _fail('c')]);
      final result = await rule.evaluate({});
      expect(result.decidedBy.map((r) => r.ruleName), ['c']);
    });

    test('AndRule fully passing names every sub-result', () async {
      final rule =
          AndRule<Context>('and1', [_pass('a'), _pass('b'), _pass('c')]);
      final result = await rule.evaluate({});
      expect(result.decidedBy.map((r) => r.ruleName), ['a', 'b', 'c']);
    });

    test('AndRule vacuous pass names nothing', () async {
      final rule = AndRule<Context>('and1', const []);
      final result = await rule.evaluate({});
      expect(result.decidedBy, isEmpty);
    });

    test('OrRule passing early names just the decisive pass', () async {
      final rule = OrRule<Context>('or1', [_fail('a'), _pass('b'), _fail('c')]);
      final result = await rule.evaluate({});
      expect(result.decidedBy.map((r) => r.ruleName), ['b']);
    });

    test('OrRule all-fail names every sub-result', () async {
      // Mirrors the AndRule last-item case with the opposite polarity --
      // OrRule's all-fail verdict is only known once every item is seen,
      // genuinely collective, not attributable to the last one alone.
      final rule = OrRule<Context>('or1', [_fail('a'), _fail('b'), _fail('c')]);
      final result = await rule.evaluate({});
      expect(result.decidedBy.map((r) => r.ruleName), ['a', 'b', 'c']);
    });

    test('OrRule vacuous fail names nothing', () async {
      final rule = OrRule<Context>('or1', const []);
      final result = await rule.evaluate({});
      expect(result.decidedBy, isEmpty);
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

    test('decidedBy is the inner result in both directions', () async {
      // Unconditional, unlike failingLeaves' own self-as-leaf rule --
      // 'inner passed' is genuinely why a failing NotRule failed, not an
      // inconsistency to paper over.
      final failing =
          await NotRule<Context>('not1', _fail('inner')).evaluate({});
      expect(failing.decidedBy.map((r) => r.ruleName), ['inner']);

      final passing =
          await NotRule<Context>('not2', _pass('inner')).evaluate({});
      expect(passing.decidedBy.map((r) => r.ruleName), ['inner']);
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
