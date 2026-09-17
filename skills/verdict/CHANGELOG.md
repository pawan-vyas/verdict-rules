# Changelog

Release history for the verdict AI-agent skill. Format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

This versions **the guidance**, not any language's API, so it moves on its own
cadence — see `docs/maintenance/releases/verdict-agent-skill.md`. Tagged `skill-vX.Y.Z`.

## [0.5.9] - 2026-09-17

- **Dart's `references/dart/agent-notes.md` install snippet fixed** --
  was a caret-pinned `pubspec.yaml` dependency block (`^0.0.2`, already
  stale by the time this was caught -- Dart had shipped 0.0.3), matching
  a real staleness risk found live in three places this same day. Now
  `dart pub add verdict_rules`, matching pub.dev's own real "Installing"
  tab for this package (checked directly), which resolves to whatever
  is current automatically -- removing the drift risk instead of just
  fixing this one instance of it. Also notes it works unchanged in a
  Flutter project (`flutter pub add verdict_rules` there).

## [0.5.8] - 2026-09-16

- **C# lands as a shipped language.** `references/csharp/agent-notes.md`
  -- install/import, the full API in one screen (including the
  `CancellationToken cancellationToken = default` every async method
  carries, checked between sub-rules by every composite and by
  `RulesEngine`'s own run methods), mistakes specific to C# (forgetting
  `: IRule`, `Task.WhenAll` in a composite, catching
  `KeyNotFoundException` where `RuleNames`/`GroupNames` should be
  reached for instead, an already-`Func<...>`-typed value not
  implicitly converting to `RulePredicate`, a method group or lambda
  missing the trailing `CancellationToken` parameter needed to satisfy
  `RulePredicate`'s arity), testing checklist, and the fetch recipe.
  `MANIFEST.toml` gains `"csharp"` in `languages` (every existing
  `fetch_group` topic becomes fetchable for C# now that its own
  `<topic>/csharp.md` exists for every topic except the Python-only
  `graduation-requirement-verdict` sample) and C#'s own quickstart
  `[[fetch]]` entry.

## [0.5.7] - 2026-09-16

- **`MANIFEST.toml` gains two `fetch_group` entries**, for two new
  sample scenarios demonstrating UI-side capability --
  `docs/samples/signup-form-readiness/` (an `AndRule` gating a form's
  submit control, re-evaluated live) and
  `docs/samples/premium-upsell-panel/` (an `OrRule` gating a UI panel,
  with a genuinely async branch and a visible short-circuit saving).
  Both are JS/TS and Dart only today -- the same "an unresolved fetch
  404s, expected, not an error" handling as any other per-language
  gap, no special-case needed.

## [0.5.6] - 2026-09-15

- **Dart lands as a shipped language.** `references/dart/agent-notes.md`
  -- install/import, the full API in one screen, mistakes specific to
  Dart (forgetting `implements Rule`, `Future.wait` in a composite,
  `RuleResult.detail` being a non-nullable `String`, `ArgumentError`'s
  genericity compared to JS's `UnknownLookupError` or Python's
  `KeyError`), testing checklist, and the fetch recipe. `MANIFEST.toml`
  gains `"dart"` in `languages` (every existing `fetch_group` topic
  becomes fetchable for Dart once its own `<topic>/dart.md` exists --
  none do yet) and Dart's own quickstart `[[fetch]]` entry.

## [0.5.5] - 2026-09-15

- **`MANIFEST.toml`'s fetch tier no longer hand-duplicates one row per
  language per topic.** Every topic that fetches a spec plus each
  language's own implementation (testing, every extending scenario,
  every sample) is now a single `[[fetch_group]]` — a `readme` and a
  `{lang}`-templated `pattern` — expanded against a new top-level
  `languages` list. A language with no file for a given topic yet is
  skipped rather than erroring, so a topic doesn't have to land in
  every language at once. Shipping a new language now means adding it
  to `languages` once, not editing all fourteen topic blocks — the
  file that used to grow by two lines per language per topic (a shared
  file every language's own branch had to write new rows into, the
  exact shape that caused two real rebase conflicts already this
  project) now grows by one line, total, per language. Nothing an
  agent fetches changed: `scripts/skill_manifest.py`'s `rows("fetch",
  ...)` returns the identical (source, destination) pairs as before,
  confirmed by diffing its output against the pre-change manifest.
- **`commands/verdict-fetch-docs.md` now describes both manifest
  shapes.** It previously told an agent to fetch each `[[fetch]]`
  entry only — since the shipped `MANIFEST.toml` is read directly by a
  consumer's own agent, not through this repository's Python parser,
  an agent following the old instructions literally would have
  silently skipped every `[[fetch_group]]` topic once this schema
  shipped. Fixed before it ever shipped that way.
- `references/python/agent-notes.md` and `references/js/agent-notes.md`
  updated to match: the manifest holds two entry shapes now, one
  needing `{lang}` substitution before fetching.

## [0.5.4] - 2026-09-15

- **`references/python/agent-notes.md`'s testing section names
  property-based and oracle/differential testing**, matching the fix
  already made to the JS/TS reference — Python's own file had the
  identical gap, since both predate the pattern being named at all.
  Points at `python/examples/graduation_verdict/` as the real, shipped
  instance.

## [0.5.3] - 2026-09-15

- **`references/js/agent-notes.md`'s testing section names property-based
  and oracle/differential testing** for a rule set built from stored
  configuration at runtime, alongside the contracts it already named
  (short-circuit proofs, vacuous-truth polarities, the present-but-failing
  fallback case). Found via a real eval run: an agent satisfied every
  contract already named here but reached for one more hand-picked
  fixture instead of systematic coverage for the one dimension this
  section didn't name — the guidance existed already, but only in
  fetch-tier docs an agent correctly won't fetch before a real release
  tag exists.

## [0.5.2] - 2026-09-15

- **The skill routes to JS/TS for the first time.** Adds
  `references/js/agent-notes.md`, matching Python's own depth: install
  and import, the full API on one screen, mistakes specific to
  generated JS/TS code (`Promise.all` in a composite, `||`'s
  falsy-coercion footgun where Python's `or` was saved by its result
  types happening to be truthy but this one is not, catching
  `UnknownLookupError` where `ruleNames`/`groupNames` should be reached
  for instead), and the testing checklist.
- Documents that a language's evals can run before that language's own
  first registry publish, and how the fixture manifest's pinned version
  is meant to be read in that case (`evals/README.md`).
- **JS/TS gets its own concrete architecture and testing docs**:
  `docs/architecture/js.md` ships automatically (the whole directory is
  bundled) and `docs/testing/js.md` is a new fetch-tier row, both at
  the same depth as Python's own. `references/js/agent-notes.md`
  corrected: it claimed `docs/extension.md` was bundled already, which
  was true when first written but not after this session's `docs/extending/`
  split moved it to fetch-tier.
- **JS/TS gets its own quickstart**,
  `js/packages/verdict-rules/docs/quickstart.md`, a new fetch-tier row
  matching Python's own. `references/js/agent-notes.md`'s own fetch
  recipe updated to match — it previously said this language had no
  quickstart or samples yet, which had already stopped being true
  earlier in this same entry.

## [0.5.1] - 2026-09-15

No guidance changed.

- **`plugin.json`'s and `marketplace.json`'s own descriptions drop "only
  Python ships today."** The clause was correct when written and would
  have gone stale the moment a second language actually shipped, on a
  page neither file can retroactively edit once a marketplace caches
  it. "Polyglot design" alone states the fact that stays true regardless
  of which languages exist yet.

## [0.5.0] - 2026-09-13

- **`docs/architecture.md` (bundled) is now `docs/architecture/`, a
  directory: `README.md` carries the shared, language-agnostic design;
  each language gets its own concrete file alongside it (`python.md`
  today). The manifest's bundled entry copies the whole directory, so a
  future language's own file ships automatically with no manifest edit.
  `SKILL.md`'s routing table points at the new path.
- **Sample docs are now one directory per scenario**,
  `docs/samples/<scenario>/README.md` (the language-agnostic spec) plus
  `<language>.md` (that language's implementation) alongside it — no
  numbering, no per-language samples directory scattered inside that
  language's own package tree. All 7 scenarios now have a spec, read
  once and implemented per language. The manifest's fetch tier was
  rewritten to match: each scenario fetches its spec and Python
  implementation as a pair, preserving the directory shape so the
  implementation doc's own same-directory link to its spec still
  resolves once fetched.
- **`graduation_verdict`'s docs were untangled from being partly
  Python-only.** Its language-agnostic design now lives at
  `docs/samples/graduation-requirement-verdict/`, same as every other
  scenario; its data contract stays at `fixtures/graduation_verdict/`,
  now with the "how to extend the curriculum" guidance folded in. The
  Python implementation's own code and operational testing doc stay in
  `python/examples/graduation_verdict/`, unmoved — only the docs that
  were duplicating the spec moved out. A new
  `docs/maintenance/adding-a-fixture.md` documents this shape for a
  future cross-language fixture, not shipped (maintainer document).
- **`docs/maintenance.md` (never shipped) is now `docs/maintenance/`**,
  split into one focused, keyword-named file per concern, plus a
  `releases/` directory following the same shared-plus-per-target
  pattern as the architecture split. Not bundled or fetched — this
  entry exists because `extension.md`'s and `architecture/README.md`'s
  own links into it changed shape.
- **`docs/extension.md` (bundled) is now `docs/extending/`**, one
  directory per extension scenario ("recipes" renamed to "scenarios"
  throughout), each with a language-agnostic spec (`README.md`) and,
  where one exists, a concrete per-language file (`python.md` today) —
  the same shape as the samples split above. No longer bundled: seven
  scenarios is too much to ship on every install, so the manifest's
  single bundled entry became one fetch-tier pair per scenario,
  preserving the directory shape the same way the samples entries do.
  `SKILL.md`'s routing and its predicate-isolation example both point
  at the new paths.
- **Every `docs/extending/` and `docs/samples/` fetch entry's
  destination now mirrors its source path**, landing at
  `references/docs/extending/...` and `references/docs/samples/...`
  instead of a `references/python/...` alias — these are
  language-agnostic docs, the same status `docs/testing.md` already
  had, so they get the same treatment: one destination, read by every
  language's agent, with no second entry needed when a new language
  ships. Only a destination whose *source* is itself language-specific
  (the quickstart, the example project) still aliases to a shorter
  `references/python/...` path.
- **`docs/testing.md` is now `docs/testing/`**, the last doc still
  bundling one language's specifics into an otherwise-shared file:
  `README.md` carries the seven testing contracts and the checklist,
  generically; `python.md` carries the current coverage snapshot, the
  concrete file layout, and a table naming which test proves which
  contract. The manifest's single fetch entry becomes a pair, both
  destinations mirroring their source paths per the rule above.

## [0.4.0] - 2026-09-13

- **`extension.md`'s Recipe 4 now names the testing bar explicitly.**
  "As the rule set grows more varied, testing needs to scale with it —
  property-based or oracle/differential testing over a wide space, not
  hand-picked fixtures one at a time." This is bundled-tier, so it ships
  regardless of a consumer's own release-tag state — `testing.md`, which
  carries the same guidance in full depth, is fetch-tier and can be
  unreachable before a language's own first tag exists.
- **The manifest is now `MANIFEST.toml`**, not a hand-rolled
  tab-separated file. Same two tiers, same fields, machine-parsable
  without a custom parser — read via stdlib `tomllib` (Python 3.11+),
  through one shared module (`scripts/skill_manifest.py`) instead of two
  independent hand-rolled readers. No content moved; nothing a consumer
  reads changed shape.

## [0.3.4] - 2026-09-13

No guidance changed.

- **`.claude-plugin/marketplace.json`'s own `description` corrected to
  plain ASCII** — the same em-dash the 0.3.3 fix missed, sitting in a
  second file that duplicates part of `plugin.json`'s own description.

## [0.3.3] - 2026-09-13

No guidance changed.

- **`description` field corrected to plain ASCII** — it carried a
  JSON-escaped em-dash (`—`) in two places. Every conformant parser
  reads it identically either way; corrected for readability of the raw
  file, not for behavior.

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
