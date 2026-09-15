# Verdict — Python

> The Python implementation of Verdict — a small, zero-dependency,
> async-native rule-evaluation engine.

## Install

```bash
pip install verdict-rules
```

The distribution on PyPI is named `verdict-rules` (the name `verdict`
was already taken by an unrelated package), but the import name is
plain `verdict`:

```python
from verdict import Rule, FunctionRule, AndRule, OrRule, RulesEngine
```

## A first rule

A `FunctionRule`'s predicate returns a full `RuleResult`, not a bare
boolean — that keeps it in control of `detail`/`data`, not just pass/fail:

```python
import asyncio
from verdict import FunctionRule, AndRule, RuleResult, RulesEngine

async def under_limit(context: dict) -> RuleResult:
    return RuleResult(
        rule_name="under_limit",
        passed=context["requests_this_minute"] < context["limit"],
    )

async def in_good_standing(context: dict) -> RuleResult:
    return RuleResult(
        rule_name="in_good_standing",
        passed=context["account_status"] == "active",
    )

async def main() -> None:
    can_proceed = AndRule(
        "can_proceed",
        [
            FunctionRule("under_limit", under_limit),
            FunctionRule("in_good_standing", in_good_standing),
        ],
    )
    engine = RulesEngine([can_proceed])
    result = await engine.run_named(
        "can_proceed",
        {"requests_this_minute": 3, "limit": 10, "account_status": "active"},
    )
    print(result.passed)  # True

asyncio.run(main())
```

## If it has the shape, it is a rule

`Rule` is a `Protocol`, and Python's typing is **structural** — so any
object with the right attributes and an `evaluate` coroutine already
*is* a `Rule`. No inheritance, no registration:

```python
class OverEighteen:
    name = "over_18"
    group = None

    async def evaluate(self, context: dict) -> RuleResult:
        return RuleResult(rule_name="over_18", passed=context["age"] >= 18)

await AndRule("eligible", [OverEighteen()]).evaluate({"age": 21})
```

Most rules need no class at all either: `FunctionRule` wraps a plain
async function.

## What it guarantees

- **Sequential evaluation, never concurrent.** Composites use a plain
  `for` loop with `await`, never `asyncio.gather`. Short-circuiting only
  means something if later work never *starts* — and because the
  returned boolean is identical either way, getting this wrong is silent.
- **Vacuous truth has a polarity.** `AndRule([])` passes, `OrRule([])`
  fails. Deliberately asymmetric.
- **Emptiness is not absence.** An empty composite folds to its
  identity; an unknown rule name or group **raises** `KeyError`. A
  group exists only because some rule declared it, so a lookup matching
  nothing can only be a mistake — and a misspelled group silently
  approving is the worst failure an eligibility check can have. Use
  `rule_names` / `group_names` to check rather than catch.
- **`RuleResult.data` is opaque** — only what actually ran, never
  padded, never flattened.
- **Zero runtime dependencies.**

## Where to go next

| Doc | For |
| --- | --- |
| [`docs/quickstart.md`](https://github.com/pawan-vyas/verdict-rules/blob/python-v0.2.6/python/packages/verdict-rules/docs/quickstart.md) | The quickstart — core concepts and a full worked example |
| [`docs/architecture/`](https://github.com/pawan-vyas/verdict-rules/blob/python-v0.2.6/docs/architecture/README.md) | Why it's shaped this way, in depth — type structure, the execution model |
| [`docs/extending/`](https://github.com/pawan-vyas/verdict-rules/blob/python-v0.2.6/docs/extending/README.md) | Building on top of it from your own code, with no changes here |
| [`docs/maintenance/`](https://github.com/pawan-vyas/verdict-rules/blob/python-v0.2.6/docs/maintenance/README.md) | Changing this package itself |
| [`docs/testing/`](https://github.com/pawan-vyas/verdict-rules/blob/python-v0.2.6/docs/testing/README.md) | How the test suite is organized, and what a change needs to prove |
| [`docs/samples/`](https://github.com/pawan-vyas/verdict-rules/blob/python-v0.2.6/docs/samples/README.md) | Worked examples — dynamic discounts, fee waivers, tier promotions, moderation routing, data-driven rule sets |
| [`examples/`](https://github.com/pawan-vyas/verdict-rules/blob/python-v0.2.6/python/examples/README.md) | Full, tested mini-projects behind the more comprehensive samples — real code, real tests, real docs |
