# Changelog

Release history for the verdict AI-agent skill. Format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

This versions **the guidance**, not any language's API, so it moves on its own
cadence — see `docs/maintenance.md`. Tagged `skill-vX.Y.Z`.

## [0.3.2] - 2026-09-13

No guidance changed.

- **`MANIFEST`'s fetch-tier source paths and the curl recipe in
  `references/python/agent-notes.md`** now point at
  `python/packages/verdict-rules/docs/...`, matching the Python
  distribution-root restructure. Both previously pointed at
  `python/docs/...`.

## [0.3.1] - 2026-09-13

- **States plainly, for the first time, that a predicate's own exception
  is never caught** — unchanged behavior, newly documented. Added to
  Step 2's list alongside sequential evaluation, vacuous-truth polarity,
  emptiness-vs-absence, and result opacity — the same kind of fact that
  produces silently-wrong code if assumed otherwise, since "engine" invites
  the opposite assumption from what this package actually does.
- **`extension.md` gains Recipe 7**, the wrapper that isolates one flaky
  predicate's exception from the rest of a run without changing anything
  about how the engine itself behaves — bundled automatically, no
  MANIFEST change needed.

## [0.3.0] - 2026-09-12

Re-architected so the skill carries the engine's own documentation rather
than restating it, and so adding a language touches nothing shared.

- **The reference files are now the repository's own documents.**
  `architecture.md` and `extension.md` are copied verbatim into the
  bundle by `scripts/build.sh`, from a hand-authored `MANIFEST`. Six
  hand-written files per language become one. They previously restated
  the same facts in compressed form, which is why they went stale twice —
  once when `run_group` changed, once when `try_run_*` was added.
- **`SKILL.md` names no language.** It routes to
  `references/<language>/agent-notes.md`, so each language owns one
  directory and a new SDK is purely additive. Its description no longer
  says "if/elif", which was Python syntax in a polyglot skill.
- **Deeper material is fetched on demand**, pinned to the version the
  consuming project actually has installed — the testing guide, the
  quickstart, the samples and the worked example. A `/verdict-fetch-docs`
  command does it where a harness has commands; `agent-notes.md` carries
  the equivalent recipe where it does not. Fetching from the default
  branch is explicitly refused: documentation for a version a project
  does not have is worse than none.
- **`when-to-extend-the-core.md` is gone.** It duplicated
  `docs/future_plan.md` almost verbatim, and was maintainer guidance
  living in a consumer artifact — which is why maintainer-facing edits
  kept forcing skill releases consumers gained nothing from.
- `scripts/check_skill_bundle.py` validates the assembled bundle: that
  `SKILL.md` routes to nothing the manifest omits, and that links
  between bundled documents resolve where the manifest put them.

Hand-written skill content drops from 788 lines to 248.

## [0.2.1] - 2026-09-12

- **`testing-patterns.md` now asks for the whole fallback matrix**, not
  just the absent case. Testing `try_run_group` only against a missing
  group looks complete and is not: the dangerous case is a *present*
  group wrongly returning `None`, which makes a caller's default fire
  and approve something that actually failed.
- **`gotchas.md` gains the Python-specific `or` footgun.** Other
  languages write this with `??`, which fires only on null; Python's
  `or` fires on any falsy value. It works here only because the result
  types are always truthy — a property of this library rather than of
  the pattern — so the explicit `if x is not None` form is what the
  skill now recommends.

## [0.2.0] - 2026-09-12

- Documents `try_run_named`/`try_run_group` in `core-concepts.md`, and
  the framing that matters: they are the primitives, and the raising
  forms are assertions on top.
- `gotchas.md` now warns against reaching for the `try_` forms to avoid
  thinking about absence, which is the way they will be misused.
- `testing-patterns.md` asks for both halves to be tested, and for
  `None` to be distinguished from `passed=False`.

## [0.1.1] - 2026-09-12

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
