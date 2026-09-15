<!-- Title: Verdict Quickstart (JS/TS) -->
# Verdict — Quickstart

> The five names you need, and one complete example using all of them.
> See [`../README.md`](../README.md) for this package's own
> `npm install`/first-rule quickstart, the top-level
> [`../../../../README.md`](../../../../README.md) for what Verdict is in
> narrative form, and
> [`../../../../docs/architecture/`](../../../../docs/architecture/README.md)
> for the full design reasoning — this doc is just "how do I start."

## Core concepts

- **`Rule`** — anything with a `name`, an optional `group`, and an
  `evaluate(context: Context): Promise<RuleResult>` method. A plain
  TypeScript `interface`: any object of the right shape already *is* a
  `Rule`, with no `implements`, no base class, no registration — the
  same property Python's `Protocol` gives.
- **`FunctionRule`** — wraps a plain async predicate as a `Rule`. The
  common case: most rules are "run this function against the context."
- **`AndRule`** / **`OrRule`** — composite rules that combine other
  rules, short-circuiting the same way a boolean `&&`/`||` expression
  would (`AndRule` stops at the first failure, `OrRule` stops at the
  first pass).
- **`RulesEngine`** — holds a set of rules and runs them three ways:
  `runAll` (every rule, full diagnostic picture — deliberately does
  **not** short-circuit), `runNamed` (one specific rule by name),
  `runGroup` (every rule sharing a group label). An unknown name or
  label throws `UnknownLookupError`; `tryRunNamed`/`tryRunGroup` return
  `undefined` instead, for callers whose own domain has an answer for
  absence — see
  [`../../../../docs/extending/absence-vs-failure/`](../../../../docs/extending/absence-vs-failure/README.md).
- **`RuleResult`** / **`RunResult`** — plain, readonly interfaces, not
  classes — construct them as object literals. `RuleResult.data` is a
  fully opaque slot for a caller's own domain object to ride through
  evaluation — Verdict never reads or depends on its shape.

## One complete example

```ts
import { AndRule, FunctionRule, RulesEngine, type Context, type RuleResult } from "verdict-rules";

async function underDailyLimit(context: Context): Promise<RuleResult> {
  const spentToday = context.spent_today as number;
  const limit = context.daily_limit as number;
  return {
    ruleName: "under_daily_limit",
    passed: spentToday < limit,
    detail: `${spentToday} of ${limit}`,
  };
}

async function accountInGoodStanding(context: Context): Promise<RuleResult> {
  return { ruleName: "account_in_good_standing", passed: context.account_status === "active" };
}

const canProceed = new AndRule("can_proceed", [
  new FunctionRule("under_daily_limit", underDailyLimit),
  new FunctionRule("account_in_good_standing", accountInGoodStanding),
]);

const engine = new RulesEngine([canProceed]);
const result = await engine.runNamed("can_proceed", {
  spent_today: 42,
  daily_limit: 100,
  account_status: "active",
});
console.log(result.passed); // true
```

```mermaid
sequenceDiagram
    participant Caller as 📞 (top-level await)
    participant Engine as ⚙️ RulesEngine
    participant Combo as 🔀 AndRule<br/>can_proceed
    participant R1 as ✅ under_daily_limit
    participant R2 as ✅ account_in_good_standing

    Caller->>Engine: runNamed("can_proceed", context)
    Engine->>Combo: evaluate(context)
    Combo->>R1: evaluate(context)
    R1-->>Combo: { passed: true }
    Combo->>R2: evaluate(context)
    R2-->>Combo: { passed: true }
    Note over Combo: Both sub-rules passed —<br/>AndRule itself passes
    Combo-->>Engine: { passed: true }
    Engine-->>Caller: { passed: true }
```

> **Reading the Sequence**:
>
> 1. **The engine looks up `"can_proceed"` by name** — `runNamed` is
>    exactly one `Map` lookup plus one `evaluate()` call on whatever it
>    finds.
> 2. **`AndRule` evaluates its two sub-rules in order** — the plain
>    field comparison, then the account-status check — stopping at the
>    first failure if there is one (neither fails here, so both run).
> 3. **The composite's own result is what the engine hands back** — the
>    caller only sees one `RuleResult`, `passed: true`; the two
>    sub-rules' own results live nested in that one result's `data`, not
>    flattened into anything the caller has to unpack for this simple
>    case.

## Next: build rules from your own configuration, not just hard-coded ones

Because rules are just objects, they're straightforward to build up at
runtime from whatever configuration a caller already has, rather than
hand-writing one `FunctionRule` per case — see
[`../../../../docs/extending/data-driven-rule-construction/`](../../../../docs/extending/data-driven-rule-construction/README.md)
for the scenario and
[`../../../../docs/samples/data-driven-rule-sets/js.md`](../../../../docs/samples/data-driven-rule-sets/js.md)
for a fuller worked version of the same pattern.

## Related docs

- [`../../../../README.md`](../../../../README.md) — the narrative front door.
- [`../../../../docs/architecture/`](../../../../docs/architecture/README.md) — the full
  design reasoning.
- [`../../../../docs/extending/`](../../../../docs/extending/README.md) — building on top
  of this package from your own code.
- [`../../../../docs/testing/`](../../../../docs/testing/README.md) — `npm run build
  && npm test`, and what a test here actually needs to prove.
- [`../../../../docs/samples/`](../../../../docs/samples/README.md) — more worked examples.
