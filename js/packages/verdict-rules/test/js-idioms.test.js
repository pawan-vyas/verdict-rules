import assert from "node:assert/strict";
import { describe, it } from "node:test";

import { AndRule, RulesEngine, UnknownLookupError } from "../dist/index.js";
import { pass } from "./helpers.js";

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
