"""Unit tests for verdict.rule: FunctionRule, AndRule, OrRule."""

from __future__ import annotations

import pytest

from verdict.result import RuleResult
from verdict.rule import AndRule, FunctionRule, OrRule, PredicateOutcome


def _pass(name: str, data: object = None) -> FunctionRule:
    """A FunctionRule that always passes, for composing test fixtures."""
    async def predicate(context: dict) -> PredicateOutcome:
        return PredicateOutcome(passed=True, data=data)
    return FunctionRule(name, predicate)


def _fail(name: str, detail: str = "") -> FunctionRule:
    """A FunctionRule that always fails, for composing test fixtures."""
    async def predicate(context: dict) -> PredicateOutcome:
        return PredicateOutcome(passed=False, detail=detail)
    return FunctionRule(name, predicate)


async def _noop_predicate(context: dict) -> PredicateOutcome:
    """A predicate that always passes — used where only the rule's
    name/group matter to the test, not its evaluation outcome."""
    return PredicateOutcome(passed=True)


class TestFunctionRule:
    async def test_wraps_a_passing_predicate(self) -> None:
        rule = _pass("r1")
        result = await rule.evaluate({})
        assert result.passed is True
        assert result.rule_name == "r1"

    async def test_wraps_a_failing_predicate(self) -> None:
        rule = _fail("r1", detail="nope")
        result = await rule.evaluate({})
        assert result.passed is False
        assert result.detail == "nope"

    async def test_a_leaf_result_names_nothing_in_decided_by(self) -> None:
        """A bare leaf result -- FunctionRule.evaluate's own output, never
        built by SequentialEvaluator/ShortCircuitEvaluator/NotRule -- has no
        sub_results to point into, so decided_by must be empty for both a
        passing and a failing outcome. Nothing previously read decided_by on
        a leaf at all; every other decided_by test in this suite exercises a
        composite built on top of one."""
        passing = await _pass("r1").evaluate({})
        failing = await _fail("r1").evaluate({})
        assert passing.decided_by == []
        assert failing.decided_by == []

    async def test_predicate_receives_the_context(self) -> None:
        seen = {}

        async def predicate(context: dict) -> PredicateOutcome:
            seen.update(context)
            return PredicateOutcome(passed=True)

        rule = FunctionRule("r1", predicate)
        await rule.evaluate({"user_id": 42})
        assert seen == {"user_id": 42}

    def test_carries_name_and_group(self) -> None:
        rule = FunctionRule("r1", _noop_predicate, group="g1")
        assert rule.name == "r1"
        assert rule.group == "g1"

    def test_group_defaults_to_none(self) -> None:
        rule = FunctionRule("r1", _noop_predicate)
        assert rule.group is None

    def test_repr_shows_the_name(self) -> None:
        rule = FunctionRule("over_18", _noop_predicate)
        assert repr(rule) == 'FunctionRule "over_18"'

    def test_repr_shows_the_group_when_present(self) -> None:
        rule = FunctionRule("over_18", _noop_predicate, group="age")
        assert repr(rule) == 'FunctionRule "over_18" (age)'

    async def test_predicate_returning_the_wrong_type_raises_type_error(self) -> None:
        """RulePredicate's annotation is only checker-enforced, never
        runtime-enforced — an externally-authored predicate that returns
        the wrong shape must fail loudly, not silently duck-type through."""

        async def predicate(context: dict) -> PredicateOutcome:
            return "not a PredicateOutcome"  # type: ignore[return-value]

        rule = FunctionRule("r1", predicate)
        with pytest.raises(TypeError, match="r1"):
            await rule.evaluate({})

    async def test_type_error_names_the_actual_wrong_type_returned(self) -> None:
        """The message must name whatever type was actually returned, not a
        fixed placeholder -- using a type other than ``NoneType`` catches a
        mutant that hardcodes the name instead of reading ``type(outcome)``."""

        async def predicate(context: dict) -> PredicateOutcome:
            return 42  # type: ignore[return-value]

        rule = FunctionRule("r1", predicate)
        with pytest.raises(TypeError, match="int"):
            await rule.evaluate({})

    async def test_result_carries_the_predicate_s_data(self) -> None:
        """FunctionRule.evaluate must forward PredicateOutcome.data onto the
        RuleResult it builds -- nothing in the passing/failing-path tests
        above exercises a non-None payload."""
        rule = _pass("r1", data={"score": 42})
        result = await rule.evaluate({})
        assert result.data == {"score": 42}

    async def test_predicate_returning_a_rule_result_directly_raises_type_error(self) -> None:
        """The specific regression this check closes: a predicate built
        against the old, pre-PredicateOutcome calling convention returns a
        RuleResult directly. RuleResult duck-types close enough to
        PredicateOutcome (both carry `passed`/`detail`/`data`) that this
        would otherwise silently "work" by accident instead of surfacing
        the mismatch."""

        async def predicate(context: dict) -> PredicateOutcome:
            return RuleResult(rule_name="wrong-shape", passed=True)  # type: ignore[return-value]

        rule = FunctionRule("r1", predicate)
        with pytest.raises(TypeError, match="r1"):
            await rule.evaluate({})


class TestAndRule:
    async def test_all_pass_yields_pass(self) -> None:
        rule = AndRule("and1", [_pass("a"), _pass("b")])
        result = await rule.evaluate({})
        assert result.passed is True
        assert result.rule_name == "and1"
        assert [r.rule_name for r in result.sub_results] == ["a", "b"]

    async def test_one_failure_yields_fail(self) -> None:
        rule = AndRule("and1", [_pass("a"), _fail("b", detail="bad")])
        result = await rule.evaluate({})
        assert result.passed is False
        assert result.rule_name == "and1"
        # AndRule's own `detail` is empty -- composing a shared
        # ShortCircuitEvaluator means there's no per-composite channel left
        # to build a descriptive string from a sub-rule's own name/detail
        # (see StepDecider's signature: bool | None, nothing else). The
        # failing sub-rule and its own detail are still fully recoverable
        # from `sub_results`/`decided_by` instead.
        assert result.detail == ""
        assert len(result.decided_by) == 1
        failing = result.decided_by[0]
        assert failing.rule_name == "b"
        assert failing.detail == "bad"

    async def test_short_circuits_after_first_failure(self) -> None:
        calls: list[str] = []

        async def tracked_pass(context: dict) -> PredicateOutcome:
            calls.append("c")
            return PredicateOutcome(passed=True)

        rule = AndRule("and1", [_fail("a"), FunctionRule("c", tracked_pass)])
        await rule.evaluate({})
        assert calls == []  # never reached — 'a' already failed

    async def test_sub_results_carries_sub_results_up_to_failure(self) -> None:
        rule = AndRule("and1", [_pass("a"), _fail("b"), _pass("c")])
        result = await rule.evaluate({})
        assert [r.rule_name for r in result.sub_results] == ["a", "b"]

    async def test_empty_rule_list_vacuously_passes(self) -> None:
        rule = AndRule("and1", [])
        result = await rule.evaluate({})
        assert result.passed is True

    def test_repr_shows_the_name_and_sub_rule_count(self) -> None:
        rule = AndRule("and1", [_pass("a"), _pass("b")])
        assert repr(rule) == 'AndRule "and1" — 2 sub-rule(s)'

    def test_repr_shows_the_group_when_present(self) -> None:
        rule = AndRule("and1", [_pass("a")], group="checkout")
        assert repr(rule) == 'AndRule "and1" (checkout) — 1 sub-rule(s)'


class TestOrRule:
    async def test_any_pass_yields_pass(self) -> None:
        rule = OrRule("or1", [_fail("a"), _pass("b")])
        result = await rule.evaluate({})
        assert result.passed is True
        assert result.rule_name == "or1"
        assert [r.rule_name for r in result.sub_results] == ["a", "b"]

    async def test_all_fail_yields_fail(self) -> None:
        rule = OrRule("or1", [_fail("a"), _fail("b")])
        result = await rule.evaluate({})
        assert result.passed is False
        assert result.rule_name == "or1"
        # OrRule's own `detail` is empty, same reasoning as AndRule's -- see
        # the comment in TestAndRule.test_one_failure_yields_fail.
        assert result.detail == ""
        assert [r.rule_name for r in result.sub_results] == ["a", "b"]

    async def test_short_circuits_after_first_pass(self) -> None:
        calls: list[str] = []

        async def tracked_fail(context: dict) -> PredicateOutcome:
            calls.append("c")
            return PredicateOutcome(passed=False)

        rule = OrRule("or1", [_pass("a"), FunctionRule("c", tracked_fail)])
        await rule.evaluate({})
        assert calls == []  # never reached — 'a' already passed

    async def test_empty_rule_list_vacuously_fails(self) -> None:
        rule = OrRule("or1", [])
        result = await rule.evaluate({})
        assert result.passed is False

    def test_repr_shows_the_name_and_sub_rule_count(self) -> None:
        rule = OrRule("or1", [_fail("a"), _pass("b")])
        assert repr(rule) == 'OrRule "or1" — 2 sub-rule(s)'

    def test_repr_shows_the_group_when_present(self) -> None:
        rule = OrRule("or1", [_pass("a")], group="checkout")
        assert repr(rule) == 'OrRule "or1" (checkout) — 1 sub-rule(s)'


class TestExceptionPropagation:
    """AndRule/OrRule catch nothing either — a sub-rule's own exception
    propagates straight out of evaluate(), the same guarantee test_engine.py
    proves for run_all/run_group. See docs/extending/isolating-flaky-predicates/
    for the wrapper a consumer opts into if they want the opposite."""

    async def test_and_rule_does_not_catch_a_sub_rule_s_exception(self) -> None:
        async def flaky(context: dict) -> PredicateOutcome:
            raise RuntimeError("boom")

        rule = AndRule("and1", [_pass("a"), FunctionRule("flaky", flaky)])
        with pytest.raises(RuntimeError):
            await rule.evaluate({})

    async def test_or_rule_does_not_catch_a_sub_rule_s_exception(self) -> None:
        async def flaky(context: dict) -> PredicateOutcome:
            raise RuntimeError("boom")

        rule = OrRule("or1", [_fail("a"), FunctionRule("flaky", flaky)])
        with pytest.raises(RuntimeError):
            await rule.evaluate({})
