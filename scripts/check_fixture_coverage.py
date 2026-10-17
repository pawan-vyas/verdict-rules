#!/usr/bin/env python3
"""Assert fixtures/graduation_verdict/ actually exercises everything its own
README claims to prove, rather than trusting that claim unverified.

Scoped to graduation_verdict specifically, not every fixture under
fixtures/: this is the one fixture built around outcome/run-mode/error-kind
coverage (see its own README's "What each expectation proves" table).
fixtures/marketplace_eligibility/ proves a different contract entirely --
the generic-context design (ProjectingRule reuse across two typed contexts)
-- with no equivalent notion of "outcome kind" or "run mode" to count, so
nothing here generalizes to it without inventing a requirement that
fixture's own README never claims to satisfy.

Each check below cites the exact README section its requirement comes
from, so a future edit to the fixture's own contract has an obvious
matching edit here, not a check drifting out of sync with what the fixture
documents about itself.
"""

from __future__ import annotations

import json
import sys
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parent.parent
FIXTURE_DIR = REPO_ROOT / "fixtures/graduation_verdict"

# "Rule names are part of the contract": four top-level composite rules,
# so a student's rules_evaluated (short-circuiting a sequential AndRule of
# these four) can range from 1 to this count.
TOP_LEVEL_RULE_COUNT = 4


def load(name: str):
    with (FIXTURE_DIR / name).open(encoding="utf-8") as f:
        return json.load(f)


def main() -> int:
    students = load("students.json")
    edge_cases = load("edge_cases.json")
    errors: list[str] = []

    all_expected = [s["expected"] for s in students.values()] + [
        c["expected"] for c in edge_cases.values()
    ]

    # "## What each expectation proves" -- passed: the verdict itself.
    outcomes = {e["passed"] for e in all_expected}
    if outcomes != {True, False}:
        errors.append(
            f"Both a passing and a failing student/edge-case are required (outcome kinds "
            f"'## What each expectation proves', 'passed'); found only {sorted(outcomes)}."
        )

    # "bob and gita both fail ... evaluated 1 ... evaluated 4" -- short-circuiting
    # is real only if every possible stopping point is actually observed, not
    # just two of them.
    evaluated_counts = {e["rules_evaluated"] for e in all_expected if "rules_evaluated" in e}
    required_counts = set(range(1, TOP_LEVEL_RULE_COUNT + 1))
    missing_counts = required_counts - evaluated_counts
    if missing_counts:
        errors.append(
            f"Every short-circuit stopping point from 1 to {TOP_LEVEL_RULE_COUNT} must be "
            f"observed somewhere in rules_evaluated ('### The contrasts worth understanding', "
            f"bob vs. gita); missing {sorted(missing_counts)}."
        )

    # "RuleResult.data is never flattened -- the path is preserved through
    # nesting" -- only provable if chains of more than one depth actually
    # appear; a fixture with only depth-0/1 chains would pass a parser that
    # silently flattened everything past the first level.
    chain_depths = {
        len(e["failing_chain"]) for e in all_expected if e.get("failing_chain")
    }
    required_depths = {1, 2, 3}
    missing_depths = required_depths - chain_depths
    if missing_depths:
        errors.append(
            f"failing_chain needs at least one case at each nesting depth 1-3 to prove nested "
            f"composite data isn't flattened ('failing_chain' row in '## What each expectation "
            f"proves'); missing depth(s) {sorted(missing_depths)}."
        )

    # "farhan and gita fail while run_all passes" -- the two run modes answer
    # different questions only if at least one case actually shows them
    # disagreeing in each direction (composite fails/run_all passes, and the
    # reverse -- harish is the fixture's own example of the second).
    divergences = {
        (e["passed"], e["run_all"]["passed"])
        for e in all_expected
        if "run_all" in e and e["run_all"]["passed"] != e["passed"]
    }
    directions = {d[0] for d in divergences}  # the composite's own outcome on each divergent case
    if directions != {True, False}:
        errors.append(
            "run_all must diverge from the composite verdict in both directions -- composite "
            "passes while run_all fails, and composite fails while run_all passes ('farhan and "
            f"gita fail while run_all passes' / 'harish graduates but his elective group fails', "
            f"'### The contrasts worth understanding'); found divergence direction(s) {directions or 'none'}."
        )

    # "harish graduates but his elective group fails" -- a group verdict is
    # provably independent of the composite's own verdict only if at least
    # one case shows them disagreeing.
    group_divergence = any(
        group["passed"] != e["passed"]
        for e in all_expected
        if "groups" in e
        for group in e["groups"].values()
    )
    if not group_divergence:
        errors.append(
            "At least one student needs a group verdict that disagrees with the composite's own "
            "verdict, to prove group dispatch is computed independently ('harish graduates but "
            "his elective group fails', '### The contrasts worth understanding'); none found."
        )

    # "## Emptiness is not absence" -- AndRule([]) passes vacuously.
    vacuous_pass = any(
        c["curriculum"]["subjects"] == [] and c["expected"]["passed"] is True
        for c in edge_cases.values()
    )
    if not vacuous_pass:
        errors.append(
            "edge_cases.json needs a case with an empty curriculum that still passes, proving "
            "AndRule([]) folds to its identity ('## Emptiness is not absence'); none found."
        )

    # "Unknown lookups signal absence -- two ways, both required" -- all four
    # combinations (strict raises / non-strict returns null, for both a rule
    # and a group) must each appear at least once.
    required_lookup_flags = {
        "run_group_raises",
        "try_run_group_returns_null",
        "run_named_raises",
        "try_run_named_returns_null",
    }
    seen_flags = {
        flag
        for e in all_expected
        if "lookups" in e
        for lookup in e["lookups"].values()
        for flag, value in lookup.items()
        if flag in required_lookup_flags and value is True
    }
    missing_flags = required_lookup_flags - seen_flags
    if missing_flags:
        errors.append(
            "All four absence combinations (strict raises / non-strict returns null, for a rule "
            "and for a group) must each appear at least once ('Unknown lookups signal absence -- "
            f"two ways, both required', '## Emptiness is not absence'); missing {sorted(missing_flags)}."
        )

    if errors:
        for e in errors:
            print(f"::error::{e}")
        print(f"\n{len(errors)} coverage gap(s) in fixtures/graduation_verdict/.", file=sys.stderr)
        return 1

    print(
        f"fixtures/graduation_verdict/ covers every outcome, run-mode, and absence combination "
        f"its own README documents, across {len(students)} student(s) and {len(edge_cases)} edge case(s)."
    )
    return 0


if __name__ == "__main__":
    sys.exit(main())
