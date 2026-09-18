"""Rule definitions — the unit of evaluation this package is built around.

A :class:`Rule` has a ``name``, an optional ``group``, and an
``evaluate(context)`` coroutine returning a :class:`~verdict.result.RuleResult`.
:class:`FunctionRule` wraps a plain predicate. :class:`AndRule`/:class:`OrRule`
compose other rules, short-circuiting the same way a boolean ``and``/``or``
expression does.

``Rule`` is generic over the context it reads from (``TContext``), erased at
runtime. ``Rule[dict[str, Any]]`` is the dict-context spelling; the bare
``Rule`` erases to ``Rule[Any]``.
"""

from __future__ import annotations

from typing import Awaitable, Callable, Generic, Protocol, TypeVar, runtime_checkable

from verdict.result import RuleResult

TContext = TypeVar("TContext")


@runtime_checkable
class Rule(Protocol[TContext]):
    """Structural interface every rule (plain or composite) satisfies.

    Attributes:
        name: Unique identifier for this rule within a
            :class:`~verdict.engine.RulesEngine` — used for
            :meth:`~verdict.engine.RulesEngine.run_named` lookups and to
            label this rule's own entry in a
            :class:`~verdict.result.RunResult`.
        group: Optional group label — rules sharing a group can be run
            together via :meth:`~verdict.engine.RulesEngine.run_group`.
            ``None`` if this rule doesn't belong to any group.
    """

    name: str
    group: str | None

    async def evaluate(self, context: TContext) -> RuleResult:
        """Evaluate this rule against ``context``.

        Args:
            context: Data the rule's condition reads from.

        Returns:
            The outcome of evaluating this rule.
        """
        ...


class FunctionRule(Generic[TContext]):
    """Wraps a plain async predicate as a :class:`Rule`.

    ``TContext`` is inferred from the wrapped predicate's own type
    annotation. An untyped predicate infers ``Any``.

    Attributes:
        name: See :class:`Rule`.
        group: See :class:`Rule`.
    """

    def __init__(
        self,
        name: str,
        predicate: Callable[[TContext], Awaitable[RuleResult]],
        group: str | None = None,
    ) -> None:
        """Initialise with a name and the predicate to run.

        Args:
            name: Unique identifier for this rule.
            predicate: Async callable that inspects ``context`` and
                returns the full :class:`~verdict.result.RuleResult`.
            group: Optional group label, see :class:`Rule`.
        """
        self.name = name
        self.group = group
        self._predicate = predicate

    async def evaluate(self, context: TContext) -> RuleResult:
        """Run the wrapped predicate against ``context``.

        Args:
            context: See :meth:`Rule.evaluate`.

        Returns:
            Whatever the wrapped predicate returns, unchanged.
        """
        return await self._predicate(context)


class AndRule(Generic[TContext]):
    """Composite rule that passes only if every sub-rule passes.

    Short-circuits on the first failing sub-rule.

    Every sub-rule must share the same ``TContext``.

    Attributes:
        name: See :class:`Rule`.
        group: See :class:`Rule`.
    """

    def __init__(self, name: str, rules: list[Rule[TContext]], group: str | None = None) -> None:
        """Initialise with the ordered sub-rules to combine.

        Args:
            name: Unique identifier for this composite rule.
            rules: Sub-rules evaluated in order until one fails (or all
                pass). An empty list vacuously passes.
            group: Optional group label, see :class:`Rule`.
        """
        self.name = name
        self.group = group
        self._rules = rules

    async def evaluate(self, context: TContext) -> RuleResult:
        """Evaluate sub-rules in order, stopping at the first failure.

        Args:
            context: See :meth:`Rule.evaluate`.

        Returns:
            A :class:`~verdict.result.RuleResult` for this composite
            rule itself — ``passed`` is ``True`` only if every sub-rule
            evaluated passed; ``data`` carries the list of per-sub-rule
            results gathered so far.
        """
        sub_results: list[RuleResult] = []
        for rule in self._rules:
            result = await rule.evaluate(context)
            sub_results.append(result)
            if not result.passed:
                return RuleResult(
                    rule_name=self.name,
                    passed=False,
                    detail=f"'{rule.name}' failed: {result.detail}".rstrip(": "),
                    data=sub_results,
                )
        return RuleResult(rule_name=self.name, passed=True, data=sub_results)


class OrRule(Generic[TContext]):
    """Composite rule that passes if any sub-rule passes.

    Short-circuits on the first passing sub-rule. The same
    same-``TContext`` requirement across sub-rules applies here too.

    Attributes:
        name: See :class:`Rule`.
        group: See :class:`Rule`.
    """

    def __init__(self, name: str, rules: list[Rule[TContext]], group: str | None = None) -> None:
        """Initialise with the ordered sub-rules to combine.

        Args:
            name: Unique identifier for this composite rule.
            rules: Sub-rules evaluated in order until one passes (or all
                fail). An empty list vacuously fails.
            group: Optional group label, see :class:`Rule`.
        """
        self.name = name
        self.group = group
        self._rules = rules

    async def evaluate(self, context: TContext) -> RuleResult:
        """Evaluate sub-rules in order, stopping at the first pass.

        Args:
            context: See :meth:`Rule.evaluate`.

        Returns:
            A :class:`~verdict.result.RuleResult` for this composite
            rule itself — ``passed`` is ``True`` as soon as any sub-rule
            passes; ``data`` carries the list of per-sub-rule results
            gathered so far.
        """
        sub_results: list[RuleResult] = []
        for rule in self._rules:
            result = await rule.evaluate(context)
            sub_results.append(result)
            if result.passed:
                return RuleResult(rule_name=self.name, passed=True, data=sub_results)
        return RuleResult(
            rule_name=self.name,
            passed=False,
            detail="no sub-rule passed",
            data=sub_results,
        )
