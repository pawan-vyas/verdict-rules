"""Unit tests for verdict.engine.RulesEngine."""

from __future__ import annotations

import pytest

from verdict.engine import RulesEngine
from verdict.result import RuleResult
from verdict.rule import AndRule, FunctionRule, OrRule


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

    async def test_unknown_group_raises(self) -> None:
        """An unknown group is a mistake, not a legitimately empty set.

        A group exists only because some rule declared it, so a lookup
        matching nothing can only be a typo or a stale name. Returning a
        vacuous pass would mean a misspelled group silently approves.
        Reported the same way run_named reports an unknown rule name.
        """
        engine = RulesEngine([_pass("a", group="g1")])
        with pytest.raises(KeyError, match="no-such-group"):
            await engine.run_group("no-such-group", {})

    async def test_ungrouped_rules_are_never_matched(self) -> None:
        """A rule with no group label makes no group exist."""
        engine = RulesEngine([_pass("a")])  # no group
        with pytest.raises(KeyError, match="g1"):
            await engine.run_group("g1", {})

    async def test_empty_composite_still_passes_vacuously(self) -> None:
        """The deliberate contrast: emptiness is not absence.

        An empty AndRule was *given* an empty list and folds to its
        identity; an unknown group was *asked for* something that does
        not exist. The first is arithmetic, the second is a bug.
        """
        assert (await AndRule("none", []).evaluate({})).passed is True
        assert (await OrRule("none", []).evaluate({})).passed is False


class TestTryLookups:
    """The non-throwing primitives, and that the strict ones sit on top.

    These exist because the engine cannot know what an absent group means.
    For one consumer it is "no constraint applies, pass"; for another "skip
    and do not count it"; for a third "the configuration is wrong, fail".
    A library default would be right for one of them and wrong for the rest,
    so the caller is handed the distinction rather than a guess.
    """

    async def test_try_run_group_returns_the_result_when_present(self) -> None:
        engine = RulesEngine([_pass("a", group="g1"), _fail("b", group="g1")])
        result = await engine.try_run_group("g1", {})
        assert result is not None
        assert [r.rule_name for r in result.results] == ["a", "b"]
        assert result.passed is False

    async def test_try_run_group_returns_none_when_absent(self) -> None:
        engine = RulesEngine([_pass("a", group="g1")])
        assert await engine.try_run_group("no-such-group", {}) is None

    async def test_try_run_named_returns_the_result_when_present(self) -> None:
        engine = RulesEngine([_pass("a")])
        result = await engine.try_run_named("a", {})
        assert result is not None
        assert result.rule_name == "a"

    async def test_try_run_named_returns_none_when_absent(self) -> None:
        engine = RulesEngine([_pass("a")])
        assert await engine.try_run_named("nope", {}) is None

    async def test_none_means_absent_never_failed(self) -> None:
        """The distinction the return type is carrying.

        A rule that exists and fails is a RuleResult with passed=False.
        Only a rule that does not exist is None. Collapsing the two would
        make a typo indistinguishable from a legitimate failure.
        """
        engine = RulesEngine([_fail("present", group="g1")])

        failed = await engine.try_run_named("present", {})
        assert failed is not None and failed.passed is False

        absent = await engine.try_run_named("absent", {})
        assert absent is None

    async def test_strict_forms_are_the_try_forms_plus_an_assertion(self) -> None:
        """run_* is a wrapper, not a parallel implementation.

        Asserting it here keeps the two from drifting: there is one lookup
        path, and the strict form adds only the raise.
        """
        engine = RulesEngine([_pass("a", group="g1")])

        assert (await engine.run_named("a", {})).rule_name == (
            (await engine.try_run_named("a", {})).rule_name  # type: ignore[union-attr]
        )
        strict = await engine.run_group("g1", {})
        lenient = await engine.try_run_group("g1", {})
        assert lenient is not None
        assert strict.passed == lenient.passed
        assert len(strict.results) == len(lenient.results)

    # The full matrix a caller faces: three states a lookup can be in, against
    # the three things a caller can decide absence means. Written as a table
    # because the interesting rows are the ones where the default must NOT
    # fire — a fallback that fires on a present-but-failing group would turn a
    # real rejection into a silent approval, which is the whole failure this
    # API exists to let callers avoid.
    #
    #   group state        | `or True`  | `or False` | strict
    #   -------------------+------------+------------+---------
    #   present, passing   | True       | True       | passed=True
    #   present, failing   | False      | False      | passed=False   <- default must not fire
    #   absent             | True       | False      | raises
    @pytest.mark.parametrize(
        ("group", "default_true", "default_false", "strict_raises"),
        [
            ("passing", True, True, False),
            ("failing", False, False, False),
            ("absent", True, False, True),
        ],
    )
    async def test_fallback_matrix(
        self,
        group: str,
        default_true: bool,
        default_false: bool,
        strict_raises: bool,
    ) -> None:
        engine = RulesEngine([
            _pass("p", group="passing"),
            _fail("f", group="failing"),
        ])

        result = await engine.try_run_group(group, {})

        # Decision 1: absence means "no constraint applies here".
        assert (result.passed if result is not None else True) is default_true

        # Decision 2: absence means "the configuration is wrong".
        assert (result.passed if result is not None else False) is default_false

        # Decision 3: absence was never expected — the strict form says so.
        if strict_raises:
            with pytest.raises(KeyError):
                await engine.run_group(group, {})
        else:
            strict = await engine.run_group(group, {})
            assert strict.passed is default_true
            assert strict.passed is default_false

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

    async def test_skipping_counts_only_what_exists(self) -> None:
        """The fourth shape: absence contributes nothing either way."""
        engine = RulesEngine([_fail("f", group="failing")])

        evaluated = [
            r
            for r in [
                await engine.try_run_group("failing", {}),
                await engine.try_run_group("absent", {}),
            ]
            if r is not None
        ]

        assert len(evaluated) == 1, "an absent group contributes nothing"
        assert evaluated[0].passed is False, "a present one still reports honestly"


class TestIntrospection:
    """rule_names/group_names let a caller check instead of catching."""

    async def test_reports_registered_names_in_order(self) -> None:
        engine = RulesEngine([_pass("a", group="g1"), _pass("b", group="g2"), _pass("c")])
        assert engine.rule_names == ("a", "b", "c")
        assert engine.group_names == ("g1", "g2")

    async def test_group_names_is_exactly_what_run_group_accepts(self) -> None:
        engine = RulesEngine([_pass("a", group="g1"), _pass("b")])
        for group in engine.group_names:
            await engine.run_group(group, {})  # must not raise
        assert "g2" not in engine.group_names
        with pytest.raises(KeyError):
            await engine.run_group("g2", {})

    async def test_empty_engine_reports_nothing(self) -> None:
        engine = RulesEngine([])
        assert engine.rule_names == ()
        assert engine.group_names == ()


class TestConstruction:
    def test_duplicate_names_last_one_wins_in_by_name_lookup(self) -> None:
        first = _pass("a")
        second = _fail("a")
        engine = RulesEngine([first, second])
        assert engine._by_name["a"] is second


class TestExceptionPropagation:
    """A predicate's own exception is never caught anywhere in the engine
    — it propagates exactly as if the caller had invoked the predicate
    directly, with nothing in between. This is a documented guarantee
    (SKILL.md, extension.md Recipe 7), so a regression here — someone
    "helpfully" wrapping the loop in a try/except — would silently break
    a stated promise rather than fail loudly, which is exactly the
    failure mode this test exists to catch."""

    async def test_run_all_does_not_catch_a_predicate_s_exception(self) -> None:
        async def flaky(context: dict) -> RuleResult:
            raise TimeoutError("external check unreachable")

        engine = RulesEngine([_pass("a"), FunctionRule("flaky", flaky), _pass("c")])
        with pytest.raises(TimeoutError):
            await engine.run_all({})

    async def test_run_group_does_not_catch_a_predicate_s_exception(self) -> None:
        async def flaky(context: dict) -> RuleResult:
            raise ValueError("bad input")

        engine = RulesEngine([FunctionRule("flaky", flaky, group="g")])
        with pytest.raises(ValueError):
            await engine.run_group("g", {})
