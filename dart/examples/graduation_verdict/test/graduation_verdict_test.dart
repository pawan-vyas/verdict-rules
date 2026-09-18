/// Tests for the graduation-verdict example.
///
/// Also functions as an integration/e2e regression net for verdict_rules
/// itself, exercising FunctionRule, AndRule, OrRule, a custom Rule shape,
/// and all three RulesEngine run modes together. See
/// docs/testing/README.md.
import 'dart:convert';
import 'dart:io';

import 'package:graduation_verdict/graduation_verdict.dart';
import 'package:test/test.dart';
import 'package:verdict_rules/verdict_rules.dart';

final _fixtures =
    Directory('${Directory.current.path}/../../../fixtures/graduation_verdict')
        .absolute
        .path;
final _curriculum = loadCurriculum('$_fixtures/policies.json');
final _policies = _curriculum.$1;
final _electiveMinimum = _curriculum.$2;
final _students = loadStudents('$_fixtures/students.json');
final _edgeCases =
    jsonDecode(File('$_fixtures/edge_cases.json').readAsStringSync())
        as Map<String, Object?>;

SubjectPolicy _policy(String subjectId) =>
    _policies.firstWhere((p) => p.subjectId == subjectId);

/// Walk the first failing branch down, collecting rule names.
///
/// A nested failure is reachable by following data downward; a result's
/// data is never flattened.
List<String> _failingChain(RuleResult result) {
  final chain = <String>[];
  var node = result;
  while (node.data is List<RuleResult> &&
      (node.data as List<RuleResult>).isNotEmpty) {
    final subs = node.data as List<RuleResult>;
    final failing = subs.where((sub) => !sub.passed);
    if (failing.isEmpty) break;
    final next = failing.first;
    chain.add(next.ruleName);
    node = next;
  }
  return chain;
}

void main() {
  group('rule shape dispatch', () {
    // See docs/samples/graduation-requirement-verdict/README.md.

    test('an academic subject is a plain FunctionRule', () {
      final rule = ruleForSubject(_policy('MATH101'));
      expect(rule, isA<FunctionRule>());
    });

    test('a vocational subject is an AndRule of two', () {
      final rule = ruleForSubject(_policy('WORKSHOP201'));
      expect(rule, isA<AndRule>());
    });

    test('a language subject with exemption is an OrRule', () {
      final rule = ruleForSubject(_policy('FRENCH101'));
      expect(rule, isA<OrRule>());
    });

    test('tags core vs. elective groups correctly', () {
      expect(ruleForSubject(_policy('MATH101')).group, 'core');
      expect(ruleForSubject(_policy('ELECTIVE_ART')).group, 'elective');
    });

    test('throws for an unknown subject type', () {
      final badPolicy = _policy('MATH101').copyWith(subjectType: 'portfolio');
      expect(() => ruleForSubject(badPolicy), throwsArgumentError);
    });
  });

  group('vocational AndRule', () {
    // The AndRule built for a vocational subject needs both scores to pass.

    test('fails if only the written score passes', () async {
      final rule = ruleForSubject(_policy('WORKSHOP201'));
      final context = contextFromJson({
        'scores': {
          'WORKSHOP201': {'written_pct': 50, 'practical_pct': 45},
        },
        'cgpa': 0,
        'cgpa_floor': 0,
        'attendance_pct': 0,
        'attendance_floor': 0,
      });
      final result = await rule.evaluate(context);
      expect(result.passed, isFalse);
    });

    test('passes if both scores clear their own bar', () async {
      final rule = ruleForSubject(_policy('WORKSHOP201'));
      final context = contextFromJson({
        'scores': {
          'WORKSHOP201': {'written_pct': 50, 'practical_pct': 70},
        },
        'cgpa': 0,
        'cgpa_floor': 0,
        'attendance_pct': 0,
        'attendance_floor': 0,
      });
      final result = await rule.evaluate(context);
      expect(result.passed, isTrue);
    });
  });

  group('language OrRule', () {
    // The OrRule built for a language subject accepts either the paper or an exemption.

    test('passes via exemption when the paper fails', () async {
      final rule = ruleForSubject(_policy('FRENCH101'));
      final context = contextFromJson({
        'scores': {
          'FRENCH101': {'written_pct': 20, 'has_exemption': true},
        },
        'cgpa': 0,
        'cgpa_floor': 0,
        'attendance_pct': 0,
        'attendance_floor': 0,
      });
      final result = await rule.evaluate(context);
      expect(result.passed, isTrue);
    });

    test('fails when neither the paper nor an exemption applies', () async {
      final rule = ruleForSubject(_policy('FRENCH101'));
      final context = contextFromJson({
        'scores': {
          'FRENCH101': {'written_pct': 20, 'has_exemption': false},
        },
        'cgpa': 0,
        'cgpa_floor': 0,
        'attendance_pct': 0,
        'attendance_floor': 0,
      });
      final result = await rule.evaluate(context);
      expect(result.passed, isFalse);
    });
  });

  group('engine run modes', () {
    // runNamed/runGroup/runAll each serve the specific job the sample spec
    // claims. See docs/samples/graduation-requirement-verdict/README.md.

    test('runNamed looks up one subject', () async {
      final (engine, _) = buildGraduationCheck(_policies, _electiveMinimum);
      final result =
          await engine.runNamed('WORKSHOP201', _students['alice']!.context);
      expect(result.ruleName, 'WORKSHOP201');
      expect(result.passed, isTrue);
    });

    test("runGroup('core') reports every core subject", () async {
      final (engine, _) = buildGraduationCheck(_policies, _electiveMinimum);
      final result = await engine.runGroup('core', _students['alice']!.context);
      expect(result.results.map((r) => r.ruleName).toSet(),
          {'MATH101', 'ENG101', 'WORKSHOP201', 'FRENCH101'});
    });

    test("runGroup('elective') reports every elective subject", () async {
      final (engine, _) = buildGraduationCheck(_policies, _electiveMinimum);
      final result =
          await engine.runGroup('elective', _students['alice']!.context);
      expect(result.results.map((r) => r.ruleName).toSet(),
          {'ELECTIVE_ART', 'ELECTIVE_MUSIC', 'ELECTIVE_CS'});
    });

    test('runAll reports every subject', () async {
      final (engine, _) = buildGraduationCheck(_policies, _electiveMinimum);
      final result = await engine.runAll(_students['alice']!.context);
      expect(result.results.length, _policies.length);
    });

    test('runGroup never short-circuits, unlike the graduates composite',
        () async {
      // bob fails ENG101 (core); runGroup still reports every other core subject.
      final (engine, _) = buildGraduationCheck(_policies, _electiveMinimum);
      final result = await engine.runGroup('core', _students['bob']!.context);
      expect(result.results.length, 4);
      expect(result.passed, isFalse);
    });
  });

  group('shared fixture contract', () {
    // Every expectation in the shared fixture, asserted. Each port of
    // this example reproduces these exact numbers. See
    // fixtures/graduation_verdict/README.md for what each field checks.

    for (final studentId in _students.keys) {
      test('$studentId: verdict matches', () async {
        final (_, graduates) =
            buildGraduationCheck(_policies, _electiveMinimum);
        final record = _students[studentId]!;
        final expected = record.expected;
        final result = await graduates.evaluate(record.context);
        expect(
          result.passed,
          expected['passed'],
          reason:
              '$studentId: expected passed=${expected['passed']}, got ${result.passed} (${result.detail})',
        );
      });

      test('$studentId: short-circuit count matches', () async {
        // bob and gita both fail; bob stops after one rule, gita runs
        // all four.
        final (_, graduates) =
            buildGraduationCheck(_policies, _electiveMinimum);
        final record = _students[studentId]!;
        final expected = record.expected;
        final result = await graduates.evaluate(record.context);
        final data = result.data as List<RuleResult>;
        expect(data.length, expected['rules_evaluated']);
      });

      test('$studentId: failing chain matches', () async {
        // deepak's failure is three levels deep.
        final (_, graduates) =
            buildGraduationCheck(_policies, _electiveMinimum);
        final record = _students[studentId]!;
        final expected = record.expected;
        final result = await graduates.evaluate(record.context);
        final chain = _failingChain(result);
        expect(chain, expected['failing_chain']);
        expect(chain.isNotEmpty ? chain.first : null, expected['failing_rule']);
      });

      test('$studentId: runAll never short-circuits', () async {
        final (engine, _) = buildGraduationCheck(_policies, _electiveMinimum);
        final record = _students[studentId]!;
        final expected = record.expected['run_all'] as Map<String, Object?>;
        final result = await engine.runAll(record.context);
        expect(result.results.length, expected['evaluated']);
        expect(result.passed, expected['passed']);
      });

      test('$studentId: group results match', () async {
        // harish graduates while his elective group "fails", because the
        // group verdict is all-must-pass and the composite's requirement
        // is at-least-two-of-three.
        final (engine, _) = buildGraduationCheck(_policies, _electiveMinimum);
        final record = _students[studentId]!;
        final groups = record.expected['groups'] as Map<String, Object?>;
        for (final entry in groups.entries) {
          final expected = entry.value as Map<String, Object?>;
          final result = await engine.runGroup(entry.key, record.context);
          expect(result.results.length, expected['evaluated'],
              reason: '$studentId/${entry.key}');
          expect(result.passed, expected['passed'],
              reason: '$studentId/${entry.key}');
        }
      });
    }
  });

  group('vacuous-truth edge cases', () {
    // The degenerate curricula, from the shared fixture's edge_cases.json.
    // AndRule([]) passes while an at-least-N rule over an empty set fails
    // for N > 0. Nothing in the main student set exercises an empty rule
    // list.

    for (final caseName in _edgeCases.keys) {
      test('$caseName matches the fixture', () async {
        final testCase = _edgeCases[caseName] as Map<String, Object?>;
        final (policies, electiveMinimum) =
            curriculumFromJson(testCase['curriculum'] as Map<String, Object?>);
        final expected = testCase['expected'] as Map<String, Object?>;
        final student =
            contextFromJson(testCase['student'] as Map<String, Object?>);

        final (engine, graduates) =
            buildGraduationCheck(policies, electiveMinimum);
        final result = await graduates.evaluate(student);

        expect(result.passed, expected['passed'], reason: caseName);
        final data = result.data as List<RuleResult>;
        expect(data.length, expected['rules_evaluated'], reason: caseName);
        final chain = _failingChain(result);
        expect(chain, expected['failing_chain'], reason: caseName);

        final runAll = await engine.runAll(student);
        final expectedRunAll = expected['run_all'] as Map<String, Object?>;
        expect(runAll.results.length, expectedRunAll['evaluated'],
            reason: caseName);
        expect(runAll.passed, expectedRunAll['passed'], reason: caseName);

        // The strict form throws; the try-prefixed form returns null.
        final lookups = expected['lookups'] as Map<String, Object?>;

        final unknownGroup = lookups['unknown_group'] as Map<String, Object?>;
        final group = unknownGroup['name'] as String;
        if (unknownGroup['run_group_raises'] == true) {
          expect(() => engine.runGroup(group, student), throwsArgumentError);
        }
        if (unknownGroup['try_run_group_returns_null'] == true) {
          expect(await engine.tryRunGroup(group, student), isNull,
              reason: caseName);
        }

        final unknownRule = lookups['unknown_rule'] as Map<String, Object?>;
        final rule = unknownRule['name'] as String;
        if (unknownRule['run_named_raises'] == true) {
          expect(() => engine.runNamed(rule, student), throwsArgumentError);
        }
        if (unknownRule['try_run_named_returns_null'] == true) {
          expect(await engine.tryRunNamed(rule, student), isNull,
              reason: caseName);
        }
      });
    }
  });

  group('build once, apply many times', () {
    // The "scale" claim: one built engine/composite pair, reused across
    // every student, never rebuilt per lookup.

    test('the same built objects serve every student', () async {
      final (_, graduates) = buildGraduationCheck(_policies, _electiveMinimum);
      final results = <String, bool>{};
      for (final entry in _students.entries) {
        results[entry.key] =
            (await graduates.evaluate(entry.value.context)).passed;
      }
      final expected = {
        for (final entry in _students.entries)
          entry.key: entry.value.expected['passed'],
      };
      expect(results, expected);
    });
  });
}
