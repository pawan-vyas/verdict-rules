# TypeScript — v0.3.0 PR Plan

> Read [`README.md`](README.md) first — the resolved design and release shape
> live there. This file is TS's own complete, self-contained checklist. Goes
> **second**, after Python — closest in risk profile to Python, sanity-checks
> the resolved pattern actually transfers before the two higher-risk
> languages, and implements the new fixture against the spec Python's PR
> wrote (never inventing its own variant of it).

## 1. Test-suite parity

**None needed for the 9-contract checklist** — per the audit in `README.md`,
`docs/testing/js.md` already documents full coverage (32 tests, all 9
contracts). Verify this is still true (re-run `npm run test:coverage`, diff
the table against the actual test file) rather than assuming no drift.

## 2. `graduation_verdict` fixture — port

**The real work item for this language's parity.** Port the *existing*,
dict-context `graduation_verdict` fixture from Python to
`js/packages/verdict-rules/`, following whatever structure
`docs/maintenance/adding-a-language.md`'s Stage 4 already specifies (the
shared fixture, an oracle/differential suite in TS's own idiom). This is the
dict-context version — no `TContext` anywhere in this step; that's what
proves it's a faithful port, not a new design.

## 3. New cross-language fixture — implement against Python's spec

Implement the fixture designed in `python.md` §3, in TS, against
`fixtures/<name>/README.md` as written — do not redesign the domain or
policies here. Same coverage requirement: typed `Rule<TContext>`, dict-context
`Rule` in the same domain, a `ProjectingRule` adapter, both `RulesEngine`
forms, each with a real assertion, not just present in the code.

## 4. Core `Rule<TContext>` migration

Per `README.md`'s resolved TS mechanics — **no default type parameter**:

- `rule.ts`: `Rule<TContext>` (no `= Context` default). `RulePredicate<TContext>`,
  `FunctionRule<TContext>`, `AndRule<TContext>`, `OrRule<TContext>` — same
  treatment, same bodies as today, just the parameter type moved from
  `Context` to `TContext`.
- `engine.ts`: `RulesEngine<TContext>`, same shape.
- New: `ProjectingRule<TOuter, TInner>` class (see `README.md`'s
  cross-context-reuse section).
- This is a **real, if narrow, migration** — confirm the actual blast radius
  matches what `README.md` predicts: existing bare object literals
  (`const overEighteen = { name, async evaluate(ctx) {...} }`) need zero
  changes; only explicit type annotations (`const r: Rule = ...`, a function
  parameter typed `rule: Rule`) need `<Context>` added. Grep the existing
  codebase and docs for `: Rule` (not followed by `<`) to find every site
  that actually needs the migration, rather than assuming.
- New tests: a `tsc`-based compile-fail check proving `AndRule<TContext>`
  rejects a mismatched sub-rule (a `// @ts-expect-error` test, or a
  `dtslint`-style type-only test file) — decide the concrete mechanism as
  part of this PR.
- Docs: explicit `Rule<Context>` shown as the deliberate dict-context
  spelling everywhere the quickstart/README currently shows bare `Rule`.

## 5. Docs, samples, quickstart, changelog, skill notes — TS's own files only

- `js/packages/verdict-rules/README.md` — add the typed-context path
  alongside the existing dict example; the dict example stays, unreplaced.
- `docs/extending/*/js.md` (7 files, same scenario dirs as Python) — one-line
  pointer to the typed-context option per file, not a rewrite.
- `docs/samples/*/js.md` (one per existing sample dir with a JS variant) —
  same one-line pointer treatment.
- `js/packages/verdict-rules/CHANGELOG.md` — new `0.3.0` entry: the generic
  `Rule<TContext>` addition, explicitly noting the no-default decision and
  its narrow migration cost, plus a pointer to the new fixture.
- `skills/verdict/references/js/agent-notes.md` — new entry under "mistakes
  that show up in generated code": when to reach for `Rule<TContext>` vs.
  `Rule<Context>`, and the fact there's no default (an agent used to other
  languages' defaults might assume one exists here).

## Done when

- Existing test suite passes unchanged (post-parity-check).
- Ported `graduation_verdict` passes, matching Python's expected-outcomes
  table exactly.
- The new fixture (Python's spec, TS implementation) passes.
- New generics-specific tests (the `AndRule<TContext>` compile-fail check)
  pass.
- The doc/changelog/skill sweep above is complete.
- Explicit approval received before merge.
