"""Verdict — a small, zero-dependency, async-native rule-evaluation engine.

Rules are named, composable units (:class:`FunctionRule` for a plain
predicate, :class:`AndRule`/:class:`OrRule` for short-circuiting
composition); a :class:`RulesEngine` runs a set of them against a
context, by any of three modes (all, one named rule, one group).

See ``README.md`` for a quickstart and a worked example, and
``docs/architecture/`` for the full architecture write-up.
"""

from verdict.engine import RulesEngine
from verdict.result import RuleResult, RunResult
from verdict.rule import AndRule, FunctionRule, OrRule, Rule, TContext

__all__ = [
    "AndRule",
    "FunctionRule",
    "OrRule",
    "Rule",
    "RuleResult",
    "RulesEngine",
    "RunResult",
    "TContext",
]
