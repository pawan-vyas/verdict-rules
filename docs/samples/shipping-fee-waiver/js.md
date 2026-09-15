<!-- Title: Sample — Shipping Fee Waiver (JS/TS) -->
# Sample: Shipping Fee Waiver

> The problem, the design, and what a solution must demonstrate are
> language-agnostic and live in [`README.md`](README.md) — read that
> first. This page is the JS/TS implementation of it.

## The naive way (and why it breaks down)

The obvious first implementation is a conditional ladder, one `if` per
qualifying path:

```ts
interface Order {
  promoCode?: string;
  isPremiumMember: boolean;
  total: number;
  freeShippingThreshold: number;
}

async function shipsFree(order: Order): Promise<boolean> {
  if (await validatePromoCode(order.promoCode)) {
    return true;
  }
  if (order.isPremiumMember) {
    return true;
  }
  if (order.total >= order.freeShippingThreshold) {
    return true;
  }
  return false;
}
```

Adding a fourth path means opening this function and inserting another
`if` among the ones already there, at risk to the branches already
correct and already shipped. Here that cost is concrete: the
promo-code check is an external service call, and it happens to run
first, so it runs on **every single order** — including a $500 order
from a non-member with no promo code, which qualifies on total alone
and never needed the network round trip at all. See the spec for the
rest of what this shape gets wrong.

## The `verdict-rules` way

```ts
import { FunctionRule, OrRule, type Context, type RuleResult } from "verdict-rules";

interface PromoCodeService {
  validate(code: string | undefined): Promise<boolean>;
}

async function orderTotalOverThreshold(context: Context): Promise<RuleResult> {
  const passed = (context.orderTotal as number) >= (context.freeShippingThreshold as number);
  return { ruleName: "order_total_over_threshold", passed };
}

async function hasPremiumMembership(context: Context): Promise<RuleResult> {
  return { ruleName: "has_premium_membership", passed: context.isPremiumMember as boolean };
}

async function hasValidPromoCode(context: Context): Promise<RuleResult> {
  // The expensive path: only reached if both cheaper checks above failed.
  const service = context.promoCodeService as PromoCodeService;
  const isValid = await service.validate(context.promoCode as string | undefined);
  return { ruleName: "has_valid_promo_code", passed: isValid };
}

const shipsFree = new OrRule("ships_free", [
  new FunctionRule("order_total_over_threshold", orderTotalOverThreshold),
  new FunctionRule("has_premium_membership", hasPremiumMembership),
  new FunctionRule("has_valid_promo_code", hasValidPromoCode), // cheapest-last, on purpose
]);
```

The comment on the last rule is the whole fix, made visible: cost order
is now a stated decision at the point it matters, not something the
next person to edit this file has to reconstruct from scratch.

Proving the skip, not just asserting it — a call-counting fake stands
in for the real service:

```ts
class FakePromoService implements PromoCodeService {
  calls = 0;
  async validate(code: string | undefined): Promise<boolean> {
    this.calls += 1;
    return code === "SAVE10";
  }
}

const promoService = new FakePromoService();
const order: Context = {
  orderTotal: 120,
  freeShippingThreshold: 50,
  isPremiumMember: false,
  promoCode: undefined,
  promoCodeService: promoService,
};

const result = await shipsFree.evaluate(order);
result.passed;
promoService.calls;
// true, 0 -- order_total alone already qualifies; the promo service is never called
```

## Related

- [`README.md`](README.md) —
  the language-agnostic spec this page implements.
- [`dynamic-discounts/js.md`](../dynamic-discounts/js.md) — the `AndRule`
  mirror image (every condition must pass, not just one).
- [`../../architecture/README.md`](../../architecture/README.md#execution-model-sequential-not-concurrent) —
  why short-circuiting only means something because evaluation is
  sequential, never concurrent.
