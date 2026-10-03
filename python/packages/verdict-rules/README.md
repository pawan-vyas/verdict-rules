# Verdict — Python

> The Python implementation of Verdict — a small, zero-dependency,
> async-native rule-evaluation engine.

## Install

```bash
pip install verdict-rules
```

```python
from verdict import Rule, FunctionRule, AndRule, OrRule, NotRule, RulesEngine
```

## A first rule

`FunctionRule` wraps a plain async predicate — a reusable one can build
several named rules from the same code, each checking a different field
against its own floor:

```python
import asyncio
from verdict import AndRule, FunctionRule, PredicateOutcome, RulesEngine

def at_least(name: str, field: str, floor: float) -> FunctionRule:
    async def predicate(context: dict) -> PredicateOutcome:
        value = context[field]
        return PredicateOutcome(passed=value >= floor, detail=f"{value} vs {floor}")
    return FunctionRule(name, predicate)

async def main() -> None:
    eligible = AndRule("eligible", [
        at_least("age_ok", "age", 18),
        at_least("score_ok", "score", 60),
    ])
    engine = RulesEngine([eligible])
    verdict = await engine.run_named("eligible", {"age": 21, "score": 55})
    print(verdict.passed)                          # False
    failed = verdict.failing_leaves[0]
    print(failed.rule_name, "|", failed.detail)    # score_ok | 55 vs 60

asyncio.run(main())
```

A predicate reports a `PredicateOutcome`; the `FunctionRule` wrapping it
owns the name and builds the `RuleResult`. A composite's own `detail` is
empty — read `failing_leaves` for which leaf check refused, and key an
audit trail on that leaf's `rule_name` rather than the composite's.

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
  `rule_names` / `group_names` to check membership, or `try_run_named` /
  `try_run_group` where your own domain has an answer for absence —
  both return `None` instead of raising.
- **`RuleResult.data` is opaque** — never read or written by this
  package; it carries whatever a predicate attached, unchanged.
- **A composite's children live in `sub_results`**, and are exactly what
  it evaluated: never padded to the full sub-rule list, never flattened
  into the parent. `leaves`/`failing_leaves` walk that tree for you, and
  `decided_by` names which sub-results explain a result's own verdict.
- **Zero runtime dependencies.**

## Where to go next

| Doc | For |
| --- | --- |
| [`docs/quickstart.md`](https://github.com/pawan-vyas/verdict-rules/blob/python-v0.4.0/python/packages/verdict-rules/docs/quickstart.md) | The quickstart — core concepts and a full worked example |
| [`docs/architecture/`](https://github.com/pawan-vyas/verdict-rules/blob/python-v0.4.0/docs/architecture/README.md) | Why it's shaped this way, in depth — type structure, the execution model |
| [`docs/extending/`](https://github.com/pawan-vyas/verdict-rules/blob/python-v0.4.0/docs/extending/README.md) | Building on top of it from your own code, with no changes here |
| [`docs/maintenance/`](https://github.com/pawan-vyas/verdict-rules/blob/python-v0.4.0/docs/maintenance/README.md) | Changing this package itself |
| [`docs/testing/`](https://github.com/pawan-vyas/verdict-rules/blob/python-v0.4.0/docs/testing/README.md) | How the test suite is organized, and what a change needs to prove |
| [`fixtures/`](https://github.com/pawan-vyas/verdict-rules/blob/python-v0.4.0/fixtures/README.md) | The two worked scenarios — each one's problem, design, and the cross-language data contract every port reproduces |
| [`examples/`](https://github.com/pawan-vyas/verdict-rules/blob/python-v0.4.0/python/examples/README.md) | Full, tested mini-projects behind the more comprehensive samples — real code, real tests, real docs |
