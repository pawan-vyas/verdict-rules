# Changelog

Release history for the `VerdictRules` NuGet package. Format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/); versioning follows
[semantic versioning](https://semver.org/), scoped to this package — it
releases independently of the other language SDKs.

NuGet surfaces release notes from `PackageReleaseNotes` metadata rather than
from this file, so the csproj points here instead of carrying a copy.

Tagged `csharp-vX.Y.Z`.

## [0.3.2] - 2026-09-19

### Fixed

- **An already-cancelled `CancellationToken` was ignored on every path
  that did not loop over more than one rule.** `ThrowIfCancellationRequested`
  was only called *inside* the `foreach` of the composites and of
  `RunAllAsync`/`TryRunGroupAsync`, so:
  - `RunNamedAsync`/`TryRunNamedAsync` invoked the rule's predicate --
    arbitrary consumer code, possibly a database or HTTP call -- with a
    token that was already cancelled. A single rule has no "between
    rules", so there was no check anywhere on that path.
  - An empty `AndRule`/`OrRule` returned its vacuous result, and
    `RunAllAsync` on an engine holding no rules returned a vacuous
    `RunResult`, as though the cancellation had never happened.
  - `TryRunNamedAsync`/`TryRunGroupAsync` returned `null` for an absent
    name or group, which a caller reads as "no such rule" rather than
    "cancelled".
  - `FunctionRule`/`FunctionRule<TContext>` forwarded straight to the
    predicate with no check of its own.

  The contract is now uniform and stated as such: **no rule evaluation
  begins on an already-cancelled token.** Every composite and every
  engine run method checks once on entry, *in addition to* the existing
  per-iteration check -- the mid-run check is what stops the next
  sub-rule when a token is cancelled *during* a run, and is deliberately
  kept. `FunctionRule` throws synchronously, being a guard on a method
  that is intentionally not `async`.

  Fourteen tests in `CancellationContractTests` cover this across both
  arities; each one fails against 0.3.1 with "No exception was thrown".

### Changed

- **`FunctionRule`, `AndRule`, `OrRule` and `RulesEngine` are now closed
  specializations of their own generic siblings, by composition.** Each
  holds an instance of `FunctionRule<TContext>`/`AndRule<TContext>`/
  `OrRule<TContext>`/`RulesEngine<TContext>` closed over
  `IReadOnlyDictionary<string, object?>` and forwards to it, rather than
  carrying a second, independent copy of the same logic. No public
  signature changes, and no behavior changes beyond the cancellation fix
  above.

  This is what the fix exposed: the two arities were duplicate
  implementations, and the cancellation gap existed identically in both
  because it had to be written twice. Every evaluation guarantee --
  sequential sub-rule evaluation, short-circuit polarity, vacuous truth,
  name/group indexing, lookup strictness, cancellation -- is now defined
  exactly once and cannot drift between arities.

  Composition rather than inheritance, deliberately: `IRule` already
  *is* `IRule<IReadOnlyDictionary<string, object?>>` at the interface
  level, so no base class is needed for substitutability, and each
  non-generic type stays `sealed` with its own constructor signature
  instead of inheriting a generic one that would leak `TContext` into
  its public surface. `IReadOnlyList<T>` covariance carries
  `IReadOnlyList<IRule>` into the generic constructor unchanged;
  `RulePredicate`, being a nominal delegate type rather than a closure
  of `RulePredicate<TContext>`, is rewrapped in `FunctionRule` -- the
  one place the two delegate types meet.

## [0.3.1] - 2026-09-18

### Changed

- Every doc comment and inline comment in the package's own source
  trimmed to state current behavior only -- design rationale,
  alternatives-considered framing, and cross-references to the deeper
  docs for "the full reasoning" cut, not relocated.

## [0.3.0] - 2026-09-18

### Added

- `IRule<TContext>`, generic over the context a rule reads from,
  alongside new generic siblings `FunctionRule<TContext>`,
  `AndRule<TContext>`, `OrRule<TContext>`, and `RulesEngine<TContext>`.
  `IRule` is now the closed specialization
  `IRule : IRule<IReadOnlyDictionary<string, object?>>`; every existing
  `: IRule` implementation keeps compiling unchanged. Fully additive;
  zero breaking changes.
- `RulePredicate<TContext>`, the generic sibling of `RulePredicate`.

### Changed

- `docs/architecture/csharp.md` gained a "Generic context, concretely"
  section.

## [0.0.1] - 2026-09-17

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
