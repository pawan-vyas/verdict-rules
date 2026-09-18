"""Chaos/differential testing: compares
`graduation_verdict.build_graduation_check` against
`oracle.expected_graduates` across a wide, randomly-generated space of
curricula and students.

Every case is generated from a seeded `random.Random`, never the global
`random` module, so a disagreement is exactly reproducible from its seed.

See docs/testing/README.md for the full design.
"""

from __future__ import annotations

import random

import pytest

from chaos_data import generate_case
from graduation_verdict import build_graduation_check
from oracle import expected_graduates

# Pinned. Do not change without a deliberate reason — bumping this reshuffles
# every case's data, silently trading which edge cases get covered for which.
# If you need *more* coverage, raise NUM_CASES instead; that's strictly
# additive; changing CHAOS_SEED is not.
CHAOS_SEED = 20260907
NUM_CASES = 500


@pytest.mark.parametrize("case_index", range(NUM_CASES))
async def test_engine_agrees_with_independent_oracle(case_index: int) -> None:
    """For a deterministically-generated, schema-valid random case, the real
    engine's verdict and the independent oracle's verdict must always match.

    Each case is seeded from `CHAOS_SEED + case_index`, not a single shared
    generator advanced across all `NUM_CASES` calls, so a failing case
    reproduces on its own via `random.Random(CHAOS_SEED + case_index)`
    through `generate_case`.
    """
    rng = random.Random(CHAOS_SEED + case_index)
    policies, context, elective_minimum = generate_case(rng)

    _, graduates = build_graduation_check(policies, elective_minimum)
    actual = (await graduates.evaluate(context)).passed
    expected = expected_graduates(policies, context, elective_minimum)

    assert actual == expected, (
        f"case_index={case_index} (seed={CHAOS_SEED + case_index}): "
        f"engine said {actual}, oracle said {expected}\n"
        f"elective_minimum={elective_minimum}\n"
        f"policies={policies}\n"
        f"context={context}"
    )
