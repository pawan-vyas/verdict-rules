# Python — v0.3.0 PR Plan

> Read [`README.md`](README.md) first — the resolved design and release shape
> live there. This file is Python's own complete, self-contained checklist.
> Python goes **first**: it sets the reference pattern, is the litmus-test
> language for "did the migration actually break anything" (via the untouched
> `graduation_verdict`), and is where the new cross-language fixture's
> language-agnostic spec gets written.

## 1. Test-suite parity

**None needed.** Per the audit in `README.md`, Python is the baseline every
other language is being brought up to — its own `docs/testing/python.md`
table is already the complete 9-contract checklist. Verify this is still
true (re-run the suite, diff the table against the actual test file) rather
than assuming it hasn't drifted since that doc was last touched.

## 2. `graduation_verdict` fixture

**Untouched, on purpose.** Do not add generics anywhere in
`python/examples/graduation_verdict/`. This is the litmus test — it proving
out unchanged, dict-context, after `Rule` becomes generic elsewhere in the
package, is the actual evidence the migration didn't break the dict path.
Re-run its full suite as a gate on this PR, but make zero edits to it.

## 3. New cross-language fixture — design + Python implementation

This is the one place Python's PR does more than "port the pattern" — **the
language-agnostic spec gets written here**, informed by `README.md`'s
"Fixtures" section direction (production-grade, generalized business-flow
domain, never naming a real consuming project). Concretely:

1. Design the fixture's domain, policies, and expected-outcomes table —
   mirror `fixtures/graduation_verdict/README.md`'s own structure (a
   `policies.json`/`students.json`/`edge_cases.json`-equivalent shape, or
   whatever this fixture's own domain calls for).
2. Write `fixtures/<name>/README.md` (name TBD during design — do not
   default to a placeholder without deciding it deliberately).
3. Implement the Python side under `python/examples/<name>/`, exercising, at
   minimum, in real assertions: a typed `Rule[TContext]`, a dict-context
   `Rule` in the same domain, a `ProjectingRule`-equivalent adapter reusing
   one rule across two differently-shaped contexts, and both `RulesEngine`
   forms (plain and `RulesEngine[TContext]`).
4. This is a genuine design step, not a mechanical port — expect it to need
   its own back-and-forth before the spec is fixed. Every subsequent
   language's PR implements against whatever gets written here; none of them
   re-derive the domain independently.

## 4. Core `Rule[TContext]` migration

Per `README.md`'s resolved Python mechanics:

- `rule.py`: `Rule` becomes `Protocol[TContext]`; `FunctionRule`, `AndRule`,
  `OrRule` become `Generic[TContext]`. Same bodies, only the context
  parameter's type moves from `dict` to `TContext`.
- `engine.py`: `RulesEngine` becomes `Generic[TContext]`. **The existing
  non-generic behavior isn't going anywhere** — a bare, unparameterized
  `RulesEngine`/`Rule`/etc. still works exactly as today (Python's generics
  are erased at runtime; this is a static-typing-only change).
- New: a `ProjectingRule` class (see `README.md`'s cross-context-reuse
  section) — one small, additive, `Generic[TOuter, TInner]` class.
- Confirm `isinstance(x, Rule)` still works unconditionally (it does, by
  construction — write a test proving it rather than asserting it in a
  docstring only).
- Confirm `FunctionRule("x", predicate)` infers `TContext` from a typed
  predicate's own annotation, and infers permissively from an untyped one —
  write both cases as tests, not just documented behavior.
- New tests: `AndRule[TContext]` rejecting a mismatched sub-rule — Python has
  no compile step, so this is necessarily a static-analysis-level check
  (a `reveal_type`/`assert_type`-based test run through mypy or pyright in
  CI, not a runtime test) rather than a "does not compile" test the way
  C#/TS/Dart can express it. Decide the concrete mechanism as part of this
  PR, not left implicit.
- Docs: `Rule[dict[str, Any]]` documented as the explicit "same strictness as
  before" spelling; a note pointing at pyright's `reportMissingTypeArgument`
  / mypy's `disallow_any_generics` for consumers who want compiler-enforced
  explicitness.

## 5. Docs, samples, quickstart, changelog, skill notes — Python's own files only

- `python/packages/verdict-rules/README.md`, `docs/quickstart.md` — add the
  typed-context path alongside the existing dict example; don't replace the
  dict example, since it stays first-class.
- `docs/extending/*/python.md` (7 files: `absence-vs-failure`,
  `data-driven-rule-construction`, `domain-adapter-module`,
  `isolating-flaky-predicates`, `nesting-composites`, `new-rule-shape`,
  `wrapping-a-predicate`) — each stays *correct as written* (dict-context
  isn't deprecated); add one line per file pointing at the typed-context
  option, not a rewrite.
- `docs/samples/*/python.md` (one per existing sample dir under
  `docs/samples/` that has a Python variant — check each sample's own scope
  note before assuming every one needs a line added) — same one-line pointer
  treatment as `docs/extending/`.
- `python/packages/verdict-rules/CHANGELOG.md` — new `0.3.0` entry: the
  generic `Rule[TContext]` addition, explicitly noting it's non-breaking at
  runtime, plus a pointer to the new fixture.
- `skills/verdict/references/python/agent-notes.md` — new entry under
  "mistakes that show up in generated code": when to reach for
  `Rule[TContext]` vs. plain `Rule` (dict), stated as a real design choice
  with the reuse-across-contexts case named explicitly, not "prefer generic."

## Done when

- Existing test suite passes unchanged.
- `graduation_verdict` passes unchanged, untouched.
- The new fixture's spec is written and its Python implementation passes.
- New generics-specific tests (inference, `isinstance`, the
  `AndRule[TContext]` mismatch check) pass.
- The doc/changelog/skill sweep above is complete.
- Explicit approval received before merge.
