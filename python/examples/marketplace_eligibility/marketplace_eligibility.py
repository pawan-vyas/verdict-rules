"""Marketplace eligibility, implemented with `verdict`.

See docs/samples/marketplace-eligibility/README.md for the design and
fixtures/marketplace_eligibility/README.md for the fixture contract.
"""

from __future__ import annotations

import json
from dataclasses import dataclass
from pathlib import Path
from typing import Callable, Generic, TypeVar

from verdict import AndRule, FunctionRule, Rule, RuleResult, RulesEngine

TOuter = TypeVar("TOuter")
TInner = TypeVar("TInner")

_HERE = Path(__file__).parent
# Fixture data lives at the repo root, shared by every language's own port of
# this example — see fixtures/marketplace_eligibility/README.md for the contract.
_FIXTURES = _HERE.parents[2] / "fixtures" / "marketplace_eligibility"

PRICE_FLOOR_CENTS = 100
ALLOWED_CATEGORIES = ("books", "electronics", "home")
PURCHASE_LIMIT_CENTS = 100_000
HIGH_VALUE_THRESHOLD_CENTS = 50_000
BLOCKED_COUNTRIES = ("ir", "nk")
NEW_SELLER_THRESHOLD_DAYS = 30


@dataclass(frozen=True)
class IdentityFlag:
    """The narrow context `is_verified_identity` is written against.

    Shares no fields with either `SellerListingContext` or
    `BuyerPurchaseContext`.
    """

    verified: bool


@dataclass(frozen=True)
class SellerListingContext:
    """What a listing-eligibility check reads."""

    seller_id: str
    seller_verified: bool
    listing_price_cents: int
    category: str


@dataclass(frozen=True)
class BuyerPurchaseContext:
    """What a purchase-eligibility check reads.

    Shares no fields with `SellerListingContext`.
    """

    buyer_id: str
    buyer_verified: bool
    purchase_amount_cents: int
    buyer_balance_cents: int


class ProjectingRule(Generic[TOuter, TInner]):
    """Adapts a ``Rule[TInner]`` to run inside a composite built on ``TOuter``.

    Not part of verdict itself. See
    docs/extending/reusing-a-rule-across-contexts/python.md.
    """

    def __init__(self, inner: Rule[TInner], project: Callable[[TOuter], TInner]) -> None:
        self.name = inner.name
        self.group = inner.group
        self._inner = inner
        self._project = project

    async def evaluate(self, context: TOuter) -> RuleResult:
        return await self._inner.evaluate(self._project(context))


async def _is_verified_identity(context: IdentityFlag) -> RuleResult:
    """Reused on both sides via `ProjectingRule`."""
    return RuleResult(rule_name="is_verified_identity", passed=context.verified)


def _seller_identity_rule() -> ProjectingRule[SellerListingContext, IdentityFlag]:
    inner = FunctionRule("is_verified_identity", _is_verified_identity)
    return ProjectingRule(inner, lambda ctx: IdentityFlag(verified=ctx.seller_verified))


def _buyer_identity_rule() -> ProjectingRule[BuyerPurchaseContext, IdentityFlag]:
    inner = FunctionRule("is_verified_identity", _is_verified_identity)
    return ProjectingRule(inner, lambda ctx: IdentityFlag(verified=ctx.buyer_verified))


async def _price_floor_met(context: SellerListingContext) -> RuleResult:
    return RuleResult(
        rule_name="price_floor_met",
        passed=context.listing_price_cents >= PRICE_FLOOR_CENTS,
        detail=f"{context.listing_price_cents} vs {PRICE_FLOOR_CENTS}",
    )


async def _category_allowed(context: SellerListingContext) -> RuleResult:
    return RuleResult(
        rule_name="category_allowed",
        passed=context.category in ALLOWED_CATEGORIES,
        detail=f"{context.category!r} not in {ALLOWED_CATEGORIES}" if context.category not in ALLOWED_CATEGORIES else "",
    )


async def _sufficient_balance(context: BuyerPurchaseContext) -> RuleResult:
    return RuleResult(
        rule_name="sufficient_balance",
        passed=context.buyer_balance_cents >= context.purchase_amount_cents,
        detail=f"balance {context.buyer_balance_cents} vs amount {context.purchase_amount_cents}",
    )


async def _purchase_limit_not_exceeded(context: BuyerPurchaseContext) -> RuleResult:
    return RuleResult(
        rule_name="purchase_limit_not_exceeded",
        passed=context.purchase_amount_cents <= PURCHASE_LIMIT_CENTS,
        detail=f"{context.purchase_amount_cents} vs limit {PURCHASE_LIMIT_CENTS}",
    )


def build_seller_check(seller_id: str) -> tuple[AndRule[SellerListingContext], Rule[SellerListingContext]]:
    """Build the typed listing-eligibility composite for one seller.

    Returns:
        A `(listing_eligible, seller_verified)` pair — the full
        composite, and the identity sub-rule alone.
    """
    seller_verified = _seller_identity_rule()
    listing_eligible: AndRule[SellerListingContext] = AndRule(
        "listing_eligible",
        [
            seller_verified,
            FunctionRule("price_floor_met", _price_floor_met),
            FunctionRule("category_allowed", _category_allowed),
        ],
    )
    return listing_eligible, seller_verified


def build_buyer_check(buyer_id: str) -> tuple[AndRule[BuyerPurchaseContext], Rule[BuyerPurchaseContext]]:
    """Build the typed purchase-eligibility composite for one buyer.

    Returns:
        A `(purchase_eligible, buyer_verified)` pair, the same shape as
        `build_seller_check`'s own return value.
    """
    buyer_verified = _buyer_identity_rule()
    purchase_eligible: AndRule[BuyerPurchaseContext] = AndRule(
        "purchase_eligible",
        [
            buyer_verified,
            FunctionRule("sufficient_balance", _sufficient_balance),
            FunctionRule("purchase_limit_not_exceeded", _purchase_limit_not_exceeded),
        ],
    )
    return purchase_eligible, buyer_verified


async def _high_value_flag(context: dict) -> RuleResult:
    return RuleResult(rule_name="high_value_flag", passed=context["amount_cents"] > HIGH_VALUE_THRESHOLD_CENTS)


async def _blocked_country_flag(context: dict) -> RuleResult:
    return RuleResult(rule_name="blocked_country_flag", passed=context["country"] in BLOCKED_COUNTRIES)


async def _new_seller_flag(context: dict) -> RuleResult:
    return RuleResult(rule_name="new_seller_flag", passed=context["seller_age_days"] < NEW_SELLER_THRESHOLD_DAYS)


def build_compliance_catalog() -> RulesEngine[dict]:
    """Build the dict-context compliance catalog.

    Unlike the seller/buyer sides, this is deliberately untyped. See
    docs/architecture/README.md#generic-context.
    """
    return RulesEngine(
        [
            FunctionRule("high_value_flag", _high_value_flag),
            FunctionRule("blocked_country_flag", _blocked_country_flag),
            FunctionRule("new_seller_flag", _new_seller_flag),
        ]
    )


def load_sellers(path: Path) -> dict[str, dict]:
    """Read the shared seller fixture data, keyed by seller id."""
    return json.loads(path.read_text())


def load_buyers(path: Path) -> dict[str, dict]:
    """Read the shared buyer fixture data, keyed by buyer id."""
    return json.loads(path.read_text())


def load_compliance_events(path: Path) -> dict[str, dict]:
    """Read the shared compliance-event fixture data, keyed by event id."""
    return json.loads(path.read_text())


def seller_context(seller_id: str, row: dict) -> SellerListingContext:
    return SellerListingContext(
        seller_id=seller_id,
        seller_verified=row["seller_verified"],
        listing_price_cents=row["listing_price_cents"],
        category=row["category"],
    )


def buyer_context(buyer_id: str, row: dict) -> BuyerPurchaseContext:
    return BuyerPurchaseContext(
        buyer_id=buyer_id,
        buyer_verified=row["buyer_verified"],
        purchase_amount_cents=row["purchase_amount_cents"],
        buyer_balance_cents=row["buyer_balance_cents"],
    )


async def _demo() -> None:
    sellers = load_sellers(_FIXTURES / "sellers.json")
    buyers = load_buyers(_FIXTURES / "buyers.json")
    events = load_compliance_events(_FIXTURES / "compliance_events.json")
    catalog = build_compliance_catalog()

    print("=== Marketplace Eligibility — Demo ===\n")
    print("--- Sellers ---")
    for seller_id, row in sellers.items():
        listing_eligible, _ = build_seller_check(seller_id)
        result = await listing_eligible.evaluate(seller_context(seller_id, row))
        print(f"{seller_id:14s}: {'ELIGIBLE' if result.passed else 'NOT ELIGIBLE'}")

    print("\n--- Buyers ---")
    for buyer_id, row in buyers.items():
        purchase_eligible, _ = build_buyer_check(buyer_id)
        result = await purchase_eligible.evaluate(buyer_context(buyer_id, row))
        print(f"{buyer_id:14s}: {'ELIGIBLE' if result.passed else 'NOT ELIGIBLE'}")

    print("\n--- Compliance events (one shared, untyped engine) ---")
    for event_id, row in events.items():
        flags = [name for name in catalog.rule_names if (await catalog.run_named(name, row)).passed]
        print(f"{event_id:22s}: {flags if flags else 'clean'}")


if __name__ == "__main__":
    import asyncio

    asyncio.run(_demo())
