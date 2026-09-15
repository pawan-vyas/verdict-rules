<!-- Title: Extending — Wrapping A Predicate (JS/TS) -->
# Wrapping a predicate: JS/TS

> The concept and why it's the common case are in
> [`README.md`](README.md) — read that first. This page is the concrete
> JS/TS code.

```ts
import { FunctionRule, type Context, type RuleResult } from "verdict-rules";

async function cartMeetsMinimum(context: Context): Promise<RuleResult> {
  const total = context.cartTotal as number;
  const minimum = context.minimumForOffer as number;
  return {
    ruleName: "cart_meets_minimum",
    passed: total >= minimum,
    detail: `${total} vs minimum ${minimum}`,
  };
}

const rule = new FunctionRule("cart_meets_minimum", cartMeetsMinimum);
```

## Related

- [`README.md`](README.md) — the language-agnostic scenario this page
  implements.
- [`../new-rule-shape/js.md`](../new-rule-shape/js.md) — the
  next step up, for combination logic `FunctionRule` alone can't express.
