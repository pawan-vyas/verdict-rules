<!-- Title: Extending — Keep Your Own Domain Out Of Verdict (Python) -->
# Keep your own domain out of verdict: Python

> The concept and the diagram are in [`README.md`](README.md) — read
> that first. This page is the concrete Python code, illustrating a
> rate-limiting adapter (one of the two illustrations the spec names),
> and — since the point of the boundary is that `verdict` itself is
> replaceable behind it — a second implementation of the identical
> contract that doesn't use `verdict` at all.

```python
from dataclasses import dataclass
from typing import Protocol

from verdict import FunctionRule, PredicateOutcome, RulesEngine


@dataclass
class RateLimitStatus:
    """This adapter's own domain type — verdict never sees it directly,
    only carries it through as the result's opaque payload."""
    window: str
    used: int
    quota: int


class RateLimiter(Protocol):
    """The contract every call site depends on. Nothing here mentions
    verdict — satisfying this structurally is enough, exactly the way a
    Rule itself works."""

    async def check(self, context: dict, windows: dict[str, int]) -> list[RateLimitStatus]: ...


class VerdictRateLimiter:
    """The only place in this codebase that imports from verdict."""

    async def check(self, context: dict, windows: dict[str, int]) -> list[RateLimitStatus]:
        def rule_for(window: str, quota: int) -> FunctionRule:
            async def predicate(ctx: dict) -> PredicateOutcome:
                used = ctx[f"{window}_used"]
                return PredicateOutcome(
                    passed=used < quota,
                    data=RateLimitStatus(window, used, quota),
                )
            return FunctionRule(f"{window}_under_quota", predicate)

        # run_all, not a composite: the contract promises one status per
        # window, and a composite short-circuits — the first window over
        # quota would end evaluation and the rest would be missing from the
        # returned list, silently.
        engine = RulesEngine([rule_for(w, q) for w, q in windows.items()])
        run = await engine.run_all(context)
        return [r.data for r in run.results]


class SimpleRateLimiter:
    """A hand-rolled replacement for VerdictRateLimiter — same contract,
    zero verdict. Adding this is a new class; nothing above changed to
    make room for it."""

    async def check(self, context: dict, windows: dict[str, int]) -> list[RateLimitStatus]:
        return [
            RateLimitStatus(window, context[f"{window}_used"], quota)
            for window, quota in windows.items()
        ]
```

The domain type rides through on the predicate's own `data`, which
`FunctionRule` copies onto the result it builds. Verdict never reads it —
reading it back out is this adapter's business and nobody else's.

The composition root — the one place that decides which implementation
is actually running — is a single line:

```python
# Before: wired to the verdict-backed implementation.
rate_limiter: RateLimiter = VerdictRateLimiter()

# After: swapped for the hand-rolled one. One line, here, changes.
rate_limiter: RateLimiter = SimpleRateLimiter()

# Every call site in the codebase, unaffected either way:
statuses = await rate_limiter.check(context, windows)
```

That last line is the actual proof: it's identical before and after the
swap. Nothing that calls `rate_limiter.check(...)` knows or cares which
class it's holding, because `RateLimiter` is a structural `Protocol` —
the same property that lets a plain function satisfy `Rule` with no
subclassing is what lets `SimpleRateLimiter` satisfy this adapter's own
contract with no relationship to `VerdictRateLimiter` at all.

## Related

- [`README.md`](README.md) — the language-agnostic scenario this page
  implements.
