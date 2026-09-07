"""Tests for the graduation-verdict example.

These aren't just tests of this example's own logic — because this
project exercises FunctionRule, AndRule, OrRule, a custom Rule shape,
and all three RulesEngine run modes together, this suite functions as
an integration/e2e regression net for `verdict` itself. See
docs/maintenance.md for the full reasoning behind that claim.
"""

from __future__ import annotations

from pathlib import Path

import pytest
from verdict import AndRule, FunctionRule, OrRule

from graduation_verdict import (
    build_graduation_check,
    load_curriculum,
    load_students,
    rule_for_subject,
)

_HERE = Path(__file__).parent
_POLICIES, _ELECTIVE_MINIMUM = load_curriculum(_HERE / "policies.json")
_STUDENTS = load_students(_HERE / "students.json")


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


class TestGraduationVerdictAcrossAllStudents:
    """The one test that matters most: every student in students.json gets the
    verdict its own `expected_passed` field says it should — generically, with
    no hand-listed parametrize table to keep in sync with the data file."""

    @pytest.mark.parametrize("student_id", list(_STUDENTS.keys()))
    async def test_matches_expected_passed(self, student_id: str) -> None:
        engine, graduates = build_graduation_check(_POLICIES, _ELECTIVE_MINIMUM)
        context = _STUDENTS[student_id]
        result = await graduates.evaluate(context)
        assert result.passed == context["expected_passed"], (
            f"{student_id} ({context.get('note', '')}): "
            f"expected passed={context['expected_passed']}, got {result.passed} ({result.detail})"
        )


class TestBuildOnceApplyManyTimes:
    """The 'scale' claim: one built engine/composite pair, reused across every
    student, never rebuilt per lookup."""

    async def test_same_built_objects_serve_every_student(self) -> None:
        engine, graduates = build_graduation_check(_POLICIES, _ELECTIVE_MINIMUM)
        results = {
            student_id: (await graduates.evaluate(context)).passed
            for student_id, context in _STUDENTS.items()
        }
        assert results == {sid: ctx["expected_passed"] for sid, ctx in _STUDENTS.items()}
