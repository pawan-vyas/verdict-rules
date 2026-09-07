"""Verdict — a small, zero-dependency, async-native rule-evaluation engine.

A Rule / Engine / Result design — genuinely standalone, general-purpose
infrastructure with no dependency on or knowledge of any particular
consumer's domain. Rules are named,
composable units (:class:`FunctionRule` for a plain predicate,
:class:`AndRule`/:class:`OrRule` for short-circuiting composition); a
:class:`RulesEngine` runs a set of them against a plain ``dict``
context, by any of three modes (all, one named rule, one group).

See ``README.md`` for a quickstart and a worked example, and
``docs/architecture.md`` for the full architecture write-up (type
structure, the execution model, and why it's shaped this way).
"""

from verdict.engine import RulesEngine
from verdict.result import RuleResult, RunResult
from verdict.rule import AndRule, FunctionRule, OrRule, Rule

__all__ = [
    "AndRule",
    "FunctionRule",
    "OrRule",
    "Rule",
    "RuleResult",
    "RulesEngine",
    "RunResult",
]
