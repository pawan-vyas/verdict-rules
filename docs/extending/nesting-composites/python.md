<!-- Title: Extending — Nesting Composites Arbitrarily (Python) -->
# Nesting composites arbitrarily: Python

> The concept and the diagram are in [`README.md`](README.md) — read
> that first. This page is the concrete Python code.

```python
from dataclasses import dataclass

from verdict import AndRule, FunctionRule, OrRule, PredicateOutcome


@dataclass(frozen=True)
class AccountContext:
    account_status: str
    is_premium_member: bool
    promo_code: str | None
    spend: float
    spend_threshold: float


async def is_active_account(context: AccountContext) -> PredicateOutcome:
    return PredicateOutcome(passed=context.account_status == "active")


async def is_premium_member(context: AccountContext) -> PredicateOutcome:
    return PredicateOutcome(passed=context.is_premium_member)


async def has_promo_code(context: AccountContext) -> PredicateOutcome:
    return PredicateOutcome(passed=bool(context.promo_code))


async def meets_spend_threshold(context: AccountContext) -> PredicateOutcome:
    return PredicateOutcome(passed=context.spend >= context.spend_threshold)


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

A nested result is read the same way at every level — `sub_results` holds
one level, and never the whole tree:

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

[r.rule_name for r in result.sub_results]
# ['is_active_account', 'has_a_valid_reason']

inner = result.sub_results[1]
[r.rule_name for r in inner.sub_results]
# ['is_premium_member', 'has_promo_code']
# meets_spend_threshold never ran — has_a_valid_reason short-circuited
# once has_promo_code passed, exactly as a plain, unnested OrRule would
```

Three views answer three different questions about the same tree:

```python
[r.rule_name for r in result.leaves]
# ['is_active_account', 'is_premium_member', 'has_promo_code']
# fully recursive — the terminal checks, however deep

result.failing_leaves
# [] — a passing result has none, even though is_premium_member failed
# on the way to the inner OrRule's pass

[r.rule_name for r in result.decided_by]
# ['is_active_account', 'has_a_valid_reason']
# one level: an AndRule that had to evaluate everything is explained by
# everything

[r.rule_name for r in inner.decided_by]
# ['has_promo_code']
# one level again, but an OrRule that stopped early is explained by just
# the sub-rule that stopped it
```

`data` is not part of this. It is an opaque slot for a caller's own
payload, never written to by a composite — a composite's children are in
`sub_results`.

## Related

- [`README.md`](README.md) — the language-agnostic scenario this page
  implements.
