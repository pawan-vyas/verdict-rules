"""Unit tests for verdict.rule: FunctionRule, AndRule, OrRule."""

from __future__ import annotations

import pytest

from verdict.result import RuleResult
from verdict.rule import AndRule, FunctionRule, OrRule


def _pass(name: str, data: object = None) -> FunctionRule:
    """A FunctionRule that always passes, for composing test fixtures."""
    async def predicate(context: dict) -> RuleResult:
        return RuleResult(rule_name=name, passed=True, data=data)
    return FunctionRule(name, predicate)


def _fail(name: str, detail: str = "") -> FunctionRule:
    """A FunctionRule that always fails, for composing test fixtures."""
    async def predicate(context: dict) -> RuleResult:
        return RuleResult(rule_name=name, passed=False, detail=detail)
    return FunctionRule(name, predicate)


async def _noop_predicate(context: dict) -> RuleResult:
    """A predicate that always passes — used where only the rule's
    name/group matter to the test, not its evaluation outcome."""
    return RuleResult(rule_name="noop", passed=True)


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

    async def test_predicate_receives_the_context(self) -> None:
        seen = {}

        async def predicate(context: dict) -> RuleResult:
            seen.update(context)
            return RuleResult(rule_name="r1", passed=True)

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


class TestAndRule:
    async def test_all_pass_yields_pass(self) -> None:
        rule = AndRule("and1", [_pass("a"), _pass("b")])
        result = await rule.evaluate({})
        assert result.passed is True
        assert result.rule_name == "and1"

    async def test_one_failure_yields_fail(self) -> None:
        rule = AndRule("and1", [_pass("a"), _fail("b", detail="bad")])
        result = await rule.evaluate({})
        assert result.passed is False
        assert "b" in result.detail
        assert "bad" in result.detail

    async def test_short_circuits_after_first_failure(self) -> None:
        calls: list[str] = []

        async def tracked_pass(context: dict) -> RuleResult:
            calls.append("c")
            return RuleResult(rule_name="c", passed=True)

        rule = AndRule("and1", [_fail("a"), FunctionRule("c", tracked_pass)])
        await rule.evaluate({})
        assert calls == []  # never reached — 'a' already failed

    async def test_data_carries_sub_results_up_to_failure(self) -> None:
        rule = AndRule("and1", [_pass("a"), _fail("b"), _pass("c")])
        result = await rule.evaluate({})
        assert [r.rule_name for r in result.data] == ["a", "b"]

    async def test_empty_rule_list_vacuously_passes(self) -> None:
        rule = AndRule("and1", [])
        result = await rule.evaluate({})
        assert result.passed is True


class TestOrRule:
    async def test_any_pass_yields_pass(self) -> None:
        rule = OrRule("or1", [_fail("a"), _pass("b")])
        result = await rule.evaluate({})
        assert result.passed is True

    async def test_all_fail_yields_fail(self) -> None:
        rule = OrRule("or1", [_fail("a"), _fail("b")])
        result = await rule.evaluate({})
        assert result.passed is False
        assert result.detail == "no sub-rule passed"

    async def test_short_circuits_after_first_pass(self) -> None:
        calls: list[str] = []

        async def tracked_fail(context: dict) -> RuleResult:
            calls.append("c")
            return RuleResult(rule_name="c", passed=False)

        rule = OrRule("or1", [_pass("a"), FunctionRule("c", tracked_fail)])
        await rule.evaluate({})
        assert calls == []  # never reached — 'a' already passed

    async def test_empty_rule_list_vacuously_fails(self) -> None:
        rule = OrRule("or1", [])
        result = await rule.evaluate({})
        assert result.passed is False


class TestExceptionPropagation:
    """AndRule/OrRule catch nothing either — a sub-rule's own exception
    propagates straight out of evaluate(), the same guarantee test_engine.py
    proves for run_all/run_group. See extension.md Recipe 7 for the wrapper
    a consumer opts into if they want the opposite."""

    async def test_and_rule_does_not_catch_a_sub_rule_s_exception(self) -> None:
        async def flaky(context: dict) -> RuleResult:
            raise RuntimeError("boom")

        rule = AndRule("and1", [_pass("a"), FunctionRule("flaky", flaky)])
        with pytest.raises(RuntimeError):
            await rule.evaluate({})

    async def test_or_rule_does_not_catch_a_sub_rule_s_exception(self) -> None:
        async def flaky(context: dict) -> RuleResult:
            raise RuntimeError("boom")

        rule = OrRule("or1", [_fail("a"), FunctionRule("flaky", flaky)])
        with pytest.raises(RuntimeError):
            await rule.evaluate({})
