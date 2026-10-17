/// Failing-run-to-fixture shrinking: given a (policies, context,
/// electiveMinimum) case that fails some check, progressively simplify it
/// down to a minimal case that still reproduces the exact same failure,
/// then serialize that minimal case out as a debugging fixture.
///
/// Generic over *what counts as a failure* -- callers supply [StillFails],
/// so the same shrinker serves a disagreement with the oracle, a broken
/// structural invariant, or any other reproducible failure shape.
library;

import 'dart:convert';
import 'dart:io';
import 'dart:math';

import 'subject_policy.dart';

/// One case under investigation.
typedef FailingCase = (List<SubjectPolicy>, Map<String, Object?>, int);

/// Whether [policies]/[context]/[electiveMinimum] still reproduces the
/// failure under investigation. `true` means "still fails the same way,
/// keep shrinking"; `false` means this simplification already made the
/// failure go away, so [shrinkFailingCase] reverts it.
typedef StillFails = Future<bool> Function(
  List<SubjectPolicy> policies,
  Map<String, Object?> context,
  int electiveMinimum,
);

/// One simplification strategy: returns a smaller case, or `null` once it
/// has nothing left to simplify for the given case.
typedef _ShrinkStep = FailingCase? Function(FailingCase);

FailingCase? _dropLastSubject(FailingCase current) {
  final (policies, context, electiveMinimum) = current;
  if (policies.length <= 1) return null;

  final dropped = policies.last;
  final newPolicies = policies.sublist(0, policies.length - 1);
  final scores =
      Map<String, Object?>.from(context['scores'] as Map<String, Object?>)
        ..remove(dropped.subjectId);
  final newContext = {...context, 'scores': scores};
  final remainingElectives = newPolicies.where((p) => p.isElective).length;
  return (newPolicies, newContext, min(electiveMinimum, remainingElectives));
}

FailingCase? _reduceElectiveMinimum(FailingCase current) {
  final (policies, context, electiveMinimum) = current;
  if (electiveMinimum <= 0) return null;
  return (policies, context, electiveMinimum - 1);
}

const _simplifiableScoreFields = ['written_pct', 'practical_pct'];
const _simplifiableContextFields = [
  'cgpa',
  'cgpa_floor',
  'attendance_pct',
  'attendance_floor',
];

FailingCase? _zeroOneValue(FailingCase current) {
  final (policies, context, electiveMinimum) = current;
  final scores = context['scores'] as Map<String, Object?>;
  for (final subjectId in scores.keys) {
    final subject = scores[subjectId] as Map<String, Object?>;
    for (final field in _simplifiableScoreFields) {
      final value = subject[field];
      if (value is double && value != 0.0) {
        final newSubject = {...subject, field: 0.0};
        final newScores = {...scores, subjectId: newSubject};
        return (policies, {...context, 'scores': newScores}, electiveMinimum);
      }
    }
  }
  for (final field in _simplifiableContextFields) {
    final value = context[field];
    if (value is double && value != 0.0) {
      return (policies, {...context, field: 0.0}, electiveMinimum);
    }
  }
  return null;
}

/// Ordered list of simplification strategies -- adding a new kind of
/// simplification is a new entry here, never a new branch inside
/// [shrinkFailingCase] itself.
final List<_ShrinkStep> _shrinkSteps = [
  _dropLastSubject,
  _reduceElectiveMinimum,
  _zeroOneValue,
];

/// Greedily simplify [policies]/[context]/[electiveMinimum] while
/// [stillFails] keeps reporting the same failure.
///
/// Runs repeated passes over [_shrinkSteps], applying each step
/// repeatedly for as long as the simplified case keeps failing and
/// reverting the moment it stops. Stops at a fixed point: a full pass
/// where no step produced a smaller case that still fails.
Future<FailingCase> shrinkFailingCase(
  List<SubjectPolicy> policies,
  Map<String, Object?> context,
  int electiveMinimum,
  StillFails stillFails,
) async {
  var current = (policies, context, electiveMinimum);
  var madeProgress = true;
  while (madeProgress) {
    madeProgress = false;
    for (final step in _shrinkSteps) {
      var candidate = step(current);
      while (candidate != null) {
        final (candidatePolicies, candidateContext, candidateMinimum) =
            candidate;
        final candidateStillFails = await stillFails(
            candidatePolicies, candidateContext, candidateMinimum);
        if (!candidateStillFails) break;
        current = candidate;
        madeProgress = true;
        candidate = step(current);
      }
    }
  }
  return current;
}

Map<String, Object?> _policyToJson(SubjectPolicy policy) => {
      'subject_id': policy.subjectId,
      'subject_type': policy.subjectType,
      'written_min_pct': policy.writtenMinPct,
      if (policy.practicalMinPct != null)
        'practical_min_pct': policy.practicalMinPct,
      if (policy.exemptionAllowed) 'exemption_allowed': policy.exemptionAllowed,
      if (policy.isElective) 'is_elective': policy.isElective,
    };

/// Inverse of `curriculumFromJson` -- turns a policy list back into the
/// shared `{ elective_minimum, subjects }` JSON shape.
Map<String, Object?> curriculumToJson(
        List<SubjectPolicy> policies, int electiveMinimum) =>
    {
      'elective_minimum': electiveMinimum,
      'subjects': policies.map(_policyToJson).toList(),
    };

/// Inverse of `contextFromJson` -- turns a rule-ready context back into
/// the shared bare `scores`/`cgpa`/`attendance_*` JSON shape.
Map<String, Object?> contextToJson(Map<String, Object?> context) {
  final scores = context['scores'] as Map<String, Object?>;
  final jsonScores = scores.map((subjectId, raw) {
    final subject = raw as Map<String, Object?>;
    final entry = <String, Object?>{'written_pct': subject['written_pct']};
    if (subject.containsKey('practical_pct')) {
      entry['practical_pct'] = subject['practical_pct'];
    }
    if (subject.containsKey('has_exemption')) {
      entry['has_exemption'] = subject['has_exemption'];
    }
    return MapEntry(subjectId, entry);
  });

  return {
    'scores': jsonScores,
    'cgpa': context['cgpa'],
    'cgpa_floor': context['cgpa_floor'],
    'attendance_pct': context['attendance_pct'],
    'attendance_floor': context['attendance_floor'],
  };
}

/// Write a shrunk, minimal failing case out to [path] as a standalone
/// debugging fixture, in the same `{curriculum, student}` shape
/// fixtures/graduation_verdict/edge_cases.json uses. [note] records what
/// failure this case reproduces.
void writeShrunkFixture(
  String path,
  List<SubjectPolicy> policies,
  Map<String, Object?> context,
  int electiveMinimum,
  String note,
) {
  final json = {
    'note': note,
    'curriculum': curriculumToJson(policies, electiveMinimum),
    'student': contextToJson(context),
  };
  File(path)
      .writeAsStringSync(const JsonEncoder.withIndent('  ').convert(json));
}
