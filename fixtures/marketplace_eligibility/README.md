<!-- Title: Marketplace Eligibility Fixture Contract -->
# Marketplace eligibility — shared fixture contract

> The single set of inputs and expected outcomes every language's own
> port of the generic-context showcase must reproduce. Unlike
> [`graduation_verdict/`](../graduation_verdict/README.md), which proves
> the engine survives a port at all, this fixture proves the **generic
> context** design survives a port: a typed `Rule<TContext>`, a
> dict-context `Rule` in the same domain, a `ProjectingRule` adapter
> reusing one rule across two differently-shaped contexts, and both
> `RulesEngine` forms, all in one place.

## The domain

A generalized two-sided marketplace, structurally similar to real
systems but naming none: sellers list items, buyers purchase them, and
a compliance team flags events for review. Two typed contexts that
share no fields, one dict-context catalog, one rule reused across both
typed contexts.

## Files

| File | Contents |
| :-- | :-- |
| `sellers.json` | Four sellers, each carrying the fields a listing-eligibility check reads and an `expected` block. |
| `buyers.json` | Four buyers, each carrying the fields a purchase-eligibility check reads and an `expected` block. |
| `compliance_events.json` | Four heterogeneous compliance events, each carrying an `expected` block of independent flags. |

## What each expectation proves

### `sellers.json` — typed `Rule<SellerListingContext>`, engine mode 1

| Field | Proves |
| :-- | :-- |
| `expected.listing_eligible` | The composite verdict: verified, price floor met, category allowed. |
| `expected.seller_verified` | The `ProjectingRule`-wrapped identity check specifically — isolates *why* a failure happened when more than one rule could explain it. |

**`seller_bob` is unverified but otherwise perfect** — proves the
identity check, not the price or category checks, is what fails him.
**`seller_carla` is verified with a valid category but an
under-floor price** — proves the reused identity rule passing doesn't
mask a different rule failing. **`seller_dan` is verified with a valid
price but a disallowed category** — the third, independent failure
mode, so all three seller-side rules are each proven capable of being
the one that fails.

### `buyers.json` — typed `Rule<BuyerPurchaseContext>`, engine mode 2

| Field | Proves |
| :-- | :-- |
| `expected.purchase_eligible` | The composite verdict: verified, sufficient balance, within the purchase limit. |
| `expected.buyer_verified` | The same `ProjectingRule`-wrapped identity check, reused against a context sharing no fields with `SellerListingContext` — the cross-context reuse this whole fixture exists to prove. |

**`buyer_frank` is unverified but otherwise perfect** — the identity
check fails him, mirroring `seller_bob`. **`buyer_gita` is verified
with an affordable purchase limit but an insufficient balance** —
proves the balance check independently. **`buyer_hank` is verified
with plenty of balance but a purchase over the limit** — the third
independent failure mode.

### `compliance_events.json` — dict-context `Rule`, engine mode 3 (untyped)

A **heterogeneous catalog**, not a composite: three independent checks
that read different, overlapping subsets of one event, run through a
plain, untyped `RulesEngine` because no single typed context would fit
all three naturally in a system where checks are added over time by a
compliance team, not by a type author. Each event's `expected` block
pins all three flags independently — every combination of "flag fires"
and "flag doesn't fire" appears at least once across the four events,
so a port can't accidentally wire one flag's condition to another's.

| Flag | Proves |
| :-- | :-- |
| `expected.high_value_flag` | `amount_cents` alone decides this, regardless of country or seller age. |
| `expected.blocked_country_flag` | `country` alone decides this, regardless of amount or seller age. |
| `expected.new_seller_flag` | `seller_age_days` alone decides this, regardless of amount or country. |

## The rule reused across contexts: `is_verified_identity`

Written once against a narrow `IdentityFlag { verified: bool }`
context, then wrapped in a `ProjectingRule` at each reuse site —
`SellerListingContext -> IdentityFlag` for sellers,
`BuyerPurchaseContext -> IdentityFlag` for buyers. The rule's own logic
never changes; only the projection differs. This is the fixture's
concrete instance of
[`../../docs/extending/reusing-a-rule-across-contexts/README.md`](../../docs/extending/reusing-a-rule-across-contexts/README.md).

## Thresholds (data, not code, in every language's own port)

| Threshold | Value |
| :-- | :-- |
| `price_floor_cents` | 100 |
| `allowed_categories` | `["books", "electronics", "home"]` |
| `purchase_limit_cents` | 100000 |
| `high_value_threshold_cents` | 50000 |
| `blocked_countries` | `["ir", "nk"]` |
| `new_seller_threshold_days` | 30 |

These are pinned here, not hardcoded in any language's implementation —
see `docs/samples/marketplace-eligibility/README.md` for where each
language's own factory function reads them from.

## What is deliberately *not* pinned

- **The `detail` string** on any `RuleResult` — idiomatic phrasing,
  worded naturally per language, the same convention
  [`graduation_verdict`](../graduation_verdict/README.md) already
  establishes.
- **Short-circuit counts.** Unlike `graduation_verdict`, this fixture's
  purpose is proving the generic-context design, not re-proving
  short-circuiting — that guarantee is already pinned exhaustively
  there. `AndRule<TContext>` here still short-circuits (it's the same
  type), but this fixture doesn't additionally assert on how many
  sub-rules ran.
- **An oracle/chaos suite.** `graduation_verdict`'s differential suite
  exists to satisfy Stage 4 ("Prove") of
  [`../../docs/maintenance/adding-a-language.md`](../../docs/maintenance/adding-a-language.md)
  for a *new* language; this fixture's job is narrower (proving
  generics), so a curated suite is sufficient.

## Regenerating

The expectations were **observed from a passing implementation**, never
hand-written. If a deliberate behavioural change makes them stale,
regenerate them from the reference implementation rather than editing
by hand — and treat any unexplained diff as a regression, not a fixture
problem.
