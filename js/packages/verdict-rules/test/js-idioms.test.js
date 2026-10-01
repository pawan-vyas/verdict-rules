import assert from "node:assert/strict";
import { inspect } from "node:util";
import { describe, it } from "node:test";

import { AndRule, FunctionRule, OrRule, RulesEngine, UnknownLookupError } from "../dist/index.js";
import { fail, pass } from "./helpers.js";

/**
 * Coverage for JavaScript-specific idioms, not universal contracts.
 * `rule.test.js` and `engine.test.js` together are this package's 1:1 port
 * of Python's own core contract suite; this file proves things true only
 * because of how JavaScript itself works, so it stays separate from that
 * mirror rather than diluting it.
 */

describe("structural typing", () => {
  it("a plain object of the right shape is a Rule, with nothing declared", async () => {
    // No class, no `implements`, no registration -- shape alone is enough.
    // Python's Protocol gives the same guarantee; a statically-typed
    // language with nominal interfaces (C#, Dart) cannot.
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

describe("UnknownLookupError", () => {
  it("is catchable by type and inspectable by field", async () => {
    // Python raises the built-in KeyError, C# the built-in
    // KeyNotFoundException, Dart the built-in StateError -- JavaScript has
    // no built-in equivalent, so the package exports its own. This proves
    // its shape, which the parity files only assert by `instanceof`.
    const engine = new RulesEngine([pass("a", "g1")]);

    await assert.rejects(
      () => engine.runNamed("nope", {}),
      (err) => {
        assert.ok(err instanceof UnknownLookupError);
        assert.ok(err instanceof Error);
        assert.equal(err.name, "UnknownLookupError");
        assert.equal(err.kind, "rule");
        assert.equal(err.key, "nope");
        return true;
      },
    );

    await assert.rejects(
      () => engine.runGroup("no-such-group", {}),
      (err) => {
        assert.equal(err.kind, "group");
        assert.equal(err.key, "no-such-group");
        return true;
      },
    );
  });
});

describe("console.log / util.inspect representation", () => {
  // RuleResult/RunResult are plain object literals (TS interfaces have no
  // runtime shape to attach a method to), so only rule and engine types get
  // this -- see rule.test.js/engine.test.js for their ordinary behavior.

  it("FunctionRule shows its name", () => {
    const rule = new FunctionRule("over_18", async () => ({ ruleName: "over_18", passed: true }));
    assert.equal(rule.toString(), 'FunctionRule "over_18"');
    assert.equal(inspect(rule), 'FunctionRule "over_18"');
  });

  it("FunctionRule shows its group when present", () => {
    const rule = new FunctionRule("over_18", async () => ({ ruleName: "over_18", passed: true }), "age");
    assert.equal(inspect(rule), 'FunctionRule "over_18" (age)');
  });

  it("AndRule shows its name and sub-rule count", () => {
    const rule = new AndRule("all", [pass("a"), pass("b")]);
    assert.equal(rule.toString(), 'AndRule "all" — 2 sub-rule(s)');
    assert.equal(inspect(rule), 'AndRule "all" — 2 sub-rule(s)');
  });

  it("AndRule shows its group when present", () => {
    const rule = new AndRule("all", [pass("a")], "checkout");
    assert.equal(inspect(rule), 'AndRule "all" (checkout) — 1 sub-rule(s)');
  });

  it("OrRule shows its name and sub-rule count", () => {
    const rule = new OrRule("any", [fail("a"), pass("b")]);
    assert.equal(rule.toString(), 'OrRule "any" — 2 sub-rule(s)');
    assert.equal(inspect(rule), 'OrRule "any" — 2 sub-rule(s)');
  });

  it("RulesEngine shows its rule and group counts", () => {
    const engine = new RulesEngine([pass("a", "g1"), pass("b", "g2"), pass("c")]);
    assert.equal(engine.toString(), "RulesEngine — 3 rule(s), 2 group(s)");
    assert.equal(inspect(engine), "RulesEngine — 3 rule(s), 2 group(s)");
  });
});
