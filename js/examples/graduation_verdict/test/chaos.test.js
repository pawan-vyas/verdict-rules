/**
 * Chaos/differential testing: compares `buildGraduationCheck` against
 * `expectedGraduates` across a wide, randomly-generated space of curricula
 * and students.
 *
 * Every case is generated from a seeded `Rng`, never any ambient
 * randomness, so a disagreement is exactly reproducible from its seed.
 *
 * See docs/testing.md for the full design.
 */
import assert from "node:assert/strict";
import { describe, it } from "node:test";

import { generateCase } from "../chaos-data.js";
import { buildGraduationCheck } from "../graduation-verdict.js";
import { expectedGraduates } from "../oracle.js";
import { Rng } from "../rng.js";

// Pinned. Do not change without a deliberate reason -- bumping this reshuffles
// every case's data, silently trading which edge cases get covered for which.
// If you need *more* coverage, raise NUM_CASES instead; that's strictly
// additive; changing CHAOS_SEED is not.
const CHAOS_SEED = 20260907;
const NUM_CASES = 500;

describe("engine agrees with an independent oracle", () => {
  for (let caseIndex = 0; caseIndex < NUM_CASES; caseIndex++) {
    it(`case ${caseIndex}`, async () => {
      // Each case is seeded from CHAOS_SEED + caseIndex, not a single shared
      // generator advanced across all NUM_CASES calls, so a failing case
      // reproduces on its own via `new Rng(CHAOS_SEED + caseIndex)` through
      // `generateCase`.
      const rng = new Rng(CHAOS_SEED + caseIndex);
      const { policies, context, electiveMinimum } = generateCase(rng);

      const { graduates } = buildGraduationCheck(policies, electiveMinimum);
      const actual = (await graduates.evaluate(context)).passed;
      const expected = expectedGraduates(policies, context, electiveMinimum);

      assert.equal(
        actual,
        expected,
        `caseIndex=${caseIndex} (seed=${CHAOS_SEED + caseIndex}): engine said ${actual}, oracle said ${expected}\n` +
          `electiveMinimum=${electiveMinimum}\npolicies=${JSON.stringify(policies)}\ncontext=${JSON.stringify(context)}`,
      );
    });
  }
});
