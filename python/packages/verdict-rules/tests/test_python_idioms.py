"""Coverage for a Python-specific idiom, not a universal contract.

Separated out of test_engine.py on purpose: test_rule.py and
test_engine.py together are this package's core contract suite, proving
behavior every implementation guarantees regardless of language. This
file proves something true only because of how Python itself works —
it belongs beside that suite, not inside it.
"""

from __future__ import annotations

from verdict.engine import RulesEngine
from verdict.result import RuleResult
from verdict.rule import FunctionRule


def _fail(name: str, group: str | None = None) -> FunctionRule:
    async def predicate(context: dict) -> RuleResult:
        return RuleResult(rule_name=name, passed=False)
    return FunctionRule(name, predicate, group=group)


class TestTruthiness:
    """``RuleResult``/``RunResult`` stay truthy no matter their content.

    Python-specific: no other language in this suite has an implicit
    truthiness conversion for a plain object, so this idiom — and the
    footgun it guards against — simply doesn't exist there.
    """

    async def test_results_are_always_truthy(self) -> None:
        """Load-bearing for the ``x or default`` idiom the docs mention.

        Python's ``or`` fires on any falsy value, not only ``None``. These
        types are plain dataclasses with no ``__bool__`` or ``__len__``, so
        every instance is truthy and ``or`` reaches its default only on an
        actual absence. Adding ``__len__`` to :class:`RunResult` later would
        make an empty-results instance falsy and silently break that — so the
        property is pinned here rather than left as an accident.
        """
        engine = RulesEngine([_fail("f", group="failing")])

        failing = await engine.try_run_group("failing", {})
        assert failing is not None
        assert failing.passed is False
        assert bool(failing) is True, "a failing RunResult must still be truthy"

        empty = await engine.run_all({})
        assert bool(empty) is True, "a zero-result RunResult must still be truthy"

        rule_result = await engine.try_run_named("f", {})
        assert rule_result is not None and rule_result.passed is False
        assert bool(rule_result) is True, "a failing RuleResult must still be truthy"
