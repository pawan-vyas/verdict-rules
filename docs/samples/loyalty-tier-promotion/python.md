<!-- Title: Sample — Loyalty Tier Promotion -->
# Sample: Loyalty Tier Promotion

> The problem, the design, and what a solution must demonstrate are
> language-agnostic and live in [`README.md`](README.md) — read that
> first. This page is the Python implementation of it.

## The naive way (and why it breaks down)

The obvious first implementation ends up as *two* functions that have to
be kept in sync by hand — one for the yes/no decision, one for the
customer-facing checklist:

```python
async def gold_checklist(customer: dict) -> dict:
    return {
        "spend": customer["trailing_12mo_spend"] >= customer["gold_spend_threshold"],
        "orders": customer["trailing_12mo_orders"] >= customer["gold_order_threshold"],
        "returns": customer["return_rate"] <= customer["gold_max_return_rate"],
        "standing": customer["account_status"] == "active",
    }


async def is_eligible_for_gold(customer: dict) -> bool:
    checklist = await gold_checklist(customer)
    return all(checklist.values())
```

Nothing enforces the coupling between the two functions — adding a
fifth requirement to one without the other produces a promotion
decision the UI's own checklist can't explain. See the spec for the
rest of what this shape gets wrong.

## The `verdict` way

```python
from verdict import FunctionRule, RuleResult, RulesEngine


async def meets_spend_threshold(context: dict) -> RuleResult:
    passed = context["trailing_12mo_spend"] >= context["gold_spend_threshold"]
    return RuleResult(rule_name="meets_spend_threshold", passed=passed)


async def meets_order_count(context: dict) -> RuleResult:
    passed = context["trailing_12mo_orders"] >= context["gold_order_threshold"]
    return RuleResult(rule_name="meets_order_count", passed=passed)


async def return_rate_below_max(context: dict) -> RuleResult:
    passed = context["return_rate"] <= context["gold_max_return_rate"]
    return RuleResult(rule_name="return_rate_below_max", passed=passed)


async def account_in_good_standing(context: dict) -> RuleResult:
    return RuleResult(rule_name="account_in_good_standing", passed=context["account_status"] == "active")


# Registered as four independent named rules on one engine — not nested
# in an AndRule — precisely so run_all() reports every criterion's own
# outcome, with no short-circuiting hiding a later criterion's result.
engine = RulesEngine([
    FunctionRule("meets_spend_threshold", meets_spend_threshold),
    FunctionRule("meets_order_count", meets_order_count),
    FunctionRule("return_rate_below_max", return_rate_below_max),
    FunctionRule("account_in_good_standing", account_in_good_standing),
])


async def promotion_checklist(customer_context: dict):
    result = await engine.run_all(customer_context)
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
customer = {
    "trailing_12mo_spend": 6000,
    "gold_spend_threshold": 5000,
    "trailing_12mo_orders": 20,
    "gold_order_threshold": 15,
    "return_rate": 0.08,
    "gold_max_return_rate": 0.05,
    "account_status": "active",
}

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
