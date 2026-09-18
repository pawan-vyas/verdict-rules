/// A second, independent, verdict_rules-free implementation of the graduation decision.
///
/// This is deliberately the "naive way" from
/// docs/samples/graduation-requirement-verdict/README.md, generalized to
/// score *any* policy list rather than the fixed 7-subject curriculum --
/// its entire job is to be obviously correct by inspection, so it can
/// serve as ground truth for the chaos suite's differential testing. See
/// that suite's own doc comment and docs/testing/README.md for the full
/// reasoning: two independently written implementations (this plain loop,
/// and the verdict_rules-based engine in graduation_check.dart) must agree
/// on every input, or one of them is wrong.
///
/// Never import package:verdict_rules here -- an oracle that shares a bug
/// with the system it's checking proves nothing.
library;

import 'subject_policy.dart';

/// Whether one subject's own scores clear its policy's bar -- plain
/// if/else, no rule involved.
bool _subjectPasses(SubjectPolicy policy, Map<String, Object?> context) {
  final scores = context['scores'] as Map<String, Object?>;
  final subject = scores[policy.subjectId] as Map<String, Object?>;
  final writtenPct = subject['written_pct'] as double;

  if (policy.subjectType == 'vocational') {
    final practicalPct = subject['practical_pct'] as double;
    return writtenPct >= policy.writtenMinPct &&
        practicalPct >= policy.practicalMinPct!;
  }

  if (policy.subjectType == 'language' && policy.exemptionAllowed) {
    final hasExemption = subject['has_exemption'] as bool? ?? false;
    return writtenPct >= policy.writtenMinPct || hasExemption;
  }

  // academic, or a language subject with no exemption path
  return writtenPct >= policy.writtenMinPct;
}

/// Compute the graduation decision directly, with no verdict_rules involved at all.
///
/// Returns whether this student graduates, computed independently of
/// `ruleForSubject`/`buildGraduationCheck` -- the two must always agree.
bool expectedGraduates(List<SubjectPolicy> policies,
    Map<String, Object?> context, int electiveMinimum) {
  final corePolicies = policies.where((p) => !p.isElective).toList();
  final electivePolicies = policies.where((p) => p.isElective).toList();

  if (!corePolicies.every((p) => _subjectPasses(p, context))) {
    return false;
  }

  final passedElectives =
      electivePolicies.where((p) => _subjectPasses(p, context)).length;
  if (passedElectives < electiveMinimum) {
    return false;
  }

  if ((context['cgpa'] as double) < (context['cgpa_floor'] as double))
    return false;
  if ((context['attendance_pct'] as double) <
      (context['attendance_floor'] as double)) return false;

  return true;
}
