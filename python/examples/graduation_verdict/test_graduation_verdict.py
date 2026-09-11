"""Tests for the graduation-verdict example.

These aren't just tests of this example's own logic — because this
project exercises FunctionRule, AndRule, OrRule, a custom Rule shape,
and all three RulesEngine run modes together, this suite functions as
an integration/e2e regression net for `verdict` itself. See
docs/maintenance.md for the full reasoning behind that claim.
"""

from __future__ import annotations

import json
from pathlib import Path

import pytest
from verdict import AndRule, FunctionRule, OrRule

from graduation_verdict import (
    SubjectPolicy,
    build_graduation_check,
    load_curriculum,
    load_students,
    rule_for_subject,
)

_HERE = Path(__file__).parent
# Fixture data lives at the repo root, shared by every language's own port of
# this example — see fixtures/graduation_verdict/README.md for the contract.
_FIXTURES = _HERE.parents[2] / "fixtures" / "graduation_verdict"
_POLICIES, _ELECTIVE_MINIMUM = load_curriculum(_FIXTURES / "policies.json")
_STUDENTS = load_students(_FIXTURES / "students.json")
_EDGE_CASES = json.loads((_FIXTURES / "edge_cases.json").read_text())


def _policy(subject_id: str):
    return next(p for p in _POLICIES if p.subject_id == subject_id)


class TestRuleShapeDispatch:
    """Each subject_type must produce the Rule shape docs/architecture.md claims."""

    def test_academic_subject_is_a_plain_function_rule(self) -> None:
        rule = rule_for_subject(_policy("MATH101"))
        assert isinstance(rule, FunctionRule)

    def test_vocational_subject_is_an_and_rule_of_two(self) -> None:
        rule = rule_for_subject(_policy("WORKSHOP201"))
        assert isinstance(rule, AndRule)

    def test_language_subject_with_exemption_is_an_or_rule(self) -> None:
        rule = rule_for_subject(_policy("FRENCH101"))
        assert isinstance(rule, OrRule)

    def test_core_vs_elective_group_tagging(self) -> None:
        assert rule_for_subject(_policy("MATH101")).group == "core"
        assert rule_for_subject(_policy("ELECTIVE_ART")).group == "elective"

    def test_unknown_subject_type_raises(self) -> None:
        from dataclasses import replace
        bad_policy = replace(_policy("MATH101"), subject_type="portfolio")
        with pytest.raises(ValueError):
            rule_for_subject(bad_policy)


class TestVocationalAndRule:
    """The AndRule built for a vocational subject needs both scores to pass."""

    async def test_fails_if_only_written_passes(self) -> None:
        rule = rule_for_subject(_policy("WORKSHOP201"))
        context = {"scores": {"WORKSHOP201": {"written_pct": 50, "practical_pct": 45}}}
        result = await rule.evaluate(context)
        assert result.passed is False

    async def test_passes_if_both_scores_clear_their_own_bar(self) -> None:
        rule = rule_for_subject(_policy("WORKSHOP201"))
        context = {"scores": {"WORKSHOP201": {"written_pct": 50, "practical_pct": 70}}}
        result = await rule.evaluate(context)
        assert result.passed is True


class TestLanguageOrRule:
    """The OrRule built for a language subject accepts either the paper or an exemption."""

    async def test_passes_via_exemption_when_the_paper_fails(self) -> None:
        rule = rule_for_subject(_policy("FRENCH101"))
        context = {"scores": {"FRENCH101": {"written_pct": 20, "has_exemption": True}}}
        result = await rule.evaluate(context)
        assert result.passed is True

    async def test_fails_when_neither_the_paper_nor_an_exemption_applies(self) -> None:
        rule = rule_for_subject(_policy("FRENCH101"))
        context = {"scores": {"FRENCH101": {"written_pct": 20, "has_exemption": False}}}
        result = await rule.evaluate(context)
        assert result.passed is False


class TestEngineRunModes:
    """run_named/run_group/run_all each serve the specific job docs/architecture.md claims."""

    async def test_run_named_looks_up_one_subject(self) -> None:
        engine, _ = build_graduation_check(_POLICIES, _ELECTIVE_MINIMUM)
        result = await engine.run_named("WORKSHOP201", _STUDENTS["alice"])
        assert result.rule_name == "WORKSHOP201"
        assert result.passed is True

    async def test_run_group_core_reports_every_core_subject(self) -> None:
        engine, _ = build_graduation_check(_POLICIES, _ELECTIVE_MINIMUM)
        result = await engine.run_group("core", _STUDENTS["alice"])
        assert {r.rule_name for r in result.results} == {"MATH101", "ENG101", "WORKSHOP201", "FRENCH101"}

    async def test_run_group_elective_reports_every_elective_subject(self) -> None:
        engine, _ = build_graduation_check(_POLICIES, _ELECTIVE_MINIMUM)
        result = await engine.run_group("elective", _STUDENTS["alice"])
        assert {r.rule_name for r in result.results} == {"ELECTIVE_ART", "ELECTIVE_MUSIC", "ELECTIVE_CS"}

    async def test_run_all_reports_every_subject(self) -> None:
        engine, _ = build_graduation_check(_POLICIES, _ELECTIVE_MINIMUM)
        result = await engine.run_all(_STUDENTS["alice"])
        assert len(result.results) == len(_POLICIES)

    async def test_run_group_never_short_circuits_unlike_the_graduates_composite(self) -> None:
        # bob fails ENG101 (core) — run_group must still report every other core subject.
        engine, _ = build_graduation_check(_POLICIES, _ELECTIVE_MINIMUM)
        result = await engine.run_group("core", _STUDENTS["bob"])
        assert len(result.results) == 4
        assert result.passed is False


def _failing_chain(result) -> list[str]:
    """Walk the first failing branch down, collecting rule names.

    This is what proves `RuleResult.data` is never flattened: a nested
    failure has to still be reachable by following `data` downward.
    """
    chain: list[str] = []
    node = result
    while isinstance(node.data, list) and node.data:
        nxt = next((sub for sub in node.data if not sub.passed), None)
        if nxt is None:
            break
        chain.append(nxt.rule_name)
        node = nxt
    return chain


class TestSharedFixtureContract:
    """Every expectation in the shared fixture, asserted.

    This is the cross-language contract: each port of this example must
    reproduce these exact numbers. See
    fixtures/graduation_verdict/README.md for what each field proves and
    why the counts matter more than the booleans.
    """

    @pytest.mark.parametrize("student_id", list(_STUDENTS.keys()))
    async def test_verdict_matches(self, student_id: str) -> None:
        _, graduates = build_graduation_check(_POLICIES, _ELECTIVE_MINIMUM)
        context = _STUDENTS[student_id]
        expected = context["expected"]
        result = await graduates.evaluate(context)
        assert result.passed == expected["passed"], (
            f"{student_id} ({context.get('note', '')}): "
            f"expected passed={expected['passed']}, got {result.passed} ({result.detail})"
        )

    @pytest.mark.parametrize("student_id", list(_STUDENTS.keys()))
    async def test_short_circuit_count_matches(self, student_id: str) -> None:
        """The assertion a boolean-only fixture cannot make.

        bob and gita both fail, but bob stops after one rule and gita
        runs all four. An implementation that evaluated sub-rules
        concurrently would return both booleans correctly and fail here.
        """
        _, graduates = build_graduation_check(_POLICIES, _ELECTIVE_MINIMUM)
        context = _STUDENTS[student_id]
        expected = context["expected"]
        result = await graduates.evaluate(context)
        assert len(result.data) == expected["rules_evaluated"], (
            f"{student_id}: expected {expected['rules_evaluated']} sub-rules to run, "
            f"got {len(result.data)} — short-circuiting is broken or over-eager"
        )

    @pytest.mark.parametrize("student_id", list(_STUDENTS.keys()))
    async def test_failing_chain_matches(self, student_id: str) -> None:
        """Proves nesting survives: deepak's failure is three levels deep."""
        _, graduates = build_graduation_check(_POLICIES, _ELECTIVE_MINIMUM)
        context = _STUDENTS[student_id]
        expected = context["expected"]
        result = await graduates.evaluate(context)
        chain = _failing_chain(result)
        assert chain == expected["failing_chain"], (
            f"{student_id}: expected failing chain {expected['failing_chain']}, got {chain}"
        )
        assert (chain[0] if chain else None) == expected["failing_rule"]

    @pytest.mark.parametrize("student_id", list(_STUDENTS.keys()))
    async def test_run_all_never_short_circuits(self, student_id: str) -> None:
        """run_all reports every registered rule for every student, always."""
        engine, _ = build_graduation_check(_POLICIES, _ELECTIVE_MINIMUM)
        context = _STUDENTS[student_id]
        expected = context["expected"]["run_all"]
        result = await engine.run_all(context)
        assert len(result.results) == expected["evaluated"]
        assert result.passed == expected["passed"]

    @pytest.mark.parametrize("student_id", list(_STUDENTS.keys()))
    async def test_group_results_match(self, student_id: str) -> None:
        """harish is the interesting one: he graduates while his elective
        group 'fails', because the group verdict is all-must-pass and the
        composite's requirement is at-least-two-of-three."""
        engine, _ = build_graduation_check(_POLICIES, _ELECTIVE_MINIMUM)
        context = _STUDENTS[student_id]
        for group, expected in context["expected"]["groups"].items():
            result = await engine.run_group(group, context)
            assert len(result.results) == expected["evaluated"], f"{student_id}/{group}"
            assert result.passed == expected["passed"], f"{student_id}/{group}"


class TestVacuousTruthEdgeCases:
    """The degenerate curricula, from the shared fixture's edge_cases.json.

    `AndRule([])` passing while an at-least-N rule over an empty set
    fails for N > 0 is deliberately asymmetric, and it is the kind of
    thing a port gets backwards without noticing — nothing in the main
    student set exercises an empty rule list at all.
    """

    @pytest.mark.parametrize("case_name", list(_EDGE_CASES.keys()))
    async def test_edge_case_matches_fixture(self, case_name: str) -> None:
        case = _EDGE_CASES[case_name]
        policies = [SubjectPolicy(**s) for s in case["curriculum"]["subjects"]]
        minimum = case["curriculum"]["elective_minimum"]
        expected = case["expected"]
        student = case["student"]

        engine, graduates = build_graduation_check(policies, minimum)
        result = await graduates.evaluate(student)

        assert result.passed == expected["passed"], f"{case_name}: {case['note']}"
        assert len(result.data) == expected["rules_evaluated"], case_name
        chain = _failing_chain(result)
        assert chain == expected["failing_chain"], case_name

        run_all = await engine.run_all(student)
        assert len(run_all.results) == expected["run_all"]["evaluated"], case_name
        assert run_all.passed == expected["run_all"]["passed"], case_name

        for group, exp in expected["groups"].items():
            group_result = await engine.run_group(group, student)
            assert len(group_result.results) == exp["evaluated"], f"{case_name}/{group}"
            assert group_result.passed == exp["passed"], f"{case_name}/{group}"


class TestBuildOnceApplyManyTimes:
    """The 'scale' claim: one built engine/composite pair, reused across every
    student, never rebuilt per lookup."""

    async def test_same_built_objects_serve_every_student(self) -> None:
        engine, graduates = build_graduation_check(_POLICIES, _ELECTIVE_MINIMUM)
        results = {
            student_id: (await graduates.evaluate(context)).passed
            for student_id, context in _STUDENTS.items()
        }
        assert results == {sid: ctx["expected"]["passed"] for sid, ctx in _STUDENTS.items()}
