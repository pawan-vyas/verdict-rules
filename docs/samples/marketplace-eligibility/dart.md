<!-- Title: Sample — Marketplace Eligibility -->
# Sample: Marketplace Eligibility

> The problem, the design, and what a solution must demonstrate are
> language-agnostic and live in [`README.md`](README.md) — read that
> first. This page points at the Dart implementation of it, which is a
> full tested project rather than a markdown code block.

See [`../../../dart/examples/marketplace_eligibility/`](../../../dart/examples/marketplace_eligibility/README.md)
for how to run it, how to test it, and the full file listing.

## Where the design lives in the code

| Spec concept | `lib/src/marketplace_check.dart` |
| --- | --- |
| The narrow, reused context | `IdentityFlag` (`contexts.dart`) |
| The rule written once | `_isVerifiedIdentity` |
| The projecting adapter | `ProjectingRule<TOuter, TInner>` (`projecting_rule.dart`) |
| Each side's own projection | `_sellerIdentityRule()`, `_buyerIdentityRule()` |
| The typed seller/buyer composites | `buildSellerCheck()`, `buildBuyerCheck()` |
| The dict-context compliance catalog | `buildComplianceCatalog()` |

**Adding a fourth compliance flag** is a one-line addition to
`buildComplianceCatalog()`'s rule list — no change to
`SellerListingContext`, `BuyerPurchaseContext`, or any existing flag.
**Adding a third context that also needs identity verification** is one
new `ProjectingRule<TOuter, IdentityFlag>` construction, reusing the
same `_isVerifiedIdentity` function — no change to the rule itself.

## Related

- [`README.md`](README.md) — the language-agnostic spec this page
  implements.
- [`../../../dart/examples/marketplace_eligibility/`](../../../dart/examples/marketplace_eligibility/README.md) —
  the full project: implementation, tests, and a runnable demo.
- [`../../extending/reusing-a-rule-across-contexts/dart.md`](../../extending/reusing-a-rule-across-contexts/dart.md) —
  the standalone version of this project's `ProjectingRule` pattern.
- [`../graduation-requirement-verdict/dart.md`](../graduation-requirement-verdict/dart.md) —
  the flagship dict-context sample this one deliberately doesn't
  replace.
