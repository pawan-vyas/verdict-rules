import assert from "node:assert/strict";
import { describe, it } from "node:test";

import { AndRule, NotRule, OrRule, RuleResult, RulesEngine, RunResult } from "../dist/index.js";
import { fail, pass } from "./helpers.js";

/**
 * A result has to be able to leave the process -- into a log line, an audit
 * record, an HTTP response.
 *
 * Two independent shapes make that fail, and both are fixed by the same
 * principle: store a tree, derive everything else.
 *
 * `leaves`/`failingLeaves` are prototype getters rather than stored arrays
 * because a leaf's own leaves list is `[this]` -- storing it put the result
 * inside itself, and `JSON.stringify` threw "Converting circular structure
 * to JSON". `decidedBy` is derived from stored indices because storing the
 * child results themselves made the graph a DAG, and a tree-shaped encoder
 * expands a shared node once per path, doubling output per nesting level.
 */

function reviveResult(payload) {
  return new RuleResult(payload.ruleName, payload.passed, {
    detail: payload.detail,
    data: payload.data,
    subResults: payload.subResults.map(reviveResult),
    decidedByIndices: payload.decidedByIndices,
  });
}

describe("serialization", () => {
  it("a leaf result round-trips", () => {
    const leaf = new RuleResult("a", false, { detail: "too young", data: { age: 15 } });

    const restored = reviveResult(JSON.parse(JSON.stringify(leaf)));

    assert.equal(restored.ruleName, "a");
    assert.equal(restored.passed, false);
    assert.equal(restored.detail, "too young");
    assert.deepEqual(restored.data, { age: 15 });
    assert.deepEqual(restored.subResults, []);
  });

  it("a deep partly-failing tree round-trips intact", async () => {
    const a = new AndRule("a", [
      pass("a1"),
      new OrRule("a2", [fail("a2x"), pass("a2y")]),
      new NotRule("a3", pass("a3-inner")),
    ]);
    const result = await new AndRule("root", [a, pass("b")]).evaluate({});

    const restored = reviveResult(JSON.parse(JSON.stringify(result)));

    // root short-circuited on 'a', so 'b' never ran and must be absent --
    // not present-and-failing.
    assert.deepEqual(
      restored.subResults.map((r) => r.ruleName),
      ["a"],
    );
    // The two derived accessors are recomputed from the restored tree rather
    // than carried across, so agreeing proves nothing they depend on was
    // lost in transit.
    assert.deepEqual(
      restored.leaves.map((l) => l.ruleName),
      result.leaves.map((l) => l.ruleName),
    );
    assert.deepEqual(
      restored.failingLeaves.map((l) => l.ruleName),
      ["a3"],
    );
    assert.deepEqual(
      restored.decidedBy.map((r) => r.ruleName),
      result.decidedBy.map((r) => r.ruleName),
    );
  });

  it("the derived accessors are absent from the serialized form", async () => {
    const result = await new AndRule("root", [pass("a"), fail("b")]).evaluate({});

    const payload = JSON.parse(JSON.stringify(result));

    // `data` is unset here, and `JSON.stringify` omits an `undefined`-valued
    // property entirely rather than emitting null -- a real divergence from
    // Python's own `asdict`, which emits `"data": null`. The next assertion
    // covers the key appearing once something is actually in it.
    assert.deepEqual(Object.keys(payload).sort(), [
      "decidedByIndices",
      "detail",
      "passed",
      "ruleName",
      "subResults",
    ]);
    assert.ok("data" in JSON.parse(JSON.stringify(new RuleResult("a", true, { data: 1 }))));

    for (const derived of ["leaves", "failingLeaves", "decidedBy"]) {
      assert.ok(!(derived in payload), `${derived} should not be serialized`);
    }
  });

  it("a RunResult round-trips", async () => {
    const engine = new RulesEngine([pass("a"), fail("b")]);
    const run = await engine.runAll({});

    const payload = JSON.parse(JSON.stringify(run));

    assert.equal(payload.passed, false);
    assert.deepEqual(Object.keys(payload).sort(), ["passed", "results"]);
    const restored = new RunResult(payload.results.map(reviveResult));
    assert.deepEqual(
      restored.failingLeaves.map((l) => l.ruleName),
      ["b"],
    );
  });

  it("a result built from an array the caller then appended to still serializes", () => {
    // The early-define/late-init shape. Copying on construction severs the
    // alias, so the encoder walks a finite tree instead of throwing on a
    // circular structure.
    const kids = [];
    const result = new RuleResult("p", false, { subResults: kids });
    kids.push(result);

    assert.deepEqual(JSON.parse(JSON.stringify(result)).subResults, []);
  });

  it("serialized size grows with depth, not exponentially in it", async () => {
    // Forty levels of real nesting. With the deciding children stored as
    // objects rather than positions, each level doubled the output: depth 16
    // measured 13.6 MB and depth 40 was unencodable.
    let rule = fail("leaf");
    for (let level = 0; level < 40; level += 1) {
      rule = new AndRule(`level${level}`, [rule]);
    }

    const encoded = JSON.stringify(await rule.evaluate({}));

    assert.ok(encoded.length < 10_000, `expected linear growth, got ${encoded.length} chars`);
    assert.ok(encoded.includes('"leaf"'));
  });

  it("a cyclic payload in data fails at the encoder, not inside the package", () => {
    // `data` is opaque: the package never reads it and never promises it is
    // encodable. A caller putting a cycle of their own in there gets
    // JSON.stringify's own failure, not a package error and not a silently
    // dropped field.
    const cyclic = {};
    cyclic.self = cyclic;
    const result = new RuleResult("a", true, { data: cyclic });

    assert.throws(() => JSON.stringify(result), TypeError);
  });
});
