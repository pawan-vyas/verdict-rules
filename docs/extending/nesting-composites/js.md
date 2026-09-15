<!-- Title: Extending — Nesting Composites Arbitrarily (JS/TS) -->
# Nesting composites arbitrarily: JS/TS

> The concept and the diagram are in [`README.md`](README.md) — read
> that first. This page is the concrete JS/TS code.

```ts
import { AndRule, FunctionRule, OrRule, type Context, type RuleResult } from "verdict-rules";

async function isActiveAccount(context: Context): Promise<RuleResult> {
  return { ruleName: "is_active_account", passed: context.accountStatus === "active" };
}

async function isPremiumMember(context: Context): Promise<RuleResult> {
  return { ruleName: "is_premium_member", passed: context.isPremiumMember as boolean };
}

async function hasPromoCode(context: Context): Promise<RuleResult> {
  return { ruleName: "has_promo_code", passed: Boolean(context.promoCode) };
}

async function meetsSpendThreshold(context: Context): Promise<RuleResult> {
  return {
    ruleName: "meets_spend_threshold",
    passed: (context.spend as number) >= (context.spendThreshold as number),
  };
}

// Nesting doesn't care what built its sub-rules -- each of the four leaves
// here is a plain FunctionRule, but any Rule (a custom shape, another
// composite) would compose exactly the same way.
const qualifies = new AndRule("qualifies", [
  new FunctionRule("is_active_account", isActiveAccount),
  new OrRule("has_a_valid_reason", [
    new FunctionRule("is_premium_member", isPremiumMember),
    new FunctionRule("has_promo_code", hasPromoCode),
    new FunctionRule("meets_spend_threshold", meetsSpendThreshold),
  ]),
]);
```

```ts
const context: Context = {
  accountStatus: "active",
  isPremiumMember: false,
  promoCode: "SAVE10",
  spend: 20,
  spendThreshold: 100,
};
const result = await qualifies.evaluate(context);
result.passed;
// true
result.data;
// [ { ruleName: 'is_active_account', passed: true },
//   { ruleName: 'has_a_valid_reason', passed: true,
//     data: [ { ruleName: 'is_premium_member', passed: false },
//             { ruleName: 'has_promo_code', passed: true } ] } ]
// meetsSpendThreshold never ran -- has_a_valid_reason short-circuited
// once has_promo_code passed, exactly as a plain, unnested OrRule would
```

## Related

- [`README.md`](README.md) — the language-agnostic scenario this page
  implements.
