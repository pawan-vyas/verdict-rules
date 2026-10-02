"""Unit tests for the composition primitives behind AndRule/OrRule, and the
new shipped NotRule: SequentialEvaluator, ShortCircuitEvaluator, and the
AndRule.failed/passing / OrRule.passed/failing / NotRule.negated accessors.

See .agents/plans/composite-rule-and-leaves-redesign/README.md §3-§3e for
the design this pins down.
"""

from __future__ import annotations

from verdict.result import RuleResult
from verdict.rule import (
    AndRule,
    FunctionRule,
    NotRule,
    OrRule,
    PredicateOutcome,
    SequentialEvaluator,
    ShortCircuitEvaluator,
)


def _pass(name: str) -> FunctionRule:
    async def predicate(context: dict) -> PredicateOutcome:
        return PredicateOutcome(passed=True)
    return FunctionRule(name, predicate)


def _fail(name: str) -> FunctionRule:
    async def predicate(context: dict) -> PredicateOutcome:
        return PredicateOutcome(passed=False)
    return FunctionRule(name, predicate)


def _tracked_pass(name: str, calls: list[str]) -> FunctionRule:
    async def predicate(context: dict) -> PredicateOutcome:
        calls.append(name)
        return PredicateOutcome(passed=True)
    return FunctionRule(name, predicate)


class TestSequentialEvaluator:
    """The general-purpose piece ShortCircuitEvaluator (and a custom
    composite like AtLeastNRule) is built on."""

    async def test_empty_rule_list_returns_the_vacuous_result_directly(self) -> None:
        """Checked before the loop runs at all -- there's no last element
        to fall back on when the list started empty."""
        evaluator = SequentialEvaluator(decider=lambda latest, so_far, total: None, vacuous_result=True)
        result = await evaluator.evaluate("e", [], {})
        assert result.passed is True
        assert result.sub_results == ()

    async def test_decider_returning_a_value_stops_evaluation_immediately(self) -> None:
        calls: list[str] = []
        rules = [_tracked_pass("a", calls), _tracked_pass("b", calls), _tracked_pass("c", calls)]

        def decider(latest: RuleResult, so_far: list[RuleResult], total: int) -> bool | None:
            return False if len(so_far) == 1 else None

        evaluator = SequentialEvaluator(decider=decider, vacuous_result=True)
        result = await evaluator.evaluate("e", rules, {})

        assert result.passed is False
        assert calls == ["a"], "evaluation must stop after the first rule, never reach b/c"
        assert [r.rule_name for r in result.sub_results] == ["a"]

    async def test_decider_returning_none_throughout_falls_back_to_vacuous_result(self) -> None:
        rules = [_pass("a"), _pass("b")]
        evaluator = SequentialEvaluator(decider=lambda latest, so_far, total: None, vacuous_result=False)
        result = await evaluator.evaluate("e", rules, {})
        assert result.passed is False  # the vacuous_result, not a verdict from the rules
        assert [r.rule_name for r in result.sub_results] == ["a", "b"]

    async def test_sub_results_always_carries_every_rule_actually_evaluated(self) -> None:
        rules = [_pass("a"), _pass("b"), _pass("c")]
        evaluator = SequentialEvaluator(decider=lambda latest, so_far, total: None, vacuous_result=True)
        result = await evaluator.evaluate("e", rules, {})
        assert [r.rule_name for r in result.sub_results] == ["a", "b", "c"]


class TestShortCircuitEvaluator:
    """The exact shape AndRule (stop_on=False)/OrRule (stop_on=True) compose."""

    async def test_stop_on_false_behaves_like_and_rule(self) -> None:
        calls: list[str] = []
        evaluator: ShortCircuitEvaluator[dict] = ShortCircuitEvaluator(stop_on=False)
        rules = [_pass("a"), _fail("b"), _tracked_pass("c", calls)]
        result = await evaluator.evaluate("e", rules, {})
        assert result.passed is False
        assert calls == [], "stops at the first failure -- 'c' never runs"
        assert [r.rule_name for r in result.sub_results] == ["a", "b"]

    async def test_stop_on_true_behaves_like_or_rule(self) -> None:
        calls: list[str] = []
        evaluator: ShortCircuitEvaluator[dict] = ShortCircuitEvaluator(stop_on=True)
        rules = [_fail("a"), _pass("b"), _tracked_pass("c", calls)]
        result = await evaluator.evaluate("e", rules, {})
        assert result.passed is True
        assert calls == [], "stops at the first pass -- 'c' never runs"
        assert [r.rule_name for r in result.sub_results] == ["a", "b"]

    async def test_empty_rules_vacuous_result_is_derived_from_stop_on(self) -> None:
        and_shaped: ShortCircuitEvaluator[dict] = ShortCircuitEvaluator(stop_on=False)
        or_shaped: ShortCircuitEvaluator[dict] = ShortCircuitEvaluator(stop_on=True)
        assert (await and_shaped.evaluate("e", [], {})).passed is True
        assert (await or_shaped.evaluate("e", [], {})).passed is False

    async def test_all_pass_with_stop_on_false_runs_every_rule(self) -> None:
        calls: list[str] = []
        evaluator: ShortCircuitEvaluator[dict] = ShortCircuitEvaluator(stop_on=False)
        rules = [_tracked_pass("a", calls), _tracked_pass("b", calls)]
        result = await evaluator.evaluate("e", rules, {})
        assert result.passed is True
        assert calls == ["a", "b"]

    async def test_all_fail_with_stop_on_true_runs_every_rule(self) -> None:
        calls: list[str] = []
        evaluator: ShortCircuitEvaluator[dict] = ShortCircuitEvaluator(stop_on=True)
        rules = [_fail("a"), _fail("b")]
        result = await evaluator.evaluate("e", rules, {})
        assert result.passed is False
        assert [r.rule_name for r in result.sub_results] == ["a", "b"]


class TestAndRuleAccessors:
    async def test_failed_returns_the_sole_decisive_failure(self) -> None:
        rule = AndRule("and1", [_pass("a"), _fail("b")])
        result = await rule.evaluate({})
        failed = AndRule.failed(result)
        assert failed is not None
        assert failed.rule_name == "b"

    async def test_failed_returns_none_when_the_and_rule_passed(self) -> None:
        rule = AndRule("and1", [_pass("a"), _pass("b")])
        result = await rule.evaluate({})
        assert AndRule.failed(result) is None

    async def test_failed_returns_none_for_an_empty_and_rule(self) -> None:
        rule = AndRule("and1", [])
        result = await rule.evaluate({})
        assert AndRule.failed(result) is None

    async def test_passing_returns_every_sub_result_when_the_and_rule_passed(self) -> None:
        rule = AndRule("and1", [_pass("a"), _pass("b")])
        result = await rule.evaluate({})
        assert [r.rule_name for r in AndRule.passing(result)] == ["a", "b"]

    async def test_passing_excludes_the_decisive_failure(self) -> None:
        rule = AndRule("and1", [_pass("a"), _pass("b"), _fail("c")])
        result = await rule.evaluate({})
        assert [r.rule_name for r in AndRule.passing(result)] == ["a", "b"]


class TestOrRuleAccessors:
    async def test_passed_returns_the_sole_decisive_pass(self) -> None:
        rule = OrRule("or1", [_fail("a"), _pass("b")])
        result = await rule.evaluate({})
        passed = OrRule.passed(result)
        assert passed is not None
        assert passed.rule_name == "b"

    async def test_passed_returns_none_when_the_or_rule_failed(self) -> None:
        rule = OrRule("or1", [_fail("a"), _fail("b")])
        result = await rule.evaluate({})
        assert OrRule.passed(result) is None

    async def test_passed_returns_none_for_an_empty_or_rule(self) -> None:
        rule = OrRule("or1", [])
        result = await rule.evaluate({})
        assert OrRule.passed(result) is None

    async def test_failing_returns_every_sub_result_when_the_or_rule_failed(self) -> None:
        rule = OrRule("or1", [_fail("a"), _fail("b")])
        result = await rule.evaluate({})
        assert [r.rule_name for r in OrRule.failing(result)] == ["a", "b"]

    async def test_failing_excludes_the_decisive_pass(self) -> None:
        rule = OrRule("or1", [_fail("a"), _fail("b"), _pass("c")])
        result = await rule.evaluate({})
        assert [r.rule_name for r in OrRule.failing(result)] == ["a", "b"]


class TestNotRule:
    async def test_passes_when_the_inner_rule_fails(self) -> None:
        rule = NotRule("not1", _fail("inner"))
        result = await rule.evaluate({})
        assert result.passed is True

    async def test_fails_when_the_inner_rule_passes(self) -> None:
        rule = NotRule("not1", _pass("inner"))
        result = await rule.evaluate({})
        assert result.passed is False

    async def test_sub_results_truthfully_carries_the_one_inner_result(self) -> None:
        inner_rule = _pass("inner")
        rule = NotRule("not1", inner_rule)
        result = await rule.evaluate({})
        assert len(result.sub_results) == 1
        assert result.sub_results[0].rule_name == "inner"

    async def test_negated_returns_the_inner_result(self) -> None:
        rule = NotRule("not1", _fail("inner"))
        result = await rule.evaluate({})
        inner = NotRule.negated(result)
        assert inner.rule_name == "inner"
        assert inner.passed is False

    async def test_a_failed_not_rule_has_itself_as_its_own_failing_leaf(self) -> None:
        """The negation case failing_leaves exists to handle correctly --
        the inner rule passed, so there's no failing descendant to recurse
        into; the failed NotRule result has to be the leaf itself."""
        rule = NotRule("not1", _pass("inner"))
        result = await rule.evaluate({})
        assert result.failing_leaves == [result]

    def test_repr_shows_the_name(self) -> None:
        rule = NotRule("not1", _pass("inner"))
        assert repr(rule) == 'NotRule "not1"'

    def test_repr_shows_the_group_when_present(self) -> None:
        rule = NotRule("not1", _pass("inner"), group="g1")
        assert repr(rule) == 'NotRule "not1" (g1)'

    async def test_does_not_catch_the_inner_rule_s_exception(self) -> None:
        import pytest

        async def flaky(context: dict) -> PredicateOutcome:
            raise RuntimeError("boom")

        rule = NotRule("not1", FunctionRule("flaky", flaky))
        with pytest.raises(RuntimeError):
            await rule.evaluate({})
