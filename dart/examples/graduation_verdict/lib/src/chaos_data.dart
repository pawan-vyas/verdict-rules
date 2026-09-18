/// Deterministic, randomized (policy, student) case generation for the chaos suite.
///
/// "Deterministic" is the load-bearing word: every function here takes a
/// `Random` instance explicitly -- never a shared or ambient generator --
/// so a case built from a given seed is exactly reproducible. The chaos
/// suite seeds one `Random` per case from a pinned constant plus that
/// case's own index, so any single failing case can be regenerated on its
/// own without replaying every case before it. `dart:math`'s own `Random`
/// is already seedable, so no custom generator is needed here, unlike
/// JavaScript's `Math.random()`.
///
/// Every value generated stays within the schema's valid domain (a real
/// percentage, a real subjectType, etc.) -- this generates a wide space of
/// *valid* curricula and students, not malformed input. Garbage-input
/// handling is a different, narrower concern this suite doesn't cover.
library;

import 'dart:math';

import 'subject_policy.dart';

const _subjectTypes = ['academic', 'vocational', 'language'];

/// A double uniformly distributed in [min, max).
extension RandomUniform on Random {
  double uniform(double min, double max) => min + nextDouble() * (max - min);

  /// One element chosen uniformly from [items].
  T choice<T>(List<T> items) => items[nextInt(items.length)];
}

/// Build one randomized, but schema-valid, subject policy.
SubjectPolicy randomPolicy(Random rng, String subjectId) {
  final subjectType = rng.choice(_subjectTypes);
  return SubjectPolicy(
    subjectId: subjectId,
    subjectType: subjectType,
    writtenMinPct: rng.uniform(0, 100),
    practicalMinPct: subjectType == 'vocational' ? rng.uniform(0, 100) : null,
    exemptionAllowed: subjectType == 'language' && rng.choice([true, false]),
    isElective: rng.choice([true, false]),
  );
}

/// Build one randomized student context matching the given policies.
///
/// Returns a context in exactly the shape `graduation_check.dart`'s rules
/// and `oracle.dart`'s `expectedGraduates` both expect.
Map<String, Object?> randomContext(Random rng, List<SubjectPolicy> policies) {
  final scores = <String, Object?>{};
  for (final policy in policies) {
    final entry = <String, Object?>{'written_pct': rng.uniform(0, 100)};
    if (policy.subjectType == 'vocational') {
      entry['practical_pct'] = rng.uniform(0, 100);
    }
    if (policy.subjectType == 'language') {
      entry['has_exemption'] = rng.choice([true, false]);
    }
    scores[policy.subjectId] = entry;
  }

  return {
    'scores': scores,
    'cgpa': rng.uniform(0, 10),
    'cgpa_floor': rng.uniform(0, 10),
    'attendance_pct': rng.uniform(0, 100),
    'attendance_floor': rng.uniform(0, 100),
  };
}

/// Build one complete, self-consistent randomized case.
///
/// [rng] is the sole source of randomness, so the same `rng` state always
/// produces the same case. Returns a record ready to hand to both
/// `buildGraduationCheck` and `expectedGraduates`. The elective minimum is
/// always achievable (bounded by how many electives were actually
/// generated), so a mismatch between the two implementations is never
/// explained away as "an impossible curriculum."
(List<SubjectPolicy>, Map<String, Object?>, int) generateCase(Random rng,
    {int numSubjects = 7}) {
  final policies =
      List.generate(numSubjects, (i) => randomPolicy(rng, 'SUBJ$i'));
  final context = randomContext(rng, policies);
  final electiveCount = policies.where((p) => p.isElective).length;
  final electiveMinimum = rng.nextInt(electiveCount + 1);
  return (policies, context, electiveMinimum);
}
