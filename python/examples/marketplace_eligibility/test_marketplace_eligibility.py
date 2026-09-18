"""Tests for the marketplace-eligibility example.

This project exercises Rule[TContext] end to end: two typed contexts
sharing no fields, a rule reused across both via ProjectingRule, and a
dict-context catalog coexisting in the same codebase. See
docs/samples/marketplace-eligibility/README.md for the design and
fixtures/marketplace_eligibility/README.md for the shared contract this
suite reproduces.
"""

from __future__ import annotations

import json
from pathlib import Path

import pytest

from marketplace_eligibility import (
    build_buyer_check,
    build_compliance_catalog,
    build_seller_check,
    buyer_context,
    load_buyers,
    load_compliance_events,
    load_sellers,
    seller_context,
)

_HERE = Path(__file__).parent
_FIXTURES = _HERE.parents[2] / "fixtures" / "marketplace_eligibility"
_SELLERS = load_sellers(_FIXTURES / "sellers.json")
_BUYERS = load_buyers(_FIXTURES / "buyers.json")
_EVENTS = load_compliance_events(_FIXTURES / "compliance_events.json")


class TestSellerListingEligibility:
    """Every expectation in sellers.json, asserted -- the typed Rule[SellerListingContext] side."""

    @pytest.mark.parametrize("seller_id", list(_SELLERS.keys()))
    async def test_seller_verified_matches(self, seller_id: str) -> None:
        row = _SELLERS[seller_id]
        _, seller_verified = build_seller_check(seller_id)
        result = await seller_verified.evaluate(seller_context(seller_id, row))
        assert result.passed == row["expected"]["seller_verified"], seller_id

    @pytest.mark.parametrize("seller_id", list(_SELLERS.keys()))
    async def test_listing_eligible_matches(self, seller_id: str) -> None:
        row = _SELLERS[seller_id]
        listing_eligible, _ = build_seller_check(seller_id)
        result = await listing_eligible.evaluate(seller_context(seller_id, row))
        assert result.passed == row["expected"]["listing_eligible"], seller_id


class TestBuyerPurchaseEligibility:
    """Every expectation in buyers.json, asserted -- the typed Rule[BuyerPurchaseContext] side."""

    @pytest.mark.parametrize("buyer_id", list(_BUYERS.keys()))
    async def test_buyer_verified_matches(self, buyer_id: str) -> None:
        row = _BUYERS[buyer_id]
        _, buyer_verified = build_buyer_check(buyer_id)
        result = await buyer_verified.evaluate(buyer_context(buyer_id, row))
        assert result.passed == row["expected"]["buyer_verified"], buyer_id

    @pytest.mark.parametrize("buyer_id", list(_BUYERS.keys()))
    async def test_purchase_eligible_matches(self, buyer_id: str) -> None:
        row = _BUYERS[buyer_id]
        purchase_eligible, _ = build_buyer_check(buyer_id)
        result = await purchase_eligible.evaluate(buyer_context(buyer_id, row))
        assert result.passed == row["expected"]["purchase_eligible"], buyer_id


class TestProjectingRuleReusesOneRuleAcrossContexts:
    """The identity check is one instance, projected two ways -- not two copies."""

    async def test_the_same_inner_rule_is_shared_by_both_sides(self) -> None:
        _, seller_verified = build_seller_check("seller_alice")
        _, buyer_verified = build_buyer_check("buyer_erin")
        # Both projections wrap a distinct FunctionRule instance (each side
        # builds its own), but the *predicate* they wrap is the same function
        # object -- the logic itself is defined exactly once.
        assert seller_verified._inner._predicate is buyer_verified._inner._predicate  # type: ignore[attr-defined]

    async def test_the_projection_reads_a_different_field_per_side(self) -> None:
        # seller_verified reads seller_verified; buyer_verified reads
        # buyer_verified -- proving the adapter, not the rule, is what
        # changes between reuse sites.
        _, seller_verified = build_seller_check("seller_bob")  # seller_verified=False
        row = _SELLERS["seller_bob"]
        result = await seller_verified.evaluate(seller_context("seller_bob", row))
        assert result.passed is False


class TestComplianceCatalogIsDictContextAndHeterogeneous:
    """Every expectation in compliance_events.json, asserted -- the untyped RulesEngine side."""

    @pytest.mark.parametrize("event_id", list(_EVENTS.keys()))
    async def test_high_value_flag_matches(self, event_id: str) -> None:
        row = _EVENTS[event_id]
        catalog = build_compliance_catalog()
        result = await catalog.run_named("high_value_flag", row)
        assert result.passed == row["expected"]["high_value_flag"], event_id

    @pytest.mark.parametrize("event_id", list(_EVENTS.keys()))
    async def test_blocked_country_flag_matches(self, event_id: str) -> None:
        row = _EVENTS[event_id]
        catalog = build_compliance_catalog()
        result = await catalog.run_named("blocked_country_flag", row)
        assert result.passed == row["expected"]["blocked_country_flag"], event_id

    @pytest.mark.parametrize("event_id", list(_EVENTS.keys()))
    async def test_new_seller_flag_matches(self, event_id: str) -> None:
        row = _EVENTS[event_id]
        catalog = build_compliance_catalog()
        result = await catalog.run_named("new_seller_flag", row)
        assert result.passed == row["expected"]["new_seller_flag"], event_id

    async def test_flags_are_independent_not_a_composite_verdict(self) -> None:
        # event_high_value trips exactly one flag -- proving the three
        # checks are looked up independently, not folded into one AND/OR.
        row = _EVENTS["event_high_value"]
        catalog = build_compliance_catalog()
        fired = [name for name in catalog.rule_names if (await catalog.run_named(name, row)).passed]
        assert fired == ["high_value_flag"]

    async def test_a_new_flag_is_additive_not_a_shared_context_change(self) -> None:
        # Registering a fourth check needs no change to SellerListingContext,
        # BuyerPurchaseContext, or any existing flag.
        from verdict import FunctionRule, RuleResult, RulesEngine

        async def _weekend_flag(context: dict) -> RuleResult:
            return RuleResult(rule_name="weekend_flag", passed=context.get("is_weekend", False))

        catalog = build_compliance_catalog()
        extended = RulesEngine(
            [*(catalog._rules), FunctionRule("weekend_flag", _weekend_flag)]  # type: ignore[attr-defined]
        )
        result = await extended.run_named("weekend_flag", {"is_weekend": True})
        assert result.passed is True


def test_data_files_actually_loaded() -> None:
    """Sanity check the fixture loaders against the real files, not a stub."""
    raw_sellers = json.loads((_FIXTURES / "sellers.json").read_text())
    raw_buyers = json.loads((_FIXTURES / "buyers.json").read_text())
    raw_events = json.loads((_FIXTURES / "compliance_events.json").read_text())
    assert set(_SELLERS.keys()) == set(raw_sellers.keys())
    assert set(_BUYERS.keys()) == set(raw_buyers.keys())
    assert set(_EVENTS.keys()) == set(raw_events.keys())
