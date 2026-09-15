# Changelog

Release history for the `verdict-rules` JS/TS package. Format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/); versioning follows
[semantic versioning](https://semver.org/), scoped to this package — it
releases independently of the other language SDKs and of the AI-agent skill.

## [0.0.2] - 2026-09-15

- **The npm landing page's first example now demonstrates `RulesEngine`**,
  wrapping the rule and running it by name instead of stopping at the
  composite's own `.evaluate()` — `RulesEngine` is one of the five named
  primitives, and the first example is meant to show all of them.
  `package-readmes.md`'s own authoring template now mandates this for
  every language's first example and `docs/quickstart.md`'s worked
  example alike, so the two never demonstrate a different subset of the
  API from each other.
- **Drops the `## Development` section.** It duplicated a subset of
  `CONTRIBUTING.md`'s own JS/TS section on a page a consumer landed on
  to install the package, not to change it — `package-readmes.md` no
  longer mandates this section for any language.
- **Cuts the blockquote to a single fact.** The second sentence pointed
  to the top-level `README.md` while narrating what this file itself
  was for — the document talking about itself, the same failure
  `package-readmes.md` already names for every other doc in this repo.
  That pointer moves to `## Where to go next`'s own first row instead.

## [0.0.1] - 2026-09-14

Initial publish.

- `Rule` (a structural `interface`), `FunctionRule`, `AndRule`, `OrRule`,
  `RulesEngine`, `RuleResult`, `RunResult`.
- Sequential, never concurrent evaluation, so short-circuiting is a real
  contract rather than a best-effort optimisation.
- Vacuous-truth polarity decided per composite: `AndRule([])` passes,
  `OrRule([])` fails.
- Unknown rule names and unknown groups throw rather than returning a vacuous
  pass — emptiness folds to an identity, absence is an error.
- `RulesEngine.tryRunNamed` / `tryRunGroup`, returning `undefined` rather than
  throwing when nothing matches — the primitives the throwing forms are built
  on. `undefined` means absent, never failed.
- `RulesEngine.ruleNames` / `groupNames` for enumerating an engine.
- `UnknownLookupError`, carrying `kind` and `key`, so an unknown lookup is
  catchable by type rather than by matching message text.
- Ships as ESM, CommonJS, and an ES2019 global bundle for a plain `<script>`
  tag or a CDN URL. Node 18+, zero runtime dependencies, types included.
