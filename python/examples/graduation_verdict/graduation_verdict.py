"""Graduation requirement verdict, implemented with `verdict`.

See docs/samples/graduation-requirement-verdict/README.md for the
design and fixtures/graduation_verdict/README.md for the fixture
contract. Every function is exercised by test_graduation_verdict.py.
"""

from __future__ import annotations

import json
from dataclasses import dataclass
from pathlib import Path
from typing import Any, Callable

from verdict import AndRule, FunctionRule, OrRule, Rule, RuleResult, RulesEngine

_HERE = Path(__file__).parent
# Fixture data lives at the repo root, shared by every language's own port of
# this example — see fixtures/graduation_verdict/README.md for the contract.
_FIXTURES = _HERE.parents[2] / "fixtures" / "graduation_verdict"


@dataclass(frozen=True)
class SubjectPolicy:
    """One subject's grading policy — read from wherever a real curriculum stores it.

    Attributes:
        subject_id: Unique subject code (e.g. "MATH101").
        subject_type: "academic" | "vocational" | "language" — decides
            which `Rule` shape `rule_for_subject` builds.
        written_min_pct: Minimum passing percentage on the written score
            every subject type checks.
        practical_min_pct: Minimum passing percentage on the practical
            score — vocational subjects only, `None` otherwise.
        exemption_allowed: Whether an approved exemption can substitute
            for the written score — language subjects only.
        is_elective: Whether this subject counts toward the elective
            requirement rather than the core requirement.
    """

    subject_id: str
    subject_type: str
    written_min_pct: float
    practical_min_pct: float | None = None
    exemption_allowed: bool = False
    is_elective: bool = False


class AtLeastNRule:
    """Passes if at least `minimum` of the given sub-rules pass.

    Not part of `verdict` itself; see docs/extending/new-rule-shape/.
    Evaluates every sub-rule unconditionally.
    """

    def __init__(
        self, name: str, rules: list[Rule[dict[str, Any]]], minimum: int, group: str | None = None
    ) -> None:
        self.name = name
        self.group = group
        self._rules = rules
        self._minimum = minimum

    async def evaluate(self, context: dict[str, Any]) -> RuleResult:
        sub_results = [await rule.evaluate(context) for rule in self._rules]
        passed_count = sum(1 for r in sub_results if r.passed)
        return RuleResult(
            rule_name=self.name,
            passed=passed_count >= self._minimum,
            detail=f"{passed_count} of {len(self._rules)} passed, needed {self._minimum}",
            data=sub_results,
        )


def _written_rule(policy: SubjectPolicy, *, name: str) -> FunctionRule[dict[str, Any]]:
    async def predicate(context: dict[str, Any]) -> RuleResult:
        pct = context["scores"][policy.subject_id]["written_pct"]
        return RuleResult(
            rule_name=name,
            passed=pct >= policy.written_min_pct,
            detail=f"{pct} vs {policy.written_min_pct}",
        )
    return FunctionRule(name, predicate)


def _practical_rule(policy: SubjectPolicy, *, name: str) -> FunctionRule[dict[str, Any]]:
    async def predicate(context: dict[str, Any]) -> RuleResult:
        pct = context["scores"][policy.subject_id]["practical_pct"]
        return RuleResult(
            rule_name=name,
            passed=pct >= policy.practical_min_pct,
            detail=f"{pct} vs {policy.practical_min_pct}",
        )
    return FunctionRule(name, predicate)


def _exemption_rule(policy: SubjectPolicy, *, name: str) -> FunctionRule[dict[str, Any]]:
    async def predicate(context: dict[str, Any]) -> RuleResult:
        exempt = context["scores"][policy.subject_id].get("has_exemption", False)
        return RuleResult(rule_name=name, passed=exempt)
    return FunctionRule(name, predicate)


def _vocational_subject_rule(policy: SubjectPolicy, sid: str, group: str) -> Rule[dict[str, Any]]:
    return AndRule(sid, [
        _written_rule(policy, name=f"{sid}:written"),
        _practical_rule(policy, name=f"{sid}:practical"),
    ], group=group)


def _language_subject_rule(policy: SubjectPolicy, sid: str, group: str) -> Rule[dict[str, Any]]:
    if policy.exemption_allowed:
        return OrRule(sid, [
            _written_rule(policy, name=f"{sid}:written"),
            _exemption_rule(policy, name=f"{sid}:exemption"),
        ], group=group)
    return FunctionRule(sid, _written_rule(policy, name=sid)._predicate, group=group)


def _academic_subject_rule(policy: SubjectPolicy, sid: str, group: str) -> Rule[dict[str, Any]]:
    return FunctionRule(sid, _written_rule(policy, name=sid)._predicate, group=group)


# One builder per subject_type, keyed by the value itself — adding a fifth
# subject type is a new function plus a new row here, never a new branch in
# rule_for_subject.
_SUBJECT_RULE_BUILDERS: dict[str, Callable[[SubjectPolicy, str, str], Rule[dict[str, Any]]]] = {
    "vocational": _vocational_subject_rule,
    "language": _language_subject_rule,
    "academic": _academic_subject_rule,
}


def rule_for_subject(policy: SubjectPolicy) -> Rule[dict[str, Any]]:
    """Turn one subject's policy into a Rule — the shape depends on its type.

    Args:
        policy: The subject's own policy row.

    Returns:
        An `AndRule` (written AND practical) for a vocational subject, an
        `OrRule` (written OR exemption) for a language subject with an
        exemption path, or a plain `FunctionRule` otherwise.

    Raises:
        ValueError: `policy.subject_type` isn't one of the known types.
    """
    group = "elective" if policy.is_elective else "core"
    sid = policy.subject_id

    builder = _SUBJECT_RULE_BUILDERS.get(policy.subject_type)
    if builder is None:
        raise ValueError(f"unknown subject_type {policy.subject_type!r} for subject {sid!r}")
    return builder(policy, sid, group)


async def cgpa_met(context: dict[str, Any]) -> RuleResult:
    return RuleResult(rule_name="cgpa_met", passed=context["cgpa"] >= context["cgpa_floor"])


async def attendance_met(context: dict[str, Any]) -> RuleResult:
    return RuleResult(
        rule_name="attendance_met",
        passed=context["attendance_pct"] >= context["attendance_floor"],
    )


def load_curriculum(path: Path) -> tuple[list[SubjectPolicy], int]:
    """Read the curriculum — subject policies and the elective-count threshold — from a JSON file.

    Args:
        path: Path to a JSON object with an `elective_minimum` integer
            and a `subjects` array of subject-policy objects.

    Returns:
        A `(policies, elective_minimum)` pair — one `SubjectPolicy` per
        entry, missing optional fields filled with their dataclass
        defaults, and the minimum number of electives required to
        graduate.
    """
    curriculum = json.loads(path.read_text())
    policies = [
        SubjectPolicy(
            subject_id=row["subject_id"],
            subject_type=row["subject_type"],
            written_min_pct=row["written_min_pct"],
            practical_min_pct=row.get("practical_min_pct"),
            exemption_allowed=row.get("exemption_allowed", False),
            is_elective=row.get("is_elective", False),
        )
        for row in curriculum["subjects"]
    ]
    return policies, curriculum["elective_minimum"]


def load_students(path: Path) -> dict[str, dict]:
    """Read the batch of student records from a JSON file.

    Args:
        path: Path to a JSON object keyed by student id.

    Returns:
        The parsed dict as-is — each value's own shape is the `context`
        dict `verdict` expects, plus a `note` and `expected_passed`
        field the rules themselves don't read.
    """
    return json.loads(path.read_text())


def build_graduation_check(
    policies: list[SubjectPolicy], elective_minimum: int
) -> tuple[RulesEngine[dict[str, Any]], AndRule[dict[str, Any]]]:
    """Build both structures from one policy list: a diagnostic engine and a fast verdict.

    Args:
        policies: Every subject's own policy.
        elective_minimum: How many electives must pass.

    Returns:
        A `(engine, graduates)` pair built from the same underlying
        `Rule` objects — `engine` serves `run_named`/`run_group`/
        `run_all` lookups, `graduates` is the short-circuiting
        pass/fail composite.
    """
    subject_rules = [rule_for_subject(p) for p in policies]
    engine = RulesEngine(subject_rules)

    core_rules = [r for r in subject_rules if r.group == "core"]
    elective_rules = [r for r in subject_rules if r.group == "elective"]

    graduates: AndRule[dict[str, Any]] = AndRule("graduates", [
        AndRule("all_core_subjects_pass", core_rules),
        AtLeastNRule("elective_requirement", elective_rules, minimum=elective_minimum),
        FunctionRule("cgpa_met", cgpa_met),
        FunctionRule("attendance_met", attendance_met),
    ])
    return engine, graduates


async def _demo() -> None:
    policies, elective_minimum = load_curriculum(_FIXTURES / "policies.json")
    students = load_students(_FIXTURES / "students.json")
    engine, graduates = build_graduation_check(policies, elective_minimum)

    print("=== Graduation Requirement Verdict — Demo ===")
    print(f"Built once from policies.json: {len(policies)} subjects\n")

    alice = students["alice"]
    print("--- Detailed lookup for 'alice' ---")
    named = await engine.run_named("WORKSHOP201", alice)
    print(f"run_named('WORKSHOP201'): {'PASS' if named.passed else 'FAIL'}")
    core = await engine.run_group("core", alice)
    print("run_group('core'):     " + ", ".join(f"{r.rule_name}={'PASS' if r.passed else 'FAIL'}" for r in core.results))
    elective = await engine.run_group("elective", alice)
    print("run_group('elective'): " + ", ".join(f"{r.rule_name}={'PASS' if r.passed else 'FAIL'}" for r in elective.results))
    full = await engine.run_all(alice)
    print(f"run_all(): {len(full.results)} subjects reported\n")

    print("--- Batch verdict across all students (same built engine, no rebuild) ---")
    for student_id, context in students.items():
        verdict = await graduates.evaluate(context)
        status = "GRADUATES" if verdict.passed else "DOES NOT GRADUATE"
        reason = "" if verdict.passed else f"  ({verdict.detail})"
        print(f"{student_id:10s}: {status}{reason}")


if __name__ == "__main__":
    import asyncio

    asyncio.run(_demo())
