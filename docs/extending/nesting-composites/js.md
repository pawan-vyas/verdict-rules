<!-- Title: Extending — Nesting Composites Arbitrarily (JS/TS) -->
# Nesting composites arbitrarily: JS/TS

> The concept and the diagram are in [`README.md`](README.md) — read
> that first. This page is the concrete JS/TS code.

```ts
import { AndRule, FunctionRule, OrRule, type PredicateOutcome } from "verdict-rules";

interface AccountContext {
  accountStatus: string;
  isPremiumMember: boolean;
  promoCode: string | undefined;
  spend: number;
  spendThreshold: number;
}

async function isActiveAccount(context: AccountContext): Promise<PredicateOutcome> {
  return { passed: context.accountStatus === "active" };
}

async function isPremiumMember(context: AccountContext): Promise<PredicateOutcome> {
  return { passed: context.isPremiumMember };
}

async function hasPromoCode(context: AccountContext): Promise<PredicateOutcome> {
  return { passed: Boolean(context.promoCode) };
}

async function meetsSpendThreshold(context: AccountContext): Promise<PredicateOutcome> {
  return { passed: context.spend >= context.spendThreshold };
}

// Nesting doesn't care what built its sub-rules -- each of the four leaves
// here is a plain FunctionRule, but any Rule (a custom shape, another
// composite) would compose exactly the same way.
const qualifies = new AndRule<AccountContext>("qualifies", [
  new FunctionRule("is_active_account", isActiveAccount),
  new OrRule<AccountContext>("has_a_valid_reason", [
    new FunctionRule("is_premium_member", isPremiumMember),
    new FunctionRule("has_promo_code", hasPromoCode),
    new FunctionRule("meets_spend_threshold", meetsSpendThreshold),
  ]),
]);
```

A nested result is read the same way at every level — `subResults` holds
one level, and never the whole tree:

```ts
const context: AccountContext = {
  accountStatus: "active",
  isPremiumMember: false,
  promoCode: "SAVE10",
  spend: 20,
  spendThreshold: 100,
};
const result = await qualifies.evaluate(context);

result.passed;
// true

result.subResults.map((r) => r.ruleName);
// [ 'is_active_account', 'has_a_valid_reason' ]

const inner = result.subResults[1]!;
inner.subResults.map((r) => r.ruleName);
// [ 'is_premium_member', 'has_promo_code' ]
// meetsSpendThreshold never ran -- has_a_valid_reason short-circuited
// once has_promo_code passed, exactly as a plain, unnested OrRule would
```

Three views answer three different questions about the same tree:

```ts
result.leaves.map((r) => r.ruleName);
// [ 'is_active_account', 'is_premium_member', 'has_promo_code' ]
// fully recursive -- the terminal checks, however deep

result.failingLeaves;
// [] -- a passing result has none, even though is_premium_member failed
// on the way to the inner OrRule's pass

result.decidedBy.map((r) => r.ruleName);
// [ 'is_active_account', 'has_a_valid_reason' ]
// one level: an AndRule that had to evaluate everything is explained by
// everything

inner.decidedBy.map((r) => r.ruleName);
// [ 'has_promo_code' ]
// one level again, but an OrRule that stopped early is explained by just
// the sub-rule that stopped it
```

`data` is not part of this. It is an opaque slot for a caller's own
payload, never written to by a composite — a composite's children are in
`subResults`.

## Related

- [`README.md`](README.md) — the language-agnostic scenario this page
  implements.
