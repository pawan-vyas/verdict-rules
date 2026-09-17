<!-- Title: Extending — Reusing a Typed Rule Across Contexts (Python) -->
# Reusing a typed rule across contexts: Python

> The concept, the diagram, and the trade-off are in
> [`README.md`](README.md) — read that first. This page is the concrete
> Python code.

```python
from typing import Callable, Generic, TypeVar
from verdict import Rule, RuleResult

TOuter = TypeVar("TOuter")
TInner = TypeVar("TInner")


class ProjectingRule(Generic[TOuter, TInner]):
    """Adapts a Rule[TInner] to run inside a composite built on TOuter.

    Not part of verdict itself -- a consumer-defined adapter, exactly as
    free to exist as a new rule shape is, with no changes needed on
    verdict's side to support it.
    """

    def __init__(self, inner: Rule[TInner], project: Callable[[TOuter], TInner]) -> None:
        self.name = inner.name
        self.group = inner.group
        self._inner = inner
        self._project = project

    async def evaluate(self, context: TOuter) -> RuleResult:
        return await self._inner.evaluate(self._project(context))
```

Written once against its own narrow context, the same rule now projects
into two unrelated composites:

```python
from dataclasses import dataclass
from verdict import AndRule, FunctionRule


@dataclass(frozen=True)
class UserFlag:
    is_verified: bool


async def is_verified_user(context: UserFlag) -> RuleResult:
    return RuleResult(rule_name="is_verified_user", passed=context.is_verified)


@dataclass(frozen=True)
class OrderContext:
    total: float
    is_verified: bool


@dataclass(frozen=True)
class SignupContext:
    email: str
    is_verified: bool


verified_rule = FunctionRule("is_verified_user", is_verified_user)

checkout_verified = ProjectingRule(verified_rule, lambda ctx: UserFlag(ctx.is_verified))
signup_verified = ProjectingRule(verified_rule, lambda ctx: UserFlag(ctx.is_verified))

checkout: AndRule[OrderContext] = AndRule("eligible", [checkout_verified])
signup: AndRule[SignupContext] = AndRule("eligible", [signup_verified])

result = await checkout.evaluate(OrderContext(total=75.0, is_verified=True))
result.passed  # True -- delegated straight through to is_verified_user
```

`ProjectingRule` satisfies `Rule[TOuter]` structurally, the same way
every other rule in this package does — nothing about `AndRule` or
`RulesEngine` needed to change to accept it.

## Related

- [`README.md`](README.md) — the language-agnostic spec this page
  implements.
- [`../new-rule-shape/python.md`](../new-rule-shape/python.md) — the
  same pattern (a consumer-defined type satisfying `Rule` structurally)
  applied to combination logic instead of context adaptation.
