"""The engine that runs rules against a context."""

from __future__ import annotations

from collections import defaultdict
from typing import Generic

from verdict.result import RuleResult, RunResult
from verdict.rule import Rule, TContext


class RulesEngine(Generic[TContext]):
    """Holds a set of rules and runs them against a context.

    Every ``run_*`` method evaluates every matching rule unconditionally
    — no short-circuiting. Generic over ``TContext``, the same way
    :class:`~verdict.rule.Rule` is.
    """

    def __init__(self, rules: list[Rule[TContext]]) -> None:
        """Initialise with the full rule set this engine will serve.

        Args:
            rules: Every rule this engine can run. Names must be unique
                within this list — a duplicate name shadows the earlier
                one in :meth:`run_named`'s lookup.
        """
        self._rules = rules
        self._by_name: dict[str, Rule[TContext]] = {r.name: r for r in rules}
        self._by_group: dict[str, list[Rule[TContext]]] = defaultdict(list)
        for rule in rules:
            if rule.group:
                self._by_group[rule.group].append(rule)

    async def run_all(self, context: TContext) -> RunResult:
        """Evaluate every rule in this engine against ``context``.

        Args:
            context: Passed through unchanged to every rule's own
                ``evaluate()``.

        Returns:
            A :class:`~verdict.result.RunResult` — ``passed`` is
            ``True`` only if every rule passed; ``results`` holds one
            entry per rule, in the order the engine was constructed
            with.
        """
        results = [await rule.evaluate(context) for rule in self._rules]
        return RunResult(passed=all(r.passed for r in results), results=results)

    async def try_run_named(self, name: str, context: TContext) -> RuleResult | None:
        """Evaluate one rule by name, or return ``None`` if no such rule exists.

        Args:
            name: The rule's own ``name`` attribute.
            context: Passed through unchanged to the rule's ``evaluate()``.

        Returns:
            That rule's own :class:`~verdict.result.RuleResult`, or ``None``
            if no rule carries this name.
        """
        rule = self._by_name.get(name)
        if rule is None:
            return None
        return await rule.evaluate(context)

    async def run_named(self, name: str, context: TContext) -> RuleResult:
        """Evaluate exactly one rule, looked up by name.

        Args:
            name: The rule's own ``name`` attribute.
            context: Passed through unchanged to the rule's ``evaluate()``.

        Returns:
            That rule's own :class:`~verdict.result.RuleResult`.

        Raises:
            KeyError: No rule with this name exists in this engine.
        """
        result = await self.try_run_named(name, context)
        if result is None:
            raise KeyError(f"No rule named {name!r} in this engine")
        return result

    async def try_run_group(
        self, group: str, context: TContext
    ) -> RunResult | None:
        """Evaluate a group, or return ``None`` if no such group exists.

        Args:
            group: The group label to match against each rule's own
                ``group`` attribute.
            context: Passed through unchanged to every matching rule's
                ``evaluate()``.

        Returns:
            A :class:`~verdict.result.RunResult` scoped to just this group,
            or ``None`` if no rule carries this label.
        """
        rules = self._by_group.get(group)
        if not rules:
            return None
        results = [await rule.evaluate(context) for rule in rules]
        return RunResult(passed=all(r.passed for r in results), results=results)

    async def run_group(self, group: str, context: TContext) -> RunResult:
        """Evaluate every rule sharing a given group label.

        Args:
            group: The group label to match against each rule's own
                ``group`` attribute.
            context: Passed through unchanged to every matching rule's
                ``evaluate()``.

        Returns:
            A :class:`~verdict.result.RunResult` scoped to just this
            group — ``passed`` is ``True`` only if every rule in the
            group passed; ``results`` holds one entry per matching rule.

        Raises:
            KeyError: No rule in this engine carries this group label.
        """
        result = await self.try_run_group(group, context)
        if result is None:
            raise KeyError(f"No rules in group {group!r} in this engine")
        return result

    @property
    def rule_names(self) -> tuple[str, ...]:
        """Every rule name registered on this engine, in registration order."""
        return tuple(self._by_name)

    @property
    def group_names(self) -> tuple[str, ...]:
        """Every group label carried by at least one rule on this engine."""
        return tuple(self._by_group)
