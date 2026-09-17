import assert from "node:assert/strict";
import { describe, it } from "node:test";

import { AndRule, FunctionRule, OrRule, RulesEngine, UnknownLookupError } from "../dist/index.js";
import { fail, pass } from "./helpers.js";

/**
 * A strict, 1:1 port of Python's `test_engine.py` — RulesEngine. Every test
 * here has an exact Python counterpart. JS-specific coverage lives in
 * `js-idioms.test.js` instead, so this file stays auditable against
 * Python's own suite test-for-test.
 */

describe("RunAll", () => {
  // Mirrors test_all_passing_rules_yields_passed_true.
  it("all passing rules yields passed true", async () => {
    const engine = new RulesEngine([pass("a"), pass("b")]);
    const result = await engine.runAll({});
    assert.equal(result.passed, true);
    assert.deepEqual(
      result.results.map((r) => r.ruleName),
      ["a", "b"],
    );
  });

  // Mirrors test_one_failing_rule_yields_passed_false.
  it("one failing rule yields passed false", async () => {
    const engine = new RulesEngine([pass("a"), fail("b")]);
    const result = await engine.runAll({});
    assert.equal(result.passed, false);
  });

  // Mirrors test_does_not_short_circuit_unlike_and_rule.
  it("does not short-circuit unlike AndRule", async () => {
    const engine = new RulesEngine([fail("a"), pass("b")]);
    const result = await engine.runAll({});
    assert.deepEqual(
      result.results.map((r) => r.ruleName),
      ["a", "b"],
    );
  });

  // Mirrors test_empty_engine_run_all_vacuously_passes.
  it("empty engine run-all vacuously passes", async () => {
    const engine = new RulesEngine([]);
    const result = await engine.runAll({});
    assert.equal(result.passed, true);
    assert.deepEqual(result.results, []);
  });
});

describe("RunNamed", () => {
  // Mirrors test_returns_that_rule_s_own_result.
  it("returns that rule's own result", async () => {
    const engine = new RulesEngine([pass("a"), fail("b")]);
    const result = await engine.runNamed("b", {});
    assert.equal(result.ruleName, "b");
    assert.equal(result.passed, false);
  });

  // Mirrors test_unknown_name_raises_key_error.
  it("unknown name raises UnknownLookupError", async () => {
    const engine = new RulesEngine([pass("a")]);
    await assert.rejects(() => engine.runNamed("missing", {}), UnknownLookupError);
  });
});

describe("RunGroup", () => {
  // Mirrors test_runs_only_matching_group.
  it("runs only the matching group", async () => {
    const engine = new RulesEngine([
      pass("a", "g1"),
      pass("b", "g2"),
      fail("c", "g1"),
    ]);
    const result = await engine.runGroup("g1", {});
    assert.deepEqual(
      result.results.map((r) => r.ruleName),
      ["a", "c"],
    );
    assert.equal(result.passed, false);
  });

  // Mirrors test_unknown_group_raises.
  it("unknown group raises UnknownLookupError", async () => {
    const engine = new RulesEngine([pass("a", "g1")]);
    await assert.rejects(() => engine.runGroup("no-such-group", {}), UnknownLookupError);
  });

  // Mirrors test_ungrouped_rules_are_never_matched.
  it("ungrouped rules are never matched", async () => {
    const engine = new RulesEngine([pass("a")]); // no group
    await assert.rejects(() => engine.runGroup("g1", {}), UnknownLookupError);
  });

  // Mirrors test_empty_composite_still_passes_vacuously.
  it("empty composite still passes/fails vacuously", async () => {
    assert.equal((await new AndRule("none", []).evaluate({})).passed, true);
    assert.equal((await new OrRule("none", []).evaluate({})).passed, false);
  });
});

/**
 * Mirrors Python's TestTryLookups -- the non-throwing primitives, and that
 * the strict ones sit on top of them. These exist because the engine cannot
 * know what an absent group means: for one consumer it's "no constraint
 * applies, pass", for another "skip this and don't count it", for a third
 * "the configuration is wrong, fail loudly". A library default would be
 * right for one of them and wrong for the rest.
 */
describe("try lookups", () => {
  // Mirrors test_try_run_group_returns_the_result_when_present.
  it("tryRunGroup returns the result when present", async () => {
    const engine = new RulesEngine([pass("a", "g1"), fail("b", "g1")]);
    const result = await engine.tryRunGroup("g1", {});
    assert.notEqual(result, undefined);
    assert.deepEqual(
      result.results.map((r) => r.ruleName),
      ["a", "b"],
    );
    assert.equal(result.passed, false);
  });

  // Mirrors test_try_run_group_returns_none_when_absent.
  it("tryRunGroup returns undefined when absent", async () => {
    const engine = new RulesEngine([pass("a", "g1")]);
    assert.equal(await engine.tryRunGroup("no-such-group", {}), undefined);
  });

  // Mirrors test_try_run_named_returns_the_result_when_present.
  it("tryRunNamed returns the result when present", async () => {
    const engine = new RulesEngine([pass("a")]);
    const result = await engine.tryRunNamed("a", {});
    assert.notEqual(result, undefined);
    assert.equal(result.ruleName, "a");
  });

  // Mirrors test_try_run_named_returns_none_when_absent.
  it("tryRunNamed returns undefined when absent", async () => {
    const engine = new RulesEngine([pass("a")]);
    assert.equal(await engine.tryRunNamed("nope", {}), undefined);
  });

  // Mirrors test_none_means_absent_never_failed.
  it("undefined means absent, never failed", async () => {
    const engine = new RulesEngine([fail("present", "g1")]);

    const failed = await engine.tryRunNamed("present", {});
    assert.notEqual(failed, undefined);
    assert.equal(failed.passed, false);

    assert.equal(await engine.tryRunNamed("absent", {}), undefined);
  });

  // Mirrors test_strict_forms_are_the_try_forms_plus_an_assertion.
  it("strict forms are the try forms plus an assertion", async () => {
    const engine = new RulesEngine([pass("a", "g1")]);

    const namedStrict = await engine.runNamed("a", {});
    const namedTry = await engine.tryRunNamed("a", {});
    assert.equal(namedStrict.ruleName, namedTry.ruleName);

    const groupStrict = await engine.runGroup("g1", {});
    const groupTry = await engine.tryRunGroup("g1", {});
    assert.notEqual(groupTry, undefined);
    assert.equal(groupStrict.passed, groupTry.passed);
    assert.equal(groupStrict.results.length, groupTry.results.length);
  });

  /**
   * Mirrors test_fallback_matrix -- the full matrix a caller faces: three
   * states a lookup can be in, against the three things a caller can decide
   * absence means. The interesting rows are the ones where the default must
   * *not* fire -- a fallback firing on a present-but-failing group turns a
   * real rejection into a silent approval, which is the whole failure this
   * API exists to let callers avoid.
   *
   *   group state       | ?? true | ?? false | strict
   *   ------------------+---------+----------+---------------
   *   present, passing  | true    | true     | passed = true
   *   present, failing  | false   | false    | passed = false   <- must not fire
   *   absent            | true    | false    | throws
   */
  for (const [group, defaultTrue, defaultFalse, strictThrows] of [
    ["passing", true, true, false],
    ["failing", false, false, false],
    ["absent", true, false, true],
  ]) {
    it(`fallback matrix: ${group}`, async () => {
      const engine = new RulesEngine([pass("p", "passing"), fail("f", "failing")]);

      const result = await engine.tryRunGroup(group, {});

      assert.equal(result?.passed ?? true, defaultTrue);
      assert.equal(result?.passed ?? false, defaultFalse);

      if (strictThrows) {
        await assert.rejects(() => engine.runGroup(group, {}), UnknownLookupError);
      } else {
        const strict = await engine.runGroup(group, {});
        assert.equal(strict.passed, defaultTrue);
        assert.equal(strict.passed, defaultFalse);
      }
    });
  }

  // Mirrors test_skipping_counts_only_what_exists.
  it("skipping counts only what exists", async () => {
    const engine = new RulesEngine([fail("f", "failing")]);

    const evaluated = (
      await Promise.all([
        engine.tryRunGroup("failing", {}),
        engine.tryRunGroup("absent", {}),
      ])
    ).filter((r) => r !== undefined);

    assert.equal(evaluated.length, 1);
    assert.equal(evaluated[0].passed, false);
  });
});

/** Mirrors Python's TestIntrospection -- ruleNames/groupNames let a caller check instead of catching. */
describe("introspection", () => {
  // Mirrors test_reports_registered_names_in_order.
  it("reports registered names in order", () => {
    const engine = new RulesEngine([pass("a", "g1"), pass("b", "g2"), pass("c")]);
    assert.deepEqual(engine.ruleNames, ["a", "b", "c"]);
    assert.deepEqual(engine.groupNames, ["g1", "g2"]);
  });

  // Mirrors test_group_names_is_exactly_what_run_group_accepts.
  it("groupNames is exactly what runGroup accepts", async () => {
    const engine = new RulesEngine([pass("a", "g1"), pass("b")]);

    for (const group of engine.groupNames) {
      await engine.runGroup(group, {}); // must not throw
    }

    assert.ok(!engine.groupNames.includes("g2"));
    await assert.rejects(() => engine.runGroup("g2", {}), UnknownLookupError);
  });

  // Mirrors test_empty_engine_reports_nothing.
  it("empty engine reports nothing", () => {
    const engine = new RulesEngine([]);
    assert.deepEqual(engine.ruleNames, []);
    assert.deepEqual(engine.groupNames, []);
  });
});

describe("construction", () => {
  // Mirrors test_duplicate_names_last_one_wins_in_by_name_lookup.
  it("duplicate names -- last one wins in by-name lookup", async () => {
    const engine = new RulesEngine([pass("a"), fail("a")]);
    const result = await engine.runNamed("a", {});
    assert.equal(result.passed, false); // the second registration ("a", failing) won
  });
});

/**
 * Mirrors Python's TestExceptionPropagation in test_engine.py -- a
 * predicate's own exception is never caught anywhere in the engine, and
 * propagates exactly as if the caller had invoked the predicate directly.
 */
describe("engine exception propagation", () => {
  // Mirrors test_run_all_does_not_catch_a_predicate_s_exception.
  it("runAll does not catch a predicate's exception", async () => {
    const engine = new RulesEngine([
      pass("a"),
      new FunctionRule("flaky", async () => {
        throw new Error("external check unreachable");
      }),
      pass("c"),
    ]);

    await assert.rejects(() => engine.runAll({}), /external check unreachable/);
  });

  // Mirrors test_run_group_does_not_catch_a_predicate_s_exception.
  it("runGroup does not catch a predicate's exception", async () => {
    const engine = new RulesEngine([
      new FunctionRule(
        "flaky",
        async () => {
          throw new Error("bad input");
        },
        "g",
      ),
    ]);

    await assert.rejects(() => engine.runGroup("g", {}), /bad input/);
  });
});
