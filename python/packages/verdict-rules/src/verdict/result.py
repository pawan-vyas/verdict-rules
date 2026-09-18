"""Result types returned by rule evaluation."""

from __future__ import annotations

from dataclasses import dataclass, field


@dataclass(frozen=True)
class RuleResult:
    """Outcome of evaluating a single :class:`~verdict.rule.Rule`.

    Attributes:
        rule_name: The name of the rule this result came from.
        passed: Whether the rule's condition was satisfied.
        detail: Optional human-readable explanation of the outcome.
            Empty string when there's nothing to say beyond the boolean.
        data: Optional payload a caller can attach; opaque to this
            package.
    """

    rule_name: str
    passed: bool
    detail: str = ""
    data: object | None = None


@dataclass(frozen=True)
class RunResult:
    """Aggregate outcome of running a whole set of rules
    (:meth:`~verdict.engine.RulesEngine.run_all` or
    :meth:`~verdict.engine.RulesEngine.run_group`).

    Attributes:
        passed: ``True`` only if every rule in :attr:`results` passed.
        results: One :class:`RuleResult` per rule that was evaluated, in
            evaluation order. A composite rule's own sub-results are
            nested inside its own :attr:`RuleResult.data`, not
            flattened into this list.
    """

    passed: bool
    results: list[RuleResult] = field(default_factory=list)
