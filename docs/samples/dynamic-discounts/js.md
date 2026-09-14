<!-- Title: Sample — Dynamic Discount Eligibility (JS/TS) -->
# Sample: Dynamic Discount Eligibility

> The problem, the design, and what a solution must demonstrate are
> language-agnostic and live in [`README.md`](README.md) — read that
> first. This page is the JS/TS implementation of it.

## The naive way (and why it breaks down)

The obvious first implementation is a nested `if` chain, checked
straight against the cart, with the current campaign's numbers baked
directly into the code:

```ts
interface Cart {
  total: number;
  region: string;
  isFirstPurchase: boolean;
}

async function qualifiesForPromo(cart: Cart): Promise<boolean> {
  if (cart.total >= 100) {
    if (["US", "CA"].includes(cart.region)) {
      if (cart.isFirstPurchase) {
        return true;
      }
    }
  }
  return false;
}
```

This is fine for exactly as long as the campaign's numbers never
change — see the spec for the three specific ways it stops being fine.

## The `verdict-rules` way

```ts
import { AndRule, FunctionRule, RulesEngine, type Context, type RuleResult, type RunResult } from "verdict-rules";

async function cartMeetsMinimum(context: Context): Promise<RuleResult> {
  const total = context.cartTotal as number;
  const minimum = context.promoMinimum as number;
  return {
    ruleName: "cart_meets_minimum",
    passed: total >= minimum,
    detail: `cartTotal=${total}, needs >= ${minimum}`,
  };
}

async function isEligibleRegion(context: Context): Promise<RuleResult> {
  const region = context.region as string;
  const eligible = context.eligibleRegions as Set<string>;
  return {
    ruleName: "is_eligible_region",
    passed: eligible.has(region),
    detail: `region=${JSON.stringify(region)} not in ${JSON.stringify([...eligible].sort())}`,
  };
}

async function isFirstPurchase(context: Context): Promise<RuleResult> {
  return { ruleName: "is_first_purchase", passed: context.isFirstPurchase as boolean };
}

// Built once. The AndRule and the engine below both hold these same three
// objects -- nothing is declared twice, and nothing here needs to know in
// advance which of the two call sites below will use it.
const minimumRule = new FunctionRule("cart_meets_minimum", cartMeetsMinimum);
const regionRule = new FunctionRule("is_eligible_region", isEligibleRegion);
const firstPurchaseRule = new FunctionRule("is_first_purchase", isFirstPurchase);

const qualifiesForPromo = new AndRule("qualifies_for_promo", [minimumRule, regionRule, firstPurchaseRule]);
const engine = new RulesEngine([minimumRule, regionRule, firstPurchaseRule]);

/** The checkout gate: the cheapest possible yes/no, stopping at the
 * first failing condition. Nothing past this point needs to know
 * *which* condition failed -- only whether the discount applies. */
async function checkCartFast(cart: Context, campaign: Context): Promise<boolean> {
  const context = { ...cart, ...campaign };
  return (await qualifiesForPromo.evaluate(context)).passed;
}

/** The support-facing view: every condition, always -- even if more
 * than one failed at once. Never the fast path's job, so it doesn't
 * share the fast path's short-circuiting.
 *
 * `campaign` (e.g. `{ promoMinimum: 100, eligibleRegions: new Set(["US", "CA"]) }`)
 * is whatever the checkout code already reads the active campaign's
 * configured values from -- nothing here cares where it came from. */
async function whyNot(cart: Context, campaign: Context): Promise<RunResult> {
  const context = { ...cart, ...campaign };
  return engine.runAll(context);
  // result.passed is true only if every condition passed; result.results
  // is one entry per condition, always all three, regardless of how many
  // failed -- this is what a "why not?" screen actually needs.
}
```

Run against a qualifying cart, then one that fails on two conditions at
once:

```ts
const cart = { cartTotal: 120, region: "US", isFirstPurchase: true };
const campaign = { promoMinimum: 100, eligibleRegions: new Set(["US", "CA"]) };

await checkCartFast(cart, campaign);
// true

let result = await whyNot(cart, campaign);
result.passed;
// true -- every condition passed

const badCart = { cartTotal: 50, region: "MX", isFirstPurchase: true };
await checkCartFast(badCart, campaign);
// false -- stops at the first failure

result = await whyNot(badCart, campaign);
result.passed;
// false
result.results.map((r) => r.passed);
// [ false, false, true ] -- both failures visible, not just the first
```

Swapping the campaign's minimum from `100` to `75`, or adding `"MX"` to
`eligibleRegions`, is now a data change passed into either function —
no edit to `cartMeetsMinimum`/`isEligibleRegion`/`isFirstPurchase`,
the `AndRule`, or the engine.

## Related

- [`README.md`](README.md) —
  the language-agnostic spec this page implements.
- [`shipping-fee-waiver/js.md`](../shipping-fee-waiver/js.md) — the `OrRule`
  mirror image of this same idea (any one qualifying path, not all).
- [`data-driven-rule-sets/js.md`](../data-driven-rule-sets/js.md) — building
  the rules themselves from stored config, not just their values.
