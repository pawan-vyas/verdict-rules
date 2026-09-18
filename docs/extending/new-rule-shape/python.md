<!-- Title: Extending — A Genuinely New Rule Shape (Python) -->
# A genuinely new rule shape: Python

> The concept, the diagram, and the trade-off are in
> [`README.md`](README.md) — read that first. This page is the concrete
> Python code.

```python
from typing import Generic, TypeVar

from verdict import FunctionRule, Rule, RuleResult

TContext = TypeVar("TContext")


class ThresholdRule(Generic[TContext]):
    """Passes if at least `minimum` of the given sub-rules pass.

    Not part of verdict itself — a consumer-defined combinator, exactly
    as free to exist as AndRule/OrRule are, with no changes needed on
    verdict's side to support it.
    """

    def __init__(self, name: str, rules: list[Rule[TContext]], minimum: int, group: str | None = None) -> None:
        self.name = name
        self.group = group
        self._rules = rules
        self._minimum = minimum

    async def evaluate(self, context: TContext) -> RuleResult:
        sub_results = [await rule.evaluate(context) for rule in self._rules]
        passed_count = sum(1 for r in sub_results if r.passed)
        return RuleResult(
            rule_name=self.name,
            passed=passed_count >= self._minimum,
            detail=f"{passed_count} of {len(self._rules)} passed, needed {self._minimum}",
            data=sub_results,
        )
```

`ThresholdRule[TContext]` can now be handed to a `RulesEngine`, nested
inside an `AndRule`, or hold an `AndRule` as one of its own sub-rules —
every existing piece of this package already knows how to run it,
because nothing anywhere checks `isinstance(x, FunctionRule)` or
similar; the `Rule` `Protocol` (`@runtime_checkable`) is the only
contract that matters.

The same case the spec's own diagram shows — 2 of 3 needed, the third
sub-rule fails:

```python
async def rule_1(context): return RuleResult(rule_name="rule_1", passed=True)
async def rule_2(context): return RuleResult(rule_name="rule_2", passed=True)
async def rule_3(context): return RuleResult(rule_name="rule_3", passed=False)

at_least_two: ThresholdRule[dict] = ThresholdRule("at_least_two", [
    FunctionRule("rule_1", rule_1),
    FunctionRule("rule_2", rule_2),
    FunctionRule("rule_3", rule_3),
], minimum=2)

result = await at_least_two.evaluate({})
result.passed, result.detail
# (True, '2 of 3 passed, needed 2')
```

`ThresholdRule[TContext]` binds every direct sub-rule to the same
`TContext` — but a sub-rule can be a
[`ProjectingRule`](../reusing-a-rule-across-contexts/python.md), which
itself satisfies `Rule[TContext]` while its wrapped rule reads a
narrower, different type internally. The combinator stays bound to one
context; what its sub-rules actually read does not have to match:

```python
verified_rule = FunctionRule("is_verified_user", is_verified_user)  # reads UserFlag
checkout_verified = ProjectingRule(verified_rule, lambda ctx: UserFlag(ctx.is_verified))

qualifies: ThresholdRule[OrderContext] = ThresholdRule("qualifies", [
    checkout_verified,  # reads UserFlag internally, via the projection
    FunctionRule("has_promo_code", has_promo_code),  # reads OrderContext directly
], minimum=2)
```

## Related

- [`README.md`](README.md) — the language-agnostic scenario this page
  implements.
- [`../../samples/graduation-requirement-verdict/python.md`](../../samples/graduation-requirement-verdict/python.md) —
  `AtLeastNRule`, a real, tested instance of this exact pattern.
- [`../reusing-a-rule-across-contexts/python.md`](../reusing-a-rule-across-contexts/python.md) —
  `ProjectingRule` itself, used above to mix a sub-rule reading a
  narrower context into a `ThresholdRule[TContext]` bound to a wider
  one.
