<!-- Title: Extending — Keep Your Own Domain Out Of Verdict (Python) -->
# Keep your own domain out of verdict: Python

> The concept and the diagram are in [`README.md`](README.md) — read
> that first. This page is the concrete Python code, illustrating a
> rate-limiting adapter (one of the two illustrations the spec names).

```python
from dataclasses import dataclass
from verdict import AndRule, FunctionRule, RuleResult


@dataclass
class RateLimitStatus:
    """This adapter's own domain type — verdict never sees it directly,
    only hands it back as RuleResult.data's opaque payload."""
    window: str
    used: int
    quota: int


def _rate_limit_rule(window: str, quota: int) -> FunctionRule:
    """The only place in this codebase that imports from verdict."""

    async def predicate(context: dict) -> RuleResult:
        used = context[f"{window}_used"]
        status = RateLimitStatus(window=window, used=used, quota=quota)
        return RuleResult(
            rule_name=f"{window}_under_quota",
            passed=used < quota,
            data=status,
        )

    return FunctionRule(f"{window}_under_quota", predicate)


async def check_rate_limits(context: dict, windows: dict[str, int]) -> list[RateLimitStatus]:
    """Domain logic calls this, never verdict directly."""
    combined = AndRule("rate_limits", [_rate_limit_rule(w, q) for w, q in windows.items()])
    result = await combined.evaluate(context)
    return [r.data for r in result.data]
```

`check_rate_limits` is the entire surface the rest of the codebase sees —
callers pass plain facts in and get `RateLimitStatus` objects back,
never a `Rule` or a `RuleResult`. `_rate_limit_rule` is the only
function that imports from `verdict`, and `RateLimitStatus` is the
payload riding through `RuleResult.data` — opaque to verdict, unpacked
back into a real type on the way out. An access-control adapter for an
unrelated domain in the same codebase would repeat this same shape in
its own module, sharing nothing with this one but the underlying engine.

## Related

- [`README.md`](README.md) — the language-agnostic scenario this page
  implements.
