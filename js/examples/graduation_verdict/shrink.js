/**
 * Failing-run-to-fixture shrinking.
 *
 * Given a `{ policies, context, electiveMinimum }` case that fails one of
 * test/invariants.test.js's invariants, or disagrees with oracle.js, this
 * progressively simplifies it -- fewer subjects, a lower elective
 * threshold, rounder field values -- while re-checking the simplified case
 * still reproduces the *same* failure, and writes the smallest reproducing
 * case it finds out as a JSON fixture for a human to read.
 *
 * This module ships as a debugging tool; it is not exercised by the chaos
 * suite itself, since every one of the 500 generated cases already passes.
 * See ../docs/testing.md for how to use it against a genuine failure, and
 * for how its own mechanism was verified (a temporary, deliberately
 * injected bug -- never committed).
 */
import { writeFileSync } from "node:fs";

/**
 * @typedef {{ policies: object[], context: object, electiveMinimum: number }} FailingCase
 */

/**
 * Shrink a failing case to a local minimum: try every simplification one
 * step produces, keep the first one that still reproduces the failure, and
 * restart from there. Stops once no single-step simplification reproduces
 * it anymore.
 *
 * @param {FailingCase} failingCase - A case already confirmed to fail.
 * @param {(candidate: FailingCase) => boolean | Promise<boolean>} stillFails
 *   - Predicate that re-runs whatever check `failingCase` originally failed
 *   and reports whether this simplified candidate still fails it the same
 *   way. The caller owns what "the same way" means (a specific invariant,
 *   a specific disagreement with the oracle, ...).
 * @returns {Promise<FailingCase>} The smallest case found that still fails.
 */
export async function shrinkFailingCase(failingCase, stillFails) {
  let current = failingCase;
  let simplifiedThisPass = true;
  while (simplifiedThisPass) {
    simplifiedThisPass = false;
    // Sequential, not Promise.all -- shrinking is inherently iterative:
    // each step's outcome decides what the next step even tries.
    for (const candidate of candidateSimplifications(current)) {
      if (await stillFails(candidate)) {
        current = candidate;
        simplifiedThisPass = true;
        break;
      }
    }
  }
  return current;
}

/**
 * Every single-step simplification of `failingCase`, from most to least
 * structural: drop a whole subject first, then lower the elective
 * threshold, then round individual field values -- so a shrink pass tries
 * the simplifications most likely to matter before the smallest ones.
 *
 * @param {FailingCase} failingCase
 * @returns {Generator<FailingCase>}
 */
function* candidateSimplifications(failingCase) {
  const { policies, electiveMinimum } = failingCase;

  for (let i = 0; i < policies.length; i++) {
    yield withSubjectRemoved(failingCase, i);
  }

  if (electiveMinimum > 0) {
    yield { ...failingCase, electiveMinimum: electiveMinimum - 1 };
  }

  yield* withSimplerContextField(failingCase);
  yield* withSimplerPolicyField(failingCase);
}

/** Drop one subject, and its score entry, and clamp electiveMinimum to what's left. */
function withSubjectRemoved(failingCase, index) {
  const { policies, context, electiveMinimum } = failingCase;
  const removed = policies[index];
  const newPolicies = policies.filter((_, i) => i !== index);
  const newScores = { ...context.scores };
  delete newScores[removed.subjectId];
  const remainingElectives = newPolicies.filter((p) => p.isElective).length;
  return {
    policies: newPolicies,
    context: { ...context, scores: newScores },
    electiveMinimum: Math.min(electiveMinimum, remainingElectives),
  };
}

/** Numbers simpler than `value`, closest-first: 0, truncated, halved -- whichever differ from `value`. */
function simplerNumbers(value) {
  if (typeof value !== "number" || !Number.isFinite(value)) return [];
  const candidates = new Set([0, Math.trunc(value), Math.trunc(value / 2)]);
  candidates.delete(value);
  return [...candidates];
}

function replaceAt(array, index, value) {
  const copy = [...array];
  copy[index] = value;
  return copy;
}

/** Round cgpa/cgpaFloor/attendancePct/attendanceFloor and each subject's own score fields toward 0. */
function* withSimplerContextField(failingCase) {
  const { context } = failingCase;
  for (const field of ["cgpa", "cgpaFloor", "attendancePct", "attendanceFloor"]) {
    for (const value of simplerNumbers(context[field])) {
      yield { ...failingCase, context: { ...context, [field]: value } };
    }
  }
  for (const [subjectId, score] of Object.entries(context.scores)) {
    for (const field of ["writtenPct", "practicalPct"]) {
      if (score[field] === undefined) continue;
      for (const value of simplerNumbers(score[field])) {
        const newScore = { ...score, [field]: value };
        yield { ...failingCase, context: { ...context, scores: { ...context.scores, [subjectId]: newScore } } };
      }
    }
    if (score.hasExemption === true) {
      const newScore = { ...score, hasExemption: false };
      yield { ...failingCase, context: { ...context, scores: { ...context.scores, [subjectId]: newScore } } };
    }
  }
}

/** Round writtenMinPct/practicalMinPct and flip exemptionAllowed toward false, one policy/field at a time. */
function* withSimplerPolicyField(failingCase) {
  const { policies } = failingCase;
  for (let i = 0; i < policies.length; i++) {
    const policy = policies[i];
    for (const field of ["writtenMinPct", "practicalMinPct"]) {
      if (policy[field] === undefined) continue;
      for (const value of simplerNumbers(policy[field])) {
        yield { ...failingCase, policies: replaceAt(policies, i, { ...policy, [field]: value }) };
      }
    }
    if (policy.exemptionAllowed === true) {
      yield { ...failingCase, policies: replaceAt(policies, i, { ...policy, exemptionAllowed: false }) };
    }
  }
}

/**
 * Write a shrunk case out as a standalone, human-readable JSON fixture.
 *
 * This is a per-language debugging aid, not a shared cross-language
 * fixture -- it never belongs under fixtures/graduation_verdict/.
 *
 * @param {string} path - Where to write the fixture.
 * @param {FailingCase} shrunkCase - The case `shrinkFailingCase` returned.
 * @param {object} [meta] - Free-form context worth keeping alongside the
 *   case -- which invariant failed, the original case's seed, and so on.
 * @returns {string} `path`, for convenient chaining.
 */
export function writeShrunkFixture(path, shrunkCase, meta = {}) {
  const payload = {
    ...meta,
    policies: shrunkCase.policies,
    context: shrunkCase.context,
    electiveMinimum: shrunkCase.electiveMinimum,
  };
  writeFileSync(path, `${JSON.stringify(payload, null, 2)}\n`, "utf8");
  return path;
}
