# Verdict — Python

> The Python implementation of Verdict — a small, zero-dependency,
> async-native rule-evaluation engine. See the [top-level
> `README.md`](https://github.com/pawan-vyas/verdict-rules#readme) for
> what Verdict is and why it's shaped this way in narrative form; this
> doc is just "how do I install it and write my first rule" for Python
> specifically.
>
> This exact file is also what PyPI renders as the package description
> — none of its sibling files travel with a `pip install`, which is why
> every link below is an absolute GitHub URL rather than a relative
> path; on GitHub itself they work exactly the same way.

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

## Where to go next

| Doc | For |
|---|---|
| [`docs/quickstart.md`](https://github.com/pawan-vyas/verdict-rules/blob/python-v0.2.1/python/docs/quickstart.md) | The quickstart — core concepts and a full worked example |
| [`docs/architecture.md`](https://github.com/pawan-vyas/verdict-rules/blob/python-v0.2.1/docs/architecture.md) | Why it's shaped this way, in depth — type structure, the execution model |
| [`docs/extension.md`](https://github.com/pawan-vyas/verdict-rules/blob/python-v0.2.1/docs/extension.md) | Building on top of it from your own code, with no changes here |
| [`docs/maintenance.md`](https://github.com/pawan-vyas/verdict-rules/blob/python-v0.2.1/docs/maintenance.md) | Changing this package itself |
| [`docs/testing.md`](https://github.com/pawan-vyas/verdict-rules/blob/python-v0.2.1/docs/testing.md) | How the test suite is organized, and what a change needs to prove |
| [`docs/samples/`](https://github.com/pawan-vyas/verdict-rules/blob/python-v0.2.1/python/docs/samples/1_README.md) | Worked examples — dynamic discounts, fee waivers, tier promotions, moderation routing, data-driven rule sets |
| [`examples/`](https://github.com/pawan-vyas/verdict-rules/blob/python-v0.2.1/python/examples/README.md) | Full, tested mini-projects behind the more comprehensive samples — real code, real tests, real docs |

## Development

```bash
uv sync
uv run pytest
```
