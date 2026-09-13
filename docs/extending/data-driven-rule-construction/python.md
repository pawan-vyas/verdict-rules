<!-- Title: Extending — Building Rule Sets From Stored Configuration (Python) -->
# Building rule sets from stored configuration: Python

> The concept and the testing implication are in
> [`README.md`](README.md) — read that first. This page is the concrete
> Python code.

```python
from verdict import AndRule, FunctionRule, RuleResult


def make_rule(rule_config: dict) -> FunctionRule:
    async def predicate(context: dict) -> RuleResult:
        actual = context.get(rule_config["field"])
        passed = actual == rule_config["expected"]
        return RuleResult(rule_name=rule_config["name"], passed=passed)
    return FunctionRule(rule_config["name"], predicate)


def load_rule_configs() -> list[dict]:
    """Stands in for a real config source for this example."""
    return [
        {"name": "is_manager", "field": "role", "expected": "manager"},
        {"name": "in_headquarters", "field": "office", "expected": "HQ"},
    ]


configured_rules = [make_rule(cfg) for cfg in load_rule_configs()]
combined_rule = AndRule("combined", configured_rules)
```

```python
await combined_rule.evaluate({"role": "manager", "office": "HQ"})
# RuleResult(passed=True, ...)

await combined_rule.evaluate({"role": "manager", "office": "Remote"})
# RuleResult(passed=False, ...) — in_headquarters fails
```

An empty `load_rule_configs()` produces an empty `AndRule`, which
vacuously passes.

## Related

- [`README.md`](README.md) — the language-agnostic scenario this page
  implements.
- [`../../samples/data-driven-rule-sets/python.md`](../../samples/data-driven-rule-sets/python.md) —
  the fuller worked version, in Python.
