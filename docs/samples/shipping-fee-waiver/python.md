<!-- Title: Sample — Shipping Fee Waiver -->
# Sample: Shipping Fee Waiver

> The problem, the design, and what a solution must demonstrate are
> language-agnostic and live in [`README.md`](README.md) — read that
> first. This page is the Python implementation of it.

## The naive way (and why it breaks down)

The obvious first implementation is a conditional ladder, one `if` per
qualifying path:

```python
async def ships_free(order: dict) -> bool:
    if await validate_promo_code(order.get("promo_code")):
        return True
    if order["is_premium_member"]:
        return True
    if order["total"] >= order["free_shipping_threshold"]:
        return True
    return False
```

Adding a fourth path means opening this function and inserting another
`if` among the ones already there, at risk to the branches already
correct and already shipped. Here that cost is concrete: the
promo-code check is an external service call, and it happens to run
first, so it runs on **every single order** — including a $500 order
from a non-member with no promo code, which qualifies on total alone
and never needed the network round trip at all. See the spec for the
rest of what this shape gets wrong.

## The `verdict` way

```python
from verdict import FunctionRule, OrRule, RuleResult


async def order_total_over_threshold(context: dict) -> RuleResult:
    passed = context["order_total"] >= context["free_shipping_threshold"]
    return RuleResult(rule_name="order_total_over_threshold", passed=passed)


async def has_premium_membership(context: dict) -> RuleResult:
    return RuleResult(rule_name="has_premium_membership", passed=context["is_premium_member"])


async def has_valid_promo_code(context: dict) -> RuleResult:
    # The expensive path: only reached if both cheaper checks above failed.
    is_valid = await context["promo_code_service"].validate(context.get("promo_code"))
    return RuleResult(rule_name="has_valid_promo_code", passed=is_valid)


ships_free = OrRule(
    "ships_free",
    [
        FunctionRule("order_total_over_threshold", order_total_over_threshold),
        FunctionRule("has_premium_membership", has_premium_membership),
        FunctionRule("has_valid_promo_code", has_valid_promo_code),  # cheapest-last, on purpose
    ],
)
```

The comment on the last rule is the whole fix, made visible: cost order
is now a stated decision at the point it matters, not something the
next person to edit this file has to reconstruct from scratch.

Proving the skip, not just asserting it — a call-counting fake stands
in for the real service:

```python
class FakePromoService:
    def __init__(self):
        self.calls = 0

    async def validate(self, code):
        self.calls += 1
        return code == "SAVE10"


promo_service = FakePromoService()
order = {
    "order_total": 120,
    "free_shipping_threshold": 50,
    "is_premium_member": False,
    "promo_code": None,
    "promo_code_service": promo_service,
}

result = await ships_free.evaluate(order)
result.passed, promo_service.calls
# (True, 0) — order_total alone already qualifies; the promo service is never called
```

## Related

- [`README.md`](README.md) —
  the language-agnostic spec this page implements.
- [`dynamic-discounts/python.md`](../dynamic-discounts/python.md) — the `AndRule`
  mirror image (every condition must pass, not just one).
- [`../../architecture/README.md`](../../architecture/README.md#execution-model-sequential-not-concurrent) —
  why short-circuiting only means something because evaluation is
  sequential, never concurrent.
