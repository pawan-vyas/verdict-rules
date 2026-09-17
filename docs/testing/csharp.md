<!-- Title: Verdict Testing Guide (C#) -->
# Testing verdict: C# SDK

> The contracts and checklist are language-agnostic and live in
> [`README.md`](README.md) — read that first. This page is C#'s
> concrete realization: current state, file layout, and which test
> proves which contract.

## Current state, as of this writing

```text
$ cd csharp/
$ dotnet test tests/VerdictRules.Tests/VerdictRules.Tests.csproj
Passed!  - Failed: 0, Passed: 48, Skipped: 0, Total: 48, Duration: 7 ms
```

Three files, not one. `RuleTests.cs` and `EngineTests.cs` are a strict,
1:1 port of Python's own `test_rule.py`/`test_engine.py` — same test
classes, same tests, same assertions, in C# idiom — and are the two files
to check when auditing this package against Python's own suite.
`CSharpIdiomTests.cs` holds exactly the coverage with no Python
counterpart on purpose (`CancellationToken` propagation, structural typing
for delegates only, an explicit `IRule` implementation) and is deliberately
not part of that mirror.

This SDK also has the second testing layer — a full, tested example
project checked against the shared graduation fixture (see
[`../../fixtures/graduation_verdict/README.md`](../../fixtures/graduation_verdict/README.md)),
at [`../../csharp/examples/GraduationVerdict/`](../../csharp/examples/GraduationVerdict/README.md).
That satisfies Stage 4 ("Prove") of
[`../maintenance/adding-a-language.md`](../maintenance/adding-a-language.md),
including an oracle/differential suite in this language's own idiom
(500 generated cases, checked against an independent, verdict-rules-free
re-implementation — see that project's own
[`docs/testing.md`](../../csharp/examples/GraduationVerdict/docs/testing.md)).

## Test layout

```mermaid
graph LR
    RuleSrc["📄 FunctionRule.cs / AndRule.cs / OrRule.cs"]
    EngineSrc["📄 RulesEngine.cs"]
    ResultSrc["📄 RuleResult.cs / RunResult.cs"]
    RuleTest[["🧪 RuleTests.cs"]]
    EngineTest[["🧪 EngineTests.cs"]]
    IdiomTest[["🧪 CSharpIdiomTests.cs"]]

    %% Link 0: RuleSrc -> RuleTest
    RuleSrc -->|"[1]<br/>FunctionRule / AndRule / OrRule"| RuleTest
    %% Link 1: EngineSrc -> EngineTest
    EngineSrc -->|"[2]<br/>RunAllAsync / RunNamedAsync / RunGroupAsync"| EngineTest
    %% Link 2: ResultSrc -> RuleTest
    ResultSrc -.->|"[3]<br/>exercised indirectly,<br/>no dedicated test class"| RuleTest
    %% Link 3: RuleSrc -> IdiomTest
    RuleSrc -.->|"[4]<br/>CancellationToken, structural typing,<br/>not a portable contract"| IdiomTest
    %% Link 4: EngineSrc -> IdiomTest
    EngineSrc -.->|"[5]<br/>CancellationToken,<br/>not a portable contract"| IdiomTest

    style RuleSrc fill:#D0D0D0,stroke:#999999,stroke-width:2px,color:#000
    style EngineSrc fill:#D0D0D0,stroke:#999999,stroke-width:2px,color:#000
    style ResultSrc fill:#D0D0D0,stroke:#999999,stroke-width:2px,color:#000
    style RuleTest fill:#FFB84D,stroke:#E69500,stroke-width:2px,color:#000
    style EngineTest fill:#FFB84D,stroke:#E69500,stroke-width:2px,color:#000
    style IdiomTest fill:#B47EFF,stroke:#9654E8,stroke-width:2px,color:#000

    %% Link Index:
    %% 0: the three rule types are covered directly, one test class each
    %% 1: the engine's run modes are covered directly
    %% 2: RuleResult/RunResult are plain immutable classes, exercised as a side effect of the above -- no behavior of their own to test in isolation
    %% 3-4: CancellationToken propagation and structural typing for delegates only are C#-specific, not part of the Python-parity mirror
    linkStyle 0 stroke:#FFCB7A,stroke-width:2px
    linkStyle 1 stroke:#FFCB7A,stroke-width:2px
    linkStyle 2 stroke:#E0E0E0,stroke-width:2px,stroke-dasharray:5 5
    linkStyle 3 stroke:#D0AFFF,stroke-width:2px,stroke-dasharray:5 5
    linkStyle 4 stroke:#D0AFFF,stroke-width:2px,stroke-dasharray:5 5
```

## Which test proves which contract

| Contract ([`README.md`](README.md)) | Proven by |
| --- | --- |
| Short-circuiting, both directions | `AndRuleTests.ShortCircuitsAfterFirstFailure`, `OrRuleTests.ShortCircuitsAfterFirstPass` (`RuleTests.cs`) |
| Vacuous-truth polarity, both directions | `AndRuleTests.EmptyRuleListVacuouslyPasses`, `OrRuleTests.EmptyRuleListVacuouslyFails` (`RuleTests.cs`) |
| Absence throws (strict lookup) | `RunNamedTests.UnknownNameRaisesKeyNotFound`, `RunGroupTests.UnknownGroupRaises` (`EngineTests.cs`) |
| Absence returns `null` (try-prefixed lookup) | `TryLookupTests` (`EngineTests.cs`) |
| Fallback matrix — the present-failing row specifically | `TryLookupTests.FallbackMatrix` (`[Theory]`, one case per present/absent × pass/fail combination) |
| `RunAllAsync`/`RunGroupAsync` never short-circuit | `RunAllTests.DoesNotShortCircuitUnlikeAndRule` (`EngineTests.cs`) |
| No flattening of a composite's own sub-results | `AndRuleTests.DataCarriesSubResultsUpToFailure` (`RuleTests.cs`) |
| Duplicate name, last one wins | `ConstructionTests.DuplicateNamesLastOneWinsInByNameLookup` (`EngineTests.cs`) |
| A predicate's exception is never caught | `EngineExceptionPropagationTests.RunAllDoesNotCatchAPredicatesException`, `RunGroupDoesNotCatchAPredicatesException` (`EngineTests.cs`); `RuleExceptionPropagationTests.AndRuleDoesNotCatchASubRulesException`, `OrRuleDoesNotCatchASubRulesException` (`RuleTests.cs`) |

Not part of this table on purpose — `CSharpIdiomTests.cs` proves
`CancellationToken` propagation (checked between sub-rules, not just once
at entry), C#'s structural typing for delegates only, and an explicit
`IRule` implementation composing like any other. None of these are
universal contracts; none get ported to another language's own suite.

## Running tests

```bash
cd csharp
dotnet build src/VerdictRules/VerdictRules.csproj -warnaserror
dotnet test tests/VerdictRules.Tests/VerdictRules.Tests.csproj
```

There is no solution file, so a bare `dotnet build`/`dotnet test` from
`csharp/` fails with "Specify a project or solution file" — always name
the project explicitly. No external project context or environment
variables are needed — this package's test suite is as standalone as
the package itself.

## Related

- [`README.md`](README.md) — the language-agnostic contracts this page
  proves concretely.
