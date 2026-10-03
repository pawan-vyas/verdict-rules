/// A result has to be able to leave the process -- into a log line, an audit
/// record, an HTTP response.
///
/// `jsonEncode` cannot encode an arbitrary object, so [RuleResult.toJson] and
/// [RunResult.toJson] are what make that possible here, which is Dart's own
/// convention rather than a workaround.
///
/// Both emit the stored fields only. `leaves`, `failingLeaves` and
/// `decidedBy` are derived at any time, and two of them cannot be stored at
/// all: a leaf's own leaves list is itself, and holding the deciding children
/// as results rather than positions made the stored graph a DAG, which a
/// tree-shaped encoder expands once per path -- doubling output per level.
library;

import 'dart:convert';

import 'package:test/test.dart';
import 'package:verdict_rules/verdict_rules.dart';

import 'test_helpers.dart';

RuleResult _revive(Map<String, dynamic> payload) => RuleResult(
      ruleName: payload['ruleName'] as String,
      passed: payload['passed'] as bool,
      detail: payload['detail'] as String,
      data: payload['data'],
      subResults: [
        for (final sub in payload['subResults'] as List)
          _revive(sub as Map<String, dynamic>)
      ],
      decidedByIndices: [
        for (final i in payload['decidedByIndices'] as List) i as int
      ],
    );

void main() {
  group('serialization', () {
    test('a leaf result round-trips', () {
      final leaf = RuleResult(
        ruleName: 'a',
        passed: false,
        detail: 'too young',
        data: {'age': 15},
      );

      final restored = _revive(
        jsonDecode(jsonEncode(leaf)) as Map<String, dynamic>,
      );

      expect(restored.ruleName, 'a');
      expect(restored.passed, isFalse);
      expect(restored.detail, 'too young');
      expect(restored.data, {'age': 15});
      expect(restored.subResults, isEmpty);
    });

    test('a deep partly-failing tree round-trips intact', () async {
      final a = AndRule<Context>('a', [
        pass('a1'),
        OrRule<Context>('a2', [failing('a2x'), pass('a2y')]),
        NotRule<Context>('a3', pass('a3-inner')),
      ]);
      final result =
          await AndRule<Context>('root', [a, pass('b')]).evaluate({});

      final restored = _revive(
        jsonDecode(jsonEncode(result)) as Map<String, dynamic>,
      );

      // root short-circuited on 'a', so 'b' never ran and must be absent --
      // not present-and-failing.
      expect(restored.subResults.map((r) => r.ruleName), ['a']);
      // The derived views are recomputed from the restored tree rather than
      // carried across, so agreeing proves nothing they depend on was lost.
      expect(
        restored.leaves.map((l) => l.ruleName),
        result.leaves.map((l) => l.ruleName),
      );
      expect(restored.failingLeaves.map((l) => l.ruleName), ['a3']);
      expect(
        restored.decidedBy.map((r) => r.ruleName),
        result.decidedBy.map((r) => r.ruleName),
      );
    });

    test('the derived views are absent from the serialized form', () async {
      final result = await AndRule<Context>('root', [pass('a'), failing('b')])
          .evaluate({});

      final payload = jsonDecode(jsonEncode(result)) as Map<String, dynamic>;

      expect(
        payload.keys.toList()..sort(),
        [
          'data',
          'decidedByIndices',
          'detail',
          'passed',
          'ruleName',
          'subResults'
        ],
      );
      for (final derived in ['leaves', 'failingLeaves', 'decidedBy']) {
        expect(payload.containsKey(derived), isFalse, reason: derived);
      }
    });

    test('a RunResult round-trips', () async {
      final engine = RulesEngine<Context>([pass('a'), failing('b')]);
      final run = await engine.runAll({});

      final payload = jsonDecode(jsonEncode(run)) as Map<String, dynamic>;

      expect(payload['passed'], isFalse);
      expect(payload.keys.toList()..sort(), ['passed', 'results']);
      final restored = RunResult(
        passed: payload['passed'] as bool,
        results: [
          for (final r in payload['results'] as List)
            _revive(r as Map<String, dynamic>)
        ],
      );
      expect(restored.failingLeaves.map((l) => l.ruleName), ['b']);
    });

    test('a result built from a list the caller then added to still encodes',
        () {
      // The early-define/late-init shape. Copying on construction severs the
      // alias, so the encoder walks a finite tree instead of recursing.
      final kids = <RuleResult>[];
      final result = RuleResult(ruleName: 'p', passed: false, subResults: kids);
      kids.add(result);

      final payload = jsonDecode(jsonEncode(result)) as Map<String, dynamic>;

      expect(payload['subResults'], isEmpty);
    });

    test('serialized size grows with depth, not exponentially in it', () async {
      // Forty levels of real nesting. With the deciding children stored as
      // results rather than positions, each level doubled the output: depth
      // 16 measured 13.6 MB.
      Rule<Context> rule = failing('leaf');
      for (var level = 0; level < 40; level++) {
        rule = AndRule<Context>('level$level', [rule]);
      }

      final encoded = jsonEncode(await rule.evaluate({}));

      expect(encoded.length, lessThan(10000),
          reason: 'expected linear growth, got ${encoded.length} chars');
      expect(encoded, contains('"leaf"'));
    });

    test('an unencodable payload in data fails at the encoder', () {
      // `data` is opaque: the package never reads it and never promises it is
      // encodable. A caller putting something jsonEncode cannot handle in
      // there gets jsonEncode's own failure, not a package error and not a
      // silently dropped field.
      final result = RuleResult(
        ruleName: 'a',
        passed: true,
        data: Object(),
      );

      expect(
          () => jsonEncode(result), throwsA(isA<JsonUnsupportedObjectError>()));
    });
  });
}
