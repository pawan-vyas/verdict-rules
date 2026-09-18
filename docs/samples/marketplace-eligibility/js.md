<!-- Title: Sample — Marketplace Eligibility (JS/TS) -->
# Sample: Marketplace Eligibility

> The problem, the design, and what a solution must demonstrate are
> language-agnostic and live in [`README.md`](README.md) — read that
> first. This page points at the JS/TS implementation of it, which is
> a full tested project rather than a markdown code block.

See [`../../../js/examples/marketplace_eligibility/`](../../../js/examples/marketplace_eligibility/README.md)
for how to run it, how to test it, and the full file listing.

## Where the design lives in the code

| Spec concept | `marketplace-eligibility.js` |
| --- | --- |
| The narrow, reused context | `IdentityFlag` (JSDoc typedef) |
| The rule written once | `isVerifiedIdentity` |
| The projecting adapter | `ProjectingRule` |
| Each side's own projection | `sellerIdentityRule()`, `buyerIdentityRule()` |
| The typed seller/buyer composites | `buildSellerCheck()`, `buildBuyerCheck()` |
| The dict-context compliance catalog | `buildComplianceCatalog()` |

**Adding a fourth compliance flag** is a one-line addition to
`buildComplianceCatalog()`'s rule list — no change to
`SellerListingContext`, `BuyerPurchaseContext`, or any existing flag.
**Adding a third context that also needs identity verification** is one
new `ProjectingRule` call, reusing the same `isVerifiedIdentity`
predicate — no change to the rule itself.

## Related

- [`README.md`](README.md) — the language-agnostic spec this page
  implements.
- [`../../../js/examples/marketplace_eligibility/`](../../../js/examples/marketplace_eligibility/README.md) —
  the full project: implementation, tests, and a runnable demo.
- [`../../extending/reusing-a-rule-across-contexts/js.md`](../../extending/reusing-a-rule-across-contexts/js.md) —
  the standalone version of this project's `ProjectingRule` pattern.
- [`../graduation-requirement-verdict/js.md`](../graduation-requirement-verdict/js.md) —
  the flagship dict-context sample this one deliberately doesn't
  replace.
