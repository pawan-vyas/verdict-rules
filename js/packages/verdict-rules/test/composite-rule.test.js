import assert from "node:assert/strict";
import { describe, it } from "node:test";

import { AndRule, isCompositeRule, NotRule, OrRule, RuleResult } from "../dist/index.js";
import { fail, pass } from "./helpers.js";

/**
 * Unit tests for walking a rule *tree* before it is evaluated.
 *
 * `CompositeRule` plus `isCompositeRule` is what makes that possible without an
 * `instanceof` chain. The result surface answers what ran; these pin down what
 * was built -- and in particular that one walk reaches a composite this package
 * never saw, which the three built-ins alone cannot demonstrate.
 *
 * This matters more in JS than elsewhere: every built-in composite stores its
 * parts in a `#private` field, so `subRules` is not a convenience over an
 * already-reachable field, it is the only way to read them at all.
 */

/**
 * A composite defined outside the package, satisfying the interface
 * structurally. It extends nothing verdict owns -- that is the point.
 */
class AtLeastOneOf {
  constructor(name, parts) {
    this.name = name;
    this.group = undefined;
    this.subRules = [...parts];
  }

  async evaluate(context) {
    const subResults = [];
    for (const part of this.subRules) {
      subResults.push(await part.evaluate(context));
      if (subResults[subResults.length - 1].passed) {
        return new RuleResult(this.name, true, {
          subResults,
          decidedByIndices: [subResults.length - 1],
        });
      }
    }
    return new RuleResult(this.name, false, { subResults });
  }
}

/** One walk, with no knowledge of which composite it is looking at. */
function leafNames(rule) {
  return isCompositeRule(rule) ? rule.subRules.flatMap(leafNames) : [rule.name];
}

describe("subRules", () => {
  it("AndRule exposes its sub-rules in order", () => {
    const first = pass("a");
    const second = fail("b");
    assert.deepEqual(new AndRule("gate", [first, second]).subRules, [first, second]);
  });

  it("OrRule exposes its sub-rules in order", () => {
    const first = fail("a");
    const second = pass("b");
    assert.deepEqual(new OrRule("gate", [first, second]).subRules, [first, second]);
  });

  it("NotRule reports one sub-rule under the name every composite shares", () => {
    const inner = pass("inner");
    // `subRules`, not `rule`: a walk must not need to know this is a negation
    // to find its one part.
    assert.deepEqual(new NotRule("negated", inner).subRules, [inner]);
  });

  it("a vacuous composite reports no sub-rules rather than refusing", () => {
    assert.deepEqual(new AndRule("empty-and", []).subRules, []);
    assert.deepEqual(new OrRule("empty-or", []).subRules, []);
  });

  it("the sub-rule array is a copy taken at construction", () => {
    const parts = [pass("a")];
    const composite = new AndRule("gate", parts);
    parts.length = 0;
    assert.equal(composite.subRules.length, 1);
  });

  it("reads the same parts on every access, not a fresh array each time", () => {
    const composite = new NotRule("negated", pass("inner"));
    assert.deepEqual(composite.subRules, composite.subRules);
  });
});

describe("isCompositeRule", () => {
  it("narrows every built-in composite", () => {
    const leaf = pass("leaf");
    assert.ok(isCompositeRule(new AndRule("and", [leaf])));
    assert.ok(isCompositeRule(new OrRule("or", [leaf])));
    assert.ok(isCompositeRule(new NotRule("not", leaf)));
  });

  it("does not narrow a leaf rule", () => {
    // What makes "structure or terminal check" answerable without naming
    // concrete classes.
    assert.equal(isCompositeRule(pass("leaf")), false);
  });

  it("narrows a consumer-defined composite structurally", () => {
    assert.ok(isCompositeRule(new AtLeastOneOf("custom", [pass("a")])));
  });
});

describe("walking a tree", () => {
  it("reaches every leaf through nested built-in composites", () => {
    const tree = new AndRule("top", [
      pass("a"),
      new OrRule("either", [fail("b"), pass("c")]),
      new NotRule("not-d", fail("d")),
    ]);
    assert.deepEqual(leafNames(tree), ["a", "b", "c", "d"]);
  });

  it("reaches into a consumer-defined composite with the same walk", () => {
    // The capability the built-ins alone cannot prove, and the reason this is a
    // published interface rather than three getters.
    const tree = new AndRule("top", [
      pass("a"),
      new AtLeastOneOf("custom", [fail("b"), new NotRule("not-c", pass("c"))]),
    ]);
    assert.deepEqual(leafNames(tree), ["a", "b", "c"]);
  });

  it("leaves a consumer-defined composite evaluating normally", async () => {
    // Satisfying the inspection interface costs nothing on the evaluation side.
    const custom = new AtLeastOneOf("custom", [fail("b"), pass("c")]);
    const verdict = await custom.evaluate({});
    assert.equal(verdict.passed, true);
    assert.deepEqual(
      verdict.decidedBy.map((r) => r.ruleName),
      ["c"],
    );
  });
});
