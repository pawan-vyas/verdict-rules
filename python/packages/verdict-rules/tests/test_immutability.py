"""A rule or result owns its collections -- a caller that keeps the list it
passed in cannot change one afterwards.

Every other suite builds a rule or a result and asserts on what evaluation
computed. None of them asks whether the inputs can move underneath it, which
is the gap these cover: the collections used to be aliased, so retaining the
list handed to a composite let a caller change that composite's sub-rules --
and its verdict -- after construction, and let an early-define/late-init
pattern build a result graph containing itself, which every traversal here
recurses through.
"""

from __future__ import annotations

import pytest

from verdict.engine import RulesEngine
from verdict.result import RuleResult, RunResult
from verdict.rule import AndRule, FunctionRule, NotRule, OrRule, PredicateOutcome


async def _passes(context: object) -> PredicateOutcome:
    return PredicateOutcome(passed=True)


async def _fails(context: object) -> PredicateOutcome:
    return PredicateOutcome(passed=False)


def _rule(name: str, *, passing: bool = True) -> FunctionRule:
    return FunctionRule(name, _passes if passing else _fails)


class TestCompositesOwnTheirSubRules:
    @pytest.mark.parametrize("composite", [AndRule, OrRule])
    def test_appending_to_the_caller_s_list_does_not_add_a_sub_rule(
        self, composite: type
    ) -> None:
        held = [_rule("a")]
        rule = composite("c", held)
        held.append(_rule("injected"))
        assert "1 sub-rule(s)" in repr(rule)

    @pytest.mark.parametrize("composite", [AndRule, OrRule])
    def test_clearing_the_caller_s_list_does_not_empty_the_composite(
        self, composite: type
    ) -> None:
        held = [_rule("a"), _rule("b")]
        rule = composite("c", held)
        held.clear()
        assert "2 sub-rule(s)" in repr(rule)

    async def test_the_verdict_cannot_change_after_construction(self) -> None:
        """The sharpest form: a caller who keeps the list could otherwise flip
        a passing AndRule to failing without touching the rule at all."""
        held = [_rule("a")]
        rule = AndRule("and1", held)
        assert (await rule.evaluate({})).passed is True

        held.append(_rule("sabotage", passing=False))
        assert (await rule.evaluate({})).passed is True


class TestResultsOwnTheirChildren:
    def test_sub_results_is_copied(self) -> None:
        held = [RuleResult(rule_name="child", passed=True)]
        result = RuleResult(rule_name="parent", passed=True, sub_results=held)
        held.append(RuleResult(rule_name="injected", passed=False))
        assert [r.rule_name for r in result.sub_results] == ["child"]

    def test_decided_by_indices_is_copied(self) -> None:
        child = RuleResult(rule_name="child", passed=False)
        held = [0]
        result = RuleResult(rule_name="parent", passed=False, sub_results=(child,), decided_by_indices=held)
        held.clear()
        assert [r.rule_name for r in result.decided_by] == ["child"]

    def test_an_index_naming_a_child_that_does_not_exist_is_rejected(self) -> None:
        """The one way the indices form can be wrong, caught at construction
        rather than when something later reads `decided_by`. The objects form
        had no equivalent check available -- nothing stopped it naming a
        result that was never a child of this one."""
        child = RuleResult(rule_name="child", passed=False)

        with pytest.raises(IndexError, match="parent"):
            RuleResult(rule_name="parent", passed=False, sub_results=(child,), decided_by_indices=(1,))

    def test_decided_by_cannot_disagree_with_sub_results(self) -> None:
        """`decided_by` is derived, so there is no second stored collection
        that could drift out of step with the children it names."""
        a, b = RuleResult(rule_name="a", passed=True), RuleResult(rule_name="b", passed=False)
        result = RuleResult(rule_name="parent", passed=False, sub_results=(a, b), decided_by_indices=(1,))

        assert result.decided_by[0] is result.sub_results[1]

    def test_run_result_results_is_copied_and_stays_a_list(self) -> None:
        held = [RuleResult(rule_name="a", passed=True)]
        run = RunResult(passed=True, results=held)
        held.append(RuleResult(rule_name="injected", passed=False))
        assert [r.rule_name for r in run.results] == ["a"]
        assert run.results == [RuleResult(rule_name="a", passed=True)]

    def test_a_result_cannot_be_made_to_contain_itself(self) -> None:
        """Early-define/late-init: append the result to the very list it was
        constructed from. Copying severs it, so the traversals terminate."""
        kids: list[RuleResult] = []
        result = RuleResult(rule_name="p", passed=False, sub_results=kids)
        kids.append(result)

        assert result.sub_results == ()
        assert [leaf.rule_name for leaf in result.leaves] == ["p"]
        assert [leaf.rule_name for leaf in result.failing_leaves] == ["p"]


class TestEngineOwnsItsRules:
    async def test_appending_to_the_caller_s_list_does_not_register_a_rule(self) -> None:
        held = [_rule("a")]
        engine = RulesEngine(held)
        held.append(_rule("injected"))
        assert engine.rule_names == ("a",)
        assert len((await engine.run_all({})).results) == 1


class TestNotRuleOwnsItsChild:
    async def test_the_wrapped_rule_is_a_single_reference_not_a_collection(self) -> None:
        """NotRule holds one rule, so there is no collection to copy -- the
        guarantee it needs is only that its own result reports that child
        truthfully."""
        rule = NotRule("not_a", _rule("a"))
        result = await rule.evaluate({})
        assert result.passed is False
        assert [r.rule_name for r in result.sub_results] == ["a"]
