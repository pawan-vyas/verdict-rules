/**
 * Structural invariants on the full result tree, proved against the same
 * CHAOS_SEED-derived cases test/chaos.test.js already uses -- see
 * chaos.test.js's own header for why every case is deterministic and
 * individually reproducible from its index alone.
 *
 * Where test/chaos.test.js proves only the final boolean verdict agrees
 * with an independent oracle, this file proves the *shape* of the result
 * tree every case produces -- short-circuit behavior at every nesting
 * level, no rule silently skipped or silently run twice, and that
 * evaluating the same inputs twice is byte-for-byte reproducible.
 *
 * See ../docs/testing.md and the invariants named in
 * docs/samples/graduation-requirement-verdict/ for the tree shape this
 * file assumes.
 */
import assert from "node:assert/strict";
import { describe, it } from "node:test";

import { AndRule, FunctionRule, OrRule } from "verdict-rules";

import { generateCase } from "../chaos-data.js";
import {
  AtLeastNRule,
  attendanceMet,
  buildGraduationCheck,
  cgpaMet,
  languageChildRules,
  ruleForSubject,
  vocationalChildRules,
} from "../graduation-verdict.js";
import { Rng } from "../rng.js";

// Pinned, matching chaos.test.js's own CHAOS_SEED -- these invariants are
// checked against the exact same generated cases the oracle comparison
// already uses, not a separate sample.
const CHAOS_SEED = 20260907;
const NUM_CASES = 500;

/**
 * Rebuild the same Rule tree `buildGraduationCheck` builds, using only
 * exported, pure constructors -- never reaching into an AndRule/OrRule's own
 * private fields. Evaluating this shadow tree against the same context
 * `graduates` was evaluated with is guaranteed to reproduce the identical
 * result tree, because every Rule in this package is a pure function of its
 * context (proved separately below, under "purity").
 *
 * Alongside the tree, returns `childrenOf`: a `Rule -> Rule[]` map recording
 * each composite's own sub-rules, so the recursive checker below can pair a
 * composite's result.subResults entries against the Rule objects that produced
 * them -- including the nested AndRule/OrRule rule_for_subject builds for
 * vocational/language subjects, not just graduates' own immediate children.
 *
 * @param {object[]} policies
 * @param {number} electiveMinimum
 * @returns {{ graduatesShadow: AndRule, childrenOf: Map }}
 */
function buildShadowTree(policies, electiveMinimum) {
  const childrenOf = new Map();

  const subjectRules = policies.map((policy) => {
    const rule = ruleForSubject(policy);
    const sid = policy.subjectId;
    if (policy.subjectType === "vocational") {
      childrenOf.set(rule, vocationalChildRules(policy, sid));
    } else if (policy.subjectType === "language" && policy.exemptionAllowed) {
      childrenOf.set(rule, languageChildRules(policy, sid));
    }
    // Otherwise `rule` is a plain FunctionRule leaf -- nothing to record.
    return rule;
  });

  const coreRules = subjectRules.filter((r) => r.group === "core");
  const electiveRules = subjectRules.filter((r) => r.group === "elective");

  const coreRule = new AndRule("all_core_subjects_pass", coreRules);
  const electiveRule = new AtLeastNRule("elective_requirement", electiveRules, electiveMinimum);
  const cgpaRule = new FunctionRule("cgpa_met", cgpaMet);
  const attendanceRule = new FunctionRule("attendance_met", attendanceMet);
  childrenOf.set(coreRule, coreRules);
  childrenOf.set(electiveRule, electiveRules);

  const graduatesShadow = new AndRule("graduates", [coreRule, electiveRule, cgpaRule, attendanceRule]);
  childrenOf.set(graduatesShadow, [coreRule, electiveRule, cgpaRule, attendanceRule]);

  return { graduatesShadow, childrenOf };
}

/**
 * AndRule's own invariant: short-circuits on the first failure, never
 * stops early and never keeps going past it.
 */
function checkAndRuleInvariant(rule, result) {
  if (result.passed) {
    assert.ok(
      result.subResults.every((r) => r.passed),
      `AndRule "${rule.name}" passed but a sub-result reports failed`,
    );
    return;
  }
  assert.ok(result.subResults.length > 0, `AndRule "${rule.name}" failed with empty subResults`);
  assert.ok(
    result.subResults.slice(0, -1).every((r) => r.passed),
    `AndRule "${rule.name}": a failure before the last entry means it didn't stop at the first one`,
  );
  assert.equal(
    result.subResults.at(-1).passed,
    false,
    `AndRule "${rule.name}" failed overall but its last recorded entry passed`,
  );
}

/**
 * OrRule's own invariant, the mirror image of AndRule's: short-circuits on
 * the first pass; an all-fail result proves nothing stopped early.
 */
function checkOrRuleInvariant(rule, result) {
  if (!result.passed) {
    assert.ok(
      result.subResults.every((r) => !r.passed),
      `OrRule "${rule.name}" failed but a sub-result reports passed`,
    );
    return;
  }
  assert.ok(result.subResults.length > 0, `OrRule "${rule.name}" passed with empty subResults`);
  assert.ok(
    result.subResults.slice(0, -1).every((r) => !r.passed),
    `OrRule "${rule.name}": a pass before the last entry means it didn't stop at the first one`,
  );
  assert.equal(
    result.subResults.at(-1).passed,
    true,
    `OrRule "${rule.name}" passed overall but its last recorded entry failed`,
  );
}

/**
 * AtLeastNRule's own invariant: it stops exactly when its minimum is
 * mathematically decided -- as soon as enough sub-rules have passed to
 * guarantee it, or too many have failed for it to still be reachable --
 * never one sub-rule earlier (the call wouldn't have been decidable yet)
 * and never one later (an already-decided rule kept evaluating). A prior
 * version of this invariant pinned "never short-circuits, every sub-rule
 * always runs" -- a pre-`SequentialEvaluator` artifact, not the settled
 * design; see `AtLeastNRule`'s own class doc comment.
 */
function checkAtLeastNRuleInvariant(rule, result, children) {
  const total = children.length;
  const minimum = rule.minimum;
  const subResults = result.subResults;

  let passedSoFar = 0;
  for (let index = 0; index < subResults.length; index++) {
    passedSoFar += subResults[index].passed ? 1 : 0;
    const remainingAfter = total - (index + 1);
    const decidedTrue = passedSoFar >= minimum;
    const decidedFalse = passedSoFar + remainingAfter < minimum;
    const isLastEvaluated = index === subResults.length - 1;

    if (isLastEvaluated) {
      assert.ok(
        decidedTrue || decidedFalse,
        `AtLeastNRule "${rule.name}" stopped after ${subResults.length} of ${total} sub-rule(s) ` +
          `before its minimum (${minimum}) was mathematically decided either way`,
      );
    } else {
      assert.ok(
        !(decidedTrue || decidedFalse),
        `AtLeastNRule "${rule.name}" kept evaluating past sub-rule ${index} even though its ` +
          `minimum (${minimum}) was already decided there`,
      );
    }
  }

  assert.equal(
    result.passed,
    passedSoFar >= minimum,
    `AtLeastNRule "${rule.name}"'s own passed verdict disagrees with the pass count over the ` +
      `sub-rules it actually evaluated`,
  );
}

/** A FunctionRule leaf: nothing to recurse into. */
function checkFunctionRuleInvariant(rule, result) {
  assert.equal(
    result.subResults.length,
    0,
    `FunctionRule "${rule.name}" (a leaf) unexpectedly carries subResults`,
  );
}

// Keyed by concrete Rule class -- a lookup table, not an if/else-if ladder,
// so a fifth composite type the rest of this example ever grows is a new
// row here, never a new branch.
const INVARIANT_CHECKERS = new Map([
  [AndRule, checkAndRuleInvariant],
  [OrRule, checkOrRuleInvariant],
  [AtLeastNRule, checkAtLeastNRuleInvariant],
  [FunctionRule, checkFunctionRuleInvariant],
]);

/**
 * Walk a (rule, result) pair together, recursively, asserting each node's
 * own short-circuit/no-short-circuit invariant and recursing into every
 * composite's own children -- pairwise, by index -- using the real typed
 * Rule objects recorded in `childrenOf`.
 */
function checkInvariants(rule, result, childrenOf) {
  const checker = INVARIANT_CHECKERS.get(rule.constructor);
  assert.ok(checker, `no invariant checker registered for rule type ${rule.constructor.name} ("${rule.name}")`);

  const children = childrenOf.get(rule);
  checker(rule, result, children);

  if (children === undefined) return;
  // A short-circuited AndRule/OrRule may have fewer subResults entries
  // than the rule has children -- pair only what actually ran.
  for (let i = 0; i < result.subResults.length; i++) {
    checkInvariants(children[i], result.subResults[i], childrenOf);
  }
}

describe("structural invariants on the result tree", () => {
  for (let caseIndex = 0; caseIndex < NUM_CASES; caseIndex++) {
    it(`case ${caseIndex}: AndRule/OrRule/AtLeastNRule invariants hold throughout the tree`, async () => {
      const rng = new Rng(CHAOS_SEED + caseIndex);
      const { policies, context, electiveMinimum } = generateCase(rng);

      const { graduatesShadow, childrenOf } = buildShadowTree(policies, electiveMinimum);
      const result = await graduatesShadow.evaluate(context);

      checkInvariants(graduatesShadow, result, childrenOf);
    });

    it(`case ${caseIndex}: runAll reports exactly one result per registered rule`, async () => {
      const rng = new Rng(CHAOS_SEED + caseIndex);
      const { policies, context, electiveMinimum } = generateCase(rng);

      const { engine } = buildGraduationCheck(policies, electiveMinimum);
      const runAllResult = await engine.runAll(context);

      assert.equal(
        runAllResult.results.length,
        policies.length,
        `case ${caseIndex}: runAll must report exactly one result per registered subject rule`,
      );
    });
  }
});

/**
 * Deeply compare two RuleResult trees field by field -- rule_name/passed/
 * detail, and the full nested `data`, not just the top-level `passed`.
 * `assert.deepStrictEqual` already recurses through plain objects and
 * arrays, which is exactly this shape, so there's no need for a bespoke
 * walker here.
 */
function assertIdenticalResultTrees(a, b, message) {
  assert.deepStrictEqual(a, b, message);
}

describe("purity: evaluating the same inputs twice is byte-for-byte reproducible", () => {
  // A handful of the existing 500 generated cases, not a new sample --
  // purity either holds everywhere or it's a real bug, so a small, evenly
  // spread subset is enough to prove it without re-running all 500 twice.
  const SAMPLE_CASE_INDICES = [0, 1, 50, 123, 250, 499];

  for (const caseIndex of SAMPLE_CASE_INDICES) {
    it(`case ${caseIndex}: two independent evaluations agree in full`, async () => {
      const rng = new Rng(CHAOS_SEED + caseIndex);
      const { policies, context, electiveMinimum } = generateCase(rng);

      const first = await buildGraduationCheck(policies, electiveMinimum).graduates.evaluate(context);
      const second = await buildGraduationCheck(policies, electiveMinimum).graduates.evaluate(context);

      assertIdenticalResultTrees(first, second, `case ${caseIndex}: two builds/evaluations of the same inputs diverged`);
    });
  }
});
