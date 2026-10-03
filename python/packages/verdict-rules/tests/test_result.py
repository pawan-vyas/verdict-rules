"""Unit tests for verdict.result: RuleResult.leaves/failing_leaves,
RunResult's own forwarders, and the serializability both depend on.

`failing_leaves` is an independent recursion, not a filter over `leaves` —
see RuleResult.failing_leaves's own docstring for the exact formula this
file pins down case by case.
"""

from __future__ import annotations

import dataclasses
import json

import pytest

from verdict.result import RuleResult, RunResult
from verdict.engine import RulesEngine
from verdict.rule import AndRule, FunctionRule, NotRule, OrRule, PredicateOutcome


def _pass(name: str) -> FunctionRule:
    async def predicate(context: dict) -> PredicateOutcome:
        return PredicateOutcome(passed=True)
    return FunctionRule(name, predicate)


def _fail(name: str) -> FunctionRule:
    async def predicate(context: dict) -> PredicateOutcome:
        return PredicateOutcome(passed=False)
    return FunctionRule(name, predicate)


def _leaf(name: str, passed: bool, detail: str = "") -> RuleResult:
    return RuleResult(rule_name=name, passed=passed, detail=detail)


def _composite(name: str, passed: bool, *sub_results: RuleResult) -> RuleResult:
    return RuleResult(rule_name=name, passed=passed, sub_results=sub_results)


def _to_json(result: RuleResult | RunResult) -> str:
    """What a caller actually writes to log a result: the stdlib's own
    dataclass walk, then the stdlib's own encoder. No verdict-provided
    serializer sits in between -- that is the point of the result types
    being plain dataclasses."""
    return json.dumps(dataclasses.asdict(result))


def _from_decoded(payload: dict) -> RuleResult:
    """Rebuild a RuleResult from decoded JSON, so a round-trip can be
    compared against the original rather than only inspected."""
    return RuleResult(
        rule_name=payload["rule_name"],
        passed=payload["passed"],
        detail=payload["detail"],
        data=payload["data"],
        sub_results=[_from_decoded(sub) for sub in payload["sub_results"]],
        decided_by_indices=payload["decided_by_indices"],
    )


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

    def test_negation_nested_in_a_passing_sibling_is_still_the_whole_failure(self) -> None:
        """A nested negation pins the recursion more precisely than a
        top-level one: `all(a, not(b))` with both `a` and `b` passing --
        `not(b)` fails (b passed), the outer composite's `failing_leaves`
        has to be exactly `[not(b)]`, not an empty list (every leaf below
        it, `a` and `b`, passed) and not `[]` concatenated wrong because
        `not(b)`'s own self-as-leaf result was the only failure among
        siblings rather than the sole child."""
        a = _leaf("a", True)
        not_b = _composite("not1", False, _leaf("b", True))
        outer_and = _composite("and1", False, a, not_b)
        assert outer_and.failing_leaves == [not_b]

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

    def test_failing_leaves_forwards_to_each_result_rather_than_filtering(self) -> None:
        """RunResult.failing_leaves delegates to each result's own
        failing_leaves. It is *not* a filter over `leaves` -- see the two
        tests below for the cases where filtering gives a different, wrong
        answer."""
        a, b = _leaf("a", True), _leaf("b", False)
        composite = _composite("and1", False, a, b)
        run = RunResult(passed=False, results=[_leaf("standalone", True), composite])
        assert run.failing_leaves == [b]

    async def test_a_failed_run_never_reports_no_failures(self) -> None:
        """A failed NotRule wraps a child that passed, so it is its own
        failing leaf. Filtering `leaves` by `not passed` finds only the
        passing child and reports nothing -- a failed run with no failures."""
        engine = RulesEngine([NotRule("not_positive", _pass("positive"))])
        run = await engine.run_all({})

        assert run.passed is False
        assert [leaf.rule_name for leaf in run.failing_leaves] == ["not_positive"]
        assert run.failing_leaves == run.results[0].failing_leaves

    async def test_a_passed_run_never_reports_a_failure(self) -> None:
        """A passed OrRule can hold a branch that failed before a later one
        recovered. Filtering `leaves` surfaces that branch -- a passing run
        reporting a failure."""
        engine = RulesEngine([OrRule("either", [_fail("negative"), _pass("positive")])])
        run = await engine.run_all({})

        assert run.passed is True
        assert run.failing_leaves == []
        assert [leaf.rule_name for leaf in run.leaves] == ["negative", "positive"]

    def test_empty_run_result_has_no_leaves(self) -> None:
        run = RunResult(passed=True, results=[])
        assert run.leaves == []
        assert run.failing_leaves == []


class TestMixedCompositeTree:
    """A genuinely wide, deep tree mixing every rule kind -- AndRule,
    OrRule, NotRule, and a bare FunctionRule -- at multiple levels on
    multiple branches, built through real evaluation (not hand-built
    RuleResult literals), to prove `leaves`/`failing_leaves` report
    correctly at a scale none of the other tests here exercise.

        root = AndRule("root", [a, b, c])
          a = AndRule("a", [a1, a2, a3])
            a1 = FunctionRule (leaf)
            a2 = OrRule("a2", [a2x (fails), a2y (passes)])
            a3 = NotRule("a3", a3_inner)
          b = FunctionRule (leaf)
          c = OrRule("c", [c1, c2])
            c1 = AndRule("c1", [c1x (fails), c1y])
            c2 = NotRule("c2", c2_inner)
    """

    async def test_leaves_flatten_across_every_rule_kind_even_when_everything_passes(self) -> None:
        a1 = _pass("a1")
        a2 = OrRule("a2", [_fail("a2x"), _pass("a2y")])
        a3 = NotRule("a3", _fail("a3-inner"))  # inner fails -> NotRule passes
        a = AndRule("a", [a1, a2, a3])

        b = _pass("b")

        c1 = AndRule("c1", [_fail("c1x"), _pass("c1y")])  # short-circuits at c1x, fails
        c2 = NotRule("c2", _fail("c2-inner"))  # inner fails -> NotRule passes
        c = OrRule("c", [c1, c2])

        root = AndRule("root", [a, b, c])
        result = await root.evaluate({})

        assert result.passed is True
        # c1y never ran at all (c1 short-circuited on c1x) -- absent, not
        # present-and-passing.
        assert [leaf.rule_name for leaf in result.leaves] == [
            "a1", "a2x", "a2y", "a3-inner", "b", "c1x", "c2-inner",
        ]
        # A passing root has no failing leaves, full stop -- even though
        # c1 failed internally three branches deep, on the way to c's own
        # pass via c2.
        assert result.failing_leaves == []

    async def test_failing_leaves_pinpoints_the_exact_failure_through_multiple_levels(self) -> None:
        a1 = _pass("a1")
        a2 = OrRule("a2", [_fail("a2x"), _pass("a2y")])
        a3 = NotRule("a3", _pass("a3-inner"))  # inner passes -> NotRule fails
        a = AndRule("a", [a1, a2, a3])

        # b/c are never evaluated at all -- root short-circuits on 'a'.
        b = _pass("b")
        c = OrRule("c", [AndRule("c1", [_fail("c1x"), _pass("c1y")]), NotRule("c2", _fail("c2-inner"))])

        root = AndRule("root", [a, b, c])
        result = await root.evaluate({})

        assert result.passed is False
        assert [r.rule_name for r in result.sub_results] == ["a"]  # b, c never ran
        a3_result = result.sub_results[0].sub_results[2]
        assert a3_result.rule_name == "a3"
        # The one true failure, three levels deep (root -> a -> a3), with
        # a1/a2 (passing siblings of a3) contributing nothing and b/c
        # (never evaluated) not appearing at all.
        assert result.failing_leaves == [a3_result]

    async def test_not_wrapping_a_short_circuited_composite_with_an_earlier_passing_sibling(self) -> None:
        """Stacks everything that could plausibly go wrong at once: an
        earlier AndRule sibling that passes despite an internal failure
        (ordering independence -- the real failure comes later), NotRule
        wrapping a genuine OrRule rather than a bare leaf, that wrapped
        OrRule short-circuiting internally, and the outer AndRule *also*
        short-circuiting -- two independent prunings at different depths
        in the same tree."""
        inner_or = OrRule("inner_or", [_fail("w"), _pass("x")])  # passes; w's failure doesn't matter

        inner_or_for_not = OrRule("inner_or_for_not", [_pass("p"), _pass("q")])  # short-circuits at p; q never runs
        not_result = NotRule("not1", inner_or_for_not)  # inner_or_for_not passed -> not1 fails

        z = _pass("z")  # never reached -- root short-circuits on not1

        root = AndRule("root", [inner_or, not_result, z])
        result = await root.evaluate({})

        assert result.passed is False
        assert [leaf.rule_name for leaf in result.leaves] == ["w", "x", "p"]  # q and z both absent
        assert [leaf.rule_name for leaf in result.failing_leaves] == ["not1"]

    async def test_or_rule_all_fail_interleaves_real_leaves_and_not_fallbacks_in_order(self) -> None:
        """OrRule's all-fail case, but the failing children alternate
        between plain failing leaves and NotRule self-as-leaf fallbacks --
        stresses that concatenation preserves order across heterogeneous
        failing-leaf shapes, not just same-shaped ones."""
        rule = OrRule("root", [_fail("a"), NotRule("notB", _pass("b")), _fail("c"), NotRule("notD", _pass("d"))])
        result = await rule.evaluate({})

        assert result.passed is False
        failing = result.failing_leaves
        assert [leaf.rule_name for leaf in failing] == ["a", "notB", "c", "notD"]
        # 'a'/'c' are real leaves; 'notB'/'notD' are NotRule's own results,
        # not their (passing) inner rule.
        assert failing[0] is result.sub_results[0]
        assert failing[1] is result.sub_results[1]

    async def test_a_genuinely_vacuous_composite_nested_inside_a_larger_failing_tree(self) -> None:
        """An empty OrRule (vacuously fails, zero children by
        construction -- not by recursion finding nothing) as the actual
        failing branch, alongside an earlier sibling that passes despite
        an internal failure, and a sibling after it that never runs."""
        inner_or = OrRule("inner_or", [_fail("a3"), _pass("b3")])  # passes; a3's failure doesn't matter
        empty_or = OrRule("empty_or", [])  # vacuously fails, truly no children
        z = _pass("z")  # never reached

        root = AndRule("root", [inner_or, empty_or, z])
        result = await root.evaluate({})

        assert result.passed is False
        assert [leaf.rule_name for leaf in result.leaves] == ["a3", "b3", "empty_or"]
        assert [leaf.rule_name for leaf in result.failing_leaves] == ["empty_or"]


class TestSerialization:
    """A result has to be able to leave the process -- into a log line, an
    audit record, an HTTP response. `leaves`/`failing_leaves` are properties
    rather than stored fields precisely so that this works: a leaf result's
    own leaves list is `[itself]`, so storing it would place the result
    inside itself and every tree-shaped walk (`asdict`, a JSON encoder, a
    structured logger) would recurse until it gave up.
    """

    def test_a_leaf_result_round_trips(self) -> None:
        leaf = RuleResult(rule_name="a", passed=False, detail="too young", data={"age": 15})
        restored = _from_decoded(json.loads(_to_json(leaf)))
        assert restored == leaf

    async def test_a_deep_partly_failing_tree_round_trips_intact(self) -> None:
        a = AndRule("a", [_pass("a1"), OrRule("a2", [_fail("a2x"), _pass("a2y")]), NotRule("a3", _pass("a3-inner"))])
        root = AndRule("root", [a, _pass("b")])
        result = await root.evaluate({})

        restored = _from_decoded(json.loads(_to_json(result)))

        assert restored == result
        # The two derived accessors are recomputed from the restored tree
        # rather than carried across, so agreeing proves nothing they depend
        # on was lost in transit.
        assert [leaf.rule_name for leaf in restored.leaves] == [
            leaf.rule_name for leaf in result.leaves
        ]
        assert [leaf.rule_name for leaf in restored.failing_leaves] == ["a3"]

    async def test_the_derived_accessors_are_absent_from_the_serialized_form(self) -> None:
        result = await AndRule("root", [_pass("a"), _fail("b")]).evaluate({})
        payload = json.loads(_to_json(result))

        assert sorted(payload) == [
            "data", "decided_by_indices", "detail", "passed", "rule_name", "sub_results",
        ]
        for derived in ("leaves", "failing_leaves", "decided_by"):
            assert derived not in payload

    async def test_a_run_result_round_trips(self) -> None:
        engine = RulesEngine([_pass("a"), _fail("b")])
        run = await engine.run_all({})

        payload = json.loads(_to_json(run))

        assert payload["passed"] is False
        assert sorted(payload) == ["passed", "results"]
        restored = RunResult(passed=payload["passed"], results=[_from_decoded(r) for r in payload["results"]])
        assert restored == run
        assert [leaf.rule_name for leaf in restored.failing_leaves] == ["b"]

    def test_a_result_built_from_a_list_the_caller_then_appended_to_still_serializes(self) -> None:
        """The early-define/late-init shape: construct from an empty list,
        then append the result to that same list. Copying on construction
        severs the alias, so the walk terminates instead of recursing
        through a result that contains itself."""
        kids: list[RuleResult] = []
        result = RuleResult(rule_name="p", passed=False, sub_results=kids)
        kids.append(result)

        assert json.loads(_to_json(result))["sub_results"] == []

    async def test_serialized_size_grows_with_depth_not_exponentially_in_it(self) -> None:
        """`decided_by_indices` stores positions, not the child results
        themselves. Storing the objects made the graph a DAG -- the same
        children reachable under two fields -- and a tree-shaped encoder
        expands a shared node once per path, so each nesting level doubled
        the output. Forty levels were unencodable; sixteen were 13 MB.
        """
        rule: AndRule | FunctionRule = _fail("leaf")
        for level in range(40):
            rule = AndRule(f"level{level}", [rule])

        encoded = _to_json(await rule.evaluate({}))

        # Exponential growth would put this past any plausible bound long
        # before depth 40; linear growth keeps it in single-digit kilobytes.
        assert len(encoded) < 10_000
        assert '"leaf"' in encoded

    def test_an_unencodable_payload_in_data_fails_at_the_encoder(self) -> None:
        """`data` is opaque: verdict never reads it, and never promises it is
        encodable. A caller putting something the stdlib cannot encode in
        there gets the stdlib's own TypeError, not a verdict error and not a
        silently dropped field."""
        result = RuleResult(rule_name="a", passed=True, data=object())

        with pytest.raises(TypeError):
            _to_json(result)
