"""Failing-run-to-fixture shrinking for a generated `(policies, context,
elective_minimum)` case.

Given a case that reproduces some failure — an invariant violation from
`test_chaos_structural.py`, or a disagreement with `oracle.py` — this
module progressively simplifies it (dropping subjects, pushing scalar
fields toward their simplest value) while re-checking that it still
reproduces the *same* failure, and writes the smallest case found out as
a standalone JSON fixture for a human to read directly.

This module ships as the shrinking *mechanism*. It has never needed to
run against a real failure — the engine agrees with the oracle and every
structural invariant holds across all 500 generated cases — so its own
correctness is demonstrated with a deliberately-injected, throwaway bug
rather than a real one; see docs/testing.md for how that demonstration
was run and reverted.
"""

from __future__ import annotations

import json
from dataclasses import asdict, replace
from pathlib import Path
from typing import Any, Callable, Iterator

from graduation_verdict import SubjectPolicy

# A case still reproduces its original failure, or it doesn't — this is
# the one predicate shape every caller plugs in; see the module docstring.
ReproduceCheck = Callable[[list[SubjectPolicy], dict[str, Any], int], bool]

Case = tuple[list[SubjectPolicy], dict[str, Any], int]


def _without_subject(policies: list[SubjectPolicy], context: dict[str, Any], index: int) -> Case:
    """Drop one subject from the curriculum, and its matching score entry."""
    dropped = policies[index]
    new_policies = policies[:index] + policies[index + 1 :]
    new_scores = {sid: entry for sid, entry in context["scores"].items() if sid != dropped.subject_id}
    new_context = {**context, "scores": new_scores}
    return new_policies, new_context


def _candidate_subject_removals(
    policies: list[SubjectPolicy], context: dict[str, Any], elective_minimum: int
) -> Iterator[Case]:
    """Try dropping each subject, one at a time — the biggest single win."""
    for index in range(len(policies)):
        new_policies, new_context = _without_subject(policies, context, index)
        new_minimum = min(elective_minimum, sum(1 for p in new_policies if p.is_elective))
        yield new_policies, new_context, new_minimum


def _candidate_context_scalar_simplifications(
    policies: list[SubjectPolicy], context: dict[str, Any], elective_minimum: int
) -> Iterator[Case]:
    """Push each top-level numeric context field toward zero, one at a time."""
    for field in ("cgpa", "cgpa_floor", "attendance_pct", "attendance_floor"):
        if context[field] != 0:
            yield list(policies), {**context, field: 0}, elective_minimum


def _candidate_score_entry_simplifications(
    policies: list[SubjectPolicy], context: dict[str, Any], elective_minimum: int
) -> Iterator[Case]:
    """Push each per-subject score field toward its simplest value."""
    for subject_id, entry in context["scores"].items():
        for field, value in entry.items():
            if field in ("written_pct", "practical_pct") and value != 0:
                new_scores = {**context["scores"], subject_id: {**entry, field: 0}}
                yield list(policies), {**context, "scores": new_scores}, elective_minimum
            if field == "has_exemption" and value:
                new_scores = {**context["scores"], subject_id: {**entry, field: False}}
                yield list(policies), {**context, "scores": new_scores}, elective_minimum


def _candidate_policy_simplifications(
    policies: list[SubjectPolicy], context: dict[str, Any], elective_minimum: int
) -> Iterator[Case]:
    """Push each policy's own scalar/boolean fields toward its simplest value."""
    for index, policy in enumerate(policies):
        if policy.written_min_pct != 0:
            new_policy = replace(policy, written_min_pct=0)
            yield policies[:index] + [new_policy] + policies[index + 1 :], context, elective_minimum
        if policy.practical_min_pct not in (None, 0):
            new_policy = replace(policy, practical_min_pct=0)
            yield policies[:index] + [new_policy] + policies[index + 1 :], context, elective_minimum
        if policy.exemption_allowed:
            new_policy = replace(policy, exemption_allowed=False)
            yield policies[:index] + [new_policy] + policies[index + 1 :], context, elective_minimum
        if policy.is_elective:
            new_policy = replace(policy, is_elective=False)
            new_minimum = min(elective_minimum, sum(1 for p in policies if p.is_elective) - 1)
            yield policies[:index] + [new_policy] + policies[index + 1 :], context, max(new_minimum, 0)


def _candidate_elective_minimum_simplifications(
    policies: list[SubjectPolicy], context: dict[str, Any], elective_minimum: int
) -> Iterator[Case]:
    """Push `elective_minimum` toward zero."""
    if elective_minimum > 0:
        yield list(policies), context, elective_minimum - 1


# Every way this shrinker knows how to make a case smaller, tried in this
# order each round — a new simplification strategy is a new generator
# function plus a new row here, never a branch inside `shrink_case`.
_CANDIDATE_GENERATORS: list[Callable[[list[SubjectPolicy], dict[str, Any], int], Iterator[Case]]] = [
    _candidate_subject_removals,
    _candidate_context_scalar_simplifications,
    _candidate_score_entry_simplifications,
    _candidate_policy_simplifications,
    _candidate_elective_minimum_simplifications,
]


def shrink_case(
    policies: list[SubjectPolicy],
    context: dict[str, Any],
    elective_minimum: int,
    still_reproduces: ReproduceCheck,
    *,
    max_rounds: int = 200,
) -> Case:
    """Progressively simplify a failing case while it keeps reproducing.

    A greedy delta-debugging pass: each round tries every registered
    simplification in turn and keeps the first one that still reproduces
    the failure, then restarts from the smaller case. Stops once a full
    round makes no progress at all, or `max_rounds` is hit.

    Args:
        policies: The failing case's curriculum.
        context: The failing case's student context.
        elective_minimum: The failing case's elective threshold.
        still_reproduces: Predicate that re-checks the *original* failure
            against a candidate `(policies, context, elective_minimum)` —
            not just "does something fail", but "does the same thing
            fail". Supplied by the caller, since only the caller knows
            what failure it's chasing (an invariant violation, an oracle
            disagreement, ...).
        max_rounds: Safety cap on simplification passes.

    Returns:
        The smallest `(policies, context, elective_minimum)` this pass
        found — guaranteed to itself satisfy `still_reproduces`.

    Raises:
        ValueError: The initial case doesn't reproduce the failure at
            all — there's nothing to shrink.
    """
    if not still_reproduces(policies, context, elective_minimum):
        raise ValueError(
            "shrink_case's starting case does not reproduce the failure — "
            "nothing to shrink"
        )

    for _ in range(max_rounds):
        progressed = False
        for generate_candidates in _CANDIDATE_GENERATORS:
            for candidate_policies, candidate_context, candidate_minimum in generate_candidates(
                policies, context, elective_minimum
            ):
                if still_reproduces(candidate_policies, candidate_context, candidate_minimum):
                    policies, context, elective_minimum = (
                        candidate_policies,
                        candidate_context,
                        candidate_minimum,
                    )
                    progressed = True
                    break
            if progressed:
                break
        if not progressed:
            break

    return policies, context, elective_minimum


def write_shrunk_fixture(
    path: Path,
    policies: list[SubjectPolicy],
    context: dict[str, Any],
    elective_minimum: int,
    *,
    note: str,
) -> None:
    """Serialize a shrunk failing case to a standalone, human-readable JSON fixture.

    Args:
        path: Where to write the fixture. Parent directories are created
            as needed.
        policies: The shrunk case's curriculum.
        context: The shrunk case's student context.
        elective_minimum: The shrunk case's elective threshold.
        note: A short, human-readable description of what failure this
            case reproduces.
    """
    payload = {
        "note": note,
        "elective_minimum": elective_minimum,
        "subjects": [asdict(p) for p in policies],
        "context": context,
    }
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(payload, indent=2, sort_keys=True))
