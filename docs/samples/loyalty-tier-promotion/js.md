<!-- Title: Sample — Loyalty Tier Promotion (JS/TS) -->
# Sample: Loyalty Tier Promotion

> The problem, the design, and what a solution must demonstrate are
> language-agnostic and live in [`README.md`](README.md) — read that
> first. This page is the JS/TS implementation of it.

## The naive way (and why it breaks down)

The obvious first implementation ends up as *two* functions that have to
be kept in sync by hand — one for the yes/no decision, one for the
customer-facing checklist:

```ts
interface Customer {
  trailing12moSpend: number;
  goldSpendThreshold: number;
  trailing12moOrders: number;
  goldOrderThreshold: number;
  returnRate: number;
  goldMaxReturnRate: number;
  accountStatus: string;
}

async function goldChecklist(customer: Customer): Promise<Record<string, boolean>> {
  return {
    spend: customer.trailing12moSpend >= customer.goldSpendThreshold,
    orders: customer.trailing12moOrders >= customer.goldOrderThreshold,
    returns: customer.returnRate <= customer.goldMaxReturnRate,
    standing: customer.accountStatus === "active",
  };
}

async function isEligibleForGold(customer: Customer): Promise<boolean> {
  const checklist = await goldChecklist(customer);
  return Object.values(checklist).every(Boolean);
}
```

Nothing enforces the coupling between the two functions — adding a
fifth requirement to one without the other produces a promotion
decision the UI's own checklist can't explain. See the spec for the
rest of what this shape gets wrong.

## The `verdict-rules` way

```ts
import { FunctionRule, RulesEngine, type Context, type RuleResult, type RunResult } from "verdict-rules";

async function meetsSpendThreshold(context: Context): Promise<RuleResult> {
  const passed = (context.trailing12moSpend as number) >= (context.goldSpendThreshold as number);
  return { ruleName: "meets_spend_threshold", passed };
}

async function meetsOrderCount(context: Context): Promise<RuleResult> {
  const passed = (context.trailing12moOrders as number) >= (context.goldOrderThreshold as number);
  return { ruleName: "meets_order_count", passed };
}

async function returnRateBelowMax(context: Context): Promise<RuleResult> {
  const passed = (context.returnRate as number) <= (context.goldMaxReturnRate as number);
  return { ruleName: "return_rate_below_max", passed };
}

async function accountInGoodStanding(context: Context): Promise<RuleResult> {
  return { ruleName: "account_in_good_standing", passed: context.accountStatus === "active" };
}

// Registered as four independent named rules on one engine -- not nested
// in an AndRule -- precisely so runAll() reports every criterion's own
// outcome, with no short-circuiting hiding a later criterion's result.
const engine = new RulesEngine([
  new FunctionRule("meets_spend_threshold", meetsSpendThreshold),
  new FunctionRule("meets_order_count", meetsOrderCount),
  new FunctionRule("return_rate_below_max", returnRateBelowMax),
  new FunctionRule("account_in_good_standing", accountInGoodStanding),
]);

async function promotionChecklist(customerContext: Context): Promise<RunResult> {
  const result = await engine.runAll(customerContext);
  // result.passed is true only if all four passed -- the actual promotion decision.
  // result.results is one RuleResult per criterion, always all four -- the UI checklist.
  return result;
}
```

This is the one case where reaching for a bare `AndRule` and reaching
for the engine's `runAll()` produce genuinely different, both-correct
answers depending on what the caller actually needs — see
[`../../architecture/js.md`](../../architecture/js.md#three-ways-to-run-rules-concretely)
for the general rule of thumb.

A customer meeting three of the four criteria — the same case the
spec's own diagram shows:

```ts
const customer: Context = {
  trailing12moSpend: 6000,
  goldSpendThreshold: 5000,
  trailing12moOrders: 20,
  goldOrderThreshold: 15,
  returnRate: 0.08,
  goldMaxReturnRate: 0.05,
  accountStatus: "active",
};

const checklist = await promotionChecklist(customer);
checklist.passed;
// false -- not promoted, the return rate is over the limit
checklist.results.map((r) => [r.ruleName, r.passed]);
// [ [ 'meets_spend_threshold', true ], [ 'meets_order_count', true ],
//   [ 'return_rate_below_max', false ], [ 'account_in_good_standing', true ] ]
// all four report, not just the one that decided the outcome
```

## Related

- [`README.md`](README.md) —
  the language-agnostic spec this page implements.
- [`content-moderation-routing/js.md`](../content-moderation-routing/js.md) —
  another `run*` (this time `runGroup()`) example, for when a single
  engine needs to serve more than one independent decision.
