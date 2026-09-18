<!-- Title: Sample — Marketplace Eligibility -->
# Sample: Marketplace Eligibility

> The problem, the design, and what a solution must demonstrate are
> language-agnostic and live in [`README.md`](README.md) — read that
> first. This page points at the C# implementation of it, which is a
> full tested project rather than a markdown code block.

See [`../../../csharp/examples/MarketplaceEligibility/`](../../../csharp/examples/MarketplaceEligibility/README.md)
for how to run it, how to test it, and the full file listing.

## Where the design lives in the code

| Spec concept | `MarketplaceCheck.cs` |
| --- | --- |
| The narrow, reused context | `IdentityFlag` (`Contexts.cs`) |
| The rule written once | `IsVerifiedIdentity` |
| The projecting adapter | `ProjectingRule<TOuter, TInner>` (`ProjectingRule.cs`) |
| Each side's own projection | `SellerIdentityRule()`, `BuyerIdentityRule()` |
| The typed seller/buyer composites | `BuildSellerCheck()`, `BuildBuyerCheck()` |
| The dict-context compliance catalog | `BuildComplianceCatalog()` |

**Adding a fourth compliance flag** is a one-line addition to
`BuildComplianceCatalog()`'s rule list — no change to
`SellerListingContext`, `BuyerPurchaseContext`, or any existing flag.
**Adding a third context that also needs identity verification** is one
new `ProjectingRule<TOuter, IdentityFlag>` construction, reusing the
same `IsVerifiedIdentity` method group — no change to the rule itself.

## Related

- [`README.md`](README.md) — the language-agnostic spec this page
  implements.
- [`../../../csharp/examples/MarketplaceEligibility/`](../../../csharp/examples/MarketplaceEligibility/README.md) —
  the full project: implementation, tests, and a runnable demo.
- [`../../extending/reusing-a-rule-across-contexts/csharp.md`](../../extending/reusing-a-rule-across-contexts/csharp.md) —
  the standalone version of this project's `ProjectingRule` pattern.
- [`../graduation-requirement-verdict/csharp.md`](../graduation-requirement-verdict/csharp.md) —
  the flagship dict-context sample this one deliberately doesn't
  replace.
