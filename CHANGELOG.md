# Changelog

Each language's own docs always describe the **current** state of that
language's implementation — they're prescriptive guidance, not a
history of how it got there. Version provenance lives here instead.

This repo is polyglot (only Python ships today), and each language
releases independently on its own cadence to its own registry — so
entries are grouped by language-scoped tag (`python-vX.Y.Z` today;
`js-vX.Y.Z`/`csharp-vX.Y.Z` once those languages ship), not one
repo-wide version number. See `docs/maintenance.md`'s "How this package
is released and consumed" section for the release procedure and each
language's own `AGENTS.md` for what counts as a breaking change in that
language.

## python-v0.1.0 (unreleased)

Prepared, not yet tagged — the first tag/publish happens once
`release-python.yml` exists to respond to it, per the release procedure
in `docs/maintenance.md`. Once tagged, this section becomes
`## python-v0.1.0` with the tag's actual date.

- Initial extraction from a private monorepo into this standalone,
  publicly-published repository.
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
