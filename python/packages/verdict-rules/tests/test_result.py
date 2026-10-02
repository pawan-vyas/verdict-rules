"""Unit tests for verdict.result: RuleResult.leaves/failing_leaves and
RunResult's own forwarders.

`failing_leaves` is an independent recursion, not a filter over `leaves` —
see RuleResult.failing_leaves's own docstring and
.agents/plans/composite-rule-and-leaves-redesign/README.md §2 for the exact
formula this file pins down case by case.
"""

from __future__ import annotations

from verdict.result import RuleResult, RunResult


def _leaf(name: str, passed: bool, detail: str = "") -> RuleResult:
    return RuleResult(rule_name=name, passed=passed, detail=detail)


def _composite(name: str, passed: bool, *sub_results: RuleResult) -> RuleResult:
    return RuleResult(rule_name=name, passed=passed, sub_results=sub_results)


class TestLeaves:
    def test_a_leaf_result_is_its_own_single_leaf(self) -> None:
        leaf = _leaf("a", True)
        assert leaf.leaves == [leaf]

    def test_a_composite_flattens_its_direct_children(self) -> None:
        a, b = _leaf("a", True), _leaf("b", False)
        composite = _composite("and1", False, a, b)
        assert composite.leaves == [a, b]

    def test_nested_composites_flatten_all_the_way_down(self) -> None:
        a, b, c = _leaf("a", True), _leaf("b", True), _leaf("c", False)
        inner = _composite("inner_and", True, a, b)
        outer = _composite("outer_and", False, inner, c)
        assert outer.leaves == [a, b, c]

    def test_an_empty_composite_has_no_leaves_at_all(self) -> None:
        # A degenerate composite (e.g. AndRule([])) carries no sub_results,
        # which collapses to the leaf case -- not a composite with zero
        # children; absence of sub_results *is* the leaf signal.
        empty = _composite("and1", True)
        assert empty.leaves == [empty]


class TestFailingLeaves:
    def test_a_passed_leaf_has_no_failing_leaves(self) -> None:
        assert _leaf("a", True).failing_leaves == []

    def test_a_failed_leaf_is_its_own_failing_leaf(self) -> None:
        leaf = _leaf("a", False)
        assert leaf.failing_leaves == [leaf]

    def test_and_rule_failure_surfaces_exactly_the_failing_child(self) -> None:
        a, b = _leaf("a", True), _leaf("b", False, detail="bad")
        composite = _composite("and1", False, a, b)
        assert composite.failing_leaves == [b]

    def test_or_rule_all_fail_surfaces_every_failing_child(self) -> None:
        a, b = _leaf("a", False), _leaf("b", False)
        composite = _composite("or1", False, a, b)
        assert composite.failing_leaves == [a, b]

    def test_a_passed_result_has_no_failing_leaves_even_after_an_earlier_failed_branch(
        self,
    ) -> None:
        """The case that makes this an independent recursion, not a filter
        over `leaves`: an OrRule whose first sub-rule failed before its
        second one passed. The whole thing passed -- `failing_leaves` must
        report nothing, even though `leaves` still shows the earlier
        failure."""
        failed_first = _leaf("a", False)
        passed_second = _leaf("b", True)
        or_result = _composite("or1", True, failed_first, passed_second)

        assert or_result.leaves == [failed_first, passed_second]
        assert or_result.failing_leaves == []

    def test_a_failed_result_with_no_failing_children_is_its_own_leaf(self) -> None:
        """The negation case: a failed NotRule wraps an inner rule that
        itself *passed* -- recursing into sub_results finds no failures at
        all, so the composite's own failed result has to be the leaf,
        instead of returning an empty list that would misrepresent why
        anything failed at all."""
        passed_inner = _leaf("inner", True)
        not_result = _composite("not1", False, passed_inner)

        assert not_result.failing_leaves == [not_result]

    def test_nested_failure_flattens_to_the_deepest_actual_failures(self) -> None:
        a, b, c = _leaf("a", True), _leaf("b", False), _leaf("c", True)
        inner_or = _composite("inner_or", False, b)  # OrRule, only 'b' ran and failed
        outer_and = _composite("outer_and", False, a, inner_or)
        assert outer_and.failing_leaves == [b]


class TestRunResultForwarders:
    def test_leaves_flattens_across_every_result(self) -> None:
        a, b = _leaf("a", True), _leaf("b", False)
        composite = _composite("and1", False, a, b)
        run = RunResult(passed=False, results=[_leaf("standalone", True), composite])
        assert run.leaves == [run.results[0], a, b]

    def test_failing_leaves_is_a_plain_filter_over_leaves(self) -> None:
        """Unlike RuleResult.failing_leaves, RunResult's own version *is* a
        plain filter -- `run_all`/`run_group` never short-circuit, so every
        top-level result's own verdict is already final; no earlier
        short-circuited branch to misrepresent."""
        a, b = _leaf("a", True), _leaf("b", False)
        composite = _composite("and1", False, a, b)
        run = RunResult(passed=False, results=[_leaf("standalone", True), composite])
        assert run.failing_leaves == [b]

    def test_empty_run_result_has_no_leaves(self) -> None:
        run = RunResult(passed=True, results=[])
        assert run.leaves == []
        assert run.failing_leaves == []
