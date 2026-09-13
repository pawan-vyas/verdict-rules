<!-- Title: Extending — A Genuinely New Rule Shape (Python) -->
# A genuinely new rule shape: Python

> The concept, the diagram, and the trade-off are in
> [`README.md`](README.md) — read that first. This page is the concrete
> Python code.

```python
from verdict import Rule, RuleResult


class ThresholdRule:
    """Passes if at least `minimum` of the given sub-rules pass.

    Not part of verdict itself — a consumer-defined combinator, exactly
    as free to exist as AndRule/OrRule are, with no changes needed on
    verdict's side to support it.
    """

    def __init__(self, name: str, rules: list[Rule], minimum: int, group: str | None = None) -> None:
        self.name = name
        self.group = group
        self._rules = rules
        self._minimum = minimum

    async def evaluate(self, context: dict) -> RuleResult:
        sub_results = [await rule.evaluate(context) for rule in self._rules]
        passed_count = sum(1 for r in sub_results if r.passed)
        return RuleResult(
            rule_name=self.name,
            passed=passed_count >= self._minimum,
            detail=f"{passed_count} of {len(self._rules)} passed, needed {self._minimum}",
            data=sub_results,
        )
```

`ThresholdRule` can now be handed to a `RulesEngine`, nested inside an
`AndRule`, or hold an `AndRule` as one of its own sub-rules — every
existing piece of this package already knows how to run it, because
nothing anywhere checks `isinstance(x, FunctionRule)` or similar; the
`Rule` `Protocol` (`@runtime_checkable`) is the only contract that
matters.

## Related

- [`README.md`](README.md) — the language-agnostic scenario this page
  implements.
- [`../../samples/graduation-requirement-verdict/python.md`](../../samples/graduation-requirement-verdict/python.md) —
  `AtLeastNRule`, a real, tested instance of this exact pattern.
