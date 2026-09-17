<!-- Title: Graduation Verdict Example -->
# Graduation Requirement Verdict

> A college graduation-eligibility check, built as real, tested code
> instead of a doc snippet -- the flagship example exercising the full
> breadth of verdict-rules at once: heterogeneous `IRule` shapes built
> from external policy data, a custom `IRule` type, and all three
> `RulesEngine` run modes serving three different real callers. See
> [`docs/samples/graduation-requirement-verdict/`](../../../docs/samples/graduation-requirement-verdict/README.md)
> for the original framing question this project answers, the design,
> and what a solution must demonstrate.

## Run it

```bash
# From csharp/
dotnet run --project examples/GraduationVerdict/GraduationVerdict.csproj
```

Prints a detailed lookup for one student, then a batch verdict for all
8 -- one engine and one `graduates` composite, built once, applied to
every record.

## Test it

```bash
# From csharp/
dotnet test examples/GraduationVerdict.Tests/GraduationVerdict.Tests.csproj
```

There is no solution file, so a bare `dotnet build`/`dotnet test` from
`csharp/` fails with "Specify a project or solution file" -- always
name the project explicitly, the same as `VerdictRules.Tests` itself.

## Read more

- [`../../../docs/samples/graduation-requirement-verdict/`](../../../docs/samples/graduation-requirement-verdict/README.md) --
  the naive way this policy is usually implemented, why it breaks down,
  and both diagrams behind the design actually used here.
- [`../../../fixtures/graduation_verdict/README.md`](../../../fixtures/graduation_verdict/README.md) --
  how to add a subject, a student scenario, or a new subject type, and
  the shared cross-language fixture contract.
- [`docs/testing.md`](docs/testing.md) -- the two test suites and what
  each proves, including why this project's own tests also serve as an
  integration/e2e regression net for verdict-rules itself.

## Files

| File | What it is |
| --- | --- |
| [`../../../fixtures/graduation_verdict/policies.json`](../../../fixtures/graduation_verdict/policies.json) | The curriculum -- the elective-count threshold plus one row per subject, no code. **Shared across every language.** |
| [`../../../fixtures/graduation_verdict/students.json`](../../../fixtures/graduation_verdict/students.json) | 8 varied students, each carrying its own expected outcome -- including how many rules should run, which proves short-circuiting. **Shared.** |
| [`../../../fixtures/graduation_verdict/edge_cases.json`](../../../fixtures/graduation_verdict/edge_cases.json) | Degenerate curricula proving vacuous-truth polarity. **Shared.** |
| `SubjectPolicy.cs` | The per-subject policy record, read from `policies.json`. |
| `AtLeastNRule.cs` | The custom `IRule` shape for the elective threshold. |
| `GraduationCheck.cs` | The real implementation -- rule factory, JSON loading, and `BuildGraduationCheck`. |
| `Oracle.cs` | A second, verdict-rules-free implementation, used as ground truth by the chaos suite. |
| `ChaosData.cs` | A deterministic generator for randomized, schema-valid curricula and students, using .NET's own seedable `Random`. |
| `Program.cs` | The runnable demo. |
| `../GraduationVerdict.Tests/GraduationVerdictTests.cs` | The curated-scenario suite -- loads both JSON files, asserts generically. |
| `../GraduationVerdict.Tests/ChaosTests.cs` | 500 generated cases, checked against `Oracle.cs` -- see [`docs/testing.md`](docs/testing.md#the-chaos-suite-differential-testing-against-an-independent-oracle). |
