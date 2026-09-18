<!-- Title: Sample — Loyalty Tier Promotion (JS/TS) -->
# Sample: Loyalty Tier Promotion

> The problem, the design, and what a solution must demonstrate are
> language-agnostic and live in [`README.md`](README.md) — read that
> first. This page is the JS/TS implementation of it.

## The naive way (and why it breaks down)

The obvious first implementation is *two* functions, one for the
customer-facing checklist and one for the actual yes/no decision, each
with the four criteria's thresholds written inline:

```ts
interface Customer {
  trailing12moSpend: number;
  trailing12moOrders: number;
  returnRate: number;
  accountStatus: string;
}

async function goldChecklist(customer: Customer): Promise<Record<string, boolean>> {
  return {
    spend: customer.trailing12moSpend >= 5000,
    orders: customer.trailing12moOrders >= 15,
    returns: customer.returnRate <= 0.05,
    standing: customer.accountStatus === "active",
  };
}

async function isEligibleForGold(customer: Customer): Promise<boolean> {
  return (
    customer.trailing12moSpend >= 5000 &&
    customer.trailing12moOrders >= 20 &&
    customer.returnRate <= 0.05 &&
    customer.accountStatus === "active"
  );
}
```

`goldChecklist` promotes at 15 orders; `isEligibleForGold` at 20 —
visible by reading the two functions side by side, not a hypothetical
future drift. See the spec for the rest of what this shape gets wrong.

## The `verdict-rules` way

A typed context, not a dict — each threshold has exactly one
definition, read by name from every rule that needs it. That's what
makes the naive version's drift structurally impossible here, not
merely avoided by discipline:

```ts
import { FunctionRule, RulesEngine, type RuleResult, type RunResult } from "verdict-rules";

interface LoyaltyContext {
  trailing12moSpend: number;
  goldSpendThreshold: number;
  trailing12moOrders: number;
  goldOrderThreshold: number;
  returnRate: number;
  goldMaxReturnRate: number;
  accountStatus: string;
}

async function meetsSpendThreshold(context: LoyaltyContext): Promise<RuleResult> {
  const passed = context.trailing12moSpend >= context.goldSpendThreshold;
  return { ruleName: "meets_spend_threshold", passed };
}

async function meetsOrderCount(context: LoyaltyContext): Promise<RuleResult> {
  const passed = context.trailing12moOrders >= context.goldOrderThreshold;
  return { ruleName: "meets_order_count", passed };
}

async function returnRateBelowMax(context: LoyaltyContext): Promise<RuleResult> {
  const passed = context.returnRate <= context.goldMaxReturnRate;
  return { ruleName: "return_rate_below_max", passed };
}

async function accountInGoodStanding(context: LoyaltyContext): Promise<RuleResult> {
  return { ruleName: "account_in_good_standing", passed: context.accountStatus === "active" };
}

// Registered as four independent named rules on one engine -- not nested
// in an AndRule -- precisely so runAll() reports every criterion's own
// outcome, with no short-circuiting hiding a later criterion's result.
// RulesEngine<LoyaltyContext> documents that every rule here shares this
// one context type, the same way FunctionRule's own type argument does.
const engine = new RulesEngine<LoyaltyContext>([
  new FunctionRule("meets_spend_threshold", meetsSpendThreshold),
  new FunctionRule("meets_order_count", meetsOrderCount),
  new FunctionRule("return_rate_below_max", returnRateBelowMax),
  new FunctionRule("account_in_good_standing", accountInGoodStanding),
]);

async function promotionChecklist(context: LoyaltyContext): Promise<RunResult> {
  const result = await engine.runAll(context);
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
const customer: LoyaltyContext = {
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
