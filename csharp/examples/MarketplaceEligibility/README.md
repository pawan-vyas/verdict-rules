<!-- Title: Marketplace Eligibility Example -->
# Marketplace Eligibility

> A two-sided marketplace eligibility check, built as real, tested code
> — the flagship example exercising `IRule<TContext>` end to end: two
> typed contexts sharing no fields, one rule reused across both via a
> `ProjectingRule` adapter, and a dict-context catalog coexisting in
> the same codebase. See
> [`docs/samples/marketplace-eligibility/`](../../../docs/samples/marketplace-eligibility/README.md)
> for the original framing question this project answers, the design,
> and what a solution must demonstrate.

## Run it

```bash
# From csharp/
dotnet run --project examples/MarketplaceEligibility/MarketplaceEligibility.csproj
```

Prints every seller's and buyer's eligibility, then every compliance
event's independently-fired flags.

## Test it

```bash
# From csharp/
dotnet test examples/MarketplaceEligibility.Tests/MarketplaceEligibility.Tests.csproj
```

There is no solution file, so a bare `dotnet build`/`dotnet test` from
`csharp/` fails with "Specify a project or solution file" -- always
name the project explicitly.

## Read more

- [`../../../docs/samples/marketplace-eligibility/`](../../../docs/samples/marketplace-eligibility/README.md) —
  the naive way this problem is usually approached, why it breaks down,
  and both diagrams behind the design actually used here.
- [`../../../fixtures/marketplace_eligibility/README.md`](../../../fixtures/marketplace_eligibility/README.md) —
  the shared cross-language fixture contract.
- [`../../../docs/extending/reusing-a-rule-across-contexts/README.md`](../../../docs/extending/reusing-a-rule-across-contexts/README.md) —
  the standalone `ProjectingRule` pattern this project's identity check
  is a full-scale instance of.

## Files

| File | What it is |
| --- | --- |
| [`../../../fixtures/marketplace_eligibility/sellers.json`](../../../fixtures/marketplace_eligibility/sellers.json) | Four sellers, each with an expected outcome. **Shared across every language.** |
| [`../../../fixtures/marketplace_eligibility/buyers.json`](../../../fixtures/marketplace_eligibility/buyers.json) | Four buyers, each with an expected outcome. **Shared.** |
| [`../../../fixtures/marketplace_eligibility/compliance_events.json`](../../../fixtures/marketplace_eligibility/compliance_events.json) | Four heterogeneous compliance events, each with three independent expected flags. **Shared.** |
| `Contexts.cs` | The typed `SellerListingContext`/`BuyerPurchaseContext`/`IdentityFlag` records. |
| `ProjectingRule.cs` | The generic context-adapter class. |
| `MarketplaceCheck.cs` | The real implementation -- rule factories, JSON loading. |
| `Program.cs` | The runnable demo. |
| `../MarketplaceEligibility.Tests/MarketplaceCheckTests.cs` | The full test suite, loading all three JSON files and asserting generically. |
