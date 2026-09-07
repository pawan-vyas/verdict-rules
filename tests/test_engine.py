"""Unit tests for verdict.engine.RulesEngine."""

from __future__ import annotations

import pytest

from verdict.engine import RulesEngine
from verdict.result import RuleResult
from verdict.rule import FunctionRule


def _pass(name: str, group: str | None = None) -> FunctionRule:
    async def predicate(context: dict) -> RuleResult:
        return RuleResult(rule_name=name, passed=True)
    return FunctionRule(name, predicate, group=group)


def _fail(name: str, group: str | None = None) -> FunctionRule:
    async def predicate(context: dict) -> RuleResult:
        return RuleResult(rule_name=name, passed=False)
    return FunctionRule(name, predicate, group=group)


class TestRunAll:
    async def test_all_passing_rules_yields_passed_true(self) -> None:
        engine = RulesEngine([_pass("a"), _pass("b")])
        result = await engine.run_all({})
        assert result.passed is True
        assert [r.rule_name for r in result.results] == ["a", "b"]

    async def test_one_failing_rule_yields_passed_false(self) -> None:
        engine = RulesEngine([_pass("a"), _fail("b")])
        result = await engine.run_all({})
        assert result.passed is False

    async def test_does_not_short_circuit_unlike_and_rule(self) -> None:
        """run_all's job is a full diagnostic picture, not the fastest
        path to a boolean — every rule runs even after an earlier one
        already failed."""
        engine = RulesEngine([_fail("a"), _pass("b")])
        result = await engine.run_all({})
        assert [r.rule_name for r in result.results] == ["a", "b"]

    async def test_empty_engine_run_all_vacuously_passes(self) -> None:
        engine = RulesEngine([])
        result = await engine.run_all({})
        assert result.passed is True
        assert result.results == []


class TestRunNamed:
    async def test_returns_that_rule_s_own_result(self) -> None:
        engine = RulesEngine([_pass("a"), _fail("b")])
        result = await engine.run_named("b", {})
        assert result.rule_name == "b"
        assert result.passed is False

    async def test_unknown_name_raises_key_error(self) -> None:
        engine = RulesEngine([_pass("a")])
        with pytest.raises(KeyError):
            await engine.run_named("missing", {})


class TestRunGroup:
    async def test_runs_only_matching_group(self) -> None:
        engine = RulesEngine([
            _pass("a", group="g1"),
            _pass("b", group="g2"),
            _fail("c", group="g1"),
        ])
        result = await engine.run_group("g1", {})
        assert [r.rule_name for r in result.results] == ["a", "c"]
        assert result.passed is False

    async def test_unknown_group_vacuously_passes(self) -> None:
        engine = RulesEngine([_pass("a", group="g1")])
        result = await engine.run_group("no-such-group", {})
        assert result.passed is True
        assert result.results == []

    async def test_ungrouped_rules_are_never_matched(self) -> None:
        engine = RulesEngine([_pass("a")])  # no group
        result = await engine.run_group("g1", {})
        assert result.results == []


class TestConstruction:
    def test_duplicate_names_last_one_wins_in_by_name_lookup(self) -> None:
        first = _pass("a")
        second = _fail("a")
        engine = RulesEngine([first, second])
        assert engine._by_name["a"] is second
