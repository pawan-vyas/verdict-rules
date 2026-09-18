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

The wrap itself doesn't care what the predicate's own context looks
like — a predicate already written against a typed context wraps
exactly the same way:

```ts
interface CartContext {
  cartTotal: number;
  minimumForOffer: number;
}

async function cartMeetsMinimumTyped(context: CartContext): Promise<RuleResult> {
  return {
    ruleName: "cart_meets_minimum",
    passed: context.cartTotal >= context.minimumForOffer,
    detail: `${context.cartTotal} vs minimum ${context.minimumForOffer}`,
  };
}

const typedRule = new FunctionRule<CartContext>("cart_meets_minimum", cartMeetsMinimumTyped);
```

## Related

- [`README.md`](README.md) — the language-agnostic scenario this page
  implements.
- [`../new-rule-shape/js.md`](../new-rule-shape/js.md) — the
  next step up, for combination logic `FunctionRule` alone can't express.
