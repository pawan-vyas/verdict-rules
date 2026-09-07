"""Chaos/differential testing: does the engine ever disagree with an independent oracle?

test_graduation_verdict.py proves 8 hand-picked, hand-verified scenarios come
out right. That doesn't say anything about the enormous space of curricula
and students nobody hand-picked. This file checks a much wider space by
comparing two independent implementations against each other instead of
against a fixed expected value:

- `graduation_verdict.build_graduation_check` — the real, `verdict`-based
  implementation this whole project exists to demonstrate.
- `oracle.expected_graduates` — a plain, verdict-free re-implementation,
  deliberately dumb so it's trustworthy by inspection.

If they ever disagree, one of them is wrong — and because every case is
generated from a *seeded* random.Random (never the global `random` module),
that disagreement is exactly reproducible: the same seed always regenerates
the exact same case. "The chaos suite didn't cause any breakdown" is
therefore a real, re-checkable claim across runs, not a one-off observation
about whatever numbers came up this time.

See docs/maintenance.md for the full design reasoning.
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

    Each case is seeded from `CHAOS_SEED + case_index` — not from a single
    shared generator advanced across all `NUM_CASES` calls — specifically so
    any one failing case reproduces on its own: re-run
    `random.Random(CHAOS_SEED + case_index)` through `generate_case` and
    you get the exact same policies/context/elective_minimum that failed,
    with no need to replay every earlier case first.
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
