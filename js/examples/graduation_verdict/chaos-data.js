/**
 * Deterministic, randomized (policy, student) case generation for test/chaos.test.js.
 *
 * "Deterministic" is the load-bearing word: every function here takes an
 * `Rng` instance explicitly -- never a shared or ambient generator -- so a
 * case built from a given seed is exactly reproducible. test/chaos.test.js
 * seeds one `Rng` per case from a pinned constant plus that case's own
 * index, so any single failing case can be regenerated on its own without
 * replaying every case before it.
 *
 * Every value generated stays within the schema's valid domain (a real
 * percentage, a real subjectType, etc.) -- this generates a wide space of
 * *valid* curricula and students, not malformed input. Garbage-input
 * handling is a different, narrower concern this suite doesn't cover.
 */
import { subjectPolicy } from "./graduation-verdict.js";

const SUBJECT_TYPES = ["academic", "vocational", "language"];

/**
 * Build one randomized, but schema-valid, subject policy.
 *
 * @param {import("./rng.js").Rng} rng - The seeded generator to draw from.
 * @param {string} subjectId - Identifier for the generated subject.
 * @returns {object} A subjectPolicy whose type, thresholds, and flags are
 *   all randomized independently.
 */
export function randomPolicy(rng, subjectId) {
  const subjectType = rng.choice(SUBJECT_TYPES);
  return subjectPolicy({
    subjectId,
    subjectType,
    writtenMinPct: rng.uniform(0, 100),
    practicalMinPct: subjectType === "vocational" ? rng.uniform(0, 100) : undefined,
    exemptionAllowed: subjectType === "language" ? rng.choice([true, false]) : false,
    isElective: rng.choice([true, false]),
  });
}

/**
 * Build one randomized student context matching the given policies.
 *
 * @param {import("./rng.js").Rng} rng - The seeded generator to draw from.
 * @param {object[]} policies - The curriculum this context's scores must
 *   cover -- one entry generated per policy, shaped to that policy's own
 *   type.
 * @returns {object} A context in exactly the shape graduation-verdict.js's
 *   rules and oracle.js's `expectedGraduates` both expect.
 */
export function randomContext(rng, policies) {
  const scores = {};
  for (const policy of policies) {
    const entry = { writtenPct: rng.uniform(0, 100) };
    if (policy.subjectType === "vocational") {
      entry.practicalPct = rng.uniform(0, 100);
    }
    if (policy.subjectType === "language") {
      entry.hasExemption = rng.choice([true, false]);
    }
    scores[policy.subjectId] = entry;
  }

  return {
    scores,
    cgpa: rng.uniform(0, 10),
    cgpaFloor: rng.uniform(0, 10),
    attendancePct: rng.uniform(0, 100),
    attendanceFloor: rng.uniform(0, 100),
  };
}

/**
 * Build one complete, self-consistent randomized case.
 *
 * @param {import("./rng.js").Rng} rng - The seeded generator to draw from --
 *   the sole source of randomness, so the same `rng` state always produces
 *   the same case.
 * @param {number} [numSubjects] - How many subjects the generated
 *   curriculum has.
 * @returns {{ policies: object[], context: object, electiveMinimum: number }}
 *   A triple ready to hand to both `buildGraduationCheck` and
 *   `expectedGraduates`. `electiveMinimum` is always achievable (bounded by
 *   how many electives were actually generated), so a mismatch between the
 *   two implementations is never explained away as "an impossible
 *   curriculum."
 */
export function generateCase(rng, numSubjects = 7) {
  const policies = Array.from({ length: numSubjects }, (_, i) => randomPolicy(rng, `SUBJ${i}`));
  const context = randomContext(rng, policies);
  const electiveCount = policies.filter((p) => p.isElective).length;
  const electiveMinimum = rng.randint(0, electiveCount);
  return { policies, context, electiveMinimum };
}
