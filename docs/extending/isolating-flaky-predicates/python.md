<!-- Title: Extending — Isolating A Flaky Predicate (Python) -->
# Isolating a flaky predicate: Python

> The concept and the reasoning are in [`README.md`](README.md) — read
> that first. This page is the concrete Python code.

```python
from verdict import FunctionRule, RuleResult

def defensive(name: str, predicate) -> FunctionRule:
    """Turn a predicate's own exception into a failing RuleResult,
    instead of letting it propagate out of the run that contains it."""

    async def wrapped(context: dict) -> RuleResult:
        try:
            return await predicate(context)
        except Exception as exc:
            return RuleResult(rule_name=name, passed=False, detail=str(exc))

    return FunctionRule(name, wrapped)

rule = defensive("promo_code_valid", check_promo_code_against_external_service)
```

Now a timeout in the promo-code check reports as `passed=False, detail="..."`
— one entry in `RunResult.results`, same as any other failing rule — and
every other rule in that `run_all`/`run_group` still runs and still
reports.

## Related

- [`README.md`](README.md) — the language-agnostic scenario this page
  implements.
