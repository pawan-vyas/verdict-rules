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

`FunctionRule` wraps a plain async predicate — a reusable one can build
several named rules from the same code, each checking a different field
against its own floor:

```python
import asyncio
from verdict import FunctionRule, AndRule, RuleResult, RulesEngine

def at_least(name: str, field: str, floor: float) -> FunctionRule:
    async def predicate(context: dict) -> RuleResult:
        value = context[field]
        return RuleResult(rule_name=name, passed=value >= floor, detail=f"{value} vs {floor}")
    return FunctionRule(name, predicate)

async def main() -> None:
    eligible = AndRule("eligible", [
        at_least("age_ok", "age", 18),
        at_least("score_ok", "score", 60),
    ])
    engine = RulesEngine([eligible])
    verdict = await engine.run_named("eligible", {"age": 21, "score": 55})
    print(verdict.passed)  # False
    print(verdict.detail)  # "'score_ok' failed: 55 vs 60"

asyncio.run(main())
```

## If it has the shape, it is a rule

`Rule` is a `Protocol`, and Python's typing is **structural** — so any
object with the right attributes and an `evaluate` coroutine already
*is* a `Rule`. No inheritance, no registration:

```python
class IsBusinessHours:
    name = "is_business_hours"
    group = None

    async def evaluate(self, context: dict) -> RuleResult:
        hour = context["hour"]
        return RuleResult(rule_name="is_business_hours", passed=9 <= hour < 17)

await AndRule("open", [IsBusinessHours()]).evaluate({"hour": 21})
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
| [`docs/quickstart.md`](https://github.com/pawan-vyas/verdict-rules/blob/python-v0.2.7/python/packages/verdict-rules/docs/quickstart.md) | The quickstart — core concepts and a full worked example |
| [`docs/architecture/`](https://github.com/pawan-vyas/verdict-rules/blob/python-v0.2.7/docs/architecture/README.md) | Why it's shaped this way, in depth — type structure, the execution model |
| [`docs/extending/`](https://github.com/pawan-vyas/verdict-rules/blob/python-v0.2.7/docs/extending/README.md) | Building on top of it from your own code, with no changes here |
| [`docs/maintenance/`](https://github.com/pawan-vyas/verdict-rules/blob/python-v0.2.7/docs/maintenance/README.md) | Changing this package itself |
| [`docs/testing/`](https://github.com/pawan-vyas/verdict-rules/blob/python-v0.2.7/docs/testing/README.md) | How the test suite is organized, and what a change needs to prove |
| [`docs/samples/`](https://github.com/pawan-vyas/verdict-rules/blob/python-v0.2.7/docs/samples/README.md) | Worked examples — dynamic discounts, fee waivers, tier promotions, moderation routing, data-driven rule sets |
| [`examples/`](https://github.com/pawan-vyas/verdict-rules/blob/python-v0.2.7/python/examples/README.md) | Full, tested mini-projects behind the more comprehensive samples — real code, real tests, real docs |
