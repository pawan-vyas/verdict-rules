# C# — v0.3.0 PR Plan

> Read [`README.md`](README.md) first — the resolved design and release shape
> live there. This file is C#'s own complete, self-contained checklist. Goes
> **third**, after Python and TypeScript — a genuinely new coexistence
> pattern (not a port of either prior language's mechanics), implementing
> the new fixture against Python's already-fixed spec.

## 1. Test-suite parity — two known gaps, plus a real count-vs-coverage check

Per `README.md`'s corrected scope: parity means Python's *whole* suite (41
tests, 29 here today), not just the 9-contract checklist. The two items
below are the *known, self-documented* gaps — confirm they're the complete
picture by diffing Python's full test list against `EngineTests.cs`
directly, rather than assuming nothing else is missing once these two land.
The target is coverage, not literally reaching 41 — a `[Theory]` covering
several of Python's separate test functions in one parameterized case is
full parity, not a shortfall.

Per `csharp/docs/testing/csharp.md`'s own words: *"Not yet covered: a
duplicate-rule-name registration (which name wins the by-name lookup) and a
predicate's own thrown exception propagating uncaught through
`RunAllAsync`/`RunGroupAsync`/`AndRule`/`OrRule` — both real contracts, both
covered in JS's... suites. Worth porting before this package leaves 0.0.x."*

Add, mirroring JS's own test structure and naming conventions
(`EngineTests.cs`):

- A duplicate-rule-name test: two rules sharing a name, registered in one
  `RulesEngine`, and the by-name lookup resolves to the *last* one — the
  same "last one wins, like a dict/map literal" contract every other
  language already tests.
- A predicate-exception-propagation test, covering all four call sites JS's
  own suite covers individually: `RunAllAsync`, `RunGroupAsync`, `AndRule`,
  `OrRule` — each gets its own case proving a thrown exception is never
  caught, not one shared test asserting only one of the four.

Update `docs/testing/csharp.md`'s own "Not yet covered" note and contract
table once these land — that note is stale the moment these tests exist.

## 2. `graduation_verdict` fixture — port

Port the *existing*, dict-context `graduation_verdict` fixture to
`csharp/`, per `docs/maintenance/adding-a-language.md` Stage 4 (shared
fixture, oracle/differential suite in C#'s own idiom). Dict-context only —
`IReadOnlyDictionary<string, object?>`, no `TContext` anywhere in this step.

## 3. New cross-language fixture — implement against Python's spec

Implement the fixture designed in `python.md` §3 (see that file once
written), in C#, against `fixtures/<name>/README.md` — do not redesign the
domain. Same coverage requirement: typed `IRule<TContext>`, dict-context
`IRule` in the same domain, a `ProjectingRule<TOuter, TInner>` adapter, both
`RulesEngine`/`RulesEngine<TContext>` forms, each with a real test.

## 4. Core `IRule<TContext>` migration

Per `README.md`'s resolved C# mechanics — arity coexistence, one specific
closed-generic specialization:

- `IRule.cs`: add `IRule<TContext>` (new). Change `IRule` to
  `public interface IRule : IRule<IReadOnlyDictionary<string, object?>> { }`
  — this is the *only* inheritance relationship in this migration. Verify it
  compiles with **zero** changes to any existing `: IRule` implementer (this
  is the concrete, testable claim that it's non-breaking — write a smoke
  test that references an existing rule class unchanged).
- Do **not** make `IRule<TContext>` inherit `IRule` in the other direction —
  that's the mechanically broken shape (forces every typed rule to implement
  an unimplementable dict-shaped method). This was verified against the real
  .NET source: `IComparer<in T>` has no base interface at all.
- `RulePredicate.cs`: add `RulePredicate<TContext>` as a new, independent
  generic delegate — same name, different arity, `RulePredicate` (arity 0)
  completely unchanged.
- `FunctionRule.cs`, `AndRule.cs`, `OrRule.cs`: each gets a new
  `<TContext>` generic sibling class, same name, different arity. **The
  existing non-generic classes' internals are not touched at all** — each
  generic sibling is a fresh, independent implementation (matching
  `IComparer`/`IComparer<T>`'s own independence), not a wrapper delegating to
  the old one.
- `RulesEngine.cs`: add `RulesEngine<TContext>`, same treatment.
- New: `ProjectingRule<TOuter, TInner> : IRule<TOuter>` (see `README.md`'s
  cross-context-reuse section) — the exact shape already sketched there.
- `CancellationToken` support (already present throughout the non-generic
  API per this package's own existing convention) must be present in every
  new generic sibling too — same signature shape, `CancellationToken ct = default`,
  checked between sub-rules in `AndRule<TContext>`/`OrRule<TContext>` and
  between rules in `RulesEngine<TContext>`'s run methods, matching the
  existing non-generic classes' own tested behavior exactly.
- New tests: a compile-fail test proving `AndRule<TContext>` rejects a
  mismatched sub-rule — a `Roslyn`-based analyzer test, or a
  separate-project-that-must-not-compile harness (this repo doesn't have
  one yet; decide the concrete mechanism as part of this PR rather than
  skipping the assertion because the harness doesn't exist).
- New test: confirm an existing `: IRule` implementer needs zero source
  changes (the concrete non-breaking proof named above).

## 5. Docs, samples, quickstart, changelog, skill notes — C#'s own files only

- `csharp/src/VerdictRules/README.md`, `docs/quickstart.md` — add the typed
  path alongside the existing dict example; the dict example stays.
- `docs/architecture/csharp.md` — has one flagged `RulePredicate` signature
  reference (found in the pre-migration audit); update it to reflect both
  forms existing side by side.
- `docs/extending/*/csharp.md` (7 files, same scenario dirs as Python) —
  one-line pointer per file to the typed-context option, not a rewrite.
- `docs/samples/*/csharp.md` (one per existing sample dir with a C# variant)
  — same one-line pointer treatment.
- `csharp/src/VerdictRules/CHANGELOG.md` — new `0.3.0` entry: the generic
  `IRule<TContext>` family, explicitly noting it's fully additive (no
  breaking changes), plus a pointer to the new fixture.
- `skills/verdict/references/csharp/agent-notes.md` — new entry under
  "mistakes that show up in generated code": when to reach for
  `IRule<TContext>` vs. `IRule`, and the specific `IRule : IRule<...>`
  relationship (so a generated `class Foo : IRule<TContext> : IRule` double
  declaration mistake doesn't happen — `IRule` and `IRule<TContext>` are
  independent, never both implemented at once for one rule).

## Done when

- Existing test suite passes unchanged.
- The two new parity tests (duplicate-name, predicate-exception) pass, and
  `docs/testing/csharp.md`'s stale "Not yet covered" note is removed.
- Ported `graduation_verdict` passes, matching Python's expected-outcomes
  table exactly.
- The new fixture (Python's spec, C# implementation) passes.
- New generics-specific tests (non-breaking proof, compile-fail check) pass.
- The doc/changelog/skill sweep above is complete.
- Explicit approval received before merge.
