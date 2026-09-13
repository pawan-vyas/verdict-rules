<!-- Title: Sample — Dynamic Discount Eligibility -->
# Sample: Dynamic Discount Eligibility

> The problem, the design, and what a solution must demonstrate are
> language-agnostic and live in [`README.md`](README.md) — read that
> first. This page is the Python implementation of it.

## The naive way (and why it breaks down)

The obvious first implementation is a nested `if` chain, checked
straight against the cart, with the current campaign's numbers baked
directly into the code:

```python
async def qualifies_for_promo(cart: dict) -> bool:
    if cart["total"] >= 100:
        if cart["region"] in ("US", "CA"):
            if cart["is_first_purchase"]:
                return True
    return False
```

This is fine for exactly as long as the campaign's numbers never
change — see the spec for the three specific ways it stops being fine.

## The `verdict` way

```python
from verdict import AndRule, FunctionRule, RuleResult, RulesEngine


async def cart_meets_minimum(context: dict) -> RuleResult:
    total, minimum = context["cart_total"], context["promo_minimum"]
    return RuleResult(
        rule_name="cart_meets_minimum",
        passed=total >= minimum,
        detail=f"cart_total={total}, needs >= {minimum}",
    )


async def is_eligible_region(context: dict) -> RuleResult:
    region, eligible = context["region"], context["eligible_regions"]
    return RuleResult(
        rule_name="is_eligible_region",
        passed=region in eligible,
        detail=f"region={region!r} not in {sorted(eligible)}",
    )


async def is_first_purchase(context: dict) -> RuleResult:
    return RuleResult(rule_name="is_first_purchase", passed=context["is_first_purchase"])


# Built once. The AndRule and the engine below both hold these same three
# objects — nothing is declared twice, and nothing here needs to know in
# advance which of the two call sites below will use it.
minimum_rule = FunctionRule("cart_meets_minimum", cart_meets_minimum)
region_rule = FunctionRule("is_eligible_region", is_eligible_region)
first_purchase_rule = FunctionRule("is_first_purchase", is_first_purchase)

qualifies_for_promo = AndRule("qualifies_for_promo", [minimum_rule, region_rule, first_purchase_rule])
engine = RulesEngine([minimum_rule, region_rule, first_purchase_rule])


async def check_cart_fast(cart: dict, campaign: dict) -> bool:
    """The checkout gate: the cheapest possible yes/no, stopping at the
    first failing condition. Nothing past this point needs to know
    *which* condition failed — only whether the discount applies."""
    context = {**cart, **campaign}
    return (await qualifies_for_promo.evaluate(context)).passed


async def why_not(cart: dict, campaign: dict) -> RulesEngine:
    """The support-facing view: every condition, always — even if more
    than one failed at once. Never the fast path's job, so it doesn't
    share the fast path's short-circuiting.

    `campaign` (e.g. `{"promo_minimum": 100, "eligible_regions": {"US", "CA"}}`)
    is whatever the checkout code already reads the active campaign's
    configured values from — nothing here cares where it came from.
    """
    context = {**cart, **campaign}
    return await engine.run_all(context)
    # result.passed is true only if every condition passed; result.results
    # is one entry per condition, always all three, regardless of how many
    # failed — this is what a "why not?" screen actually needs.
```

Run against a qualifying cart, then one that fails on two conditions at
once:

```python
cart = {"cart_total": 120, "region": "US", "is_first_purchase": True}
campaign = {"promo_minimum": 100, "eligible_regions": {"US", "CA"}}

await check_cart_fast(cart, campaign)
# True

result = await why_not(cart, campaign)
result.passed
# True — every condition passed

bad_cart = {"cart_total": 50, "region": "MX", "is_first_purchase": True}
await check_cart_fast(bad_cart, campaign)
# False — stops at the first failure

result = await why_not(bad_cart, campaign)
result.passed
# False
[r.passed for r in result.results]
# [False, False, True] — both failures visible, not just the first
```

Swapping the campaign's minimum from `100` to `75`, or adding `"MX"` to
`eligible_regions`, is now a data change passed into either function —
no edit to `cart_meets_minimum`/`is_eligible_region`/`is_first_purchase`,
the `AndRule`, or the engine.

## Related

- [`README.md`](README.md) —
  the language-agnostic spec this page implements.
- [`shipping-fee-waiver/python.md`](../shipping-fee-waiver/python.md) — the `OrRule`
  mirror image of this same idea (any one qualifying path, not all).
- [`data-driven-rule-sets/python.md`](../data-driven-rule-sets/python.md) — building
  the rules themselves from stored config, not just their values.
