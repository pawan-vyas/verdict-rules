# Shared Docs — v0.3.0 Plan

> Read [`README.md`](README.md) first — **this checklist no longer lands as
> its own trailing PR.** The release shape described below (a fifth PR,
> after four independent language PRs) is superseded: this content now
> lands as part of the single cross-language core-migration PR described in
> `README.md`'s revised "Release shape," alongside all four languages' own
> generics implementation, tests, and per-language docs sweeps, in the same
> merge. The reasoning below for *why* this sweep can't be written before
> the real implementation exists still holds — it just means "before the
> single migration PR's own implementation work is done," not "before four
> separate language PRs exist." It touches the files no single language
> owns: the language-agnostic READMEs, the architecture docs, the root
> README, and the skill's shared (non-per-language) content.

## Why this is last, not first

Every one of these files describes a *cross-language* claim ("here's how
`Rule<TContext>` works, in general" / "here's the new fixture, and what each
language's implementation of it demonstrates"). Writing them before the four
implementations exist would mean documenting a design instead of a shipped
reality — exactly the failure mode `AGENTS.md`'s own "before treating a
behaviour change as done" section exists to prevent (a doc describing
something untrue, with nothing failing to catch it). This PR is where that
sweep actually happens, once there's real, working code in all four
languages to check the words against.

## Checklist

### `docs/architecture/`

- `docs/architecture/README.md` and its class diagrams — the current design
  overview doesn't mention generics at all; add a section (and, if the
  existing diagram style calls for it per the `mermaid-diagrams` skill, a
  diagram) explaining `Rule<TContext>`, why context and outcome are treated
  differently, and the `RulesEngine`/`RulesEngine<TContext>` coexistence.
- `docs/architecture/python.md` — the flagged mermaid class diagram
  (`evaluate(context: dict)` × 4, `run_all(context: dict)` × 4 — 8 signatures
  total) needs every one updated to show `TContext`, not just prose nearby.
- `docs/architecture/csharp.md` — the flagged `RulePredicate` signature
  reference needs the same treatment (already itemized in `csharp.md` §5,
  since it's small enough to land with that PR — **verify it actually
  landed there**; if it didn't, it's this PR's job to catch it, not skip it).
- `docs/architecture/js.md`, `docs/architecture/dart.md` — check for the same
  class-diagram-signature pattern; the initial audit didn't flag these, but
  audits can miss things — verify directly rather than trusting the earlier
  grep.

### `docs/extending/`

- The 7 scenario `README.md` files (language-agnostic) —
  `absence-vs-failure`, `data-driven-rule-construction`,
  `domain-adapter-module`, `isolating-flaky-predicates`,
  `nesting-composites`, `new-rule-shape`, `wrapping-a-predicate`. Each
  per-language file (`python.md`/`js.md`/`csharp.md`/`dart.md`) was already
  given its one-line pointer by that language's own PR — verify all four
  actually did it (don't assume) before touching the shared `README.md`
  alongside them if it needs an equivalent, language-agnostic mention of the
  typed-context option existing.

### `docs/samples/`

- Same shape as `docs/extending/` — check each sample dir's shared
  `README.md` for anything that needs the same language-agnostic pointer,
  after confirming all four per-language files got theirs.

### `docs/testing/`

- `docs/testing/README.md` — the canonical 9-contract checklist doesn't
  mention generics at all. Decide whether `Rule<TContext>` introduces any
  *new* cross-language contract worth adding to this list (candidate: "a
  mismatched-context sub-rule is rejected by the type system, every
  language, proven by a compile-fail test" — this was named as a per-language
  test requirement in every language's own plan; whether it graduates to a
  named entry in this shared checklist is this PR's call to make, once all
  four have actually implemented their own version of that test).
- Confirm each language's own `docs/testing/<lang>.md` "which test proves
  which contract" table was updated by that language's PR (C#'s and Dart's
  stale "not yet covered" notes specifically) — verify, don't assume.

### `docs/maintenance/`

- `docs/maintenance/adding-a-language.md` — Stage 4's "the shared graduation
  fixture passes" checkbox item may need a second line once a fifth language
  is ever added, noting the *second* fixture (the new one from this release)
  also needs porting at that stage. Add it here, once the new fixture has a
  real name and shape to reference.
- Re-run the "Before treating a behaviour change as done" sweep from
  `AGENTS.md` itself against this exact release, as a final check — not
  because it's expected to find something, but because that's what the
  checklist is for.

### Root `README.md`

- Check the Status/quickstart sections for any place a dict-only context
  example is presented as the *only* way to use the library — add the
  typed-context option alongside, don't replace.

### `fixtures/`

- `fixtures/<new-name>/README.md` was written in Python's PR (§3 there) —
  confirm it's accurate against what all four languages actually
  implemented, not just what was originally designed; amend if any
  language's implementation legitimately needed to deviate from the
  original spec (and if any did, that's worth a note explaining why, not a
  silent edit).

### Skill

- `skills/verdict/references/*/agent-notes.md` — each language's own PR
  already added its own entry (§5 in each language plan); this PR's job is
  cross-checking, not authoring: confirm all four are consistent in how they
  frame "when to reach for typed vs. dict context" (the same underlying
  guidance, phrased idiomatically per language — not four different, drifted
  opinions).
- `skills/verdict/.claude-plugin/plugin.json` (or wherever the skill's own
  version lives — confirm the exact file) — version bump, since this is a
  real change to the guidance the skill gives, not just an internal refactor.
- `skills/verdict/CHANGELOG.md` — new entry for the version bump above.
- Consider a new eval under `skills/verdict-workspace/evals/` — per
  `AGENTS.md`'s own rule, a behavioural change that introduces a real
  judgment call (typed vs. dict context, which direction an agent could get
  wrong by defaulting to habit either way) "earns an eval." Not mandatory,
  but worth a deliberate yes/no here rather than defaulting to skipping it.

### Every package's `CHANGELOG.md`

- Each language's own `0.3.0` entry was written in that language's PR. This
  PR's job is confirming **each one reads as a standalone, factual
  description of that language's own change** — no reference to the other
  three languages, no "coordinated release" or "joint" language, no
  cross-language narration of any kind (`README.md`'s own principle,
  restated here because it's the one thing this PR could accidentally
  undo while trying to make the four entries "consistent"). Facts that
  should genuinely match across all four (the fixture's actual name, the
  feature itself) may coincide because they describe the same real change —
  that's different from the *text* referencing the other languages, which
  it never should.

## Verification, before calling this PR done

- Every relative link touched or added actually resolves (per this repo's
  own doc-authoring convention) — check, don't assume.
- Every mermaid diagram touched or added passes the `mermaid-diagrams`
  skill's validator.
- Every code sample in every touched doc actually runs, against the real,
  now-generic-supporting installed package in each language — not eyeballed.

## Done when

- Every item above is checked against what the four language PRs actually
  shipped, not what this plan predicted they would ship.
- All four package CHANGELOGs read as standalone, factual, independent
  entries — none referencing the other three languages or this release's
  own coordination.
- Explicit approval received before merge — and only after this PR merges
  does any language's `v0.3.0` tag get cut.
