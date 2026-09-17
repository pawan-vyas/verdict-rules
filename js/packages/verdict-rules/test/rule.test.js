import assert from "node:assert/strict";
import { describe, it } from "node:test";

import { AndRule, FunctionRule, OrRule } from "../dist/index.js";
import { fail, pass } from "./helpers.js";

/**
 * A strict, 1:1 port of Python's `test_rule.py` — FunctionRule, AndRule,
 * OrRule. Every test here has an exact Python counterpart. JS-specific
 * coverage (structural typing on a bare object literal) lives in
 * `js-idioms.test.js` instead, so this file stays auditable against
 * Python's own suite test-for-test.
 */

describe("FunctionRule", () => {
  // Mirrors test_wraps_a_passing_predicate.
  it("wraps a passing predicate", async () => {
    const rule = pass("r1");
    const result = await rule.evaluate({});
    assert.equal(result.passed, true);
    assert.equal(result.ruleName, "r1");
  });

  // Mirrors test_wraps_a_failing_predicate.
  it("wraps a failing predicate", async () => {
    const rule = fail("r1", undefined, "nope");
    const result = await rule.evaluate({});
    assert.equal(result.passed, false);
    assert.equal(result.detail, "nope");
  });

  // Mirrors test_predicate_receives_the_context.
  it("predicate receives the context", async () => {
    let seen;
    const rule = new FunctionRule("r1", async (ctx) => {
      seen = ctx;
      return { ruleName: "r1", passed: true };
    });
    await rule.evaluate({ userId: 42 });
    assert.deepEqual(seen, { userId: 42 });
  });

  // Mirrors test_carries_name_and_group.
  it("carries name and group", () => {
    const rule = pass("r1", "g1");
    assert.equal(rule.name, "r1");
    assert.equal(rule.group, "g1");
  });

  // Mirrors test_group_defaults_to_none.
  it("group defaults to undefined", () => {
    const rule = pass("r1");
    assert.equal(rule.group, undefined);
  });
});

describe("AndRule", () => {
  // Mirrors test_all_pass_yields_pass.
  it("all pass yields pass", async () => {
    const rule = new AndRule("and1", [pass("a"), pass("b")]);
    const result = await rule.evaluate({});
    assert.equal(result.passed, true);
    assert.equal(result.ruleName, "and1");
  });

  // Mirrors test_one_failure_yields_fail.
  it("one failure yields fail", async () => {
    const rule = new AndRule("and1", [pass("a"), fail("b", undefined, "bad")]);
    const result = await rule.evaluate({});
    assert.equal(result.passed, false);
    assert.match(result.detail, /b/);
    assert.match(result.detail, /bad/);
  });

  // Mirrors test_short_circuits_after_first_failure.
  it("short-circuits after first failure", async () => {
    const log = [];
    const rule = new AndRule("and1", [
      fail("a"),
      new FunctionRule("c", async () => {
        log.push("c");
        return { ruleName: "c", passed: true };
      }),
    ]);
    await rule.evaluate({});
    assert.deepEqual(log, []); // never reached -- 'a' already failed
  });

  // Mirrors test_data_carries_sub_results_up_to_failure.
  it("data carries sub-results up to failure", async () => {
    const rule = new AndRule("and1", [pass("a"), fail("b"), pass("c")]);
    const result = await rule.evaluate({});
    assert.deepEqual(
      result.data.map((r) => r.ruleName),
      ["a", "b"],
    );
  });

  // Mirrors test_empty_rule_list_vacuously_passes.
  it("empty rule list vacuously passes", async () => {
    const result = await new AndRule("and1", []).evaluate({});
    assert.equal(result.passed, true);
  });
});

describe("OrRule", () => {
  // Mirrors test_any_pass_yields_pass.
  it("any pass yields pass", async () => {
    const rule = new OrRule("or1", [fail("a"), pass("b")]);
    const result = await rule.evaluate({});
    assert.equal(result.passed, true);
  });

  // Mirrors test_all_fail_yields_fail.
  it("all fail yields fail", async () => {
    const rule = new OrRule("or1", [fail("a"), fail("b")]);
    const result = await rule.evaluate({});
    assert.equal(result.passed, false);
    assert.equal(result.detail, "no sub-rule passed");
  });

  // Mirrors test_short_circuits_after_first_pass.
  it("short-circuits after first pass", async () => {
    const log = [];
    const rule = new OrRule("or1", [
      pass("a"),
      new FunctionRule("c", async () => {
        log.push("c");
        return { ruleName: "c", passed: false };
      }),
    ]);
    await rule.evaluate({});
    assert.deepEqual(log, []); // never reached -- 'a' already passed
  });

  // Mirrors test_empty_rule_list_vacuously_fails.
  it("empty rule list vacuously fails", async () => {
    const result = await new OrRule("or1", []).evaluate({});
    assert.equal(result.passed, false);
  });
});

/**
 * Mirrors Python's TestExceptionPropagation in test_rule.py -- AndRule/OrRule
 * catch nothing either; a sub-rule's own exception propagates straight out
 * of evaluate(). See docs/extending/isolating-flaky-predicates/ for the
 * wrapper a consumer opts into if the opposite is wanted.
 */
describe("rule-level exception propagation", () => {
  // Mirrors test_and_rule_does_not_catch_a_sub_rule_s_exception.
  it("AndRule does not catch a sub-rule's exception", async () => {
    const flaky = new FunctionRule("flaky", async () => {
      throw new Error("boom");
    });
    await assert.rejects(
      () => new AndRule("and1", [pass("a"), flaky]).evaluate({}),
      /boom/,
    );
  });

  // Mirrors test_or_rule_does_not_catch_a_sub_rule_s_exception.
  it("OrRule does not catch a sub-rule's exception", async () => {
    const flaky = new FunctionRule("flaky", async () => {
      throw new Error("boom");
    });
    await assert.rejects(
      () => new OrRule("or1", [fail("a"), flaky]).evaluate({}),
      /boom/,
    );
  });
});
