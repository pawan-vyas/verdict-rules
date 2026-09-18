/**
 * Deterministic, randomized (policy, student) case generation for test/chaos.test.js.
 *
 * Every function takes an `Rng` instance explicitly. Every value
 * generated stays within the schema's valid domain.
 */
import { subjectPolicy } from "./graduation-verdict.js";

const SUBJECT_TYPES = ["academic", "vocational", "language"];

// (practicalMinPct, exemptionAllowed) per subjectType -- the two policy
// fields whose valid range depends on which type generated them. A new
// subject type is a new entry here.
const POLICY_EXTRAS_BY_SUBJECT_TYPE = {
  vocational: (rng) => ({ practicalMinPct: rng.uniform(0, 100), exemptionAllowed: false }),
  language: (rng) => ({ practicalMinPct: undefined, exemptionAllowed: rng.choice([true, false]) }),
  academic: () => ({ practicalMinPct: undefined, exemptionAllowed: false }),
};

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
  const { practicalMinPct, exemptionAllowed } = POLICY_EXTRAS_BY_SUBJECT_TYPE[subjectType](rng);
  return subjectPolicy({
    subjectId,
    subjectType,
    writtenMinPct: rng.uniform(0, 100),
    practicalMinPct,
    exemptionAllowed,
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
 * @param {import("./rng.js").Rng} rng - The seeded generator to draw from.
 * @param {number} [numSubjects] - How many subjects the generated
 *   curriculum has.
 * @returns {{ policies: object[], context: object, electiveMinimum: number }}
 *   A triple ready to hand to both `buildGraduationCheck` and
 *   `expectedGraduates`. `electiveMinimum` is bounded by how many
 *   electives were actually generated.
 */
export function generateCase(rng, numSubjects = 7) {
  const policies = Array.from({ length: numSubjects }, (_, i) => randomPolicy(rng, `SUBJ${i}`));
  const context = randomContext(rng, policies);
  const electiveCount = policies.filter((p) => p.isElective).length;
  const electiveMinimum = rng.randint(0, electiveCount);
  return { policies, context, electiveMinimum };
}
