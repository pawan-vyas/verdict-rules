"""The engine that runs rules against a context.

Holds a rule collection and offers three execution modes (all, named,
grouped), resolved via plain dict lookups rather than an if/elif chain.
"""

from __future__ import annotations

from collections import defaultdict

from verdict.result import RuleResult, RunResult
from verdict.rule import Rule


class RulesEngine:
    """Holds a set of rules and runs them against a context.

    Unlike :class:`~verdict.rule.AndRule`/:class:`~verdict.rule.OrRule`
    (which short-circuit to reach a single composite verdict
    efficiently), every ``run_*`` method here evaluates every matching
    rule unconditionally — the point of the engine's own run methods is
    a full diagnostic picture (every rule's outcome), not the fastest
    path to one boolean. Compose rules with ``AndRule``/``OrRule``
    first if short-circuiting is what a particular call site wants.
    """

    def __init__(self, rules: list[Rule]) -> None:
        """Initialise with the full rule set this engine will serve.

        Args:
            rules: Every rule this engine can run, by any of its three
                execution modes. Names must be unique within this list —
                a duplicate name silently shadows the earlier one in
                :meth:`run_named`'s lookup, same as an ordinary dict
                literal would.
        """
        self._rules = rules
        self._by_name: dict[str, Rule] = {r.name: r for r in rules}
        self._by_group: dict[str, list[Rule]] = defaultdict(list)
        for rule in rules:
            if rule.group:
                self._by_group[rule.group].append(rule)

    async def run_all(self, context: dict) -> RunResult:
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

    async def run_named(self, name: str, context: dict) -> RuleResult:
        """Evaluate exactly one rule, looked up by name.

        Args:
            name: The rule's own ``name`` attribute.
            context: Passed through unchanged to the rule's ``evaluate()``.

        Returns:
            That rule's own :class:`~verdict.result.RuleResult`.

        Raises:
            KeyError: No rule with this name exists in this engine.
        """
        rule = self._by_name.get(name)
        if rule is None:
            raise KeyError(f"No rule named {name!r} in this engine")
        return await rule.evaluate(context)

    async def run_group(self, group: str, context: dict) -> RunResult:
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
                A group exists only by virtue of a rule declaring it, so
                an empty-but-real group is not representable — a lookup
                that matches nothing is always a mistake (a typo, or a
                stale group name), never a legitimately empty set. It is
                reported the same way :meth:`run_named` reports an
                unknown rule name, rather than silently returning a
                vacuous pass. Use :attr:`group_names` to check first if
                the group may legitimately be absent.
        """
        rules = self._by_group.get(group)
        if not rules:
            raise KeyError(f"No rules in group {group!r} in this engine")
        results = [await rule.evaluate(context) for rule in rules]
        return RunResult(passed=all(r.passed for r in results), results=results)

    @property
    def rule_names(self) -> tuple[str, ...]:
        """Every rule name registered on this engine, in registration order.

        Returns:
            The names accepted by :meth:`run_named`, so a caller that
            cannot know in advance whether a rule exists can check
            rather than catch.
        """
        return tuple(self._by_name)

    @property
    def group_names(self) -> tuple[str, ...]:
        """Every group label carried by at least one rule on this engine.

        Returns:
            The labels accepted by :meth:`run_group`. A group is only
            present because some rule declared it, so this is exactly
            the set of lookups that will not raise.
        """
        return tuple(self._by_group)
