"""A second, independent, verdict-free implementation of the graduation decision.

Used as ground truth by test_chaos.py's differential testing. Never
import from `verdict` here.
"""

from __future__ import annotations

from graduation_verdict import SubjectPolicy


def _subject_passes(policy: SubjectPolicy, context: dict) -> bool:
    """Whether one subject's own scores clear its policy's bar — plain if/else, no Rule involved."""
    scores = context["scores"][policy.subject_id]

    if policy.subject_type == "vocational":
        return (
            scores["written_pct"] >= policy.written_min_pct
            and scores["practical_pct"] >= policy.practical_min_pct
        )

    if policy.subject_type == "language" and policy.exemption_allowed:
        return scores["written_pct"] >= policy.written_min_pct or scores.get("has_exemption", False)

    # academic, or a language subject with no exemption path
    return scores["written_pct"] >= policy.written_min_pct


def expected_graduates(policies: list[SubjectPolicy], context: dict, elective_minimum: int) -> bool:
    """Compute the graduation decision directly, with no `verdict` involved at all.

    Args:
        policies: Every subject's own policy (core and elective alike).
        context: One student's scores, cgpa, and attendance.
        elective_minimum: How many electives must pass.

    Returns:
        Whether this student graduates.
    """
    core_policies = [p for p in policies if not p.is_elective]
    elective_policies = [p for p in policies if p.is_elective]

    if not all(_subject_passes(p, context) for p in core_policies):
        return False

    passed_electives = sum(1 for p in elective_policies if _subject_passes(p, context))
    if passed_electives < elective_minimum:
        return False

    if context["cgpa"] < context["cgpa_floor"]:
        return False
    if context["attendance_pct"] < context["attendance_floor"]:
        return False

    return True
