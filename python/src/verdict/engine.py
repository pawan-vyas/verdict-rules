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

    async def try_run_named(self, name: str, context: dict) -> RuleResult | None:
        """Evaluate one rule by name, or return ``None`` if no such rule exists.

        This is the primitive; :meth:`run_named` is a two-line assertion on
        top of it. The distinction matters when absence is an expected,
        legitimate state rather than a mistake — a rule set that varies per
        tenant, an optional group behind a feature flag, a name carried in
        configuration that a given deployment has not adopted yet.

        In those cases the caller decides what absence means, because the
        engine cannot: for one consumer a missing rule means "nothing to
        enforce, pass", for another "skip this and do not count it", for a
        third "the configuration is wrong, fail loudly". A single library
        default would be right for one of them and wrong for the rest.

        Args:
            name: The rule's own ``name`` attribute.
            context: Passed through unchanged to the rule's ``evaluate()``.

        Returns:
            That rule's own :class:`~verdict.result.RuleResult`, or ``None``
            if no rule carries this name. ``None`` means *absent*, never
            *failed* — a rule that exists and fails returns a
            :class:`~verdict.result.RuleResult` with ``passed=False``.
        """
        rule = self._by_name.get(name)
        if rule is None:
            return None
        return await rule.evaluate(context)

    async def run_named(self, name: str, context: dict) -> RuleResult:
        """Evaluate exactly one rule, looked up by name.

        The strict form, and the one to reach for by default: if a name is
        not expected to be absent, an absent name is a bug worth hearing
        about immediately. Use :meth:`try_run_named` when absence is a state
        your own domain has an answer for.

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
        self, group: str, context: dict
    ) -> RunResult | None:
        """Evaluate a group, or return ``None`` if no such group exists.

        This is the primitive; :meth:`run_group` is a two-line assertion on
        top of it. See :meth:`try_run_named` for when reaching for it is
        right — the short version is that the engine cannot know whether an
        absent group means "no constraint applies here" or "the
        configuration is broken", and only the caller can.

        Args:
            group: The group label to match against each rule's own
                ``group`` attribute.
            context: Passed through unchanged to every matching rule's
                ``evaluate()``.

        Returns:
            A :class:`~verdict.result.RunResult` scoped to just this group,
            or ``None`` if no rule carries this label.

            ``None`` means *absent*, never *vacuously passed*. That
            distinction is the whole point: a group exists only by virtue of
            a rule declaring it, so an empty-but-real group is not
            representable, and a lookup matching nothing can only be a typo
            or a stale name. Returning a passing
            :class:`~verdict.result.RunResult` here would mean a misspelled
            group silently approves.
        """
        rules = self._by_group.get(group)
        if not rules:
            return None
        results = [await rule.evaluate(context) for rule in rules]
        return RunResult(passed=all(r.passed for r in results), results=results)

    async def run_group(self, group: str, context: dict) -> RunResult:
        """Evaluate every rule sharing a given group label.

        The strict form, and the one to reach for by default. Use
        :meth:`try_run_group` when absence is a state your own domain has an
        answer for.

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
        """Every rule name registered on this engine, in registration order.

        Returns:
            The names :meth:`run_named` accepts without raising. Useful
            for enumerating an engine; when the question is only "does
            this one exist", :meth:`try_run_named` answers it in a single
            call rather than a scan followed by a lookup.
        """
        return tuple(self._by_name)

    @property
    def group_names(self) -> tuple[str, ...]:
        """Every group label carried by at least one rule on this engine.

        Returns:
            The labels :meth:`run_group` accepts without raising. A group
            is only present because some rule declared it, so this is
            exactly the set of lookups that will not raise; when the
            question is only "does this one exist",
            :meth:`try_run_group` answers it in a single call.
        """
        return tuple(self._by_group)
