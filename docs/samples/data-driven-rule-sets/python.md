<!-- Title: Sample — Data-Driven Rule Sets -->
# Sample: Data-Driven Rule Sets

> The problem, the design, and what a solution must demonstrate are
> language-agnostic and live in [`README.md`](README.md) — read that
> first. This page is the Python implementation of it.

## The naive way (and why it breaks down)

The obvious first implementation compiles every configured condition
straight into an `if`/`elif` ladder in application code:

```python
async def matches_condition_grant(category: str | None, groups: list[str]) -> bool:
    if category == "Manager" and "Headquarters" in groups:
        return True
    if category == "Analyst" and ("Support" in groups or "Headquarters" in groups):
        return True
    return False
```

This is, functionally, a two-row configuration table hand-transcribed
into source code — every new condition an admin wants is a developer
task, a PR, and a deploy. See the spec for the rest of what this shape
gets wrong.

## The `verdict` way

```python
from dataclasses import dataclass

from verdict import AndRule, OrRule, Rule, RuleResult


@dataclass(frozen=True)
class ConfiguredRow:
    """One stored row describing a single, independently-editable rule."""
    id: int
    field: str
    operator: str  # "gte" | "eq" | "in"
    value: object


def _rule_for_row(row: ConfiguredRow) -> Rule:
    """Turn one stored row into a Rule — the only place that knows how."""

    async def predicate(context: dict) -> RuleResult:
        actual = context.get(row.field)
        passed = {
            "gte": lambda: actual is not None and actual >= row.value,
            "eq": lambda: actual == row.value,
            "in": lambda: actual in row.value,
        }[row.operator]()
        return RuleResult(rule_name=f"row:{row.id}", passed=passed)

    class _RowRule:
        name = f"row:{row.id}"
        group = None
        evaluate = staticmethod(predicate)

    return _RowRule()


async def fetch_current_rows() -> list[ConfiguredRow]:
    """Stand-in for a real DB read — the only I/O in this whole pattern."""
    ...  # SELECT * FROM some_rule_table WHERE ...


async def evaluate_against_current_config(context: dict, *, combine: str) -> bool:
    """Build the current rule set fresh and evaluate it — nothing retained between calls.

    Args:
        combine: "all" (AndRule — every row must pass; empty config passes) or
            "any" (OrRule — at least one row must pass; empty config fails).
            Which default is correct is a domain decision, made explicitly here,
            not inferred — see the spec's "reading the diagram" note on this.
    """
    rows = await fetch_current_rows()
    rules = [_rule_for_row(row) for row in rows]
    combined: Rule = AndRule("combined", rules) if combine == "all" else OrRule("combined", rules)
    result = await combined.evaluate(context)
    return result.passed
```

## Related

- [`README.md`](README.md) —
  the language-agnostic spec this page implements.
- [`../../extending/data-driven-rule-construction/`](../../extending/data-driven-rule-construction/README.md) —
  the general scenario this sample is a fuller version of, including how
  two unrelated domains share one engine without coupling to each
  other.
- [`dynamic-discounts/python.md`](../dynamic-discounts/python.md) — a smaller, single-`AndRule`
  instance of the same idea.
