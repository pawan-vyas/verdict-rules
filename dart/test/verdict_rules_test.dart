import 'package:test/test.dart';
import 'package:verdict_rules/verdict_rules.dart';

/// A rule that records every evaluation, so short-circuiting can be proven by
/// what actually ran rather than by the final boolean alone. A port that
/// evaluates concurrently returns the same boolean and fails only here.
FunctionRule counting(String name, bool passes, List<String> log,
        {String? group}) =>
    FunctionRule(
      name,
      (ctx) async {
        log.add(name);
        return RuleResult(ruleName: name, passed: passes);
      },
      group: group,
    );

/// A plain top-level function, declared as nothing in particular.
Future<RuleResult> hasQuorum(Map<String, Object?> ctx) async =>
    RuleResult(ruleName: 'quorum', passed: ctx.length >= 3);

void main() {
  group('FunctionRule', () {
    test('returns whatever the predicate returns, unchanged', () async {
      final rule = FunctionRule(
        'r',
        (ctx) async => RuleResult(
            ruleName: 'r', passed: true, detail: 'why', data: {'k': 1}),
      );
      final result = await rule.evaluate({});
      expect(result.passed, isTrue);
      expect(result.detail, 'why');
      expect(result.data, {'k': 1});
    });

    test('carries its group label', () {
      expect(FunctionRule('r', (ctx) async => RuleResult(ruleName: 'r', passed: true),
              group: 'g')
          .group, 'g');
    });
  });

  group('AndRule', () {
    test('passes when every sub-rule passes, and evaluates all of them',
        () async {
      final log = <String>[];
      final result = await AndRule('all', [
        counting('a', true, log),
        counting('b', true, log),
      ]).evaluate({});
      expect(result.passed, isTrue);
      expect(log, ['a', 'b']);
    });

    test('short-circuits: later sub-rules never run', () async {
      final log = <String>[];
      final result = await AndRule('all', [
        counting('a', true, log),
        counting('b', false, log),
        counting('c', true, log),
      ]).evaluate({});
      expect(result.passed, isFalse);
      expect(log, ['a', 'b'], reason: "'c' must never have been evaluated");
    });

    test('data holds only what ran, never padded, never flattened', () async {
      final log = <String>[];
      final result = await AndRule('outer', [
        AndRule('inner', [counting('deep', false, log)]),
        counting('never', true, log),
      ]).evaluate({});
      final top = result.data! as List<RuleResult>;
      expect(top, hasLength(1), reason: 'only the failing sub-rule ran');
      expect(top.single.ruleName, 'inner');
      final nested = top.single.data! as List<RuleResult>;
      expect(nested.single.ruleName, 'deep',
          reason: 'nesting is preserved, not flattened into the parent');
    });

    test('empty passes vacuously', () async {
      expect((await AndRule('none', []).evaluate({})).passed, isTrue);
    });
  });

  group('OrRule', () {
    test('short-circuits on the first pass', () async {
      final log = <String>[];
      final result = await OrRule('any', [
        counting('a', false, log),
        counting('b', true, log),
        counting('c', true, log),
      ]).evaluate({});
      expect(result.passed, isTrue);
      expect(log, ['a', 'b'], reason: "'c' must never have been evaluated");
    });

    test('empty fails vacuously — the opposite polarity to AndRule', () async {
      expect((await OrRule('none', []).evaluate({})).passed, isFalse);
    });
  });

  group('RulesEngine run modes', () {
    test('runAll never short-circuits', () async {
      final log = <String>[];
      final engine = RulesEngine([
        counting('a', false, log),
        counting('b', false, log),
        counting('c', true, log),
      ]);
      final result = await engine.runAll({});
      expect(result.passed, isFalse);
      expect(result.results, hasLength(3));
      expect(log, ['a', 'b', 'c'],
          reason: 'every rule reports even after an earlier failure');
    });

    test('runGroup evaluates only its own group, and never short-circuits',
        () async {
      final log = <String>[];
      final engine = RulesEngine([
        counting('a', true, log, group: 'g1'),
        counting('b', true, log, group: 'g2'),
        counting('c', false, log, group: 'g1'),
      ]);
      final result = await engine.runGroup('g1', {});
      expect(result.results.map((r) => r.ruleName), ['a', 'c']);
      expect(result.passed, isFalse);
    });

    test('runNamed looks one rule up', () async {
      final engine = RulesEngine([counting('a', true, [])]);
      expect((await engine.runNamed('a', {})).ruleName, 'a');
    });
  });

  group('Emptiness is not absence', () {
    test('an unknown rule name throws', () async {
      final engine = RulesEngine([counting('a', true, [], group: 'g1')]);
      expect(() => engine.runNamed('nope', {}), throwsArgumentError);
    });

    test('an unknown group throws rather than passing vacuously', () async {
      // A group exists only because some rule declared it, so a lookup that
      // matches nothing can only be a typo or a stale name. Returning a pass
      // would mean a misspelled group silently approves.
      final engine = RulesEngine([counting('a', true, [], group: 'g1')]);
      expect(() => engine.runGroup('no-such-group', {}), throwsArgumentError);
    });

    test('a rule with no group makes no group exist', () async {
      final engine = RulesEngine([counting('a', true, [])]);
      expect(() => engine.runGroup('g1', {}), throwsArgumentError);
    });

    test('but empty composites still fold to their identity', () async {
      expect((await AndRule('none', []).evaluate({})).passed, isTrue);
      expect((await OrRule('none', []).evaluate({})).passed, isFalse);
    });
  });

  group('Try lookups', () {
    // These exist because the engine cannot know what an absent group means.
    // For one consumer it is "no constraint applies, pass"; for another "skip
    // and do not count it"; for a third "the configuration is wrong, fail".
    // A library default would be right for one and wrong for the rest.

    test('returns the result when present', () async {
      final log = <String>[];
      final engine = RulesEngine([
        counting('a', true, log, group: 'g1'),
        counting('b', false, log, group: 'g1'),
      ]);

      final group = await engine.tryRunGroup('g1', {});
      expect(group, isNotNull);
      expect(group!.results.map((r) => r.ruleName), ['a', 'b']);
      expect(group.passed, isFalse);

      expect((await engine.tryRunNamed('a', {}))?.ruleName, 'a');
    });

    test('returns null when absent', () async {
      final engine = RulesEngine([counting('a', true, [], group: 'g1')]);
      expect(await engine.tryRunGroup('no-such-group', {}), isNull);
      expect(await engine.tryRunNamed('nope', {}), isNull);
    });

    test('null means absent, never failed', () async {
      // Collapsing the two would make a typo indistinguishable from a
      // legitimate rejection.
      final engine = RulesEngine([counting('present', false, [])]);
      expect((await engine.tryRunNamed('present', {}))?.passed, isFalse);
      expect(await engine.tryRunNamed('absent', {}), isNull);
    });

    test('the strict forms are the try forms plus an assertion', () async {
      // Asserting the relationship keeps the two from drifting: one lookup
      // path, and the strict form adds only the throw.
      final engine = RulesEngine([counting('a', true, [], group: 'g1')]);
      final strict = await engine.runGroup('g1', {});
      final lenient = await engine.tryRunGroup('g1', {});
      expect(lenient, isNotNull);
      expect(strict.passed, lenient!.passed);
      expect(strict.results.length, lenient.results.length);
    });

    test('the caller chooses the fallback', () async {
      final engine = RulesEngine([counting('a', true, [], group: 'present')]);
      final result = await engine.tryRunGroup('absent', {});

      expect(result?.passed ?? true, isTrue); // absence means no constraint
      expect(result?.passed ?? false, isFalse); // absence means misconfigured
      expect([result].whereType<RunResult>(), isEmpty); // skip it
      expect(() => engine.runGroup('absent', {}), throwsArgumentError);
    });
  });

  group('Introspection', () {
    test('reports exactly what the lookups accept', () async {
      final engine = RulesEngine([
        counting('a', true, [], group: 'g1'),
        counting('b', true, [], group: 'g2'),
        counting('c', true, []),
      ]);
      expect(engine.ruleNames, ['a', 'b', 'c']);
      expect(engine.groupNames, ['g1', 'g2']);
      for (final g in engine.groupNames) {
        await engine.runGroup(g, {});
      }
    });

    test('an empty engine reports nothing', () {
      final engine = RulesEngine([]);
      expect(engine.ruleNames, isEmpty);
      expect(engine.groupNames, isEmpty);
    });
  });

  test('a top-level function is a rule with nothing declared', () async {
    // Dart's structural typing for function types: hasQuorum was never
    // declared to be anything rule-shaped, and matching the signature is
    // enough. Passed as a tear-off, not a closure.
    final result = await AndRule('composed', [
      FunctionRule('quorum', hasQuorum),
    ]).evaluate({'a': 1, 'b': 2, 'c': 3});
    expect(result.passed, isTrue);
  });

  test('a plain class satisfying the contract works without subclassing',
      () async {
    final result = await AndRule('composed', [_Custom()]).evaluate({});
    expect(result.passed, isTrue);
  });
}

/// A rule shape owning its own name and group must say `implements Rule`,
/// because Dart has no structural typing for multi-member interfaces. Function
/// types are a different story — see the tear-off test above, where a plain
/// top-level function is a rule with nothing declared.
class _Custom implements Rule {
  @override
  String get name => 'custom';

  @override
  String? get group => null;

  @override
  Future<RuleResult> evaluate(Map<String, Object?> context) async =>
      RuleResult(ruleName: name, passed: true);
}
