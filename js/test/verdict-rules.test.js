import assert from "node:assert/strict";
import { describe, it } from "node:test";

import {
  AndRule,
  FunctionRule,
  OrRule,
  RulesEngine,
} from "../dist/index.js";

/**
 * A rule that records every evaluation, so short-circuiting can be proven by
 * what actually ran rather than by the final boolean alone. A port that
 * evaluated concurrently would return the same boolean and fail only here.
 */
const counting = (name, passes, log, group) =>
  new FunctionRule(
    name,
    async () => {
      log.push(name);
      return { ruleName: name, passed: passes };
    },
    group,
  );

describe("structural typing", () => {
  it("a plain object of the right shape is a Rule, with nothing declared", async () => {
    // The property Python's Protocol gives and Dart/C# cannot: no class, no
    // `implements`, no registration — shape alone is enough.
    const duck = {
      name: "duck",
      async evaluate() {
        return { ruleName: "duck", passed: true };
      },
    };
    const result = await new AndRule("composed", [duck]).evaluate({});
    assert.equal(result.passed, true);
  });
});

describe("FunctionRule", () => {
  it("returns whatever the predicate returns, unchanged", async () => {
    const rule = new FunctionRule("r", async () => ({
      ruleName: "r",
      passed: true,
      detail: "why",
      data: { k: 1 },
    }));
    const result = await rule.evaluate({});
    assert.equal(result.detail, "why");
    assert.deepEqual(result.data, { k: 1 });
  });
});

describe("AndRule", () => {
  it("passes when every sub-rule passes, evaluating all of them", async () => {
    const log = [];
    const result = await new AndRule("all", [
      counting("a", true, log),
      counting("b", true, log),
    ]).evaluate({});
    assert.equal(result.passed, true);
    assert.deepEqual(log, ["a", "b"]);
  });

  it("short-circuits: later sub-rules never run", async () => {
    const log = [];
    const result = await new AndRule("all", [
      counting("a", true, log),
      counting("b", false, log),
      counting("c", true, log),
    ]).evaluate({});
    assert.equal(result.passed, false);
    assert.deepEqual(log, ["a", "b"], "'c' must never have been evaluated");
  });

  it("data holds only what ran, never padded, never flattened", async () => {
    const log = [];
    const result = await new AndRule("outer", [
      new AndRule("inner", [counting("deep", false, log)]),
      counting("never", true, log),
    ]).evaluate({});
    assert.equal(result.data.length, 1, "only the failing sub-rule ran");
    assert.equal(result.data[0].ruleName, "inner");
    assert.equal(
      result.data[0].data[0].ruleName,
      "deep",
      "nesting is preserved, not flattened into the parent",
    );
  });

  it("empty passes vacuously", async () => {
    assert.equal((await new AndRule("none", []).evaluate({})).passed, true);
  });
});

describe("OrRule", () => {
  it("short-circuits on the first pass", async () => {
    const log = [];
    const result = await new OrRule("any", [
      counting("a", false, log),
      counting("b", true, log),
      counting("c", true, log),
    ]).evaluate({});
    assert.equal(result.passed, true);
    assert.deepEqual(log, ["a", "b"], "'c' must never have been evaluated");
  });

  it("empty fails vacuously — the opposite polarity to AndRule", async () => {
    assert.equal((await new OrRule("none", []).evaluate({})).passed, false);
  });
});

describe("RulesEngine run modes", () => {
  it("runAll never short-circuits", async () => {
    const log = [];
    const engine = new RulesEngine([
      counting("a", false, log),
      counting("b", false, log),
      counting("c", true, log),
    ]);
    const result = await engine.runAll({});
    assert.equal(result.passed, false);
    assert.equal(result.results.length, 3);
    assert.deepEqual(log, ["a", "b", "c"]);
  });

  it("runGroup evaluates only its own group", async () => {
    const log = [];
    const engine = new RulesEngine([
      counting("a", true, log, "g1"),
      counting("b", true, log, "g2"),
      counting("c", false, log, "g1"),
    ]);
    const result = await engine.runGroup("g1", {});
    assert.deepEqual(result.results.map((r) => r.ruleName), ["a", "c"]);
    assert.equal(result.passed, false);
  });

  it("runNamed looks one rule up", async () => {
    const engine = new RulesEngine([counting("a", true, [])]);
    assert.equal((await engine.runNamed("a", {})).ruleName, "a");
  });
});

describe("emptiness is not absence", () => {
  it("an unknown rule name throws", async () => {
    const engine = new RulesEngine([counting("a", true, [], "g1")]);
    await assert.rejects(() => engine.runNamed("nope", {}), /No rule named/);
  });

  it("an unknown group throws rather than passing vacuously", async () => {
    // A group exists only because some rule declared it, so a lookup that
    // matches nothing can only be a typo. Returning a pass would mean a
    // misspelled group silently approves.
    const engine = new RulesEngine([counting("a", true, [], "g1")]);
    await assert.rejects(
      () => engine.runGroup("no-such-group", {}),
      /No rules in group/,
    );
  });

  it("a rule with no group makes no group exist", async () => {
    const engine = new RulesEngine([counting("a", true, [])]);
    await assert.rejects(() => engine.runGroup("g1", {}), /No rules in group/);
  });

  it("but empty composites still fold to their identity", async () => {
    assert.equal((await new AndRule("none", []).evaluate({})).passed, true);
    assert.equal((await new OrRule("none", []).evaluate({})).passed, false);
  });
});

describe("introspection", () => {
  it("reports exactly what the lookups accept", async () => {
    const engine = new RulesEngine([
      counting("a", true, [], "g1"),
      counting("b", true, [], "g2"),
      counting("c", true, []),
    ]);
    assert.deepEqual(engine.ruleNames, ["a", "b", "c"]);
    assert.deepEqual(engine.groupNames, ["g1", "g2"]);
    for (const g of engine.groupNames) await engine.runGroup(g, {});
  });

  it("an empty engine reports nothing", () => {
    const engine = new RulesEngine([]);
    assert.deepEqual(engine.ruleNames, []);
    assert.deepEqual(engine.groupNames, []);
  });
});
