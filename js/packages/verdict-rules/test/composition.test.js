import assert from "node:assert/strict";
import { describe, it } from "node:test";

import { AndRule, FunctionRule, NotRule, OrRule, SequentialEvaluator, ShortCircuitEvaluator } from "../dist/index.js";
import { counting, fail, pass } from "./helpers.js";

/**
 * Unit tests for the composition primitives behind AndRule/OrRule, and the
 * new shipped NotRule: SequentialEvaluator, ShortCircuitEvaluator, and
 * RuleResult.decidedBy. A 1:1 port of Python's `test_composition.py`.
 *
 * See .agents/plans/composite-rule-and-leaves-redesign/README.md §3-§3d for
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

describe("RuleResult.decidedBy", () => {
  // decidedBy -- the one-level, non-recursive explanation for a
  // composite's own verdict. Supersedes AndRule.failed/passing and
  // OrRule.passed/failing (removed): those were static methods a caller
  // could apply to the wrong family's result and get a plausible, silently
  // wrong answer -- confirmed with concrete cases from a real adopter
  // review, not hypothetical. decidedBy closes that structurally: there is
  // no second method to reach for, every result carries its own
  // correctly-populated field.

  it("AndRule failing early names just the decisive failure", async () => {
    const rule = new AndRule("and1", [pass("a"), fail("b"), pass("c"), pass("d")]);
    const result = await rule.evaluate({});
    assert.deepEqual(
      result.decidedBy.map((r) => r.ruleName),
      ["b"],
    );
  });

  it("AndRule failing on its last item still names just that one", async () => {
    // The case a first attempt at this got wrong: subResults.length===total
    // holds here exactly like it does for a genuine full pass, so a rule
    // based on count alone can't tell them apart -- position in the list
    // is irrelevant to blame; ShortCircuitEvaluator's own stopOn is what
    // actually distinguishes them.
    const rule = new AndRule("and1", [pass("a"), pass("b"), fail("c")]);
    const result = await rule.evaluate({});
    assert.deepEqual(
      result.decidedBy.map((r) => r.ruleName),
      ["c"],
    );
  });

  it("AndRule fully passing names every sub-result", async () => {
    const rule = new AndRule("and1", [pass("a"), pass("b"), pass("c")]);
    const result = await rule.evaluate({});
    assert.deepEqual(
      result.decidedBy.map((r) => r.ruleName),
      ["a", "b", "c"],
    );
  });

  it("AndRule vacuous pass names nothing", async () => {
    const rule = new AndRule("and1", []);
    const result = await rule.evaluate({});
    assert.deepEqual(result.decidedBy, []);
  });

  it("OrRule passing early names just the decisive pass", async () => {
    const rule = new OrRule("or1", [fail("a"), pass("b"), fail("c")]);
    const result = await rule.evaluate({});
    assert.deepEqual(
      result.decidedBy.map((r) => r.ruleName),
      ["b"],
    );
  });

  it("OrRule all-fail names every sub-result", async () => {
    // Mirrors the AndRule last-item case with the opposite polarity --
    // OrRule's all-fail verdict is only known once every item is seen,
    // genuinely collective, not attributable to the last one alone.
    const rule = new OrRule("or1", [fail("a"), fail("b"), fail("c")]);
    const result = await rule.evaluate({});
    assert.deepEqual(
      result.decidedBy.map((r) => r.ruleName),
      ["a", "b", "c"],
    );
  });

  it("OrRule vacuous fail names nothing", async () => {
    const rule = new OrRule("or1", []);
    const result = await rule.evaluate({});
    assert.deepEqual(result.decidedBy, []);
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

  it("decidedBy is the inner result in both directions", async () => {
    // Unconditional, unlike failingLeaves' own self-as-leaf rule -- "inner
    // passed" is genuinely why a failing NotRule failed, not an
    // inconsistency to paper over.
    const failing = await new NotRule("not1", fail("inner")).evaluate({});
    assert.deepEqual(
      failing.decidedBy.map((r) => r.ruleName),
      ["inner"],
    );

    const passing = await new NotRule("not2", pass("inner")).evaluate({});
    assert.deepEqual(
      passing.decidedBy.map((r) => r.ruleName),
      ["inner"],
    );
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
