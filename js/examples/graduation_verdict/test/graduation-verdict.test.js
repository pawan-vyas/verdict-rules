/**
 * Tests for the graduation-verdict example.
 *
 * Also functions as an integration/e2e regression net for verdict-rules
 * itself, exercising FunctionRule, AndRule, OrRule, a custom Rule shape,
 * and all three RulesEngine run modes together. See docs/testing/README.md.
 */
import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { describe, it } from "node:test";
import { fileURLToPath } from "node:url";

import { AndRule, FunctionRule, OrRule } from "verdict-rules";

import {
  AtLeastNRule,
  buildGraduationCheck,
  contextFromObject,
  curriculumFromObject,
  loadCurriculum,
  loadStudents,
  ruleForSubject,
  subjectPolicy,
} from "../graduation-verdict.js";

const __dirname = dirname(fileURLToPath(import.meta.url));
// Fixture data lives at the repo root; see
// fixtures/graduation_verdict/README.md for the contract.
const FIXTURES = join(__dirname, "..", "..", "..", "..", "fixtures", "graduation_verdict");
const { policies: POLICIES, electiveMinimum: ELECTIVE_MINIMUM } = loadCurriculum(join(FIXTURES, "policies.json"));
const STUDENTS = loadStudents(join(FIXTURES, "students.json"));
const EDGE_CASES = JSON.parse(readFileSync(join(FIXTURES, "edge_cases.json"), "utf8"));

function policy(subjectId) {
  return POLICIES.find((p) => p.subjectId === subjectId);
}

describe("rule shape dispatch", () => {
  // See docs/samples/graduation-requirement-verdict/README.md.

  it("an academic subject is a plain FunctionRule", () => {
    const rule = ruleForSubject(policy("MATH101"));
    assert.ok(rule instanceof FunctionRule);
  });

  it("a vocational subject is an AndRule of two", () => {
    const rule = ruleForSubject(policy("WORKSHOP201"));
    assert.ok(rule instanceof AndRule);
  });

  it("a language subject with exemption is an OrRule", () => {
    const rule = ruleForSubject(policy("FRENCH101"));
    assert.ok(rule instanceof OrRule);
  });

  it("tags core vs. elective groups correctly", () => {
    assert.equal(ruleForSubject(policy("MATH101")).group, "core");
    assert.equal(ruleForSubject(policy("ELECTIVE_ART")).group, "elective");
  });

  it("throws for an unknown subject type", () => {
    const badPolicy = subjectPolicy({ ...policy("MATH101"), subjectType: "portfolio" });
    assert.throws(() => ruleForSubject(badPolicy));
  });
});

describe("vocational AndRule", () => {
  // The AndRule built for a vocational subject needs both scores to pass.

  it("fails if only the written score passes", async () => {
    const rule = ruleForSubject(policy("WORKSHOP201"));
    const context = contextFromObject({
      scores: { WORKSHOP201: { written_pct: 50, practical_pct: 45 } },
      cgpa: 0,
      cgpa_floor: 0,
      attendance_pct: 0,
      attendance_floor: 0,
    });
    const result = await rule.evaluate(context);
    assert.equal(result.passed, false);
  });

  it("passes if both scores clear their own bar", async () => {
    const rule = ruleForSubject(policy("WORKSHOP201"));
    const context = contextFromObject({
      scores: { WORKSHOP201: { written_pct: 50, practical_pct: 70 } },
      cgpa: 0,
      cgpa_floor: 0,
      attendance_pct: 0,
      attendance_floor: 0,
    });
    const result = await rule.evaluate(context);
    assert.equal(result.passed, true);
  });
});

describe("language OrRule", () => {
  // The OrRule built for a language subject accepts either the paper or an exemption.

  it("passes via exemption when the paper fails", async () => {
    const rule = ruleForSubject(policy("FRENCH101"));
    const context = contextFromObject({
      scores: { FRENCH101: { written_pct: 20, has_exemption: true } },
      cgpa: 0,
      cgpa_floor: 0,
      attendance_pct: 0,
      attendance_floor: 0,
    });
    const result = await rule.evaluate(context);
    assert.equal(result.passed, true);
  });

  it("fails when neither the paper nor an exemption applies", async () => {
    const rule = ruleForSubject(policy("FRENCH101"));
    const context = contextFromObject({
      scores: { FRENCH101: { written_pct: 20, has_exemption: false } },
      cgpa: 0,
      cgpa_floor: 0,
      attendance_pct: 0,
      attendance_floor: 0,
    });
    const result = await rule.evaluate(context);
    assert.equal(result.passed, false);
  });
});

/** A FunctionRule that records its own name to `calls` when run. */
function tracked(name, passed, calls) {
  return new FunctionRule(name, async () => {
    calls.push(name);
    return { passed };
  });
}

describe("AtLeastNRule short-circuits", () => {
  // AtLeastNRule stops calling sub-rules as soon as `minimum` is
  // mathematically decided either way -- it does not wait for every
  // sub-rule to run once the answer can no longer change.

  it("stops once the minimum is already met", async () => {
    const calls = [];
    const rule = new AtLeastNRule(
      "elective_requirement",
      [tracked("a", true, calls), tracked("b", true, calls), tracked("c", true, calls)],
      2,
    );
    const result = await rule.evaluate({});
    assert.equal(result.passed, true);
    assert.deepEqual(calls, ["a", "b"]); // 'c' must never run -- 2 passes already met minimum=2
  });

  it("stops once the minimum can no longer be reached", async () => {
    const calls = [];
    const rule = new AtLeastNRule(
      "elective_requirement",
      [tracked("a", false, calls), tracked("b", false, calls), tracked("c", true, calls)],
      2,
    );
    const result = await rule.evaluate({});
    assert.equal(result.passed, false);
    // 'c' must never run -- two failures already make minimum=2 unreachable
    // with only one sub-rule left.
    assert.deepEqual(calls, ["a", "b"]);
  });

  it("runs every sub-rule when the minimum is decided only at the end", async () => {
    const calls = [];
    const rule = new AtLeastNRule(
      "elective_requirement",
      [tracked("a", true, calls), tracked("b", false, calls), tracked("c", true, calls)],
      2,
    );
    const result = await rule.evaluate({});
    assert.equal(result.passed, true);
    // exactly 2 of 3 passing is only decidable at the last sub-rule.
    assert.deepEqual(calls, ["a", "b", "c"]);
  });
});

describe("engine run modes", () => {
  // runNamed/runGroup/runAll each serve the specific job the sample spec
  // claims. See docs/samples/graduation-requirement-verdict/README.md.

  it("runNamed looks up one subject", async () => {
    const { engine } = buildGraduationCheck(POLICIES, ELECTIVE_MINIMUM);
    const result = await engine.runNamed("WORKSHOP201", STUDENTS.alice);
    assert.equal(result.ruleName, "WORKSHOP201");
    assert.equal(result.passed, true);
  });

  it("runGroup('core') reports every core subject", async () => {
    const { engine } = buildGraduationCheck(POLICIES, ELECTIVE_MINIMUM);
    const result = await engine.runGroup("core", STUDENTS.alice);
    assert.deepEqual(
      new Set(result.results.map((r) => r.ruleName)),
      new Set(["MATH101", "ENG101", "WORKSHOP201", "FRENCH101"]),
    );
  });

  it("runGroup('elective') reports every elective subject", async () => {
    const { engine } = buildGraduationCheck(POLICIES, ELECTIVE_MINIMUM);
    const result = await engine.runGroup("elective", STUDENTS.alice);
    assert.deepEqual(
      new Set(result.results.map((r) => r.ruleName)),
      new Set(["ELECTIVE_ART", "ELECTIVE_MUSIC", "ELECTIVE_CS"]),
    );
  });

  it("runAll reports every subject", async () => {
    const { engine } = buildGraduationCheck(POLICIES, ELECTIVE_MINIMUM);
    const result = await engine.runAll(STUDENTS.alice);
    assert.equal(result.results.length, POLICIES.length);
  });

  it("runGroup never short-circuits, unlike the graduates composite", async () => {
    // bob fails ENG101 (core) -- runGroup must still report every other core subject.
    const { engine } = buildGraduationCheck(POLICIES, ELECTIVE_MINIMUM);
    const result = await engine.runGroup("core", STUDENTS.bob);
    assert.equal(result.results.length, 4);
    assert.equal(result.passed, false);
  });
});

/**
 * Walk the first failing branch down, collecting rule names.
 *
 * A nested failure is reachable by following `subResults` downward; a
 * result's `data` plays no role here -- composites never write to it.
 */
function failingChain(result) {
  const chain = [];
  let node = result;
  while (node.subResults.length > 0) {
    const next = node.subResults.find((sub) => !sub.passed);
    if (next === undefined) break;
    chain.push(next.ruleName);
    node = next;
  }
  return chain;
}

describe("shared fixture contract", () => {
  // Every expectation in the shared fixture, asserted. Each port of this
  // example reproduces these exact numbers. See
  // fixtures/graduation_verdict/README.md for what each field checks.

  for (const studentId of Object.keys(STUDENTS)) {
    it(`${studentId}: verdict matches`, async () => {
      const { graduates } = buildGraduationCheck(POLICIES, ELECTIVE_MINIMUM);
      const context = STUDENTS[studentId];
      const expected = context.expected;
      const result = await graduates.evaluate(context);
      assert.equal(
        result.passed,
        expected.passed,
        `${studentId} (${context.note ?? ""}): expected passed=${expected.passed}, got ${result.passed} (${result.detail})`,
      );
    });

    it(`${studentId}: short-circuit count matches`, async () => {
      // bob and gita both fail; bob stops after one rule, gita runs all
      // four.
      const { graduates } = buildGraduationCheck(POLICIES, ELECTIVE_MINIMUM);
      const context = STUDENTS[studentId];
      const expected = context.expected;
      const result = await graduates.evaluate(context);
      assert.equal(
        result.subResults.length,
        expected.rules_evaluated,
        `${studentId}: expected ${expected.rules_evaluated} sub-rules to run, got ${result.subResults.length}`,
      );
    });

    it(`${studentId}: failing chain matches`, async () => {
      // deepak's failure is three levels deep.
      const { graduates } = buildGraduationCheck(POLICIES, ELECTIVE_MINIMUM);
      const context = STUDENTS[studentId];
      const expected = context.expected;
      const result = await graduates.evaluate(context);
      const chain = failingChain(result);
      assert.deepEqual(chain, expected.failing_chain, `${studentId}: expected chain ${expected.failing_chain}`);
      assert.equal(chain.length > 0 ? chain[0] : null, expected.failing_rule);
    });

    it(`${studentId}: leaves match`, async () => {
      const { graduates } = buildGraduationCheck(POLICIES, ELECTIVE_MINIMUM);
      const context = STUDENTS[studentId];
      const expected = context.expected;
      const result = await graduates.evaluate(context);
      const leaves = result.leaves.map((leaf) => leaf.ruleName);
      assert.deepEqual(leaves, expected.leaves, `${studentId}: expected leaves ${expected.leaves}, got ${leaves}`);
    });

    it(`${studentId}: failing leaves match`, async () => {
      const { graduates } = buildGraduationCheck(POLICIES, ELECTIVE_MINIMUM);
      const context = STUDENTS[studentId];
      const expected = context.expected;
      const result = await graduates.evaluate(context);
      const failingLeaves = result.failingLeaves.map((leaf) => leaf.ruleName);
      assert.deepEqual(
        failingLeaves,
        expected.failing_leaves,
        `${studentId}: expected failing leaves ${expected.failing_leaves}, got ${failingLeaves}`,
      );
    });

    it(`${studentId}: decided_by matches`, async () => {
      // One-level explanation for the top-level composite's own verdict --
      // exactly the decisive member regardless of position (gita fails on
      // the *last* of four), or every member on a full pass.
      const { graduates } = buildGraduationCheck(POLICIES, ELECTIVE_MINIMUM);
      const context = STUDENTS[studentId];
      const expected = context.expected;
      const result = await graduates.evaluate(context);
      const decidedBy = result.decidedBy.map((r) => r.ruleName);
      assert.deepEqual(
        decidedBy,
        expected.decided_by,
        `${studentId}: expected decided_by ${expected.decided_by}, got ${decidedBy}`,
      );
    });

    it(`${studentId}: runAll never short-circuits`, async () => {
      const { engine } = buildGraduationCheck(POLICIES, ELECTIVE_MINIMUM);
      const context = STUDENTS[studentId];
      const expected = context.expected.run_all;
      const result = await engine.runAll(context);
      assert.equal(result.results.length, expected.evaluated);
      assert.equal(result.passed, expected.passed);
    });

    it(`${studentId}: group results match`, async () => {
      // harish graduates while his elective group "fails", because the
      // group verdict is all-must-pass and the composite's requirement is
      // at-least-two-of-three.
      const { engine } = buildGraduationCheck(POLICIES, ELECTIVE_MINIMUM);
      const context = STUDENTS[studentId];
      for (const [group, expected] of Object.entries(context.expected.groups)) {
        const result = await engine.runGroup(group, context);
        assert.equal(result.results.length, expected.evaluated, `${studentId}/${group}`);
        assert.equal(result.passed, expected.passed, `${studentId}/${group}`);
      }
    });
  }
});

describe("vacuous-truth edge cases", () => {
  // The degenerate curricula, from the shared fixture's edge_cases.json.
  // AndRule([]) passes while an at-least-N rule over an empty set fails
  // for N > 0. Nothing in the main student set exercises an empty rule
  // list.

  for (const [caseName, testCase] of Object.entries(EDGE_CASES)) {
    it(`${caseName} matches the fixture`, async () => {
      const { policies, electiveMinimum } = curriculumFromObject(testCase.curriculum);
      const expected = testCase.expected;
      const student = contextFromObject(testCase.student);

      const { engine, graduates } = buildGraduationCheck(policies, electiveMinimum);
      const result = await graduates.evaluate(student);

      assert.equal(result.passed, expected.passed, `${caseName}: ${testCase.note}`);
      assert.equal(result.subResults.length, expected.rules_evaluated, caseName);
      const chain = failingChain(result);
      assert.deepEqual(chain, expected.failing_chain, caseName);

      const runAll = await engine.runAll(student);
      assert.equal(runAll.results.length, expected.run_all.evaluated, caseName);
      assert.equal(runAll.passed, expected.run_all.passed, caseName);

      // The strict form throws; the try-prefixed form returns undefined.
      const lookups = expected.lookups;

      const group = lookups.unknown_group.name;
      if (lookups.unknown_group.run_group_raises) {
        await assert.rejects(() => engine.runGroup(group, student));
      }
      if (lookups.unknown_group.try_run_group_returns_null) {
        assert.equal(await engine.tryRunGroup(group, student), undefined, caseName);
      }

      const rule = lookups.unknown_rule.name;
      if (lookups.unknown_rule.run_named_raises) {
        await assert.rejects(() => engine.runNamed(rule, student));
      }
      if (lookups.unknown_rule.try_run_named_returns_null) {
        assert.equal(await engine.tryRunNamed(rule, student), undefined, caseName);
      }
    });
  }
});

describe("build once, apply many times", () => {
  // The "scale" claim: one built engine/composite pair, reused across
  // every student, never rebuilt per lookup.

  it("the same built objects serve every student", async () => {
    const { graduates } = buildGraduationCheck(POLICIES, ELECTIVE_MINIMUM);
    const results = {};
    for (const [studentId, context] of Object.entries(STUDENTS)) {
      results[studentId] = (await graduates.evaluate(context)).passed;
    }
    const expected = Object.fromEntries(
      Object.entries(STUDENTS).map(([sid, ctx]) => [sid, ctx.expected.passed]),
    );
    assert.deepEqual(results, expected);
  });
});
