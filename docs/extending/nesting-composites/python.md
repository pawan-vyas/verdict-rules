<!-- Title: Extending — Nesting Composites Arbitrarily (Python) -->
# Nesting composites arbitrarily: Python

> The concept and the diagram are in [`README.md`](README.md) — read
> that first. This page is the concrete Python code.

```python
from dataclasses import dataclass

from verdict import AndRule, FunctionRule, OrRule, RuleResult


@dataclass(frozen=True)
class AccountContext:
    account_status: str
    is_premium_member: bool
    promo_code: str | None
    spend: float
    spend_threshold: float


async def is_active_account(context: AccountContext) -> RuleResult:
    return RuleResult(rule_name="is_active_account", passed=context.account_status == "active")


async def is_premium_member(context: AccountContext) -> RuleResult:
    return RuleResult(rule_name="is_premium_member", passed=context.is_premium_member)


async def has_promo_code(context: AccountContext) -> RuleResult:
    return RuleResult(rule_name="has_promo_code", passed=bool(context.promo_code))


async def meets_spend_threshold(context: AccountContext) -> RuleResult:
    return RuleResult(rule_name="meets_spend_threshold", passed=context.spend >= context.spend_threshold)


# Nesting doesn't care what built its sub-rules — each of the four leaves
# here is a plain FunctionRule, but any Rule (a custom shape, another
# composite) would compose exactly the same way.
qualifies: AndRule[AccountContext] = AndRule("qualifies", [
    FunctionRule("is_active_account", is_active_account),
    OrRule("has_a_valid_reason", [
        FunctionRule("is_premium_member", is_premium_member),
        FunctionRule("has_promo_code", has_promo_code),
        FunctionRule("meets_spend_threshold", meets_spend_threshold),
    ]),
])
```

```python
context = AccountContext(
    account_status="active",
    is_premium_member=False,
    promo_code="SAVE10",
    spend=20,
    spend_threshold=100,
)
result = await qualifies.evaluate(context)
result.passed
# True
result.data
# [RuleResult(rule_name='is_active_account', passed=True, ...),
#  RuleResult(rule_name='has_a_valid_reason', passed=True, ...,
#             data=[RuleResult(rule_name='is_premium_member', passed=False, ...),
#                   RuleResult(rule_name='has_promo_code', passed=True, ...)])]
# meets_spend_threshold never ran — has_a_valid_reason short-circuited
# once has_promo_code passed, exactly as a plain, unnested OrRule would
```

## Related

- [`README.md`](README.md) — the language-agnostic scenario this page
  implements.
