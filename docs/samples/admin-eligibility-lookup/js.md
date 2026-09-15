<!-- Title: Sample — Admin Eligibility Lookup (JS/TS) -->
# Sample: Admin Eligibility Lookup

> The problem, the design, and what a solution must demonstrate are
> language-agnostic and live in [`README.md`](README.md) — read that
> first. This page is the JS/TS implementation of it.

## The naive way (and why it breaks down)

Before reaching for a rule engine at all, the obvious first
implementation is a plain object of configured checks and a lookup
function — no `verdict-rules` in sight yet:

```ts
const ELIGIBILITY_CHECKS: Record<string, Array<[string, unknown]>> = {
  gold_tier: [["spend", 1000]],
  beta_feature: [], // not filled in yet
};

async function checkEligibility(checkName: string, customer: Record<string, unknown>): Promise<boolean> {
  const conditions = ELIGIBILITY_CHECKS[checkName];
  if (conditions === undefined) {
    return false; // "not eligible" either way
  }

  return conditions.every(([field, expected]) => customer[field] === expected);
}
```

A typo and a genuine rejection render identically here, and
`Array.prototype.every` over an empty array is `true` — the same
vacuous truth every language's `all()`/`every()` gives — so a check the
team hasn't finished configuring yet silently passes. See the spec for
the rest of what this shape gets wrong.

## The `verdict-rules` way

```ts
import { AndRule, FunctionRule, RulesEngine, type Rule, type Context, type RuleResult } from "verdict-rules";

// "eligible"/"not_eligible" come from a check that exists and actually ran.
// "unknown_check" and "not_configured" are both absence-shaped, and kept
// distinct from each other and from a genuine verdict so a support agent
// never mistakes "we don't know" for "we checked and the answer is no".
type CheckStatus = "eligible" | "not_eligible" | "unknown_check" | "not_configured";

interface LookupResult {
  status: CheckStatus;
  detail: string;
}

interface Condition {
  field: string;
  expected: unknown;
}

function conditionPredicate(field: string, expected: unknown) {
  return async (context: Context): Promise<RuleResult> => {
    const actual = context[field];
    return {
      ruleName: field,
      passed: actual === expected,
      detail: `${field}=${JSON.stringify(actual)}, needs ${JSON.stringify(expected)}`,
    };
  };
}

/**
 * One configured check, plus whether it currently has zero conditions.
 *
 * An empty `conditions` array still produces a valid `AndRule` -- it
 * just vacuously passes if ever evaluated directly. `EligibilityLookup`
 * intercepts that case before evaluation (see `check` below), so the
 * vacuous pass never reaches the caller as a real "eligible".
 */
function buildCheck(name: string, conditions: Condition[]): [Rule, boolean] {
  const conditionRules = conditions.map(
    (c, i) => new FunctionRule(`${name}[${i}]`, conditionPredicate(c.field, c.expected)),
  );
  return [new AndRule(name, conditionRules), conditions.length === 0];
}

/**
 * Looks up one named eligibility check by name, typed fresh each time.
 *
 * `configuredChecks` mirrors whatever an admin settings screen already
 * holds: one entry per check, each a list of condition objects. A check
 * with an empty list is a real, valid state a row can be in while a
 * team is still filling it in -- not an error.
 */
class EligibilityLookup {
  readonly #engine: RulesEngine;
  readonly #unconfigured = new Set<string>();

  constructor(configuredChecks: Record<string, Condition[]>) {
    const rules: Rule[] = [];
    for (const [name, conditions] of Object.entries(configuredChecks)) {
      const [rule, isEmpty] = buildCheck(name, conditions);
      rules.push(rule);
      if (isEmpty) this.#unconfigured.add(name);
    }
    this.#engine = new RulesEngine(rules);
  }

  /** Look up and run one named check. Never throws on a bad `name` --
   * that is the entire point of this class existing between the raw
   * engine and the screen that renders its result. */
  async check(name: string, customer: Context): Promise<LookupResult> {
    if (this.#unconfigured.has(name)) {
      return { status: "not_configured", detail: `'${name}' has no conditions configured yet` };
    }

    const result = await this.#engine.tryRunNamed(name, customer);
    if (result === undefined) {
      return { status: "unknown_check", detail: `no eligibility check named '${name}' exists` };
    }

    return { status: result.passed ? "eligible" : "not_eligible", detail: result.detail ?? "" };
  }
}
```

Run against a small configuration — one real check, one the team hasn't
finished, and one lookup with a typo:

```ts
const lookup = new EligibilityLookup({
  gold_tier: [{ field: "spend", expected: 1000 }],
  beta_feature: [], // not filled in yet
});

await lookup.check("gold_tier", { spend: 1000 });
// { status: 'eligible', detail: '' }

await lookup.check("gold_tier", { spend: 5 });
// { status: 'not_eligible', detail: "'gold_tier[0]' failed: spend=5, needs 1000" }

await lookup.check("beta_feature", { spend: 1000 });
// { status: 'not_configured', detail: "'beta_feature' has no conditions configured yet" }

await lookup.check("gold_teir", { spend: 1000 }); // typo
// { status: 'unknown_check', detail: "no eligibility check named 'gold_teir' exists" }
```

The screen can still show "not eligible" for the last two — the page
keeps working, exactly as the naive version intended — but `status` is
what tells a support agent *which* of the four things actually
happened, rather than one boolean standing in for all of them.

### Why not just wrap `runNamed` in a `try`/`catch`?

`tryRunNamed` and `runNamed` share one lookup path — the strict form is
a two-line assertion on top of the lenient one, not a second
implementation. Reaching for `tryRunNamed` directly says "absence is
expected here and I have an answer for it," which is true on this
screen; wrapping the strict form in `try`/`catch` says the same thing
by accident, and reads as "I expect this to throw and I'm suppressing
it" to the next person editing this file. The behavior is identical
either way — the difference is only which one tells the truth about why.

## Related

- [`README.md`](README.md) —
  the language-agnostic spec this page implements.
- [`../../extending/absence-vs-failure/`](../../extending/absence-vs-failure/README.md) —
  the general absence-vs-emptiness guidance this sample is one concrete
  instance of.
- [`data-driven-rule-sets/js.md`](../data-driven-rule-sets/js.md) — building the
  `Rule` objects themselves from stored configuration, the same pattern
  `EligibilityLookup` uses to turn each check's conditions into an
  `AndRule`.
