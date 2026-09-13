<!-- Title: Extending — Nesting Composites Arbitrarily (Python) -->
# Nesting composites arbitrarily: Python

> The concept and the diagram are in [`README.md`](README.md) — read
> that first. This page is the concrete Python code.

```python
from verdict import AndRule, OrRule

# is_active_account, is_premium_member, has_promo_code, and meets_spend_threshold
# are all already-built Rule instances (e.g. FunctionRule) from elsewhere in your
# own code — nesting doesn't care what built them.
qualifies = AndRule("qualifies", [
    is_active_account,
    OrRule("has_a_valid_reason", [is_premium_member, has_promo_code, meets_spend_threshold]),
])
```

## Related

- [`README.md`](README.md) — the language-agnostic scenario this page
  implements.
