<!-- Title: Sample — Loyalty Tier Promotion -->
# Sample: Loyalty Tier Promotion

> The problem, the design, and what a solution must demonstrate are
> language-agnostic and live in [`README.md`](README.md) — read that
> first. This page is the Python implementation of it.

## The naive way (and why it breaks down)

The obvious first implementation is *two* functions, one for the
customer-facing checklist and one for the actual yes/no decision, each
with the four criteria's thresholds written inline:

```python
async def gold_checklist(customer: dict) -> dict:
    return {
        "spend": customer["trailing_12mo_spend"] >= 5000,
        "orders": customer["trailing_12mo_orders"] >= 15,
        "returns": customer["return_rate"] <= 0.05,
        "standing": customer["account_status"] == "active",
    }


async def is_eligible_for_gold(customer: dict) -> bool:
    return (
        customer["trailing_12mo_spend"] >= 5000
        and customer["trailing_12mo_orders"] >= 20
        and customer["return_rate"] <= 0.05
        and customer["account_status"] == "active"
    )
```

`gold_checklist` promotes at 15 orders; `is_eligible_for_gold` at 20 —
visible by reading the two functions side by side, not a hypothetical
future drift. See the spec for the rest of what this shape gets wrong.

## The `verdict` way

A typed context, not a dict — each threshold has exactly one
definition, read by name from every rule that needs it. That's what
makes the naive version's drift structurally impossible here, not
merely avoided by discipline:

```python
from dataclasses import dataclass

from verdict import FunctionRule, RuleResult, RulesEngine


@dataclass(frozen=True)
class LoyaltyContext:
    trailing_12mo_spend: float
    gold_spend_threshold: float
    trailing_12mo_orders: int
    gold_order_threshold: int
    return_rate: float
    gold_max_return_rate: float
    account_status: str


async def meets_spend_threshold(context: LoyaltyContext) -> RuleResult:
    passed = context.trailing_12mo_spend >= context.gold_spend_threshold
    return RuleResult(rule_name="meets_spend_threshold", passed=passed)


async def meets_order_count(context: LoyaltyContext) -> RuleResult:
    passed = context.trailing_12mo_orders >= context.gold_order_threshold
    return RuleResult(rule_name="meets_order_count", passed=passed)


async def return_rate_below_max(context: LoyaltyContext) -> RuleResult:
    passed = context.return_rate <= context.gold_max_return_rate
    return RuleResult(rule_name="return_rate_below_max", passed=passed)


async def account_in_good_standing(context: LoyaltyContext) -> RuleResult:
    return RuleResult(rule_name="account_in_good_standing", passed=context.account_status == "active")


# Registered as four independent named rules on one engine — not nested
# in an AndRule — precisely so run_all() reports every criterion's own
# outcome, with no short-circuiting hiding a later criterion's result.
# RulesEngine[LoyaltyContext] documents that every rule here shares this
# one context type, the same way FunctionRule's own type argument does.
engine = RulesEngine[LoyaltyContext]([
    FunctionRule("meets_spend_threshold", meets_spend_threshold),
    FunctionRule("meets_order_count", meets_order_count),
    FunctionRule("return_rate_below_max", return_rate_below_max),
    FunctionRule("account_in_good_standing", account_in_good_standing),
])


async def promotion_checklist(context: LoyaltyContext):
    result = await engine.run_all(context)
    # result.passed is True only if all four passed — the actual promotion decision.
    # result.results is one RuleResult per criterion, always all four — the UI checklist.
    return result
```

This is the one case where reaching for a bare `AndRule` and reaching
for the engine's `run_all()` produce genuinely different, both-correct
answers depending on what the caller actually needs — see
[`../../architecture/python.md`](../../architecture/python.md#three-ways-to-run-rules-concretely)
for the general rule of thumb.

A customer meeting three of the four criteria — the same case the
spec's own diagram shows:

```python
customer = LoyaltyContext(
    trailing_12mo_spend=6000,
    gold_spend_threshold=5000,
    trailing_12mo_orders=20,
    gold_order_threshold=15,
    return_rate=0.08,
    gold_max_return_rate=0.05,
    account_status="active",
)

checklist = await promotion_checklist(customer)
checklist.passed
# False — not promoted, the return rate is over the limit
[(r.rule_name, r.passed) for r in checklist.results]
# [('meets_spend_threshold', True), ('meets_order_count', True),
#  ('return_rate_below_max', False), ('account_in_good_standing', True)]
# all four report, not just the one that decided the outcome
```

## Related

- [`README.md`](README.md) —
  the language-agnostic spec this page implements.
- [`content-moderation-routing/python.md`](../content-moderation-routing/python.md) —
  another `run_*` (this time `run_group()`) example, for when a single
  engine needs to serve more than one independent decision.
