# AGENTS.md — C# SDK

C#-specific rules, on top of the repo-root [`../AGENTS.md`](../AGENTS.md).
Read that first.

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

  A lookup that **matches** always reports its real verdict, so a caller's
  fallback can never mask a failure. When testing code that uses one, cover the
  *present but failing* case — testing only the absent one looks complete and
  misses the direction where a bug is silent.
- **`RuleResult.Data`** holds only what actually ran. Never padded, never
  flattened into the parent's level.

## Be precise about structural typing

Do not write that "C# has no structural typing." It does, for **delegates** —
any method or lambda matching the predicate signature is a rule through
`FunctionRule`, with nothing declared. A method group works directly.

What C# lacks is structural typing for a **multi-member interface**: an object
carrying `Name`, `Group` and `EvaluateAsync` is not thereby an `IRule`, where
Python's `Protocol` and TypeScript's structural interfaces would accept it.
That narrow difference is the honest statement, and it is why `FunctionRule`
carries more weight in this SDK than in the others.

## Layout

`csharp/src/<Project>/` is the .NET convention for a repository that may hold
more than one project, and it is already what this uses — so a second package
is a new directory under `src/` and nothing existing moves. Each project's
manifest, README and changelog live together inside it.

NuGet has no changelog-file concept: release notes come from the
`PackageReleaseNotes` metadata property, which points at `CHANGELOG.md` rather
than duplicating it.

## Conventions

- `ConfigureAwait(false)` on every `await` in library code.
- `CancellationToken cancellationToken = default` as the trailing parameter
  on every public async method — `IRule.EvaluateAsync`, the `RulePredicate`
  delegate, and every `RulesEngine` run method. A new composite or engine
  method that loops over sub-rules calls
  `cancellationToken.ThrowIfCancellationRequested()` at the top of every
  iteration, not just once before the loop — checked between sub-rules, so
  cancellation raised mid-run stops before the next one starts rather than
  only whenever whichever sub-rule is currently running happens to observe
  it internally.
- `Nullable` enabled, `TreatWarningsAsErrors` on. Warnings are build failures.
- Targets `net10.0` and `netstandard2.1`; `IsTrimmable` on both,
  `IsAotCompatible` on every TFM except `netstandard2.1` (a denylist, not an
  allowlist — see the decision record below for why that shape matters).
- **Zero runtime dependencies.** `Microsoft.SourceLink.GitHub` is
  `PrivateAssets="All"`, so it is a build-time reference and never flows to a
  consumer.
- XML documentation is generated; every public member carries it.

## Debuggability is part of the API surface

`RuleResult` and `RunResult` carry `[DebuggerDisplay]`, and `RunResult` a
`[DebuggerTypeProxy]` that expands to the per-rule results. This is not
decoration: a composite's `Data` is a nested list of sub-results, and stepping
through a failing evaluation is how anyone diagnoses one. Keep them accurate
when the shape changes, and keep `ToString()` agreeing with them.

SourceLink and `.snupkg` symbols are enabled so a consumer stepping into the
package lands on real source. Both must be set *before* a version ships —
released versions cannot be made debuggable retroactively.

## Decision record: target-framework reach

`VerdictRules.csproj` multi-targets `net10.0;netstandard2.1`. `net8.0` was the
placeholder that shipped first; it was replaced once the actual tradeoff got
worked through, because `net8.0` and `net9.0` both reach end of support on
November 10, 2026 — shipping either as the sole modern target meant a brand
new package landing already near end-of-life. `net10.0` is the current LTS,
supported through November 2028.

What decided the rest of the shape:

- **`netX.Y` consumption is one-directional; `netstandard` isn't.** A package
  targeting `net8.0` can be consumed by an app on `net9.0`/`net10.0` (newer
  can consume older within the modern-.NET family), but not the reverse — a
  `net10.0`-only package is uninstallable by an app still on `net8.0`, which
  is where most production .NET apps still sit; LTS adoption lags a release
  by well over a year in practice. `netstandard2.1`, kept alongside `net10.0`,
  closes that gap for free: `net8.0`/`net9.0`/`net10.0` all fully implement
  it, so nothing on any of them is excluded.
- **`netstandard2.1` is not implemented by .NET Framework** at all — the
  highest .NET Standard version .NET Framework 4.x ever implements is `2.0`.
  Reaching .NET Framework is a `netstandard2.0` question, not a `net48` one
  (an explicit `net48` TFM would need a Windows-only CI leg and buys nothing
  a netstandard build doesn't already cover for a library with no
  Framework-only API dependency) — but it was decided **not** to reach for
  it at `0.0.1`. `netstandard2.0` and `netstandard2.1` aren't a strict
  broadening of each other: 2.0 reaches strictly more runtimes, but 2.1 has
  real API surface 2.0 doesn't (`IAsyncEnumerable`, `Span<T>` as a first-class
  citizen). The source has no netstandard2.1-only API usage today, verified
  by actually swapping the TFM and rebuilding rather than assumed — but
  committing to `netstandard2.0` now would mean either avoiding that surface
  forever or reintroducing a second legacy target with conditional
  compilation the moment something needs it. Deferred, not ruled out: nothing
  stops adding `netstandard2.0` later as its own step if real .NET Framework
  demand shows up.
- **`IsAotCompatible` had to change shape along with the TFM, not just its
  value.** The old `Condition="'$(TargetFramework)' == 'net8.0'"` was an
  allowlist keyed to one specific TFM — every future `net11.0`/`net12.0`
  addition would have needed its own edit to that same line, the "adding a
  variant means editing a file the others share" shape this repo's dispatch
  conventions argue against elsewhere. It's a denylist now:
  `Condition="'$(TargetFramework)' != 'netstandard2.1'"` — on by default, off
  only for the one TFM that structurally can't carry it (no runtime of its
  own to be AOT-compatible *with*). A future modern TFM lands already
  AOT-compatible with zero edits to this property.
- **Widening `TargetFrameworks` widens the CI test matrix and the
  once-published, permanent surface** (see "Debuggability is part of the API
  surface" above for why permanence is the operative word for this package) —
  a target added at `0.0.1` can be dropped later only as a breaking change; a
  target never offered is free to add at any point. That asymmetry is why
  `netstandard2.0` stayed deferred rather than added speculatively.

## Worth watching once evals exist here: no local source to peek at

A Python skill eval was observed opening the installed package's own `.py`
source to double-check an exact signature, despite that signature already
being fully documented in
[`references/python/agent-notes.md`](../skills/verdict/references/python/agent-notes.md)'s
"API, in one screen" section. Checked afterward: nothing in the final code
diverged from what was already documented — the read added nothing, only
cost tokens.

Whether that is a genuine trust gap (the agent does not believe a doc's
stated signature over ground truth) or just an artifact of Python installing
real, readable `.py` source one `Read` call away is unresolved by a
Python-only observation — the low cost of checking is itself a confound.
NuGet ships a compiled DLL, not source; the only path to real source is
SourceLink fetching from GitHub through a debugger, a materially
higher-friction, network-dependent act, not a local file read. A C# eval
reaching for that anyway would be real evidence of a trust gap rather than
convenience; not reaching for it would settle nothing either way, since it
stays consistent with "just convenient when free." Worth watching once this
language's own evals exist (see [`docs/maintenance/adding-a-language.md`](../docs/maintenance/adding-a-language.md) Stage 4) — not
something to guard against. Restricting an agent from reading its own public
source, for a benefit this hard to define, would cost real flexibility for
no clear correctness gain.

## Before calling a change done

```bash
cd csharp
dotnet build src/VerdictRules/VerdictRules.csproj -warnaserror
dotnet test tests/VerdictRules.Tests/VerdictRules.Tests.csproj
```

There is no solution file, so a bare `dotnet build`/`dotnet test` from
`csharp/` fails with "Specify a project or solution file" — always name
the project explicitly.

For a release, also `dotnet pack -c Release` and confirm both a `.nupkg` and a
`.snupkg` are produced.

## Tests prove behaviour, not just booleans

Short-circuiting is proven with a call log, never the final boolean.
Vacuous-truth polarities and unknown-lookup throws each get their own test.
See the repo-root [`docs/testing/`](../docs/testing/README.md).
