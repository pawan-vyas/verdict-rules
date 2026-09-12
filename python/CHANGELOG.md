# Changelog

Release history for the `verdict-rules` Python package. Format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/); versioning follows
[semantic versioning](https://semver.org/), scoped to this package — it releases
independently of the other language SDKs and of the AI-agent skill, each of
which keeps its own changelog beside its own manifest.

Tagged `python-vX.Y.Z`.

## [0.2.0] - 2026-09-12

- **Added `RulesEngine.try_run_named()` and `try_run_group()`**, which
  return `None` instead of raising when nothing matches. `None` means
  *absent*, never *failed* — a rule that exists and fails is still a
  `RuleResult` with `passed=False`.

  These exist because `0.1.1` made unknown lookups raise, and while that
  is the right default it is not right for every caller. Absence is a
  legitimate, expected state in several real situations — a rule set
  that varies per tenant, an optional group behind a feature flag, a
  name in configuration a deployment has not adopted yet — and what it
  should *mean* differs per consumer. The engine cannot know whether an
  absent group means "no constraint applies" or "the configuration is
  broken", so it hands the decision back rather than guessing.

- **`run_named()` and `run_group()` are unchanged in behaviour**, and are
  now implemented as two-line assertions on top of the `try_` forms.
  There is one lookup path rather than two implementations that could
  drift. Prefer them by default: a `KeyError` in development is a typo
  found in seconds, whereas the same typo behind
  `try_run_group(...) or default_pass` is a rule set that silently
  stopped being enforced.

- `docs/extension.md` gains **Recipe 6**, working through the four real
  shapes absence takes and why a library default would be wrong for
  three of them.

- The shared fixture now pins both halves for both lookups, so no
  language port can ship the strict form without the lenient one.

## [0.1.1] - 2026-09-12

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

## [0.1.0] - 2026-09-07

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
