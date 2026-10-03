/**
 * A rule or result owns its collections -- a caller that keeps the array it
 * passed in cannot change one afterwards.
 *
 * Every other suite builds a rule or a result and asserts on what evaluation
 * computed. None of them asks whether the inputs can move underneath it,
 * which is the gap these cover: the collections used to be aliased, so
 * retaining the array handed to a composite let a caller change that
 * composite's sub-rules -- and its verdict -- after construction, and let an
 * early-define/late-init pattern build a result graph containing itself,
 * which every traversal here recurses through.
 */

import assert from "node:assert/strict";
import { describe, it } from "node:test";

import {
  AndRule,
  FunctionRule,
  NotRule,
  OrRule,
  RuleResult,
  RulesEngine,
  RunResult,
} from "../dist/index.js";

const rule = (name, passed = true) => new FunctionRule(name, async () => ({ passed }));

describe("composites own their sub-rules", () => {
  for (const Composite of [AndRule, OrRule]) {
    it(`${Composite.name}: appending to the caller's array adds no sub-rule`, () => {
      const held = [rule("a")];
      const composite = new Composite("c", held);
      held.push(rule("injected"));
      assert.match(composite.toString(), /1 sub-rule\(s\)/);
    });

    it(`${Composite.name}: clearing the caller's array does not empty it`, () => {
      const held = [rule("a"), rule("b")];
      const composite = new Composite("c", held);
      held.length = 0;
      assert.match(composite.toString(), /2 sub-rule\(s\)/);
    });
  }

  it("the verdict cannot change after construction", async () => {
    const held = [rule("a")];
    const and = new AndRule("and1", held);
    assert.equal((await and.evaluate({})).passed, true);

    held.push(rule("sabotage", false));
    assert.equal((await and.evaluate({})).passed, true);
  });
});

describe("results own their children", () => {
  it("subResults is copied", () => {
    const held = [new RuleResult("child", true)];
    const result = new RuleResult("parent", true, { subResults: held });
    held.push(new RuleResult("injected", false));
    assert.deepEqual(
      result.subResults.map((r) => r.ruleName),
      ["child"],
    );
  });

  it("decidedByIndices is copied", () => {
    const child = new RuleResult("child", false);
    const held = [0];
    const result = new RuleResult("parent", false, { subResults: [child], decidedByIndices: held });
    held.length = 0;
    assert.deepEqual(
      result.decidedBy.map((r) => r.ruleName),
      ["child"],
    );
  });

  it("an index naming a child that does not exist is rejected", () => {
    // The one way the indices form can be wrong, caught at construction
    // rather than when something later reads decidedBy and finds a hole.
    // The objects form had no equivalent check available -- nothing stopped
    // it naming a result that was never a child of this one.
    const child = new RuleResult("child", false);

    assert.throws(() => new RuleResult("parent", false, { subResults: [child], decidedByIndices: [1] }), {
      name: "RangeError",
    });
  });

  it("decidedBy cannot disagree with subResults", () => {
    // decidedBy is derived, so there is no second stored array that could
    // drift out of step with the children it names.
    const a = new RuleResult("a", true);
    const b = new RuleResult("b", false);
    const result = new RuleResult("parent", false, { subResults: [a, b], decidedByIndices: [1] });

    assert.strictEqual(result.decidedBy[0], result.subResults[1]);
  });

  it("RunResult.results is copied", () => {
    const held = [new RuleResult("a", true)];
    const run = new RunResult(held);
    held.push(new RuleResult("injected", false));
    assert.deepEqual(
      run.results.map((r) => r.ruleName),
      ["a"],
    );
    assert.equal(run.passed, true);
  });

  it("a result cannot be made to contain itself", () => {
    // Early-define/late-init: append the result to the very array it was
    // constructed from. Copying severs it, so the traversals terminate.
    const kids = [];
    const result = new RuleResult("p", false, { subResults: kids });
    kids.push(result);

    assert.equal(result.subResults.length, 0);
    assert.deepEqual(
      result.leaves.map((l) => l.ruleName),
      ["p"],
    );
    assert.deepEqual(
      result.failingLeaves.map((l) => l.ruleName),
      ["p"],
    );
    assert.ok(JSON.stringify(result).length > 0);
  });
});

describe("the engine owns its rules", () => {
  it("appending to the caller's array registers no rule", async () => {
    const held = [rule("a")];
    const engine = new RulesEngine(held);
    held.push(rule("injected"));
    assert.deepEqual(engine.ruleNames, ["a"]);
    assert.equal((await engine.runAll({})).results.length, 1);
  });
});

describe("NotRule owns its child", () => {
  it("holds one rule, so there is no collection to copy", async () => {
    const result = await new NotRule("not_a", rule("a")).evaluate({});
    assert.equal(result.passed, false);
    assert.deepEqual(
      result.subResults.map((r) => r.ruleName),
      ["a"],
    );
  });
});
