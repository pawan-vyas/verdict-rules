/**
 * Tests for Rule's generic context parameter (TContext).
 *
 * TypeScript's generics compile away at the .js boundary this test file
 * runs at, so what's actually observable here is the *runtime* half of the
 * contract: a rule typed against a non-dict context (a plain object shape
 * standing in for a real consumer's own aggregate) runs through every
 * primitive -- FunctionRule, AndRule, OrRule, RulesEngine, all three run
 * modes -- exactly the way a dict-context (`Context`) rule always has. The
 * type-level half (no default type parameter, AndRule/OrRule requiring a
 * shared TContext) is enforced by `tsc` against src/*.ts and isn't
 * something a .js test can exercise directly. This file is deliberately
 * separate from rule.test.js/engine.test.js: those prove the
 * language-agnostic contract 1:1 against every other SDK; this one proves
 * a TypeScript-specific typing addition with no cross-language counterpart
 * to port.
 */
import assert from "node:assert/strict";
import { describe, it } from "node:test";

import { AndRule, FunctionRule, OrRule, RulesEngine } from "../dist/index.js";

/** @typedef {{ total: number, isMember: boolean }} OrderContext */

/** @param {OrderContext} context */
async function orderTotalMet(context) {
  return { ruleName: "order_total_met", passed: context.total >= 50 };
}

/** @param {OrderContext} context */
async function isMember(context) {
  return { ruleName: "is_member", passed: context.isMember };
}

describe("a typed, non-dict context runs through every primitive", () => {
  it("FunctionRule evaluates a typed context", async () => {
    const rule = new FunctionRule("order_total_met", orderTotalMet);
    const result = await rule.evaluate({ total: 75, isMember: false });
    assert.equal(result.passed, true);
  });

  it("AndRule composes typed sub-rules", async () => {
    const rule = new AndRule("eligible", [
      new FunctionRule("order_total_met", orderTotalMet),
      new FunctionRule("is_member", isMember),
    ]);
    const result = await rule.evaluate({ total: 75, isMember: true });
    assert.equal(result.passed, true);
  });

  it("AndRule short-circuits on a typed context too", async () => {
    const log = [];
    const tracked = new FunctionRule("tracked", async () => {
      log.push("tracked");
      return { ruleName: "tracked", passed: true };
    });
    const rule = new AndRule("eligible", [new FunctionRule("order_total_met", orderTotalMet), tracked]);
    await rule.evaluate({ total: 10, isMember: false }); // fails order_total_met first
    assert.deepEqual(log, []); // 'tracked' never reached -- short-circuiting survives typed contexts
  });

  it("OrRule composes typed sub-rules", async () => {
    const rule = new OrRule("eligible", [
      new FunctionRule("order_total_met", orderTotalMet),
      new FunctionRule("is_member", isMember),
    ]);
    const result = await rule.evaluate({ total: 10, isMember: true });
    assert.equal(result.passed, true);
  });

  it("RulesEngine.runAll evaluates a typed context", async () => {
    const engine = new RulesEngine([
      new FunctionRule("order_total_met", orderTotalMet),
      new FunctionRule("is_member", isMember),
    ]);
    const result = await engine.runAll({ total: 75, isMember: true });
    assert.equal(result.passed, true);
    assert.equal(result.results.length, 2);
  });

  it("RulesEngine.runNamed evaluates a typed context", async () => {
    const engine = new RulesEngine([new FunctionRule("order_total_met", orderTotalMet)]);
    const result = await engine.runNamed("order_total_met", { total: 75, isMember: false });
    assert.equal(result.passed, true);
  });

  it("RulesEngine.runGroup evaluates a typed context", async () => {
    const engine = new RulesEngine([new FunctionRule("order_total_met", orderTotalMet, "checkout")]);
    const result = await engine.runGroup("checkout", { total: 75, isMember: false });
    assert.equal(result.passed, true);
  });

  it("RulesEngine try-prefixed forms work with a typed context", async () => {
    const engine = new RulesEngine([new FunctionRule("order_total_met", orderTotalMet)]);
    const present = await engine.tryRunNamed("order_total_met", { total: 75, isMember: false });
    const absent = await engine.tryRunNamed("nope", { total: 75, isMember: false });
    assert.notEqual(present, undefined);
    assert.equal(present.passed, true);
    assert.equal(absent, undefined);
  });
});
