# Changelog

Release history for the `verdict-rules` JS/TS package. Format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/); versioning follows
[semantic versioning](https://semver.org/), scoped to this package — it
releases independently of the other language SDKs and of the AI-agent skill.

## [0.0.5] - 2026-09-16

- **The structural-typing example swaps `overEighteen`/`age` for
  `isBusinessHours`/`hour`** -- kept distinct from the main example's
  own `age` field, which already lives on the same page.
- **`docs/quickstart.md`'s complete example now nests a composite**
  (`AndRule` containing an `OrRule`, one branch of which is itself a
  further `AndRule`) and demonstrates `runGroup` alongside a passing
  and a failing call, rather than repeating the same flat two-rule
  `AndRule` + `runNamed` shape the package README's own first example
  already covers. Package identical to `0.0.4` otherwise.

## [0.0.4] - 2026-09-15

- **`keywords` drops `policy` and adds `decision-engine` and `async`**,
  now `rules-engine`, `rule-evaluation`, `eligibility`, `decision`,
  `decision-engine`, `async`. `policy` names a rule together with what
  happens when it's enforced -- this package only ever evaluates and
  never acts on the result, so a "policy" keyword invited a search for
  something this package doesn't do. `decision` stays: it's this
  package's own output, a judgment reached, not an act of enforcing
  anything. Package identical to `0.0.3` otherwise.

## [0.0.3] - 2026-09-15

- **Drops the "Where to go next" row pointing back at the top-level
  `README.md`.** `docs/architecture/` is already linked in the same
  table and already covers "why it's shaped this way," in more depth
  than a narrative pointer would — a second row saying the same thing
  again was dead weight, not a different fact.

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
