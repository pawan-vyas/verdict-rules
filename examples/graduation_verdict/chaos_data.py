"""Deterministic, randomized (policy, student) case generation for test_chaos.py.

"Deterministic" is the load-bearing word: every function here takes a
`random.Random` instance explicitly — never the global `random` module — so
a case built from a given seed is exactly reproducible. test_chaos.py seeds
one `random.Random` per case from a pinned constant plus that case's own
index, so any single failing case can be regenerated on its own without
replaying every case before it.

Every value generated stays within the schema's valid domain (a real
percentage, a real subject_type, etc.) — this generates a wide space of
*valid* curricula and students, not malformed input. Garbage-input handling
is a different, narrower concern this suite doesn't cover.
"""

from __future__ import annotations

import random

from graduation_verdict import SubjectPolicy

_SUBJECT_TYPES = ("academic", "vocational", "language")


def random_policy(rng: random.Random, subject_id: str) -> SubjectPolicy:
    """Build one randomized, but schema-valid, subject policy.

    Args:
        rng: The seeded generator to draw from.
        subject_id: Identifier for the generated subject.

    Returns:
        A `SubjectPolicy` whose type, thresholds, and flags are all
        randomized independently.
    """
    subject_type = rng.choice(_SUBJECT_TYPES)
    return SubjectPolicy(
        subject_id=subject_id,
        subject_type=subject_type,
        written_min_pct=rng.uniform(0, 100),
        practical_min_pct=rng.uniform(0, 100) if subject_type == "vocational" else None,
        exemption_allowed=rng.choice([True, False]) if subject_type == "language" else False,
        is_elective=rng.choice([True, False]),
    )


def random_context(rng: random.Random, policies: list[SubjectPolicy]) -> dict:
    """Build one randomized student context matching the given policies.

    Args:
        rng: The seeded generator to draw from.
        policies: The curriculum this context's scores must cover — one
            entry generated per policy, shaped to that policy's own type.

    Returns:
        A context dict in exactly the shape `graduation_verdict`'s rules
        and `oracle.expected_graduates` both expect.
    """
    scores = {}
    for policy in policies:
        entry = {"written_pct": rng.uniform(0, 100)}
        if policy.subject_type == "vocational":
            entry["practical_pct"] = rng.uniform(0, 100)
        if policy.subject_type == "language":
            entry["has_exemption"] = rng.choice([True, False])
        scores[policy.subject_id] = entry

    return {
        "scores": scores,
        "cgpa": rng.uniform(0, 10),
        "cgpa_floor": rng.uniform(0, 10),
        "attendance_pct": rng.uniform(0, 100),
        "attendance_floor": rng.uniform(0, 100),
    }


def generate_case(rng: random.Random, num_subjects: int = 7) -> tuple[list[SubjectPolicy], dict, int]:
    """Build one complete, self-consistent randomized case.

    Args:
        rng: The seeded generator to draw from — the sole source of
            randomness, so the same `rng` state always produces the same
            case.
        num_subjects: How many subjects the generated curriculum has.

    Returns:
        A `(policies, context, elective_minimum)` triple ready to hand to
        both `graduation_verdict.build_graduation_check` and
        `oracle.expected_graduates`. `elective_minimum` is always
        achievable (bounded by how many electives were actually
        generated), so a mismatch between the two implementations is
        never explained away as "an impossible curriculum."
    """
    policies = [random_policy(rng, f"SUBJ{i}") for i in range(num_subjects)]
    context = random_context(rng, policies)
    elective_count = sum(1 for p in policies if p.is_elective)
    elective_minimum = rng.randint(0, elective_count)
    return policies, context, elective_minimum
