<!-- Title: Sample — Marketplace Eligibility -->
# Sample: Marketplace Eligibility

> The problem, the design, and what a solution must demonstrate are
> language-agnostic and live in [`README.md`](README.md) — read that
> first. This page points at the Python implementation of it, which is
> a full tested project rather than a markdown code block.

See [`../../../python/examples/marketplace_eligibility/`](../../../python/examples/marketplace_eligibility/README.md)
for how to run it, how to test it, and the full file listing.

## Where the design lives in the code

| Spec concept | `marketplace_eligibility.py` |
| --- | --- |
| The narrow, reused context | `IdentityFlag` |
| The rule written once | `_is_verified_identity` |
| The projecting adapter | `ProjectingRule` |
| Each side's own projection | `_seller_identity_rule()`, `_buyer_identity_rule()` |
| The typed seller/buyer composites | `build_seller_check()`, `build_buyer_check()` |
| The dict-context compliance catalog | `build_compliance_catalog()` |

**Adding a fourth compliance flag** is a one-line addition to
`build_compliance_catalog()`'s rule list — no change to
`SellerListingContext`, `BuyerPurchaseContext`, or any existing flag.
**Adding a third context that also needs identity verification** is one
new `ProjectingRule` call, reusing the same `_is_verified_identity`
instance — no change to the rule itself.

## Related

- [`README.md`](README.md) — the language-agnostic spec this page
  implements.
- [`../../../python/examples/marketplace_eligibility/`](../../../python/examples/marketplace_eligibility/README.md) —
  the full project: implementation, tests, and a runnable demo.
- [`../../extending/reusing-a-rule-across-contexts/python.md`](../../extending/reusing-a-rule-across-contexts/python.md) —
  the standalone version of this project's `ProjectingRule` pattern.
- [`../graduation-requirement-verdict/python.md`](../graduation-requirement-verdict/python.md) —
  the flagship dict-context sample this one deliberately doesn't
  replace.
