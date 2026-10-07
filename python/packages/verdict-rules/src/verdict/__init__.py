"""Verdict — a small, zero-dependency, async-native rule-evaluation engine.

Rules are named, composable units (:class:`FunctionRule` for a plain
predicate, :class:`AndRule`/:class:`OrRule`/:class:`NotRule` for
short-circuiting composition, built on the more general
:class:`SequentialEvaluator`/:class:`ShortCircuitEvaluator` a custom
composite can compose directly); a :class:`RulesEngine` runs a set of them
against a context, by any of three modes (all, one named rule, one group).

See ``README.md`` for a quickstart and a worked example, and
``docs/architecture/`` for the full architecture write-up.
"""

from verdict.engine import RulesEngine
from verdict.result import RuleResult, RunResult
from verdict.rule import (
    AndRule,
    CompositeRule,
    FunctionRule,
    NotRule,
    OrRule,
    PredicateOutcome,
    Rule,
    RulePredicate,
    SequentialEvaluator,
    ShortCircuitEvaluator,
    StepDecider,
    TContext,
)

__all__ = [
    "AndRule",
    "CompositeRule",
    "FunctionRule",
    "NotRule",
    "OrRule",
    "PredicateOutcome",
    "Rule",
    "RulePredicate",
    "RuleResult",
    "RulesEngine",
    "RunResult",
    "SequentialEvaluator",
    "ShortCircuitEvaluator",
    "StepDecider",
    "TContext",
]
