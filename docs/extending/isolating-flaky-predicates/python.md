<!-- Title: Extending — Isolating A Flaky Predicate (Python) -->
# Isolating a flaky predicate: Python

> The concept and the reasoning are in [`README.md`](README.md) — read
> that first. This page is the concrete Python code.

```python
from dataclasses import dataclass
from typing import Awaitable, Callable, TypeVar

from verdict import FunctionRule, RuleResult

TContext = TypeVar("TContext")


def defensive(
    name: str, predicate: Callable[[TContext], Awaitable[RuleResult]]
) -> FunctionRule[TContext]:
    """Turn a predicate's own exception into a failing RuleResult,
    instead of letting it propagate out of the run that contains it."""

    async def wrapped(context: TContext) -> RuleResult:
        try:
            return await predicate(context)
        except Exception as exc:
            return RuleResult(rule_name=name, passed=False, detail=str(exc))

    return FunctionRule(name, wrapped)


@dataclass(frozen=True)
class PromoContext:
    promo_code: str
    simulate_timeout: bool = False


async def check_promo_code_against_external_service(context: PromoContext) -> RuleResult:
    """Stands in for a real network call that can time out."""
    if context.simulate_timeout:
        raise TimeoutError("promo-validation service did not respond")
    return RuleResult(rule_name="promo_code_valid", passed=context.promo_code == "SAVE10")


rule = defensive("promo_code_valid", check_promo_code_against_external_service)
```

```python
await rule.evaluate(PromoContext(promo_code="SAVE10"))
# RuleResult(rule_name='promo_code_valid', passed=True, ...)

await rule.evaluate(PromoContext(promo_code="SAVE10", simulate_timeout=True))
# RuleResult(rule_name='promo_code_valid', passed=False,
#            detail='promo-validation service did not respond')
```

The same timeout against the **unwrapped** predicate propagates instead
of returning a result — this is what every other rule shares a
`run_all`/`run_group` with, unless it's wrapped too:

```python
unwrapped = FunctionRule("promo_code_valid", check_promo_code_against_external_service)
await unwrapped.evaluate(PromoContext(promo_code="SAVE10", simulate_timeout=True))
# raises TimeoutError: promo-validation service did not respond
```

Now a timeout in the wrapped check reports as `passed=False, detail="..."`
— one entry in `RunResult.results`, same as any other failing rule — and
every other rule in that `run_all`/`run_group` still runs and still
reports.

## Related

- [`README.md`](README.md) — the language-agnostic scenario this page
  implements.
