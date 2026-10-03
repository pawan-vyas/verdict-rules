# Changelog

Release history for the `VerdictRules` NuGet package. Format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/); versioning follows
[semantic versioning](https://semver.org/), scoped to this package — it
releases independently of the other language SDKs.

NuGet surfaces release notes from `PackageReleaseNotes` metadata rather than
from this file, so the csproj points here instead of carrying a copy.

Tagged `csharp-vX.Y.Z`.

## [0.4.0] - 2026-10-03

### Added

- **`RuleResult.SubResults`** — a composite's own children, in
  evaluation order, holding exactly what it evaluated: never padded to
  the full sub-rule list, never flattened into the parent.
- **`RuleResult.GetLeaves()` / `GetFailingLeaves()`**, and the same pair
  on `RunResult`, flattened across every rule a run evaluated. A
  consumer keying an audit trail on the refusing rule can read
  `result.GetFailingLeaves()[0].RuleName` without knowing the tree's
  shape. `GetFailingLeaves()` is an independent recursion, not a filter
  over `GetLeaves()`: a passed result contributes none even past an
  earlier short-circuited branch that failed, and a failed result with
  no failing children is itself the leaf.

  **Methods, not properties**, and this SDK alone spells them that way.
  Each walks the subtree and allocates per call, which the Framework
  Design Guidelines put on the method side of the line; and a get-only
  collection property is traversed by any reflection-based property
  walker — `System.Text.Json`, a structured-logging destructurer, an
  object mapper — where a leaf's own leaves list being itself makes the
  walker recurse until it gives up. `[JsonIgnore]` cannot suppress it:
  read-only collection properties are serialized even with
  `IgnoreReadOnlyProperties` set, and the per-member attribute is not
  in-box for `netstandard2.1`, which this package also targets.
- **`RuleResult.GetDecidedBy()`** — which of `SubResults` explain *this*
  result's own verdict. One level, non-recursive; not the same question
  `GetFailingLeaves()` answers. A method for the same reason the two
  above are: a get-only collection property returning the children is
  serialized, which reintroduces exactly the duplication the stored form
  exists to prevent.
- **`RuleResult.DecidedByIndices`** — the stored field, the positions of
  the deciding children within `SubResults` rather than the children
  themselves. Holding the same results under two properties makes the
  stored graph a DAG, and `System.Text.Json` expands a shared node once
  per path: forty levels of nesting threw `OutOfMemoryException`, and
  sixteen measured 13 MB. An index naming a child the result does not
  have throws `ArgumentOutOfRangeException` at construction, which the
  objects form admitted no check for.
- **A result serializes.** `JsonSerializer.Serialize(result)` emits the
  stored properties only, the three derived views being methods. `Data`
  remains opaque, so encoding whatever a caller put in it is the
  caller's own responsibility.
- **Every `await` uses `ConfigureAwait(false)`**, so no continuation
  resumes on the caller's synchronization context — a deadlock a
  sync-over-async caller on a platform with one would otherwise hit.
  Now covered by its own tests rather than only by convention.
- **`NotRule` / `NotRule<TContext>`** — passes exactly when the one
  wrapped rule fails.
- **`SequentialEvaluator<TContext>` / `ShortCircuitEvaluator<TContext>`**
  — the sequencing `AndRule`/`OrRule` compose, now public so a custom
  composite composes the same primitive rather than hand-rolling a loop.
- `FunctionRule`, `AndRule`, `OrRule`, and `RulesEngine` (both arities)
  carry `[DebuggerDisplay]` and `ToString()`, matching
  `RuleResult`/`RunResult`; the composites and the engine also carry
  `[DebuggerTypeProxy]`, so a debugger expands straight to sub-rules.

### Changed

- **A predicate returns a `PredicateOutcome`, not a `RuleResult`.**
  `RulePredicate`/`RulePredicate<TContext>` are
  `Task<PredicateOutcome>` delegates now. Migration:
  `new RuleResult(name, passed, detail)` becomes
  `new PredicateOutcome(passed, detail)`. The `FunctionRule` wrapping it
  owns the name, so a predicate can no longer set a `RuleName` that
  silently disagrees with the rule it belongs to.
- **A composite's children live in `SubResults`, not `Data`.**
  Migration: the `(IReadOnlyList<RuleResult>)result.Data` cast every
  caller wrote becomes `result.SubResults`, or
  `result.GetFailingLeaves()` if the goal was the refusing leaf. `Data`
  stays opaque and now carries only what a predicate attached.
- **`AndRule`/`OrRule` leave their own `Detail` empty.** The failing
  sub-rule and its own detail are in
  `SubResults`/`GetDecidedBy()`/`GetFailingLeaves()`.

### Fixed

- **`RunResult.GetFailingLeaves()` disagreed with
  `RuleResult.GetFailingLeaves()`** in both directions. It filtered the
  flattened leaves by `!Passed` instead of forwarding to each result's own
  method, and a result's verdict is not a function of its leaves'
  verdicts: a failed `NotRule` wraps a child that *passed*, so filtering
  found a passing leaf and reported no failure on a failed run, while a
  passed `OrRule` holding a recovered-from failed branch reported that
  branch as a failure on a passing run. Now forwards per result.
  `RunResult.GetLeaves()` was always correct and is unchanged. Reported
  from downstream use of the Dart package; the same defect was present in
  all four SDKs.
- **A result hand-built with child results in `Data` is read as a
  *leaf*.** `GetLeaves()`, `GetFailingLeaves()` and `GetDecidedBy()`
  consult `SubResults` only, so a 0.3-era fixture that put children in
  `Data` still constructs and still evaluates, but reports itself as one
  terminal check rather than a tree. Nothing throws; the shape is just
  read differently.
- **A composite's sub-rules, a result's children, and an engine's rules
  are copied on construction, not aliased.** `IReadOnlyList<T>` is a
  read-only *view*, not an immutable collection, so a caller passing a
  `List<T>` kept a mutable handle to the object the composite was
  storing and could change its sub-rules — and its verdict — after
  construction. Appending a result to the very list it was built from
  produced a result containing itself, which every traversal recursed
  through.
- **`RulesEngine`'s own views could disagree.** Its name and group
  indexes are snapshots taken in the constructor while its iteration
  list was aliased, so `RuleNames` reported what was registered and
  `RunAllAsync` iterated whatever the caller's list held by then.

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
