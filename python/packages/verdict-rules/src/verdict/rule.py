"""Rule definitions — the unit of evaluation this package is built around.

A :class:`Rule` is anything with a ``name``, an optional ``group``, and an
``evaluate(context)`` coroutine returning a :class:`~verdict.result.RuleResult` —
:class:`FunctionRule` is the common case (wrap a plain predicate),
:class:`AndRule`/:class:`OrRule` compose other rules into one, short-
circuiting the same way a boolean ``and``/``or`` expression would.

``Rule`` is generic over the context it reads from (``TContext``). This is
purely a typing-level addition — Python erases generics at runtime, so
``isinstance(x, Rule)`` still only ever checks attribute/method presence,
never a type parameter, and every existing structural rule keeps satisfying
``Rule`` unconditionally regardless of whether it names a type argument. A
rule written against a plain ``dict`` context is ``Rule[dict[str, Any]]`` —
written out explicitly for a caller who wants the same strictness a typed
context gives, or left as the bare ``Rule`` (which erases to
``Rule[Any]``) for a caller who doesn't. Neither is an escape hatch from the
other: dict-context rules are exactly as first-class as typed ones, because
the same rule logic often needs to run against genuinely different context
shapes (see docs/architecture/ for the full reasoning).
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
            context: Arbitrary data the rule's condition reads from —
                Verdict never inspects or constrains its shape beyond
                whatever type this rule itself declares, the caller and
                its rules agree on it privately.

        Returns:
            The outcome of evaluating this rule.
        """
        ...


class FunctionRule(Generic[TContext]):
    """Wraps a plain async predicate as a :class:`Rule`.

    The common case: most rules are just "run this function against the
    context and see what it says," without needing a dedicated class.

    ``TContext`` is inferred from the wrapped predicate's own type
    annotation — ``FunctionRule("x", predicate)`` needs no explicit type
    argument as long as ``predicate`` itself is annotated; an untyped
    predicate infers ``Any``, matching this package's looseness before
    generics existed at all.

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
                returns the full :class:`~verdict.result.RuleResult` —
                not just a bare boolean, so the predicate keeps full
                control over ``detail``/``data``.
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

    Short-circuits on the first failing sub-rule — later sub-rules are
    never evaluated once one has already failed, so a caller can rely on
    :class:`AndRule` never doing more work (or having more side effects,
    for a sub-rule whose predicate writes something) than the minimum
    needed to reach a verdict.

    Every sub-rule must share the same ``TContext`` — the type checker
    enforces this once a caller names a type argument, which is exactly
    what a generic composite buys over dict-context: today, two rules
    secretly expecting different shapes of dict can be combined under one
    ``AndRule`` and only fail at runtime on a missing key; once typed,
    that mismatch is a type error instead. Reusing one rule across two
    genuinely different context shapes goes through an explicit adapter
    (see docs/extending/) rather than loosening this constraint.

    Attributes:
        name: See :class:`Rule`.
        group: See :class:`Rule`.
    """

    def __init__(self, name: str, rules: list[Rule[TContext]], group: str | None = None) -> None:
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

    async def evaluate(self, context: TContext) -> RuleResult:
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


class OrRule(Generic[TContext]):
    """Composite rule that passes if any sub-rule passes.

    Short-circuits on the first passing sub-rule — the mirror image of
    :class:`AndRule`. The same same-``TContext`` requirement across
    sub-rules applies here too; see :class:`AndRule` for the reasoning.

    Attributes:
        name: See :class:`Rule`.
        group: See :class:`Rule`.
    """

    def __init__(self, name: str, rules: list[Rule[TContext]], group: str | None = None) -> None:
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
