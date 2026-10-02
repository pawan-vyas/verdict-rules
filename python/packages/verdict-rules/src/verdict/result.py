"""Result types returned by rule evaluation."""

from __future__ import annotations

from dataclasses import dataclass, field
from typing import Sequence


@dataclass(frozen=True)
class RuleResult:
    """Outcome of evaluating a single :class:`~verdict.rule.Rule`.

    Attributes:
        rule_name: The name of the rule this result came from.
        passed: Whether the rule's condition was satisfied.
        detail: Optional human-readable explanation of the outcome.
            Empty string when there's nothing to say beyond the boolean.
        data: Optional payload a caller can attach; genuinely opaque to
            this package — never read or written by verdict itself. A
            composite rule's own children live in :attr:`sub_results`,
            never here.
        sub_results: This result's own children, in evaluation order —
            empty for a leaf result. A composite rule
            (:class:`~verdict.rule.AndRule`, :class:`~verdict.rule.OrRule`,
            :class:`~verdict.rule.NotRule`, or a custom composite built on
            :class:`~verdict.rule.SequentialEvaluator`) populates this
            with its own sub-results; an empty tuple *is* the leaf
            signal, structurally, not just by convention.
        decided_by: Which of :attr:`sub_results` explain *this* result's
            own verdict — a one-level, non-recursive fact, populated once
            by whatever built this result. Not the same question
            :attr:`failing_leaves` answers (recursively, the terminal
            failures): a failed ``NotRule``'s ``decided_by`` is its
            *passing* inner child, which is correct for "why did this
            fail" but is not something to keep walking into expecting
            ``failing_leaves``-equivalence. Empty for a leaf or a vacuous
            composite.
    """

    rule_name: str
    passed: bool
    detail: str = ""
    data: object | None = None
    sub_results: Sequence[RuleResult] = field(default_factory=tuple)
    decided_by: Sequence[RuleResult] = field(default_factory=tuple)

    @property
    def leaves(self) -> list[RuleResult]:
        """This result's own leaves, flattened, in evaluation order.

        A leaf is any result with no :attr:`sub_results` of its own —
        for a non-composite result, that's itself. Plain recursion over
        :attr:`sub_results`.
        """
        if not self.sub_results:
            return [self]
        return [leaf for sub in self.sub_results for leaf in sub.leaves]

    @property
    def failing_leaves(self) -> list[RuleResult]:
        """The leaves that explain why this result failed.

        An independent recursion, **not** a filter over :attr:`leaves` —
        the two formulas diverge on purpose:

        - A failed result with no failing children is itself the leaf.
          This handles negation correctly without a composite like
          :class:`~verdict.rule.NotRule` having to misrepresent its own
          structure: a failed ``NotRule`` wraps an inner rule that
          *passed*, so recursing into its sub-results would find no
          failures at all, and the flattened list would need to still
          end up with exactly one entry — this result itself.
        - A passed result always contributes no failing leaves, even if
          an earlier short-circuited branch failed on the way to that
          pass (an ``OrRule`` whose first sub-rule failed before its
          second one passed reports no failing leaves at all, correctly
          — the rule passed).
        """
        if self.passed:
            return []
        child_failures = [leaf for sub in self.sub_results for leaf in sub.failing_leaves]
        return child_failures if child_failures else [self]


@dataclass(frozen=True)
class RunResult:
    """Aggregate outcome of running a whole set of rules
    (:meth:`~verdict.engine.RulesEngine.run_all` or
    :meth:`~verdict.engine.RulesEngine.run_group`).

    Attributes:
        passed: ``True`` only if every rule in :attr:`results` passed.
        results: One :class:`RuleResult` per rule that was evaluated, in
            evaluation order. A composite rule's own sub-results live in
            that rule's own :attr:`RuleResult.sub_results`, not
            flattened into this list — see :attr:`leaves` for the
            flattened view across every rule this run evaluated.
    """

    passed: bool
    results: list[RuleResult] = field(default_factory=list)

    @property
    def leaves(self) -> list[RuleResult]:
        """Every leaf across every rule this run evaluated, flattened,
        in evaluation order. One-line forwarder to each result's own
        :attr:`RuleResult.leaves`."""
        return [leaf for result in self.results for leaf in result.leaves]

    @property
    def failing_leaves(self) -> list[RuleResult]:
        """Every failing leaf across every rule this run evaluated."""
        return [leaf for leaf in self.leaves if not leaf.passed]
