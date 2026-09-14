<!-- Title: Extending — Wrapping A Predicate (Python) -->
# Wrapping a predicate: Python

> The concept and why it's the common case are in
> [`README.md`](README.md) — read that first. This page is the concrete
> Python code.

```python
from verdict import FunctionRule, RuleResult

async def cart_meets_minimum(context: dict) -> RuleResult:
    total = context["cart_total"]
    minimum = context["minimum_for_offer"]
    return RuleResult(
        rule_name="cart_meets_minimum",
        passed=total >= minimum,
        detail=f"{total} vs minimum {minimum}",
    )

rule = FunctionRule("cart_meets_minimum", cart_meets_minimum)
```

## Related

- [`README.md`](README.md) — the language-agnostic scenario this page
  implements.
- [`../new-rule-shape/python.md`](../new-rule-shape/python.md) — the
  next step up, for combination logic `FunctionRule` alone can't express.
