"""Structural invariants over the result tree, proven on the same
generated cases `test_chaos.py` already uses for oracle comparison.

Where `test_chaos.py` only ever checks the final boolean verdict, this
file walks the full `(Rule, RuleResult)` tree together and asserts
short-circuit/no-short-circuit shape for every composite node, not
just the outcome at the root. See docs/testing.md.
"""

from __future__ import annotations

import random
from typing import Any, Callable

import pytest

from graduation_verdict import AtLeastNRule, build_graduation_check
from verdict import AndRule, FunctionRule, OrRule, Rule, RuleResult

from chaos_data import generate_case
from test_chaos import CHAOS_SEED, NUM_CASES

# A handful of the 500 chaos cases is plenty to prove purity isn't an
# accident of one particular generated shape — see docs/testing.md.
NUM_PURITY_CASES = 10


def _check_and_rule(rule: AndRule[Any], result: RuleResult) -> None:
    """AndRule stops at exactly the first failure, never before or after."""
    assert isinstance(result.data, list), f"{rule.name}: AndRule result.data must be a list"
    if result.passed:
        assert all(r.passed for r in result.data), (
            f"{rule.name}: AndRule passed but not every evaluated sub-rule passed"
        )
    else:
        assert all(r.passed for r in result.data[:-1]), (
            f"{rule.name}: AndRule failed but short-circuited before its real failure"
        )
        assert result.data[-1].passed is False, (
            f"{rule.name}: AndRule failed but its own last evaluated sub-rule passed"
        )
    # Not `strict=True`: a short-circuited AndRule's `result.data` is shorter
    # than `rule._rules` by design — only the sub-rules that actually ran
    # have a result to recurse into.
    for sub_rule, sub_result in zip(rule._rules, result.data):
        _check_rule_result_tree(sub_rule, sub_result)


def _check_or_rule(rule: OrRule[Any], result: RuleResult) -> None:
    """OrRule stops at exactly the first pass; an all-fail result ran every child."""
    assert isinstance(result.data, list), f"{rule.name}: OrRule result.data must be a list"
    if result.passed:
        assert all(not r.passed for r in result.data[:-1]), (
            f"{rule.name}: OrRule passed but short-circuited after its real pass"
        )
        assert result.data[-1].passed is True, (
            f"{rule.name}: OrRule passed but its own last evaluated sub-rule failed"
        )
    else:
        assert all(not r.passed for r in result.data), (
            f"{rule.name}: OrRule failed but one of its sub-results actually passed"
        )
    # Not `strict=True` — see the matching comment in `_check_and_rule`; an
    # OrRule that passes early also leaves `rule._rules` longer than
    # `result.data`.
    for sub_rule, sub_result in zip(rule._rules, result.data):
        _check_rule_result_tree(sub_rule, sub_result)


def _check_at_least_n_rule(rule: AtLeastNRule, result: RuleResult) -> None:
    """AtLeastNRule never short-circuits — every sub-rule always runs."""
    assert isinstance(result.data, list), f"{rule.name}: AtLeastNRule result.data must be a list"
    assert len(result.data) == len(rule._rules), (
        f"{rule.name}: AtLeastNRule ran {len(result.data)} of its {len(rule._rules)} "
        f"sub-rules — it must never short-circuit"
    )
    for sub_rule, sub_result in zip(rule._rules, result.data, strict=True):
        _check_rule_result_tree(sub_rule, sub_result)


def _check_function_rule(rule: FunctionRule[Any], result: RuleResult) -> None:
    """FunctionRule is a leaf — its own result.data is never a sub-result list."""
    assert not isinstance(result.data, list), (
        f"{rule.name}: FunctionRule (a leaf) unexpectedly produced list-shaped data"
    )


# One checker per concrete Rule shape this example ever builds — see
# rule_for_subject's own docstring in graduation_verdict.py for why these
# four are the complete set. Adding a fifth composite shape is a new
# function plus a new row here, never a branch inside a shared walker.
_RULE_RESULT_CHECKERS: dict[type, Callable[[Any, RuleResult], None]] = {
    AndRule: _check_and_rule,
    OrRule: _check_or_rule,
    AtLeastNRule: _check_at_least_n_rule,
    FunctionRule: _check_function_rule,
}


def _check_rule_result_tree(rule: Rule[Any], result: RuleResult) -> None:
    """Recursively verify the short-circuit shape of a `(rule, result)` pair.

    Args:
        rule: The real, typed `Rule` object that produced `result`.
        result: The `RuleResult` tree `rule.evaluate(...)` returned.

    Raises:
        AssertionError: `rule`'s concrete type has no registered checker,
            or an invariant for its type was violated.
    """
    checker = _RULE_RESULT_CHECKERS.get(type(rule))
    if checker is None:
        raise AssertionError(
            f"no structural checker registered for rule type {type(rule)!r} "
            f"(rule_name={rule.name!r}) — add one to _RULE_RESULT_CHECKERS"
        )
    checker(rule, result)


@pytest.mark.parametrize("case_index", range(NUM_CASES))
async def test_structural_invariants_hold(case_index: int) -> None:
    """One CHAOS_SEED-derived case's result tree matches its own rule
    tree's short-circuit shape, all the way down to the nested per-subject
    AndRule/OrRule composites — the same case generation test_chaos.py
    uses for oracle comparison, reused here for a structural check instead
    of a boolean one."""
    rng = random.Random(CHAOS_SEED + case_index)
    policies, context, elective_minimum = generate_case(rng)

    engine, graduates = build_graduation_check(policies, elective_minimum)
    result = await graduates.evaluate(context)
    _check_rule_result_tree(graduates, result)

    # A structural count, not a verdict: one result per registered rule.
    run_all = await engine.run_all(context)
    assert len(run_all.results) == len(policies), (
        f"case_index={case_index}: run_all reported {len(run_all.results)} results "
        f"for {len(policies)} registered subject rules"
    )


@pytest.mark.parametrize("case_index", range(NUM_PURITY_CASES))
async def test_purity_repeated_evaluation_is_identical(case_index: int) -> None:
    """Evaluating the exact same built rule tree against the exact same
    context twice must produce two result trees identical in every field,
    recursively — proves evaluate() carries no hidden state between calls.

    `RuleResult` is a frozen dataclass, so `==` already compares every
    field recursively (rule_name/passed/detail, and `data`'s own nested
    `RuleResult`s transitively) — no separate deep-equality helper needed.
    """
    rng = random.Random(CHAOS_SEED + case_index)
    policies, context, elective_minimum = generate_case(rng)
    _, graduates = build_graduation_check(policies, elective_minimum)

    first = await graduates.evaluate(context)
    second = await graduates.evaluate(context)

    assert first == second, (
        f"case_index={case_index}: the same rule tree evaluated against the same "
        f"context twice produced two different result trees"
    )
