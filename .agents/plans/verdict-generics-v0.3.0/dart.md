# Dart — v0.3.0 PR Plan

> Read [`README.md`](README.md) first — the resolved design and release shape
> live there. This file is Dart's own complete, self-contained checklist.
> Goes **last**, deliberately — the one real breaking migration, benefiting
> from three already-completed, working precedents (Python, TS, C#) before
> its own harder mechanics land.

## 1. Test-suite parity — the same two gaps as C#

Per the audit in `README.md`: Dart's own `docs/testing/dart.md` contract
table omits the same two rows C#'s does. Add, mirroring JS's own test
structure and naming conventions:

- A duplicate-rule-name test: two rules sharing a name, registered in one
  `RulesEngine`, and the by-name lookup resolves to the *last* one.
- A predicate-exception-propagation test, covering `runAll`, `runGroup`,
  `AndRule`, `OrRule` individually — each its own case, not one shared
  assertion.

Update `docs/testing/dart.md`'s own contract table once these land.

## 2. `graduation_verdict` fixture — port

Port the *existing*, dict-context `graduation_verdict` fixture to
`dart/packages/verdict_rules/`, per `docs/maintenance/adding-a-language.md`
Stage 4 (shared fixture, oracle/differential suite in Dart's own idiom).
Dict-context only — `Map<String, Object?>`, no `TContext` anywhere in this
step.

## 3. New cross-language fixture — implement against Python's spec

Implement the fixture designed in `python.md` §3 (see that file once
written), in Dart, against `fixtures/<name>/README.md` — do not redesign the
domain. Same coverage requirement: typed `Rule<TContext>`, dict-context
`Rule` in the same domain, a `ProjectingRule<TOuter, TInner>` adapter, both
`RulesEngine`/`RulesEngine<TContext>` forms, each with a real test.

## 4. Core `Rule<TContext>` migration — the real breaking change, scoped precisely

Per `README.md`'s resolved Dart mechanics:

- `rule.dart`: `abstract interface class Rule<TContext>`. `RulePredicate<TContext>`,
  `FunctionRule<TContext>`, `AndRule<TContext>`, `OrRule<TContext>` — same
  bodies as today, parameter type moved from `Map<String, Object?>` to
  `TContext`.
- `engine.dart`: `RulesEngine<TContext>`, same treatment.
- New: `ProjectingRule<TOuter, TInner> implements Rule<TOuter>` (see
  `README.md`'s cross-context-reuse section).
- **Confirm, don't assume, the actual blast radius.** `README.md` predicts
  the break lands specifically on `implements Rule` declarations (needing
  `implements Rule<Map<String, Object?>>`), while `FunctionRule('x', predicate)`/
  `AndRule('x', [...])` call sites keep working via constructor-argument type
  inference — grep the actual codebase, `docs/`, and every sample for
  `implements Rule` (not followed by `<`) to find the real, complete list of
  sites needing the explicit type argument, rather than trusting the
  prediction unchecked.
- New test: confirm `FunctionRule('x', predicate)` still infers its context
  type from `predicate`'s own declared parameter type with zero explicit
  type argument needed — the concrete proof the call-site story holds.
- New test: a Dart-analyzer-based check (or an `expect_error`-annotated test,
  if this repo's own Dart tooling already has an analyzer-test convention —
  check before inventing one) proving `AndRule<TContext>` rejects a
  mismatched sub-rule.

## 5. Docs, samples, quickstart, changelog, skill notes — Dart's own files only

- `dart/packages/verdict_rules/README.md`, `doc/quickstart.md` — add the
  typed path alongside the existing `Map<String, Object?>` example; the
  dict example stays, unreplaced.
- `docs/extending/*/dart.md` (7 files, same scenario dirs as Python) —
  one-line pointer per file to the typed-context option, not a rewrite.
- `docs/samples/*/dart.md` (one per existing sample dir with a Dart variant)
  — same one-line pointer treatment.
- `dart/packages/verdict_rules/CHANGELOG.md` — new `0.3.0` entry: the
  generic `Rule<TContext>` addition, **explicitly documenting the breaking
  change** (every `implements Rule` site needs an explicit type argument
  now) as a real, named migration note — not folded in as if it were
  additive like the other three languages.
- `skills/verdict/references/dart/agent-notes.md` — new entry under
  "mistakes that show up in generated code": when to reach for
  `Rule<TContext>` vs. `Rule<Map<String, Object?>>`, and specifically that
  an `implements Rule` (bare, no type argument) is now a compile error, not
  a silently-accepted default the way it might be assumed to work from
  seeing TS or C#'s own migration first.

## Done when

- Existing test suite passes (post-migration, with the required explicit
  type arguments added everywhere `implements Rule` appears).
- The two new parity tests (duplicate-name, predicate-exception) pass, and
  `docs/testing/dart.md`'s contract table is updated.
- Ported `graduation_verdict` passes, matching Python's expected-outcomes
  table exactly.
- The new fixture (Python's spec, Dart implementation) passes.
- New generics-specific tests (inference proof, compile-fail check) pass.
- `dart analyze` reports no issues (this package's own existing bar, per
  `docs/testing/dart.md`'s "Running tests" section).
- The doc/changelog/skill sweep above is complete, with the breaking change
  named explicitly in the changelog, not glossed over.
- Explicit approval received before merge.
