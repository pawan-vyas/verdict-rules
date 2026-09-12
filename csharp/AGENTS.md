# AGENTS.md — C# SDK

C#-specific rules, on top of the repo-root `AGENTS.md`. Read that first.

## The guarantees, in C# terms

- **Sequential evaluation.** `AndRule`/`OrRule` use a plain `foreach` with
  `await`. **Never `Task.WhenAll`.** Short-circuiting only means something if
  later work never *starts*, and the returned boolean is identical either way
  — so this is the one mistake here that passes its own tests.
- **Vacuous truth.** `AndRule([])` passes, `OrRule([])` fails.
- **Emptiness is not absence.** Empty composites fold to their identity;
  unknown rule names and unknown groups throw `KeyNotFoundException` from
  `RunNamedAsync`/`RunGroupAsync`, and return `null` from
  `TryRunNamedAsync`/`TryRunGroupAsync`. The `Try` forms are the
  **primitives** — the throwing ones are assertions on top, so there is one
  lookup path rather than two that can drift.
- **`RuleResult.Data`** holds only what actually ran. Never padded, never
  flattened into the parent's level.

## Conventions

- `ConfigureAwait(false)` on every `await` in library code.
- `Nullable` enabled, `TreatWarningsAsErrors` on. Warnings are build failures.
- Targets `net8.0` and `netstandard2.1`; `IsTrimmable` and, on `net8.0`,
  `IsAotCompatible`.
- **Zero runtime dependencies.** `Microsoft.SourceLink.GitHub` is
  `PrivateAssets="All"`, so it is a build-time reference and never flows to a
  consumer.
- XML documentation is generated; every public member carries it.

## Be precise about structural typing

Do not write that "C# has no structural typing." It does, for **delegates** —
any method or lambda matching the predicate signature is a rule through
`FunctionRule`, with nothing declared. A method group works directly.

What C# lacks is structural typing for a **multi-member interface**: an object
carrying `Name`, `Group` and `EvaluateAsync` is not thereby an `IRule`, where
Python's `Protocol` and TypeScript's structural interfaces would accept it.
That narrow difference is the honest statement, and it is why `FunctionRule`
carries more weight in this SDK than in the others.

## Debuggability is part of the API surface

`RuleResult` and `RunResult` carry `[DebuggerDisplay]`, and `RunResult` a
`[DebuggerTypeProxy]` that expands to the per-rule results. This is not
decoration: a composite's `Data` is a nested list of sub-results, and stepping
through a failing evaluation is how anyone diagnoses one. Keep them accurate
when the shape changes, and keep `ToString()` agreeing with them.

SourceLink and `.snupkg` symbols are enabled so a consumer stepping into the
package lands on real source. Both must be set *before* a version ships —
released versions cannot be made debuggable retroactively.

## Before calling a change done

```
cd csharp && dotnet build -warnaserror && dotnet test
```

For a release, also `dotnet pack -c Release` and confirm both a `.nupkg` and a
`.snupkg` are produced.

## Tests prove behaviour, not just booleans

Short-circuiting is proven with a call log, never the final boolean.
Vacuous-truth polarities and unknown-lookup throws each get their own test.
See the repo-root `docs/testing.md`.
