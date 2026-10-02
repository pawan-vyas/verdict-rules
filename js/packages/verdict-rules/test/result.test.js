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

  it("negation nested in a passing sibling is still the whole failure", async () => {
    // A nested negation pins the recursion more precisely than a
    // top-level one: all(a, not(b)) with both a and b passing -- not(b)
    // fails (b passed), the outer composite's failingLeaves has to be
    // exactly [not(b)], not empty (every actual leaf below it passed)
    // and not wrong because not(b)'s own self-as-leaf result was the
    // only failure among siblings rather than the sole child.
    const notB = new NotRule("not1", leaf("b", true));
    const rule = new AndRule("and1", [leaf("a", true), notB]);
    const result = await rule.evaluate({});
    const notBResult = result.subResults[1];
    assert.deepEqual(result.failingLeaves, [notBResult]);
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

describe("mixed composite tree", () => {
  // A genuinely wide, deep tree mixing every rule kind -- AndRule,
  // OrRule, NotRule, and a bare FunctionRule -- at multiple levels on
  // multiple branches, to prove leaves/failingLeaves report correctly
  // at a scale none of the other tests here exercise.
  //
  //   root = AndRule("root", [a, b, c])
  //     a = AndRule("a", [a1, a2, a3])
  //       a1 = FunctionRule (leaf)
  //       a2 = OrRule("a2", [a2x (fails), a2y (passes)])
  //       a3 = NotRule("a3", a3Inner)
  //     b = FunctionRule (leaf)
  //     c = OrRule("c", [c1, c2])
  //       c1 = AndRule("c1", [c1x (fails), c1y])
  //       c2 = NotRule("c2", c2Inner)

  it("leaves flatten across every rule kind even when everything passes", async () => {
    const a1 = leaf("a1", true);
    const a2 = new OrRule("a2", [leaf("a2x", false), leaf("a2y", true)]);
    const a3 = new NotRule("a3", leaf("a3-inner", false)); // inner fails -> passes
    const a = new AndRule("a", [a1, a2, a3]);

    const b = leaf("b", true);

    const c1 = new AndRule("c1", [leaf("c1x", false), leaf("c1y", true)]); // short-circuits, fails
    const c2 = new NotRule("c2", leaf("c2-inner", false)); // inner fails -> passes
    const c = new OrRule("c", [c1, c2]);

    const root = new AndRule("root", [a, b, c]);
    const result = await root.evaluate({});

    assert.equal(result.passed, true);
    // c1y never ran at all (c1 short-circuited on c1x) -- absent, not
    // present-and-passing.
    assert.deepEqual(
      result.leaves.map((l) => l.ruleName),
      ["a1", "a2x", "a2y", "a3-inner", "b", "c1x", "c2-inner"],
    );
    // A passing root has no failing leaves, full stop -- even though c1
    // failed internally three branches deep, on the way to c's own pass
    // via c2.
    assert.deepEqual(result.failingLeaves, []);

    // decidedBy, traced at every composite node in this tree: root/a both
    // fully pass (exhausted without an early stop-on-false trigger), so
    // every evaluated child explains them; a2/c each stop early on their
    // one decisive sub-result; a3/c2 (NotRule) are unconditionally [inner].
    assert.deepEqual(
      result.decidedBy.map((r) => r.ruleName),
      ["a", "b", "c"],
    );
    const [aResult, , cResult] = result.subResults;
    assert.deepEqual(
      aResult.decidedBy.map((r) => r.ruleName),
      ["a1", "a2", "a3"],
    );
    const [, a2Result, a3Result] = aResult.subResults;
    assert.deepEqual(
      a2Result.decidedBy.map((r) => r.ruleName),
      ["a2y"],
    );
    assert.deepEqual(
      a3Result.decidedBy.map((r) => r.ruleName),
      ["a3-inner"],
    );
    assert.deepEqual(
      cResult.decidedBy.map((r) => r.ruleName),
      ["c2"],
    );
    const [c1Result, c2Result] = cResult.subResults;
    assert.deepEqual(
      c1Result.decidedBy.map((r) => r.ruleName),
      ["c1x"],
    );
    assert.deepEqual(
      c2Result.decidedBy.map((r) => r.ruleName),
      ["c2-inner"],
    );
  });

  it("failingLeaves pinpoints the exact failure through multiple levels", async () => {
    const a1 = leaf("a1", true);
    const a2 = new OrRule("a2", [leaf("a2x", false), leaf("a2y", true)]);
    const a3 = new NotRule("a3", leaf("a3-inner", true)); // inner passes -> fails
    const a = new AndRule("a", [a1, a2, a3]);

    // b/c are never evaluated at all -- root short-circuits on 'a'.
    const b = leaf("b", true);
    const c = new OrRule("c", [
      new AndRule("c1", [leaf("c1x", false), leaf("c1y", true)]),
      new NotRule("c2", leaf("c2-inner", false)),
    ]);

    const root = new AndRule("root", [a, b, c]);
    const result = await root.evaluate({});

    assert.equal(result.passed, false);
    assert.deepEqual(result.subResults.map((r) => r.ruleName), ["a"]); // b, c never ran
    const a3Result = result.subResults[0].subResults[2];
    assert.equal(a3Result.ruleName, "a3");
    // The one true failure, three levels deep (root -> a -> a3), with
    // a1/a2 (passing siblings of a3) contributing nothing and b/c (never
    // evaluated) not appearing at all.
    assert.deepEqual(result.failingLeaves, [a3Result]);

    // decidedBy tells the same story one level at a time: root short-
    // circuited on 'a' (its one sub-result, by construction here, so this
    // doesn't exercise the last-item subtlety -- see composition.test.js
    // for that), and 'a' itself short-circuited on its own last-evaluated
    // child, a3 -- the AndRule.failing-on-its-last-item shape, nested.
    assert.deepEqual(
      result.decidedBy.map((r) => r.ruleName),
      ["a"],
    );
    const aResult = result.subResults[0];
    assert.deepEqual(
      aResult.decidedBy.map((r) => r.ruleName),
      ["a3"],
    );
    assert.deepEqual(
      a3Result.decidedBy.map((r) => r.ruleName),
      ["a3-inner"],
    );
  });

  it("NotRule wrapping a short-circuited composite with an earlier passing sibling", async () => {
    // Stacks everything that could plausibly go wrong at once: an
    // earlier AndRule sibling that passes despite an internal failure
    // (ordering independence -- the real failure comes later), NotRule
    // wrapping a genuine OrRule rather than a bare leaf, that wrapped
    // OrRule short-circuiting internally, and the outer AndRule *also*
    // short-circuiting -- two independent prunings at different depths
    // in the same tree.
    const innerOr = new OrRule("inner_or", [leaf("w", false), leaf("x", true)]); // passes

    const innerOrForNot = new OrRule("inner_or_for_not", [leaf("p", true), leaf("q", true)]); // short-circuits
    const notResult = new NotRule("not1", innerOrForNot); // passed -> fails

    const z = leaf("z", true); // never reached

    const root = new AndRule("root", [innerOr, notResult, z]);
    const result = await root.evaluate({});

    assert.equal(result.passed, false);
    assert.deepEqual(result.leaves.map((l) => l.ruleName), ["w", "x", "p"]);
    assert.deepEqual(result.failingLeaves.map((l) => l.ruleName), ["not1"]);

    // root short-circuits on notResult (its last-evaluated sub-result,
    // z never runs) -- decidedBy is just [not1], not [innerOr, not1].
    assert.deepEqual(
      result.decidedBy.map((r) => r.ruleName),
      ["not1"],
    );
    const notResultValue = result.subResults[1];
    // NotRule's own decidedBy is unconditionally [inner], here the
    // short-circuited OrRule result itself -- not flattened/recursed into
    // its own decisive sub-result ('p'). This is exactly the scope
    // boundary RuleResult.decidedBy's own doc comment warns about: do not
    // expect decidedBy[0].decidedBy[0]... to land on the same answer as
    // failingLeaves.
    assert.deepEqual(
      notResultValue.decidedBy.map((r) => r.ruleName),
      ["inner_or_for_not"],
    );
    const innerOrForNotResult = notResultValue.subResults[0];
    assert.deepEqual(
      innerOrForNotResult.decidedBy.map((r) => r.ruleName),
      ["p"],
    );
  });

  it("OrRule all-fail interleaves real leaves and Not fallbacks in order", async () => {
    const rule = new OrRule("root", [
      leaf("a", false),
      new NotRule("notB", leaf("b", true)),
      leaf("c", false),
      new NotRule("notD", leaf("d", true)),
    ]);
    const result = await rule.evaluate({});

    assert.equal(result.passed, false);
    const failing = result.failingLeaves;
    assert.deepEqual(failing.map((l) => l.ruleName), ["a", "notB", "c", "notD"]);
    assert.equal(failing[0], result.subResults[0]);
    assert.equal(failing[1], result.subResults[1]);

    // All-fail OrRule: exhausted without ever finding stopOn (true), so
    // decidedBy is every evaluated sub-result, same four as failingLeaves
    // here -- not because decidedBy and failingLeaves are the same
    // question in general (NotRule proves they aren't), but because every
    // one of these four sub-results happens to be itself a failing leaf.
    assert.deepEqual(
      result.decidedBy.map((r) => r.ruleName),
      ["a", "notB", "c", "notD"],
    );
    const notBResult = result.subResults[1];
    assert.deepEqual(
      notBResult.decidedBy.map((r) => r.ruleName),
      ["b"],
    );
  });

  it("a genuinely vacuous composite nested inside a larger failing tree", async () => {
    const innerOr = new OrRule("inner_or", [leaf("a3", false), leaf("b3", true)]);
    const emptyOr = new OrRule("empty_or", []);
    const z = leaf("z", true);

    const root = new AndRule("root", [innerOr, emptyOr, z]);
    const result = await root.evaluate({});

    assert.equal(result.passed, false);
    assert.deepEqual(result.leaves.map((l) => l.ruleName), ["a3", "b3", "empty_or"]);
    assert.deepEqual(result.failingLeaves.map((l) => l.ruleName), ["empty_or"]);

    // root short-circuits on empty_or's own vacuous failure -- z never
    // runs, so decidedBy is just [empty_or].
    assert.deepEqual(
      result.decidedBy.map((r) => r.ruleName),
      ["empty_or"],
    );
    const emptyOrResult = result.subResults[1];
    assert.equal(emptyOrResult.ruleName, "empty_or");
    // The vacuous composite's own decidedBy is empty -- nothing decided
    // it, same as its own empty subResults.
    assert.deepEqual(emptyOrResult.decidedBy, []);
  });
});
