/// Graduation requirement verdict, implemented with verdict_rules.
///
/// See docs/samples/graduation-requirement-verdict/README.md for the
/// design and fixtures/graduation_verdict/README.md for the fixture
/// contract.
library;

import 'dart:convert';
import 'dart:io';

import 'package:verdict_rules/verdict_rules.dart';

import 'at_least_n_rule.dart';
import 'subject_policy.dart';

Rule<Context> _vocationalSubjectRule(
        SubjectPolicy policy, String sid, String group) =>
    AndRule(
      sid,
      [
        FunctionRule('$sid:written', _writtenPredicate(policy, '$sid:written')),
        FunctionRule(
            '$sid:practical', _practicalPredicate(policy, '$sid:practical')),
      ],
      group: group,
    );

Rule<Context> _languageSubjectRule(
    SubjectPolicy policy, String sid, String group) {
  if (policy.exemptionAllowed) {
    return OrRule(
      sid,
      [
        FunctionRule('$sid:written', _writtenPredicate(policy, '$sid:written')),
        FunctionRule(
            '$sid:exemption', _exemptionPredicate(policy, '$sid:exemption')),
      ],
      group: group,
    );
  }
  return FunctionRule(sid, _writtenPredicate(policy, sid), group: group);
}

Rule<Context> _academicSubjectRule(
        SubjectPolicy policy, String sid, String group) =>
    FunctionRule(sid, _writtenPredicate(policy, sid), group: group);

/// One builder per subjectType, keyed by the value itself -- adding a
/// fifth subject type is a new function plus a new entry here, never a
/// new branch in [ruleForSubject].
final _subjectRuleBuilders =
    <String, Rule<Context> Function(SubjectPolicy, String, String)>{
  'vocational': _vocationalSubjectRule,
  'language': _languageSubjectRule,
  'academic': _academicSubjectRule,
};

/// Turn one subject's policy into a rule -- the shape depends on its type.
///
/// Returns an [AndRule] (written AND practical) for a vocational subject,
/// an [OrRule] (written OR exemption) for a language subject with an
/// exemption path, or a plain [FunctionRule] otherwise.
///
/// Throws [ArgumentError] if [policy.subjectType] isn't one of the known
/// types.
Rule<Context> ruleForSubject(SubjectPolicy policy) {
  final group = policy.isElective ? 'elective' : 'core';
  final sid = policy.subjectId;

  final builder = _subjectRuleBuilders[policy.subjectType];
  if (builder == null) {
    throw ArgumentError.value(
      policy.subjectType,
      'subjectType',
      "unknown subjectType for subject '$sid'",
    );
  }
  return builder(policy, sid, group);
}

RulePredicate _writtenPredicate(SubjectPolicy policy, String name) =>
    (context) async {
      final scores = context['scores'] as Map<String, Object?>;
      final subject = scores[policy.subjectId] as Map<String, Object?>;
      final pct = subject['written_pct'] as double;
      return RuleResult(
          ruleName: name,
          passed: pct >= policy.writtenMinPct,
          detail: '$pct vs ${policy.writtenMinPct}');
    };

RulePredicate _practicalPredicate(SubjectPolicy policy, String name) =>
    (context) async {
      final scores = context['scores'] as Map<String, Object?>;
      final subject = scores[policy.subjectId] as Map<String, Object?>;
      final pct = subject['practical_pct'] as double;
      return RuleResult(
        ruleName: name,
        passed: pct >= policy.practicalMinPct!,
        detail: '$pct vs ${policy.practicalMinPct}',
      );
    };

RulePredicate _exemptionPredicate(SubjectPolicy policy, String name) =>
    (context) async {
      final scores = context['scores'] as Map<String, Object?>;
      final subject = scores[policy.subjectId] as Map<String, Object?>;
      final exempt = subject['has_exemption'] as bool? ?? false;
      return RuleResult(ruleName: name, passed: exempt);
    };

Future<RuleResult> cgpaMet(Map<String, Object?> context) async => RuleResult(
      ruleName: 'cgpa_met',
      passed: (context['cgpa'] as double) >= (context['cgpa_floor'] as double),
    );

Future<RuleResult> attendanceMet(Map<String, Object?> context) async =>
    RuleResult(
      ruleName: 'attendance_met',
      passed: (context['attendance_pct'] as double) >=
          (context['attendance_floor'] as double),
    );

/// Read the curriculum -- subject policies and the elective-count threshold
/// -- from a JSON file.
///
/// Returns one [SubjectPolicy] per entry, with missing optional fields
/// filled with their defaults, and the minimum number of electives
/// required to graduate.
(List<SubjectPolicy>, int) loadCurriculum(String path) => curriculumFromJson(
    jsonDecode(File(path).readAsStringSync()) as Map<String, Object?>);

/// Convert an already-parsed curriculum map (the shared
/// `{ elective_minimum, subjects }` shape) into `(policies, electiveMinimum)`.
/// Exposed separately from [loadCurriculum] so edge_cases.json's own inline
/// curricula can be converted without re-parsing anything from disk.
(List<SubjectPolicy>, int) curriculumFromJson(Map<String, Object?> curriculum) {
  final subjects = curriculum['subjects'] as List<Object?>;
  final policies = subjects.map((row) {
    final r = row as Map<String, Object?>;
    return SubjectPolicy(
      subjectId: r['subject_id'] as String,
      subjectType: r['subject_type'] as String,
      writtenMinPct: (r['written_min_pct'] as num).toDouble(),
      practicalMinPct: (r['practical_min_pct'] as num?)?.toDouble(),
      exemptionAllowed: r['exemption_allowed'] as bool? ?? false,
      isElective: r['is_elective'] as bool? ?? false,
    );
  }).toList();
  return (policies, curriculum['elective_minimum'] as int);
}

/// One student's context plus the shared fixture's own `expected` block.
class StudentRecord {
  final Map<String, Object?> context;
  final Map<String, Object?> expected;

  const StudentRecord(this.context, this.expected);
}

/// Read the batch of student records from a JSON file.
///
/// Returns one context per student, plus the `note`/`expected` fields the
/// rules themselves never read.
Map<String, StudentRecord> loadStudents(String path) {
  final raw = jsonDecode(File(path).readAsStringSync()) as Map<String, Object?>;
  return raw.map((studentId, row) {
    final r = row as Map<String, Object?>;
    return MapEntry(
        studentId,
        StudentRecord(
            contextFromJson(r), r['expected'] as Map<String, Object?>));
  });
}

/// Convert a bare, shared-fixture-shaped context map (the `scores`/`cgpa`/
/// `attendance_*` fields the rules actually read, with no `note`/`expected`
/// wrapper) into this project's context shape. Exposed separately from
/// [loadStudents] so edge_cases.json's own inline `student` objects can be
/// converted directly.
Map<String, Object?> contextFromJson(Map<String, Object?> row) {
  final rawScores = row['scores'] as Map<String, Object?>;
  final scores = rawScores.map((subjectId, s) {
    final subject = s as Map<String, Object?>;
    final entry = <String, Object?>{
      'written_pct': (subject['written_pct'] as num).toDouble()
    };
    if (subject.containsKey('practical_pct')) {
      entry['practical_pct'] = (subject['practical_pct'] as num).toDouble();
    }
    if (subject.containsKey('has_exemption')) {
      entry['has_exemption'] = subject['has_exemption'] as bool;
    }
    return MapEntry(subjectId, entry);
  });

  return {
    'scores': scores,
    'cgpa': (row['cgpa'] as num).toDouble(),
    'cgpa_floor': (row['cgpa_floor'] as num).toDouble(),
    'attendance_pct': (row['attendance_pct'] as num).toDouble(),
    'attendance_floor': (row['attendance_floor'] as num).toDouble(),
  };
}

/// Build both structures from one policy list: a diagnostic engine and a
/// fast verdict.
///
/// Returns a `(engine, graduates)` pair built from the same underlying
/// rule objects -- `engine` serves runNamed/runGroup/runAll lookups,
/// `graduates` is the short-circuiting pass/fail composite.
(RulesEngine<Context>, AndRule<Context>) buildGraduationCheck(
    List<SubjectPolicy> policies, int electiveMinimum) {
  final subjectRules = policies.map(ruleForSubject).toList();
  final engine = RulesEngine(subjectRules);

  final coreRules = subjectRules.where((r) => r.group == 'core').toList();
  final electiveRules =
      subjectRules.where((r) => r.group == 'elective').toList();

  final graduates = AndRule('graduates', [
    AndRule('all_core_subjects_pass', coreRules),
    AtLeastNRule('elective_requirement', electiveRules, electiveMinimum),
    FunctionRule('cgpa_met', cgpaMet),
    FunctionRule('attendance_met', attendanceMet),
  ]);
  return (engine, graduates);
}
