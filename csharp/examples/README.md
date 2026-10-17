<!-- Title: Verdict Examples (C# SDK) -->
# Verdict — Examples: C# SDK

> Full, tested mini-projects behind verdict's shared fixtures — real code
> with its own test suite and docs, not just markdown snippets. Each one is
> this language's port of a scenario whose problem, design, and expected
> outcomes live in [`../../fixtures/README.md`](../../fixtures/README.md),
> data only; every language ships the same two under its own `examples/`.

| Project | What it demonstrates |
| --- | --- |
| [`GraduationVerdict/`](GraduationVerdict/README.md) | The full breadth of the engine at once — `FunctionRule`/`AndRule`/`OrRule`, a custom `IRule` shape, all three `RulesEngine` run modes, and data-driven rule construction — against a graduation-eligibility policy. Also doubles as an integration/e2e regression test for verdict itself; see the shared [fixture contract](../../fixtures/graduation_verdict/README.md) for how to extend the curriculum. |
| [`MarketplaceEligibility/`](MarketplaceEligibility/README.md) | `IRule<TContext>`'s generic-context design — two typed contexts sharing no fields, one rule reused across both via a projecting adapter, and a dict-context catalog coexisting in the same codebase. See the shared [fixture contract](../../fixtures/marketplace_eligibility/README.md) for the data every language's own port asserts against. |

Each project pairs with a `.Tests` project beside it; there is no solution file, so name a project explicitly: `dotnet test examples/GraduationVerdict.Tests/GraduationVerdict.Tests.csproj`.
