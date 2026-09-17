# Changelog

Release history for the `VerdictRules` NuGet package. Format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/); versioning follows
[semantic versioning](https://semver.org/), scoped to this package — it
releases independently of the other language SDKs.

NuGet surfaces release notes from `PackageReleaseNotes` metadata rather than
from this file, so the csproj points here instead of carrying a copy.

Tagged `csharp-vX.Y.Z`.

## [0.0.1] - 2026-09-12

Initial publish.

- `IRule`, `FunctionRule`, `AndRule`, `OrRule`, `RulesEngine`, `RuleResult`,
  `RunResult`.
- `RulePredicate`, a named delegate for `FunctionRule`'s predicate shape, so a
  field, a stored variable, or a helper wrapping a predicate never has to
  spell out the underlying `Func<...>` signature in full.
- Sequential, never concurrent evaluation, so short-circuiting is a real
  contract rather than a best-effort optimisation.
- Vacuous-truth polarity decided per composite: `AndRule([])` passes,
  `OrRule([])` fails.
- Unknown rule names and unknown groups throw `KeyNotFoundException` rather
  than returning a vacuous pass.
- `RulesEngine.TryRunNamedAsync` / `TryRunGroupAsync`, returning `null` rather
  than throwing when nothing matches — the primitives the throwing forms are
  built on. `null` means absent, never failed.
- `RulesEngine.RuleNames` / `GroupNames` for enumerating an engine.
- `CancellationToken cancellationToken = default` on every async method
  (`IRule.EvaluateAsync`, `RulePredicate`, and all five `RulesEngine` run
  methods), checked between sub-rules by every composite and by the engine's
  own run methods, so a cancellation raised mid-run stops before the next
  rule starts rather than only whenever the currently-running rule happens
  to observe it internally.
- `net10.0` and `netstandard2.1`, trimmable, AOT-compatible, zero runtime
  dependencies.
- `[DebuggerDisplay]` and a debugger type proxy so a nested result tree is
  legible while stepping; SourceLink and `.snupkg` symbols so stepping into the
  package reaches real source.
- `PackageTags` is `rules-engine;rule-evaluation;eligibility;decision;decision-engine;async`.
