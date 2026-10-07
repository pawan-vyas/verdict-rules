<!-- Title: Extending — Walking a Rule Tree (Python) -->
# Walking a rule tree: Python

> The concept, the diagram, and the trap are in
> [`README.md`](README.md) — read that first. This page is the concrete
> Python code.

`CompositeRule` is a `@runtime_checkable` `Protocol`, so `isinstance` is the
whole test. Your own composite satisfies it by having `sub_rules` — there is
nothing to subclass and nothing to register.

```python
import asyncio
from typing import Iterator, Sequence

from verdict import (
    AndRule, CompositeRule, FunctionRule, NotRule, OrRule,
    PredicateOutcome, Rule, RuleResult,
)


class AtLeastNRule:
    """A consumer-defined composite that also carries its own threshold."""

    def __init__(self, name: str, rules: Sequence[Rule[dict]], minimum: int) -> None:
        self.name = name
        self.group: str | None = None
        self._rules = tuple(rules)
        self._minimum = minimum

    @property
    def sub_rules(self) -> Sequence[Rule[dict]]:
        return self._rules

    async def evaluate(self, context: dict) -> RuleResult:
        sub_results = [await rule.evaluate(context) for rule in self._rules]
        passed = sum(1 for r in sub_results if r.passed)
        return RuleResult(
            self.name,
            passed >= self._minimum,
            sub_results=sub_results,
            decided_by_indices=tuple(
                i for i, r in enumerate(sub_results) if r.passed
            ),
        )


def walk(rule: Rule[dict]) -> Iterator[Rule[dict]]:
    """Every rule in the tree, parents before children, depth-first."""
    yield rule
    if isinstance(rule, CompositeRule):
        for part in rule.sub_rules:
            yield from walk(part)


def leaf_names(rule: Rule[dict]) -> list[str]:
    return [r.name for r in walk(rule) if not isinstance(r, CompositeRule)]


def duplicate_names(rule: Rule[dict]) -> set[str]:
    """Names used by two *different* rule objects -- a build problem."""
    seen: dict[str, Rule[dict]] = {}
    duplicates: set[str] = set()
    for found in walk(rule):
        first = seen.setdefault(found.name, found)
        if first is not found:
            duplicates.add(found.name)
    return duplicates


async def always(passed: bool) -> PredicateOutcome:
    return PredicateOutcome(passed=passed)


def leaf(name: str, passed: bool = True) -> FunctionRule:
    return FunctionRule(name, lambda _ctx: always(passed))


async def main() -> None:
    tree = AndRule("eligible", [
        leaf("verified"),
        OrRule("either_path", [leaf("auto", passed=False), leaf("manual")]),
        NotRule("not_blocked", leaf("blocked", passed=False)),
        AtLeastNRule("two_of_three", [leaf("a"), leaf("b"), leaf("c", passed=False)], 2),
    ])

    print(leaf_names(tree))
    # ['verified', 'auto', 'manual', 'blocked', 'a', 'b', 'c']

    print(duplicate_names(tree))
    # set()

    # The custom composite's own parts are reached, so its three leaves are
    # listed. A type switch over AndRule/OrRule/NotRule would have stopped at
    # `two_of_three` and reported four leaves instead of seven.
    verdict = await tree.evaluate({})
    print(verdict.passed, len(verdict.sub_results))
    # True 4


asyncio.run(main())
```

Note what `walk` yields: composites *and* leaves, with `isinstance` deciding
which is which at the point of use. That is deliberate — a registry lookup
wants the leaves, a duplicate-name check wants every name including the
composites' own, and neither needs a second traversal.

## Related

- [`README.md`](README.md) — the concept and the trap this page implements.
- [`../new-rule-shape/python.md`](../new-rule-shape/python.md) — where
  `AtLeastNRule` comes from, written out in full.
