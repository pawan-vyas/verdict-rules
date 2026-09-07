# Verdict — Python

> The Python implementation of Verdict — a small, zero-dependency,
> async-native rule-evaluation engine. See the [top-level
> `README.md`](../README.md) for what Verdict is and why it's shaped
> this way in narrative form; this doc is just "how do I install it and
> write my first rule" for Python specifically.

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

```python
import asyncio
from verdict import FunctionRule, AndRule, RulesEngine

async def under_limit(context: dict) -> bool:
    return context["requests_this_minute"] < context["limit"]

async def in_good_standing(context: dict) -> bool:
    return context["account_status"] == "active"

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
| [`docs/quickstart.md`](docs/quickstart.md) | The quickstart — core concepts and a full worked example |
| [`../docs/architecture.md`](../docs/architecture.md) | Why it's shaped this way, in depth — type structure, the execution model |
| [`../docs/extension.md`](../docs/extension.md) | Building on top of it from your own code, with no changes here |
| [`../docs/maintenance.md`](../docs/maintenance.md) | Changing this package itself |
| [`../docs/testing.md`](../docs/testing.md) | How the test suite is organized, and what a change needs to prove |
| [`docs/samples/`](docs/samples/1_README.md) | Worked examples — dynamic discounts, fee waivers, tier promotions, moderation routing, data-driven rule sets |
| [`examples/`](examples/README.md) | Full, tested mini-projects behind the more comprehensive samples — real code, real tests, real docs |

## Development

```bash
uv sync
uv run pytest
```
