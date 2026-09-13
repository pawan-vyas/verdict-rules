<!-- Title: Sample — Admin Eligibility Lookup -->
# Sample: Admin Eligibility Lookup

> The problem, the design, and what a solution must demonstrate are
> language-agnostic and live in [`README.md`](README.md) — read that
> first. This page is the Python implementation of it.

## The naive way (and why it breaks down)

Before reaching for a rule engine at all, the obvious first
implementation is a plain dict of configured checks and a lookup
function — no `verdict` in sight yet:

```python
ELIGIBILITY_CHECKS = {
    "gold_tier": [("spend", 1000)],
    "beta_feature": [],  # not filled in yet
}


async def check_eligibility(check_name: str, customer: dict) -> bool:
    try:
        conditions = ELIGIBILITY_CHECKS[check_name]
    except KeyError:
        return False  # "not eligible" either way

    return all(customer.get(field) == expected for field, expected in conditions)
```

A typo and a genuine rejection render identically here, and `all()`
over an empty iterable is `True` — Python's own vacuous truth — so a
check the team hasn't finished configuring yet silently passes. See the
spec for the rest of what this shape gets wrong.

## The `verdict` way

```python
from __future__ import annotations

from dataclasses import dataclass
from typing import Literal

from verdict import AndRule, FunctionRule, Rule, RuleResult, RulesEngine

# "eligible"/"not_eligible" come from a check that exists and actually ran.
# "unknown_check" and "not_configured" are both absence-shaped, and kept
# distinct from each other and from a genuine verdict so a support agent
# never mistakes "we don't know" for "we checked and the answer is no".
CheckStatus = Literal["eligible", "not_eligible", "unknown_check", "not_configured"]


@dataclass(frozen=True)
class LookupResult:
    status: CheckStatus
    detail: str


def _condition_predicate(field: str, expected: object):
    async def predicate(context: dict) -> RuleResult:
        actual = context.get(field)
        return RuleResult(
            rule_name=field,
            passed=actual == expected,
            detail=f"{field}={actual!r}, needs {expected!r}",
        )

    return predicate


def _build_check(name: str, conditions: list[dict]) -> tuple[Rule, bool]:
    """One configured check, plus whether it currently has zero conditions.

    An empty ``conditions`` list still produces a valid `AndRule` — it
    just vacuously passes if ever evaluated directly. `EligibilityLookup`
    intercepts that case before evaluation (see `check` below), so the
    vacuous pass never reaches the caller as a real "eligible".
    """
    condition_rules = [
        FunctionRule(f"{name}[{i}]", _condition_predicate(c["field"], c["expected"]))
        for i, c in enumerate(conditions)
    ]
    return AndRule(name, condition_rules), len(conditions) == 0


class EligibilityLookup:
    """Looks up one named eligibility check by name, typed fresh each time.

    `configured_checks` mirrors whatever an admin settings screen already
    holds: one entry per check, each a list of condition dicts. A check
    with an empty list is a real, valid state a row can be in while a
    team is still filling it in — not an error.
    """

    def __init__(self, configured_checks: dict[str, list[dict]]) -> None:
        rules: list[Rule] = []
        self._unconfigured: set[str] = set()
        for name, conditions in configured_checks.items():
            rule, is_empty = _build_check(name, conditions)
            rules.append(rule)
            if is_empty:
                self._unconfigured.add(name)
        self._engine = RulesEngine(rules)

    async def check(self, name: str, customer: dict) -> LookupResult:
        """Look up and run one named check. Never raises on a bad `name` —
        that is the entire point of this class existing between the raw
        engine and the screen that renders its result."""
        if name in self._unconfigured:
            return LookupResult("not_configured", f"'{name}' has no conditions configured yet")

        result = await self._engine.try_run_named(name, customer)
        if result is None:
            return LookupResult("unknown_check", f"no eligibility check named '{name}' exists")

        return LookupResult("eligible" if result.passed else "not_eligible", result.detail)
```

Run against a small configuration — one real check, one the team hasn't
finished, and one lookup with a typo:

```python
lookup = EligibilityLookup({
    "gold_tier": [{"field": "spend", "expected": 1000}],
    "beta_feature": [],  # not filled in yet
})

await lookup.check("gold_tier", {"spend": 1000})
# LookupResult(status='eligible', detail='')

await lookup.check("gold_tier", {"spend": 5})
# LookupResult(status='not_eligible', detail="'gold_tier[0]' failed: spend=5, needs 1000")

await lookup.check("beta_feature", {"spend": 1000})
# LookupResult(status='not_configured', detail="'beta_feature' has no conditions configured yet")

await lookup.check("gold_teir", {"spend": 1000})  # typo
# LookupResult(status='unknown_check', detail="no eligibility check named 'gold_teir' exists")
```

The screen can still show "not eligible" for the last two — the page
keeps working, exactly as the naive version intended — but `status` is
what tells a support agent *which* of the four things actually
happened, rather than one boolean standing in for all of them.

### Why not just wrap `run_named` in a `try`/`except KeyError`?

`try_run_named` and `run_named` share one lookup path — the strict form
is a two-line assertion on top of the lenient one, not a second
implementation. Reaching for `try_run_named` directly says "absence is
expected here and I have an answer for it," which is true on this
screen; wrapping the strict form in `try`/`except` says the same thing
by accident, and reads as "I expect this to raise and I'm suppressing
it" to the next person editing this file. The behavior is identical
either way — the difference is only which one tells the truth about why.

## Related

- [`README.md`](README.md) —
  the language-agnostic spec this page implements.
- [`../../extending/absence-vs-failure/`](../../extending/absence-vs-failure/README.md) —
  the general absence-vs-emptiness guidance this sample is one concrete
  instance of.
- [`data-driven-rule-sets/python.md`](../data-driven-rule-sets/python.md) — building the
  `Rule` objects themselves from stored configuration, the same pattern
  `EligibilityLookup` uses to turn each check's conditions into an
  `AndRule`.
