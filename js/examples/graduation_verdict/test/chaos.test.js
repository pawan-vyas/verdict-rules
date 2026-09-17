/**
 * Chaos/differential testing: does the engine ever disagree with an independent oracle?
 *
 * graduation-verdict.test.js proves 8 hand-picked, hand-verified scenarios
 * come out right. That doesn't say anything about the enormous space of
 * curricula and students nobody hand-picked. This file checks a much wider
 * space by comparing two independent implementations against each other
 * instead of against a fixed expected value:
 *
 * - `buildGraduationCheck` -- the real, verdict-rules-based implementation
 *   this whole project exists to demonstrate.
 * - `expectedGraduates` -- a plain, verdict-rules-free re-implementation,
 *   deliberately dumb so it's trustworthy by inspection.
 *
 * If they ever disagree, one of them is wrong -- and because every case is
 * generated from a *seeded* Rng (never any ambient randomness), that
 * disagreement is exactly reproducible: the same seed always regenerates
 * the exact same case. "The chaos suite didn't cause any breakdown" is
 * therefore a real, re-checkable claim across runs, not a one-off
 * observation about whatever numbers came up this time.
 *
 * See docs/testing.md for the full design reasoning.
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
      // Each case is seeded from CHAOS_SEED + caseIndex -- not from a single
      // shared generator advanced across all NUM_CASES calls -- specifically
      // so any one failing case reproduces on its own: re-run
      // `new Rng(CHAOS_SEED + caseIndex)` through `generateCase` and you get
      // the exact same policies/context/electiveMinimum that failed, with no
      // need to replay every earlier case first.
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
