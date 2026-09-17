"""Tests for Rule's generic context parameter (TContext).

Python erases generics at runtime, so the header question this file
answers is: does anything observable actually change? The answer these
tests prove is "no, except what a caller opts into by writing a type
argument" — every existing structural rule keeps satisfying `Rule`
unconditionally, `isinstance` checks are unaffected, and a typed context
(a dataclass, not a dict) runs through every primitive (FunctionRule,
AndRule, OrRule, RulesEngine, all three run modes) exactly the way a
dict-context rule always has. This file is deliberately separate from
test_rule.py/test_engine.py: those prove the language-agnostic contract
1:1 against every other SDK; this one proves a Python-specific typing
addition with no cross-language counterpart to port.
"""

from __future__ import annotations

from dataclasses import dataclass
from typing import Any

import pytest

from verdict import AndRule, FunctionRule, OrRule, Rule, RuleResult, RulesEngine, TContext


@dataclass(frozen=True)
class OrderContext:
    """A typed context, standing in for a real consumer's own aggregate."""

    total: float
    is_member: bool


async def _order_total_met(context: OrderContext) -> RuleResult:
    return RuleResult(rule_name="order_total_met", passed=context.total >= 50.0)


async def _is_member(context: OrderContext) -> RuleResult:
    return RuleResult(rule_name="is_member", passed=context.is_member)


class TestGenericSubscription:
    """Every generic type in this package accepts a type argument and erases cleanly."""

    def test_rule_accepts_a_type_argument(self) -> None:
        # This is purely a typing-level assertion in a type checker; at
        # runtime it's just confirming the subscription doesn't raise.
        alias = Rule[dict[str, Any]]
        assert alias is not None

    def test_function_rule_accepts_a_type_argument(self) -> None:
        alias = FunctionRule[OrderContext]
        assert alias is not None

    def test_and_rule_accepts_a_type_argument(self) -> None:
        assert AndRule[OrderContext] is not None

    def test_or_rule_accepts_a_type_argument(self) -> None:
        assert OrRule[OrderContext] is not None

    def test_rules_engine_accepts_a_type_argument(self) -> None:
        assert RulesEngine[OrderContext] is not None

    def test_type_context_is_exported(self) -> None:
        # A consumer writing their own generic Rule-adjacent type reaches
        # for this rather than declaring a fresh, unrelated TypeVar.
        assert TContext is not None


class TestErasureDoesNotChangeBehavior:
    """A subscripted or unsubscripted rule behaves identically at runtime."""

    async def test_isinstance_is_unaffected_by_a_type_argument(self) -> None:
        rule: Rule[OrderContext] = FunctionRule("r", _order_total_met)
        # isinstance only ever checks name/group/evaluate presence — the
        # `[OrderContext]` annotation above is erased before this line runs.
        assert isinstance(rule, Rule)

    async def test_an_untyped_predicate_still_works_exactly_as_before(self) -> None:
        # No type argument anywhere — the pre-generics calling convention,
        # which must keep working unconditionally.
        async def predicate(context: dict) -> RuleResult:
            return RuleResult(rule_name="r", passed=bool(context.get("ok")))

        rule = FunctionRule("r", predicate)
        result = await rule.evaluate({"ok": True})
        assert result.passed is True


class TestTypedContextEndToEnd:
    """A non-dict, dataclass context runs through every primitive identically to dict-context."""

    async def test_function_rule_evaluates_a_typed_context(self) -> None:
        rule: FunctionRule[OrderContext] = FunctionRule("order_total_met", _order_total_met)
        result = await rule.evaluate(OrderContext(total=75.0, is_member=False))
        assert result.passed is True

    async def test_and_rule_composes_typed_sub_rules(self) -> None:
        rule: AndRule[OrderContext] = AndRule(
            "eligible",
            [FunctionRule("order_total_met", _order_total_met), FunctionRule("is_member", _is_member)],
        )
        result = await rule.evaluate(OrderContext(total=75.0, is_member=True))
        assert result.passed is True

    async def test_and_rule_short_circuits_on_a_typed_context_too(self) -> None:
        calls: list[str] = []

        async def tracked(context: OrderContext) -> RuleResult:
            calls.append("tracked")
            return RuleResult(rule_name="tracked", passed=True)

        rule: AndRule[OrderContext] = AndRule(
            "eligible", [FunctionRule("order_total_met", _order_total_met), FunctionRule("tracked", tracked)]
        )
        await rule.evaluate(OrderContext(total=10.0, is_member=False))  # fails order_total_met first
        assert calls == []  # 'tracked' never reached — short-circuiting survives typed contexts

    async def test_or_rule_composes_typed_sub_rules(self) -> None:
        rule: OrRule[OrderContext] = OrRule(
            "eligible",
            [FunctionRule("order_total_met", _order_total_met), FunctionRule("is_member", _is_member)],
        )
        result = await rule.evaluate(OrderContext(total=10.0, is_member=True))
        assert result.passed is True

    async def test_rules_engine_run_all_with_a_typed_context(self) -> None:
        engine: RulesEngine[OrderContext] = RulesEngine(
            [FunctionRule("order_total_met", _order_total_met), FunctionRule("is_member", _is_member)]
        )
        result = await engine.run_all(OrderContext(total=75.0, is_member=True))
        assert result.passed is True
        assert len(result.results) == 2

    async def test_rules_engine_run_named_with_a_typed_context(self) -> None:
        engine: RulesEngine[OrderContext] = RulesEngine([FunctionRule("order_total_met", _order_total_met)])
        result = await engine.run_named("order_total_met", OrderContext(total=75.0, is_member=False))
        assert result.passed is True

    async def test_rules_engine_run_group_with_a_typed_context(self) -> None:
        engine: RulesEngine[OrderContext] = RulesEngine(
            [FunctionRule("order_total_met", _order_total_met, group="checkout")]
        )
        result = await engine.run_group("checkout", OrderContext(total=75.0, is_member=False))
        assert result.passed is True

    async def test_rules_engine_try_forms_with_a_typed_context(self) -> None:
        engine: RulesEngine[OrderContext] = RulesEngine([FunctionRule("order_total_met", _order_total_met)])
        present = await engine.try_run_named("order_total_met", OrderContext(total=75.0, is_member=False))
        absent = await engine.try_run_named("nope", OrderContext(total=75.0, is_member=False))
        assert present is not None and present.passed is True
        assert absent is None


class TestTypingBoundaryIsAdvisoryOnly:
    """The gotcha every consumer should understand before relying on `TContext`.

    Python's generics carry zero runtime enforcement. A type checker (mypy,
    pyright) is what actually catches a mismatched context at the call
    site; without one, a wrongly-shaped context still runs and fails in
    whatever way the predicate's own body fails, not with a type-system
    error. This is not a bug to fix — it's the same boundary every
    Python generic has — but it's worth a test making the shape of the
    failure explicit rather than leaving it to be discovered by surprise.
    """

    async def test_a_mismatched_context_fails_as_an_attribute_error_not_a_type_error(self) -> None:
        rule: FunctionRule[OrderContext] = FunctionRule("order_total_met", _order_total_met)
        # Handing this an unrelated object compiles fine under `from __future__
        # import annotations` and any type checker not actually run — the
        # failure only surfaces here, at the attribute access inside the
        # predicate, exactly as it would have before generics existed.
        with pytest.raises(AttributeError):
            await rule.evaluate(object())  # type: ignore[arg-type]
