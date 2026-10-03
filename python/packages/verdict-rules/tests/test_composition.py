"""Unit tests for the composition primitives behind AndRule/OrRule, and the
new shipped NotRule: SequentialEvaluator, ShortCircuitEvaluator, and
RuleResult.decided_by.

Each type's own docstring in verdict.rule states the contract these pin
down case by case -- including why ShortCircuitEvaluator recomputes
decided_by from stop_on rather than trusting SequentialEvaluator's generic
rule.
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
            return False if len(so_far) == 2 else None

        evaluator = SequentialEvaluator(decider=decider, vacuous_result=True)
        result = await evaluator.evaluate("e", rules, {})

        assert result.passed is False
        assert calls == ["a", "b"], "evaluation must stop after the second rule, never reach c"
        assert [r.rule_name for r in result.sub_results] == ["a", "b"]
        # Decided with an item still unevaluated (so_far has 2 of 3) --
        # SequentialEvaluator's own generic decided_by rule says this is
        # just the one sub-result that flipped the verdict (b), not the
        # whole so_far list (a, b) -- deciding on the *first* evaluated
        # rule alone couldn't distinguish the two, since a one-element
        # so_far is identical to "just the latest" either way.
        assert [r.rule_name for r in result.decided_by] == ["b"]

    async def test_in_loop_early_decision_landing_on_the_last_item_names_every_evaluated_sub_result(self) -> None:
        """The other half of SequentialEvaluator's own generic decided_by
        rule: decided in-loop but only once every rule has been seen
        (so_far.Count == total at the exact moment the decider resolves, on
        the final iteration) -- decided_by is every evaluated child, the
        same as a genuine full pass, not just the one that happened to run
        last. This is the in-loop branch landing on that boundary, a
        different code path from the post-loop fallback that
        test_final_decider_call_after_exhaustion_sees_the_real_arguments
        pins -- ShortCircuitEvaluator overrides this generic rule with its
        own stop_on-aware one (see TestShortCircuitEvaluator)."""
        rules = [_pass("a"), _pass("b"), _pass("c")]

        def decider(latest: RuleResult, so_far: list[RuleResult], total: int) -> bool | None:
            return True if len(so_far) == total else None

        evaluator = SequentialEvaluator(decider=decider, vacuous_result=False)
        result = await evaluator.evaluate("e", rules, {})

        assert result.passed is True
        assert [r.rule_name for r in result.decided_by] == ["a", "b", "c"]

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

    async def test_in_loop_decider_call_receives_the_real_total(self) -> None:
        """Each per-step call passes the real ``len(rules)`` as ``total`` --
        a decider that only stops relative to that count (one short of the
        end, here) proves the actual value arrived, not just *some* int."""
        calls: list[str] = []
        rules = [_tracked_pass("a", calls), _tracked_pass("b", calls), _tracked_pass("c", calls)]

        def decider(latest: RuleResult, so_far: list[RuleResult], total: int) -> bool | None:
            return True if len(so_far) == total - 1 else None

        evaluator = SequentialEvaluator(decider=decider, vacuous_result=False)
        result = await evaluator.evaluate("e", rules, {})

        assert calls == ["a", "b"], "must stop one short of the end -- decider needs the real total"
        assert result.passed is True

    async def test_final_decider_call_after_exhaustion_sees_the_real_arguments(self) -> None:
        """After the loop exhausts without deciding, SequentialEvaluator
        calls the decider one more time -- this pins every argument that
        call receives (the latest/last sub-result, the full so_far list, the
        total) and the resulting RuleResult's own rule_name, none of which
        any pre-existing test (whose deciders ignore their arguments)
        distinguishes from a mutated/omitted call."""
        rules = [_pass("a"), _pass("b"), _pass("c")]
        calls: list[tuple[RuleResult | None, tuple[RuleResult, ...] | None, int | None]] = []

        def decider(latest: RuleResult, so_far: list[RuleResult] | None, total: int | None) -> bool | None:
            calls.append((latest, tuple(so_far) if so_far is not None else None, total))
            return None

        evaluator = SequentialEvaluator(decider=decider, vacuous_result=False)
        result = await evaluator.evaluate("e", rules, {})

        # Once per rule during the loop (3), plus one more after exhaustion.
        assert len(calls) == 4
        latest, so_far_snapshot, total = calls[-1]
        assert so_far_snapshot is not None
        assert latest is so_far_snapshot[-1]
        assert [r.rule_name for r in so_far_snapshot] == ["a", "b", "c"]
        assert total == 3
        assert result.rule_name == "e"
        assert result.passed is False  # the vacuous_result -- decider always returned None
        # The post-loop fallback's own decided_by -- every evaluated child,
        # same as a genuine full pass -- unchecked by every assertion above,
        # so a mutant corrupting or dropping this keyword on the final
        # RuleResult survived undetected.
        assert [r.rule_name for r in result.decided_by] == ["a", "b", "c"]

    async def test_final_decider_call_can_still_resolve_the_outcome(self) -> None:
        """The final, post-exhaustion decider call is a real decision point,
        not dead code that always falls through to vacuous_result -- a
        decider that only ever resolves on that final call must still have
        its verdict honored."""
        rules = [_pass("a"), _pass("b")]
        seen = 0

        def decider(latest: RuleResult, so_far: list[RuleResult], total: int) -> bool | None:
            nonlocal seen
            seen += 1
            return None if seen <= len(rules) else True

        evaluator = SequentialEvaluator(decider=decider, vacuous_result=False)
        result = await evaluator.evaluate("e", rules, {})
        assert result.passed is True  # from the final decider call, not vacuous_result


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

    async def test_decided_by_names_only_the_trigger_when_found_before_exhaustion(self) -> None:
        """ShortCircuitEvaluator's own override of SequentialEvaluator's
        generic decided_by rule, using the one extra fact only it has --
        stop_on -- to tell "found the trigger, which happened to be the
        last item evaluated" apart from "genuinely exhausted every item
        without ever finding it" (see ShortCircuitEvaluator.evaluate's own
        docstring). Exercised directly against a freshly constructed
        evaluator, not AndRule/OrRule's shared class-level instance, so a
        mutation to __init__ itself (e.g. self._stop_on corrupted) is
        actually attributed to this test by coverage-based test selection --
        AndRule/OrRule only ever construct their evaluator once, at class
        body evaluation time, before any individual test runs."""
        evaluator: ShortCircuitEvaluator[dict] = ShortCircuitEvaluator(stop_on=False)
        rules = [_pass("a"), _fail("b"), _pass("c")]
        result = await evaluator.evaluate("e", rules, {})
        assert [r.rule_name for r in result.decided_by] == ["b"]

    async def test_decided_by_names_every_sub_result_when_exhausted_without_ever_triggering(self) -> None:
        evaluator: ShortCircuitEvaluator[dict] = ShortCircuitEvaluator(stop_on=True)
        rules = [_fail("a"), _fail("b")]
        result = await evaluator.evaluate("e", rules, {})
        assert [r.rule_name for r in result.decided_by] == ["a", "b"]


class TestDecidedBy:
    """RuleResult.decided_by -- the one-level, non-recursive explanation
    for a composite's own verdict. Supersedes AndRule.failed/passing and
    OrRule.passed/failing (removed): those were static methods a caller
    could apply to the wrong family's result and get a plausible, silently
    wrong answer -- confirmed with concrete cases from a real adopter
    review, not hypothetical. decided_by closes that structurally: there
    is no second method to reach for, every result carries its own
    correctly-populated field."""

    async def test_and_rule_failing_early_names_just_the_decisive_failure(self) -> None:
        rule = AndRule("and1", [_pass("a"), _fail("b"), _pass("c"), _pass("d")])
        result = await rule.evaluate({})
        assert [r.rule_name for r in result.decided_by] == ["b"]

    async def test_and_rule_failing_on_its_last_item_still_names_just_that_one(self) -> None:
        """The case a first attempt at this got wrong: soFar.Count==total
        holds here exactly like it does for a genuine full pass, so a
        rule based on count alone can't tell them apart -- position in
        the list is irrelevant to blame; ShortCircuitEvaluator's own
        stop_on is what actually distinguishes them."""
        rule = AndRule("and1", [_pass("a"), _pass("b"), _fail("c")])
        result = await rule.evaluate({})
        assert [r.rule_name for r in result.decided_by] == ["c"]

    async def test_and_rule_fully_passing_names_every_sub_result(self) -> None:
        rule = AndRule("and1", [_pass("a"), _pass("b"), _pass("c")])
        result = await rule.evaluate({})
        assert [r.rule_name for r in result.decided_by] == ["a", "b", "c"]

    async def test_and_rule_vacuous_pass_names_nothing(self) -> None:
        rule = AndRule("and1", [])
        result = await rule.evaluate({})
        assert result.decided_by == []

    async def test_or_rule_passing_early_names_just_the_decisive_pass(self) -> None:
        rule = OrRule("or1", [_fail("a"), _pass("b"), _fail("c")])
        result = await rule.evaluate({})
        assert [r.rule_name for r in result.decided_by] == ["b"]

    async def test_or_rule_all_fail_names_every_sub_result(self) -> None:
        """Mirrors the AndRule last-item case with the opposite polarity --
        OrRule's all-fail verdict is only known once every item is seen,
        genuinely collective, not attributable to the last one alone."""
        rule = OrRule("or1", [_fail("a"), _fail("b"), _fail("c")])
        result = await rule.evaluate({})
        assert [r.rule_name for r in result.decided_by] == ["a", "b", "c"]

    async def test_or_rule_vacuous_fail_names_nothing(self) -> None:
        rule = OrRule("or1", [])
        result = await rule.evaluate({})
        assert result.decided_by == []


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

    async def test_passes_the_context_through_to_the_inner_rule(self) -> None:
        """NotRule.evaluate must forward its own ``context`` argument
        unchanged to the wrapped rule -- nothing above exercises a
        context-dependent predicate."""
        seen: dict = {}

        async def predicate(context: dict) -> PredicateOutcome:
            seen.update(context)
            return PredicateOutcome(passed=True)

        rule = NotRule("not1", FunctionRule("inner", predicate))
        await rule.evaluate({"user_id": 7})
        assert seen == {"user_id": 7}

    async def test_decided_by_is_the_inner_result_in_both_directions(self) -> None:
        """Unconditional, unlike failing_leaves' own self-as-leaf rule --
        'inner passed' is genuinely why a failing NotRule failed, not an
        inconsistency to paper over."""
        failing = await NotRule("not1", _fail("inner")).evaluate({})
        assert [r.rule_name for r in failing.decided_by] == ["inner"]

        passing = await NotRule("not2", _pass("inner")).evaluate({})
        assert [r.rule_name for r in passing.decided_by] == ["inner"]

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
