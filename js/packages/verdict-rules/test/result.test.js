import assert from "node:assert/strict";
import { describe, it } from "node:test";

import { AndRule, FunctionRule, NotRule, OrRule, RulesEngine } from "../dist/index.js";

/**
 * Unit tests for `RuleResult.leaves`/`failingLeaves` and `RunResult`'s own
 * forwarders -- a 1:1 port of Python's `test_result.py`.
 *
 * `failingLeaves` is an independent recursion, **not** a filter over
 * `leaves` -- see `RuleResult.failingLeaves`'s own doc comment and
 * .agents/plans/composite-rule-and-leaves-redesign/README.md §2 for the
 * exact formula this file pins down case by case.
 *
 * Built through real `Rule`s (`FunctionRule`/`AndRule`/`OrRule`/`NotRule`)
 * rather than hand-constructed `RuleResult` object literals, since
 * `RuleResult` itself carries no public constructor in this language --
 * `leaves`/`failingLeaves` are computed once, internally, by whatever
 * `Rule` builds the result.
 */

function leaf(name, passed) {
  return new FunctionRule(name, async () => ({ passed }));
}

describe("RuleResult.leaves", () => {
  it("a leaf result is its own single leaf", async () => {
    const result = await leaf("a", true).evaluate({});
    assert.deepEqual(result.leaves, [result]);
  });

  it("a composite flattens its direct children", async () => {
    const rule = new AndRule("and1", [leaf("a", true), leaf("b", true)]);
    const result = await rule.evaluate({});
    assert.deepEqual(
      result.leaves.map((r) => r.ruleName),
      ["a", "b"],
    );
  });

  it("nested composites flatten all the way down", async () => {
    const inner = new AndRule("inner_and", [leaf("a", true), leaf("b", true)]);
    const rule = new AndRule("outer_and", [inner, leaf("c", true)]);
    const result = await rule.evaluate({});
    assert.deepEqual(
      result.leaves.map((r) => r.ruleName),
      ["a", "b", "c"],
    );
  });

  it("an empty composite has no leaves at all -- it is its own leaf", async () => {
    // A degenerate composite (AndRule([])) carries no subResults, which
    // collapses to the leaf case -- not a composite with zero children;
    // absence of subResults *is* the leaf signal.
    const result = await new AndRule("and1", []).evaluate({});
    assert.deepEqual(result.leaves, [result]);
  });
});

describe("RuleResult.failingLeaves", () => {
  it("a passed leaf has no failing leaves", async () => {
    const result = await leaf("a", true).evaluate({});
    assert.deepEqual(result.failingLeaves, []);
  });

  it("a failed leaf is its own failing leaf", async () => {
    const result = await leaf("a", false).evaluate({});
    assert.deepEqual(result.failingLeaves, [result]);
  });

  it("AndRule failure surfaces exactly the failing child", async () => {
    const rule = new AndRule("and1", [leaf("a", true), leaf("b", false)]);
    const result = await rule.evaluate({});
    assert.deepEqual(
      result.failingLeaves.map((r) => r.ruleName),
      ["b"],
    );
  });

  it("OrRule all-fail surfaces every failing child", async () => {
    const rule = new OrRule("or1", [leaf("a", false), leaf("b", false)]);
    const result = await rule.evaluate({});
    assert.deepEqual(
      result.failingLeaves.map((r) => r.ruleName),
      ["a", "b"],
    );
  });

  it("a passed result has no failing leaves, even after an earlier failed branch", async () => {
    // The case that makes this an independent recursion, not a filter over
    // `leaves`: an OrRule whose first sub-rule failed before its second one
    // passed. The whole thing passed -- `failingLeaves` must report
    // nothing, even though `leaves` still shows the earlier failure.
    const rule = new OrRule("or1", [leaf("a", false), leaf("b", true)]);
    const result = await rule.evaluate({});
    assert.deepEqual(
      result.leaves.map((r) => r.ruleName),
      ["a", "b"],
    );
    assert.deepEqual(result.failingLeaves, []);
  });

  it("a failed result with no failing children is its own leaf", async () => {
    // The negation case: a failed NotRule wraps an inner rule that itself
    // *passed* -- recursing into subResults finds no failures at all, so
    // the composite's own failed result has to be the leaf, instead of
    // returning an empty list that would misrepresent why anything failed.
    const rule = new NotRule("not1", leaf("inner", true));
    const result = await rule.evaluate({});
    assert.deepEqual(result.failingLeaves, [result]);
  });

  it("nested failure flattens to the deepest actual failures", async () => {
    // inner_or: only 'b' is evaluated and fails; outer_and never reaches 'c'.
    const innerOr = new OrRule("inner_or", [leaf("b", false)]);
    const rule = new AndRule("outer_and", [leaf("a", true), innerOr]);
    const result = await rule.evaluate({});
    assert.deepEqual(
      result.failingLeaves.map((r) => r.ruleName),
      ["b"],
    );
  });
});

describe("RunResult forwarders", () => {
  it("leaves flattens across every result", async () => {
    const engine = new RulesEngine([leaf("standalone", true), new AndRule("and1", [leaf("a", true), leaf("b", false)])]);
    const run = await engine.runAll({});
    assert.deepEqual(
      run.leaves.map((r) => r.ruleName),
      ["standalone", "a", "b"],
    );
  });

  it("failingLeaves is a plain filter over leaves", async () => {
    // Unlike RuleResult.failingLeaves, RunResult's own version *is* a plain
    // filter -- runAll/runGroup never short-circuit, so every top-level
    // result's own verdict is already final; no earlier short-circuited
    // branch to misrepresent.
    const engine = new RulesEngine([leaf("standalone", true), new AndRule("and1", [leaf("a", true), leaf("b", false)])]);
    const run = await engine.runAll({});
    assert.deepEqual(
      run.failingLeaves.map((r) => r.ruleName),
      ["b"],
    );
  });

  it("empty RunResult has no leaves", async () => {
    const engine = new RulesEngine([]);
    const run = await engine.runAll({});
    assert.deepEqual(run.leaves, []);
    assert.deepEqual(run.failingLeaves, []);
  });
});
