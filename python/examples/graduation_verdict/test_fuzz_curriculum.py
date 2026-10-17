"""Fuzz `load_curriculum` with deliberately malformed `policies.json` shapes.

`load_curriculum` does no schema validation of its own beyond the key
accesses it needs to build a `SubjectPolicy` — see its own docstring in
graduation_verdict.py. This file proves that whatever malformed input a
seeded RNG throws at it, the reader only ever does one of two things:
parses into a valid policy list, or raises one of the errors that
naturally fall out of its own dict/list access and `json.loads` — never
an unrelated crash (a stray `AttributeError`/`IndexError`) and never a
silently wrong policy list. See docs/testing.md.
"""

from __future__ import annotations

import copy
import json
import random
from pathlib import Path
from typing import Any, Callable

import pytest

from graduation_verdict import SubjectPolicy, load_curriculum

# Pinned, same reasoning as test_chaos.py's own CHAOS_SEED: reshuffling this
# trades away coverage rather than adding to it. Raise NUM_FUZZ_CASES for
# more coverage instead.
FUZZ_SEED = 20260907
NUM_FUZZ_CASES = 150

_VALID_SUBJECT_TYPES = ("academic", "vocational", "language")

# The only error types `load_curriculum`'s own dict/list access and
# `json.loads` can naturally raise against malformed input — see this
# file's own docstring. Anything else surfacing is a genuine reader bug.
_EXPECTED_ERRORS: tuple[type[Exception], ...] = (json.JSONDecodeError, KeyError, TypeError)


def _random_subject(rng: random.Random, index: int) -> dict[str, Any]:
    """One well-formed subject row, before any mutation is applied."""
    subject_type = rng.choice(_VALID_SUBJECT_TYPES)
    row: dict[str, Any] = {
        "subject_id": f"SUBJ{index}",
        "subject_type": subject_type,
        "written_min_pct": rng.uniform(0, 100),
    }
    if subject_type == "vocational":
        row["practical_min_pct"] = rng.uniform(0, 100)
    if subject_type == "language":
        row["exemption_allowed"] = rng.choice([True, False])
    row["is_elective"] = rng.choice([True, False])
    return row


def _random_curriculum(rng: random.Random) -> dict[str, Any]:
    """One well-formed curriculum document, before any mutation is applied."""
    return {
        "elective_minimum": rng.randint(0, 3),
        "subjects": [_random_subject(rng, i) for i in range(rng.randint(1, 5))],
    }


def _mutate_wrong_type_scalar(rng: random.Random, curriculum: dict[str, Any]) -> str:
    """A string where a number is expected, or vice versa."""
    if rng.choice([True, False]) and curriculum["subjects"]:
        subject = rng.choice(curriculum["subjects"])
        subject["written_min_pct"] = "not-a-number"
    else:
        curriculum["elective_minimum"] = "not-a-number"
    return json.dumps(curriculum)


def _mutate_wrong_type_bool(rng: random.Random, curriculum: dict[str, Any]) -> str:
    """A number where a bool is expected."""
    if curriculum["subjects"]:
        subject = rng.choice(curriculum["subjects"])
        subject["exemption_allowed"] = 12345
    return json.dumps(curriculum)


def _mutate_drop_top_level_key(rng: random.Random, curriculum: dict[str, Any]) -> str:
    """A required top-level key goes missing."""
    del curriculum[rng.choice(["subjects", "elective_minimum"])]
    return json.dumps(curriculum)


def _mutate_drop_subject_key(rng: random.Random, curriculum: dict[str, Any]) -> str:
    """A required per-subject key goes missing."""
    if curriculum["subjects"]:
        subject = rng.choice(curriculum["subjects"])
        subject.pop(rng.choice(["subject_id", "subject_type", "written_min_pct"]), None)
    return json.dumps(curriculum)


def _mutate_add_unexpected_key(rng: random.Random, curriculum: dict[str, Any]) -> str:
    """An unexpected key, at the top level or on a subject row."""
    if rng.choice([True, False]) or not curriculum["subjects"]:
        curriculum["unexpected_top_level_field"] = rng.uniform(0, 1)
    else:
        rng.choice(curriculum["subjects"])["unexpected_subject_field"] = [1, 2, 3]
    return json.dumps(curriculum)


def _mutate_deeply_nested_junk(rng: random.Random, curriculum: dict[str, Any]) -> str:
    """A scalar field replaced with deeply nested, unrelated structure."""
    junk = {"a": {"b": [1, 2, {"c": None}]}}
    if curriculum["subjects"]:
        rng.choice(curriculum["subjects"])["written_min_pct"] = junk
    else:
        curriculum["elective_minimum"] = junk
    return json.dumps(curriculum)


def _mutate_truncated_json(rng: random.Random, curriculum: dict[str, Any]) -> str:
    """A truncated, incomplete JSON document."""
    text = json.dumps(curriculum)
    cut = rng.randint(1, max(1, len(text) - 1))
    return text[:cut]


def _mutate_empty_subjects(rng: random.Random, curriculum: dict[str, Any]) -> str:
    """An empty `subjects` array — valid, and should parse to an empty list."""
    curriculum["subjects"] = []
    return json.dumps(curriculum)


def _mutate_null_document(rng: random.Random, curriculum: dict[str, Any]) -> str:
    """`null` in place of the whole curriculum object."""
    return json.dumps(None)


def _mutate_null_subject_entry(rng: random.Random, curriculum: dict[str, Any]) -> str:
    """`null` in place of one subject entry."""
    if curriculum["subjects"]:
        index = rng.randrange(len(curriculum["subjects"]))
        curriculum["subjects"][index] = None
    else:
        curriculum["subjects"] = [None]
    return json.dumps(curriculum)


def _mutate_subjects_not_a_list(rng: random.Random, curriculum: dict[str, Any]) -> str:
    """`subjects` replaced with a non-list value."""
    curriculum["subjects"] = rng.choice([{}, 42, "not-a-list", True])
    return json.dumps(curriculum)


def _mutate_curriculum_not_an_object(rng: random.Random, curriculum: dict[str, Any]) -> str:
    """The whole document replaced with a non-object JSON value."""
    return json.dumps(rng.choice([[], 42, "not-an-object", True]))


# Every malformation this harness knows how to produce — a new kind of
# malformed input is a new function plus a new row here, never a branch
# inside the test itself.
_MUTATIONS: list[Callable[[random.Random, dict[str, Any]], str]] = [
    _mutate_wrong_type_scalar,
    _mutate_wrong_type_bool,
    _mutate_drop_top_level_key,
    _mutate_drop_subject_key,
    _mutate_add_unexpected_key,
    _mutate_deeply_nested_junk,
    _mutate_truncated_json,
    _mutate_empty_subjects,
    _mutate_null_document,
    _mutate_null_subject_entry,
    _mutate_subjects_not_a_list,
    _mutate_curriculum_not_an_object,
]


@pytest.mark.parametrize("case_index", range(NUM_FUZZ_CASES))
def test_load_curriculum_never_crashes_oddly_on_malformed_input(
    case_index: int, tmp_path: Path
) -> None:
    """For a deterministically-generated malformed `policies.json`,
    `load_curriculum` must either succeed with a valid policy list or
    raise one of `_EXPECTED_ERRORS` — never any other exception type.
    """
    rng = random.Random(FUZZ_SEED + case_index)
    base_curriculum = _random_curriculum(rng)
    mutation = rng.choice(_MUTATIONS)
    raw_text = mutation(rng, copy.deepcopy(base_curriculum))

    policies_path = tmp_path / "policies.json"
    policies_path.write_text(raw_text)

    try:
        policies, elective_minimum = load_curriculum(policies_path)
    except _EXPECTED_ERRORS:
        return  # (b): one of the documented/expected error types
    except Exception as exc:  # noqa: BLE001 — intentionally broad, see assertion below
        pytest.fail(
            f"case_index={case_index} mutation={mutation.__name__}: "
            f"load_curriculum raised an undocumented error type {type(exc).__name__}: {exc}\n"
            f"raw_text={raw_text!r}"
        )
    else:
        # (a): a successful parse must actually be a valid policy list, not
        # just "didn't crash".
        assert isinstance(policies, list), (
            f"case_index={case_index} mutation={mutation.__name__}: "
            f"load_curriculum returned {policies!r} instead of a list"
        )
        assert all(isinstance(p, SubjectPolicy) for p in policies), (
            f"case_index={case_index} mutation={mutation.__name__}: "
            f"load_curriculum's policy list contains a non-SubjectPolicy entry"
        )
        del elective_minimum  # present on every successful parse; not itself validated here
