"""Result types returned by rule evaluation.

Deliberately plain, immutable data — a :class:`Rule` reports what
happened, nothing more. Any domain-specific payload a caller wants to
carry alongside the pass/fail outcome rides in :attr:`RuleResult.data`,
which this module (and every other module in this package) treats as
fully opaque — Verdict itself never inspects or depends on its shape,
which is what keeps the engine reusable across unrelated domains.
"""

from __future__ import annotations

from dataclasses import dataclass, field


@dataclass(frozen=True)
class RuleResult:
    """Outcome of evaluating a single :class:`~verdict.rule.Rule`.

    Attributes:
        rule_name: The name of the rule this result came from — matches
            the evaluated rule's own ``name`` attribute, so a caller
            walking a :class:`RunResult` can attribute each outcome
            back to the rule that produced it.
        passed: Whether the rule's condition was satisfied.
        detail: Optional human-readable explanation of the outcome —
            e.g. why a rule failed. Empty string when there's nothing
            worth saying beyond the boolean.
        data: Optional, fully opaque payload a caller can attach to
            carry its own domain object through the evaluation (e.g. a
            computed status object) — Verdict never reads or depends on
            its shape.
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
        results: One :class:`RuleResult` per rule that was evaluated,
            in evaluation order. A short-circuited composite rule (see
            :class:`~verdict.rule.AndRule`/:class:`~verdict.rule.OrRule`)
            still contributes exactly one entry here for itself — its
            own sub-rule results are nested inside its own
            :attr:`RuleResult.data`, not flattened into this list.
    """

    passed: bool
    results: list[RuleResult] = field(default_factory=list)
