import assert from "node:assert/strict";
import { describe, it } from "node:test";

import { AndRule, FunctionRule, NotRule, OrRule, SequentialEvaluator, ShortCircuitEvaluator } from "../dist/index.js";
import { counting, fail, pass } from "./helpers.js";

/**
 * Unit tests for the composition primitives behind AndRule/OrRule, and the
 * new shipped NotRule: SequentialEvaluator, ShortCircuitEvaluator, and the
 * AndRule.failed/passing / OrRule.passed/failing / NotRule.negated
 * accessors. A 1:1 port of Python's `test_composition.py`.
 *
 * See .agents/plans/composite-rule-and-leaves-redesign/README.md §3-§3e for
 * the design this pins down.
 */

describe("SequentialEvaluator", () => {
  // The general-purpose piece ShortCircuitEvaluator (and a custom composite
  // like AtLeastNRule) is built on.

  it("empty rule list returns the vacuous result directly", async () => {
    // Checked before the loop runs at all -- there's no last element to
    // fall back on when the list started empty.
    const evaluator = new SequentialEvaluator(() => undefined, true);
    const result = await evaluator.evaluate("e", [], {});
    assert.equal(result.passed, true);
    assert.deepEqual(result.subResults, []);
  });

  it("decider returning a value stops evaluation immediately", async () => {
    const log = [];
    const rules = [counting("a", true, log), counting("b", true, log), counting("c", true, log)];
    const decider = (latest, soFar) => (soFar.length === 1 ? false : undefined);
    const evaluator = new SequentialEvaluator(decider, true);
    const result = await evaluator.evaluate("e", rules, {});

    assert.equal(result.passed, false);
    assert.deepEqual(log, ["a"]); // evaluation stops after the first rule
    assert.deepEqual(
      result.subResults.map((r) => r.ruleName),
      ["a"],
    );
  });

  it("decider returning undefined throughout falls back to the vacuous result", async () => {
    const rules = [pass("a"), pass("b")];
    const evaluator = new SequentialEvaluator(() => undefined, false);
    const result = await evaluator.evaluate("e", rules, {});
    assert.equal(result.passed, false); // the vacuousResult, not a verdict from the rules
    assert.deepEqual(
      result.subResults.map((r) => r.ruleName),
      ["a", "b"],
    );
  });

  it("subResults always carries every rule actually evaluated", async () => {
    const rules = [pass("a"), pass("b"), pass("c")];
    const evaluator = new SequentialEvaluator(() => undefined, true);
    const result = await evaluator.evaluate("e", rules, {});
    assert.deepEqual(
      result.subResults.map((r) => r.ruleName),
      ["a", "b", "c"],
    );
  });
});

describe("ShortCircuitEvaluator", () => {
  // The exact shape AndRule (stopOn=false)/OrRule (stopOn=true) compose.

  it("stopOn=false behaves like AndRule", async () => {
    const log = [];
    const evaluator = new ShortCircuitEvaluator(false);
    const rules = [pass("a"), fail("b"), counting("c", true, log)];
    const result = await evaluator.evaluate("e", rules, {});
    assert.equal(result.passed, false);
    assert.deepEqual(log, []); // stops at the first failure -- 'c' never runs
    assert.deepEqual(
      result.subResults.map((r) => r.ruleName),
      ["a", "b"],
    );
  });

  it("stopOn=true behaves like OrRule", async () => {
    const log = [];
    const evaluator = new ShortCircuitEvaluator(true);
    const rules = [fail("a"), pass("b"), counting("c", true, log)];
    const result = await evaluator.evaluate("e", rules, {});
    assert.equal(result.passed, true);
    assert.deepEqual(log, []); // stops at the first pass -- 'c' never runs
    assert.deepEqual(
      result.subResults.map((r) => r.ruleName),
      ["a", "b"],
    );
  });

  it("empty rules' vacuous result is derived from stopOn", async () => {
    const andShaped = new ShortCircuitEvaluator(false);
    const orShaped = new ShortCircuitEvaluator(true);
    assert.equal((await andShaped.evaluate("e", [], {})).passed, true);
    assert.equal((await orShaped.evaluate("e", [], {})).passed, false);
  });

  it("all pass with stopOn=false runs every rule", async () => {
    const log = [];
    const evaluator = new ShortCircuitEvaluator(false);
    const rules = [counting("a", true, log), counting("b", true, log)];
    const result = await evaluator.evaluate("e", rules, {});
    assert.equal(result.passed, true);
    assert.deepEqual(log, ["a", "b"]);
  });

  it("all fail with stopOn=true runs every rule", async () => {
    const evaluator = new ShortCircuitEvaluator(true);
    const rules = [fail("a"), fail("b")];
    const result = await evaluator.evaluate("e", rules, {});
    assert.equal(result.passed, false);
    assert.deepEqual(
      result.subResults.map((r) => r.ruleName),
      ["a", "b"],
    );
  });
});

describe("AndRule accessors", () => {
  it("failed returns the sole decisive failure", async () => {
    const rule = new AndRule("and1", [pass("a"), fail("b")]);
    const result = await rule.evaluate({});
    const failed = AndRule.failed(result);
    assert.notEqual(failed, undefined);
    assert.equal(failed.ruleName, "b");
  });

  it("failed returns undefined when the AndRule passed", async () => {
    const rule = new AndRule("and1", [pass("a"), pass("b")]);
    const result = await rule.evaluate({});
    assert.equal(AndRule.failed(result), undefined);
  });

  it("failed returns undefined for an empty AndRule", async () => {
    const rule = new AndRule("and1", []);
    const result = await rule.evaluate({});
    assert.equal(AndRule.failed(result), undefined);
  });

  it("passing returns every sub-result when the AndRule passed", async () => {
    const rule = new AndRule("and1", [pass("a"), pass("b")]);
    const result = await rule.evaluate({});
    assert.deepEqual(
      AndRule.passing(result).map((r) => r.ruleName),
      ["a", "b"],
    );
  });

  it("passing excludes the decisive failure", async () => {
    const rule = new AndRule("and1", [pass("a"), pass("b"), fail("c")]);
    const result = await rule.evaluate({});
    assert.deepEqual(
      AndRule.passing(result).map((r) => r.ruleName),
      ["a", "b"],
    );
  });
});

describe("OrRule accessors", () => {
  it("passed returns the sole decisive pass", async () => {
    const rule = new OrRule("or1", [fail("a"), pass("b")]);
    const result = await rule.evaluate({});
    const passed = OrRule.passed(result);
    assert.notEqual(passed, undefined);
    assert.equal(passed.ruleName, "b");
  });

  it("passed returns undefined when the OrRule failed", async () => {
    const rule = new OrRule("or1", [fail("a"), fail("b")]);
    const result = await rule.evaluate({});
    assert.equal(OrRule.passed(result), undefined);
  });

  it("passed returns undefined for an empty OrRule", async () => {
    const rule = new OrRule("or1", []);
    const result = await rule.evaluate({});
    assert.equal(OrRule.passed(result), undefined);
  });

  it("failing returns every sub-result when the OrRule failed", async () => {
    const rule = new OrRule("or1", [fail("a"), fail("b")]);
    const result = await rule.evaluate({});
    assert.deepEqual(
      OrRule.failing(result).map((r) => r.ruleName),
      ["a", "b"],
    );
  });

  it("failing excludes the decisive pass", async () => {
    const rule = new OrRule("or1", [fail("a"), fail("b"), pass("c")]);
    const result = await rule.evaluate({});
    assert.deepEqual(
      OrRule.failing(result).map((r) => r.ruleName),
      ["a", "b"],
    );
  });
});

describe("NotRule", () => {
  it("passes when the inner rule fails", async () => {
    const rule = new NotRule("not1", fail("inner"));
    const result = await rule.evaluate({});
    assert.equal(result.passed, true);
  });

  it("fails when the inner rule passes", async () => {
    const rule = new NotRule("not1", pass("inner"));
    const result = await rule.evaluate({});
    assert.equal(result.passed, false);
  });

  it("subResults truthfully carries the one inner result", async () => {
    const rule = new NotRule("not1", pass("inner"));
    const result = await rule.evaluate({});
    assert.equal(result.subResults.length, 1);
    assert.equal(result.subResults[0].ruleName, "inner");
  });

  it("negated returns the inner result", async () => {
    const rule = new NotRule("not1", fail("inner"));
    const result = await rule.evaluate({});
    const inner = NotRule.negated(result);
    assert.equal(inner.ruleName, "inner");
    assert.equal(inner.passed, false);
  });

  it("a failed NotRule has itself as its own failing leaf", async () => {
    // The negation case failingLeaves exists to handle correctly -- the
    // inner rule passed, so there's no failing descendant to recurse into;
    // the failed NotRule result has to be the leaf itself.
    const rule = new NotRule("not1", pass("inner"));
    const result = await rule.evaluate({});
    assert.deepEqual(result.failingLeaves, [result]);
  });

  it("toString shows the name", () => {
    const rule = new NotRule("not1", pass("inner"));
    assert.equal(rule.toString(), 'NotRule "not1"');
  });

  it("toString shows the group when present", () => {
    const rule = new NotRule("not1", pass("inner"), "g1");
    assert.equal(rule.toString(), 'NotRule "not1" (g1)');
  });

  it("does not catch the inner rule's exception", async () => {
    const flaky = new FunctionRule("flaky", async () => {
      throw new Error("boom");
    });
    const rule = new NotRule("not1", flaky);
    await assert.rejects(() => rule.evaluate({}), /boom/);
  });
});
