<!-- Title: Extending — Deciding What A Missing Rule Set Means (Python) -->
# Deciding what a missing rule set means: Python

> The concept, the table, and the diagram are in
> [`README.md`](README.md) — read that first. This page is the concrete
> Python code.

```python
from dataclasses import dataclass

from verdict import FunctionRule, RuleResult, RulesEngine


@dataclass(frozen=True)
class UserContext:
    beta_tester: bool = False


async def is_beta_tester(context: UserContext) -> RuleResult:
    return RuleResult(rule_name="is_beta_tester", passed=context.beta_tester)


engine: RulesEngine[UserContext] = RulesEngine([FunctionRule("is_beta_tester", is_beta_tester, group="beta_checks")])

result = await engine.try_run_group("beta_checks", UserContext(beta_tester=True))
# RunResult(passed=True, results=[RuleResult(rule_name='is_beta_tester', passed=True, ...)])

await engine.try_run_group("no_such_group", UserContext())
# None — the group was never registered
```

The four situations from the spec, as four different ways to consume
that same `None`:

```python
# 1. Absence means "no constraint applies"
result = await engine.try_run_group(group, context)
allowed = result.passed if result is not None else True

# 2. Absence means "the configuration is wrong"
allowed = result.passed if result is not None else False

# 3. Absence means "skip it" — contributes nothing either way
checks = [r for r in (result,) if r is not None]

# 4. Absence is genuinely unexpected — say so immediately
result = await engine.run_group(group, context)  # raises
```

`try_run_group` is the primitive `run_group` is built on, not the other
way around:

```python
async def run_group(self, group, context):
    result = await self.try_run_group(group, context)
    if result is None:
        raise KeyError(...)
    return result
```

There is one lookup path. The strict form is a two-line assertion on top
of the lenient one, rather than a second implementation that could drift
from it.

If you only need to enumerate what exists, `engine.rule_names` and
`engine.group_names` report exactly the lookups that will not raise.

## Related

- [`README.md`](README.md) — the language-agnostic scenario this page
  implements.
