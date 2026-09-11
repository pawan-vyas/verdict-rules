# Changelog

Each language's own docs always describe the **current** state of that
language's implementation — they're prescriptive guidance, not a
history of how it got there. Version provenance lives here instead.

This repo is polyglot (only Python ships today), and each language
releases independently on its own cadence to its own registry — so
entries are grouped by language-scoped tag (`python-vX.Y.Z` today;
`js-vX.Y.Z`/`csharp-vX.Y.Z` once those languages ship), not one
repo-wide version number.

The AI-agent skill under `skills/verdict/` releases on its own cadence
too, under a `skill-vX.Y.Z` tag tracking
`.claude-plugin/plugin.json`'s version — it describes the guidance, not
any language's API, so it is deliberately unrelated to every
`<language>-vX.Y.Z` number here.

See `docs/maintenance.md`'s "How this package is released and consumed"
section for both release procedures, and each language's own
`AGENTS.md` for what counts as a breaking change in that language.

## skill-v0.1.1 (2026-09-12)

The AI-agent skill under `skills/verdict/`, which versions independently
of any language SDK — see `docs/maintenance.md`.

- **Corrected the account of unknown-group behaviour**, which
  `python-v0.1.1` changed. Three reference files stated the old
  behaviour: `core-concepts.md` as API reference,
  `gotchas.md` as a named pitfall, and `testing-patterns.md` as
  something to write a test for. An agent following any of them would
  have produced code expecting a vacuous pass where a `KeyError` is now
  raised.
- **Added the emptiness-versus-absence distinction** to `gotchas.md` as
  its own section, since it is the reasoning behind the change rather
  than a detail of it: an empty composite folds to its identity because
  the caller handed over a set, while an unknown lookup raises because a
  group exists only when some rule declares it. `testing-patterns.md`
  now asks for both to be tested separately.
- **Documented `RulesEngine.rule_names` and `group_names`** in
  `core-concepts.md`, without which an agent has no way to know that
  checking before calling is an option.

No change to `SKILL.md` itself or to the workflow it describes.

## python-v0.1.1 (2026-09-12)

- **`RulesEngine.run_group()` now raises `KeyError` for an unknown
  group**, matching `run_named()`. It previously returned a `RunResult`
  with zero results and `passed=True`. That path had no valid use: a
  group exists exactly when some rule declares it, so a lookup matching
  nothing always means the group is unknown, never that it is
  legitimately empty — and a misspelled group name silently passing is
  the worst failure mode for an eligibility or access-control caller.
  Released as a patch under the pre-1.0 carve-out documented in
  `docs/maintenance.md`.
- **Added `RulesEngine.rule_names` and `RulesEngine.group_names`.**
  Read-only tuples of the registered names, so a caller who cannot know
  in advance whether a name exists can check rather than catch.
- Empty composites are unchanged and deliberately so: `AndRule([])`
  still passes and `OrRule([])` still fails, being the identities of the
  folds they perform. `docs/architecture.md` now states the distinction
  as a design position — this package is permissive about emptiness and
  strict about absence.

## python-v0.1.0 (2026-09-07)

Initial public release.

- Core engine: `Rule` (structural `Protocol`), `FunctionRule`,
  `AndRule`, `OrRule`, `RulesEngine`, `RuleResult`, `RunResult`.
  Sequential (never concurrent) evaluation, real short-circuiting,
  vacuous-truth polarities decided explicitly per composite shape.
- Zero external dependencies; `requires-python = ">=3.10"`.
- Full doc suite (`docs/architecture.md`, `extension.md`,
  `maintenance.md`, `testing.md`, `future_plan.md`), worked samples
  (`python/docs/samples/`), and a full tested example project
  (`python/examples/graduation_verdict/`, including a 500-case
  differential/chaos test suite against an independent oracle).
- An AI-agent skill (`skills/verdict/`) with the marketplace-vendoring
  model (`.claude-plugin/`, `scripts/{install,build,get}.sh`) for
  following verdict's own conventions when building with or extending
  it, in any harness.
