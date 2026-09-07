"""Rule definitions — the unit of evaluation this package is built around.

A :class:`Rule` is anything with a ``name``, an optional ``group``, and an
``evaluate(context)`` coroutine returning a :class:`~verdict.result.RuleResult` —
:class:`FunctionRule` is the common case (wrap a plain predicate),
:class:`AndRule`/:class:`OrRule` compose other rules into one, short-
circuiting the same way a boolean ``and``/``or`` expression would.
"""

from __future__ import annotations

from typing import Awaitable, Callable, Protocol, runtime_checkable

from verdict.result import RuleResult


@runtime_checkable
class Rule(Protocol):
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

    async def evaluate(self, context: dict) -> RuleResult:
        """Evaluate this rule against ``context``.

        Args:
            context: Arbitrary key-value data the rule's condition reads
                from — Verdict never inspects or constrains its shape,
                the caller and its rules agree on it privately.

        Returns:
            The outcome of evaluating this rule.
        """
        ...


class FunctionRule:
    """Wraps a plain async predicate as a :class:`Rule`.

    The common case: most rules are just "run this function against the
    context and see what it says," without needing a dedicated class.

    Attributes:
        name: See :class:`Rule`.
        group: See :class:`Rule`.
    """

    def __init__(
        self,
        name: str,
        predicate: Callable[[dict], Awaitable[RuleResult]],
        group: str | None = None,
    ) -> None:
        """Initialise with a name and the predicate to run.

        Args:
            name: Unique identifier for this rule.
            predicate: Async callable that inspects ``context`` and
                returns the full :class:`~verdict.result.RuleResult` —
                not just a bare boolean, so the predicate keeps full
                control over ``detail``/``data``.
            group: Optional group label, see :class:`Rule`.
        """
        self.name = name
        self.group = group
        self._predicate = predicate

    async def evaluate(self, context: dict) -> RuleResult:
        """Run the wrapped predicate against ``context``.

        Args:
            context: See :meth:`Rule.evaluate`.

        Returns:
            Whatever the wrapped predicate returns, unchanged.
        """
        return await self._predicate(context)


class AndRule:
    """Composite rule that passes only if every sub-rule passes.

    Short-circuits on the first failing sub-rule — later sub-rules are
    never evaluated once one has already failed, so a caller can rely on
    :class:`AndRule` never doing more work (or having more side effects,
    for a sub-rule whose predicate writes something) than the minimum
    needed to reach a verdict.

    Attributes:
        name: See :class:`Rule`.
        group: See :class:`Rule`.
    """

    def __init__(self, name: str, rules: list[Rule], group: str | None = None) -> None:
        """Initialise with the ordered sub-rules to combine.

        Args:
            name: Unique identifier for this composite rule.
            rules: Sub-rules evaluated in order until one fails (or all
                pass). An empty list vacuously passes — no sub-rule
                means nothing to fail on.
            group: Optional group label, see :class:`Rule`.
        """
        self.name = name
        self.group = group
        self._rules = rules

    async def evaluate(self, context: dict) -> RuleResult:
        """Evaluate sub-rules in order, stopping at the first failure.

        Args:
            context: See :meth:`Rule.evaluate`.

        Returns:
            A :class:`~verdict.result.RuleResult` for this composite
            rule itself — ``passed`` is ``True`` only if every sub-rule
            evaluated passed; ``data`` carries the list of per-sub-rule
            results gathered so far (the full list on success, the
            results up to and including the first failure otherwise).
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


class OrRule:
    """Composite rule that passes if any sub-rule passes.

    Short-circuits on the first passing sub-rule — the mirror image of
    :class:`AndRule`.

    Attributes:
        name: See :class:`Rule`.
        group: See :class:`Rule`.
    """

    def __init__(self, name: str, rules: list[Rule], group: str | None = None) -> None:
        """Initialise with the ordered sub-rules to combine.

        Args:
            name: Unique identifier for this composite rule.
            rules: Sub-rules evaluated in order until one passes (or all
                fail). An empty list vacuously fails — no sub-rule means
                nothing to pass on.
            group: Optional group label, see :class:`Rule`.
        """
        self.name = name
        self.group = group
        self._rules = rules

    async def evaluate(self, context: dict) -> RuleResult:
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
