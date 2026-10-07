"""Unit tests for walking a rule *tree* before it is evaluated.

``CompositeRule`` is what makes that possible without naming concrete types.
The result surface answers what ran; these pin down what was built -- and in
particular that one walk reaches a composite this package never saw, which is
the capability the three built-ins alone cannot demonstrate.
"""

from __future__ import annotations

from typing import Sequence

import pytest

from verdict.result import RuleResult
from verdict.rule import (
    AndRule,
    CompositeRule,
    FunctionRule,
    NotRule,
    OrRule,
    PredicateOutcome,
    Rule,
)


def _pass(name: str) -> FunctionRule:
    async def predicate(context: dict) -> PredicateOutcome:
        return PredicateOutcome(passed=True)
    return FunctionRule(name, predicate)


def _fail(name: str) -> FunctionRule:
    async def predicate(context: dict) -> PredicateOutcome:
        return PredicateOutcome(passed=False)
    return FunctionRule(name, predicate)


class AtLeastOneOf:
    """A composite defined outside this package, opting into the protocol.

    It inherits nothing from verdict -- satisfying the protocol structurally is
    the whole point.
    """

    def __init__(self, name: str, parts: Sequence[Rule[dict]]) -> None:
        self.name = name
        self.group: str | None = None
        self._parts = tuple(parts)

    @property
    def sub_rules(self) -> Sequence[Rule[dict]]:
        return self._parts

    async def evaluate(self, context: dict) -> RuleResult:
        sub_results: list[RuleResult] = []
        for part in self._parts:
            sub_results.append(await part.evaluate(context))
            if sub_results[-1].passed:
                return RuleResult(
                    self.name,
                    True,
                    sub_results=sub_results,
                    decided_by_indices=(len(sub_results) - 1,),
                )
        return RuleResult(self.name, False, sub_results=sub_results)


def _leaf_names(rule: Rule[dict]) -> list[str]:
    """One walk, with no knowledge of which composite it is looking at."""
    if isinstance(rule, CompositeRule):
        return [name for part in rule.sub_rules for name in _leaf_names(part)]
    return [rule.name]


class TestSubRules:
    def test_and_rule_exposes_its_sub_rules_in_order(self) -> None:
        first, second = _pass("a"), _fail("b")
        assert list(AndRule("gate", [first, second]).sub_rules) == [first, second]

    def test_or_rule_exposes_its_sub_rules_in_order(self) -> None:
        first, second = _fail("a"), _pass("b")
        assert list(OrRule("gate", [first, second]).sub_rules) == [first, second]

    def test_not_rule_reports_one_sub_rule_under_the_shared_name(self) -> None:
        inner = _pass("inner")
        # `sub_rules`, not `rule`: a walk must not need to know this is a
        # negation to find its one part.
        assert list(NotRule("negated", inner).sub_rules) == [inner]

    @pytest.mark.parametrize("composite", [AndRule("empty", []), OrRule("empty", [])])
    def test_a_vacuous_composite_reports_no_sub_rules(self, composite: Rule[dict]) -> None:
        assert list(composite.sub_rules) == []

    def test_the_sub_rule_sequence_is_a_copy_taken_at_construction(self) -> None:
        parts = [_pass("a")]
        composite = AndRule("gate", parts)
        parts.clear()
        assert len(composite.sub_rules) == 1


class TestCompositeRuleProtocol:
    def test_every_built_in_composite_satisfies_it(self) -> None:
        leaf = _pass("leaf")
        assert isinstance(AndRule("and", [leaf]), CompositeRule)
        assert isinstance(OrRule("or", [leaf]), CompositeRule)
        assert isinstance(NotRule("not", leaf), CompositeRule)

    def test_a_leaf_rule_does_not(self) -> None:
        # What makes "structure or terminal check" answerable without naming
        # concrete types.
        assert not isinstance(_pass("leaf"), CompositeRule)

    def test_a_consumer_defined_composite_satisfies_it_structurally(self) -> None:
        assert isinstance(AtLeastOneOf("custom", [_pass("a")]), CompositeRule)


class TestWalkingATree:
    def test_one_walk_reaches_every_leaf_through_nested_built_ins(self) -> None:
        tree = AndRule(
            "top",
            [
                _pass("a"),
                OrRule("either", [_fail("b"), _pass("c")]),
                NotRule("not-d", _fail("d")),
            ],
        )
        assert _leaf_names(tree) == ["a", "b", "c", "d"]

    def test_the_same_walk_reaches_into_a_consumer_defined_composite(self) -> None:
        # The capability the built-ins alone cannot prove, and the reason this
        # is a published protocol rather than three properties.
        tree = AndRule(
            "top",
            [
                _pass("a"),
                AtLeastOneOf("custom", [_fail("b"), NotRule("not-c", _pass("c"))]),
            ],
        )
        assert _leaf_names(tree) == ["a", "b", "c"]

    @pytest.mark.asyncio
    async def test_a_consumer_defined_composite_still_evaluates_normally(self) -> None:
        # Satisfying the inspection protocol costs nothing on the evaluation
        # side: it is still just a rule.
        custom = AtLeastOneOf("custom", [_fail("b"), _pass("c")])
        result = await custom.evaluate({})
        assert result.passed
        assert [r.rule_name for r in result.decided_by] == ["c"]
