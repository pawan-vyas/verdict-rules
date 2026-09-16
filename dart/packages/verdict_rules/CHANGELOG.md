# Changelog

Release history for the `verdict_rules` Dart package, scoped to this
package — it releases independently of the other language SDKs and of
the AI-agent skill, each of which keeps its own changelog beside its
own manifest. pub.dev parses this file directly and documents whatever
heading matches `## X.Y.Z`, unlike PyPI and npm which follow
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/) but parse
nothing — so the headings below stay in pub.dev's own bare form rather
than that convention's bracketed, dated one.

Tagged `dart-vX.Y.Z`.

## 0.0.3

- **`## Install`'s dependency constraint now reads `^0.0.2`**, matching
  the version this package was actually at since `0.0.2` shipped -- it
  had been left at the original `^0.0.1` and never bumped alongside.
- **`## Install` now also shows the bare import statement**
  (`import 'package:verdict_rules/verdict_rules.dart';`), matching the
  Python package page's own Install section.
- **The first-example heading is now "A first rule"**, not the generic
  "Use" -- the shared package-README template explicitly names a
  generic "Usage"-style heading as the thing to avoid.
- **"Shape-based rules, within what Dart allows" now comes before
  "What it guarantees"**, not after -- it had been placed last,
  contradicting both the template's own section order and where
  Python's and JS's equivalent highlight sections sit on their own
  pages.
- **The `tryRunNamed`/`tryRunGroup` code example moved out of the
  "Emptiness is not absence" guarantee bullet into its own section**
  ("Absence returns null, not a thrown error"), placed alongside the
  other highlight sections before "What it guarantees" -- it had been a
  multi-paragraph, code-containing digression inside what is meant to
  be a short, skimmable bullet list. The guarantee bullet itself is now
  a short pointer to both `ruleNames`/`groupNames` and
  `tryRunNamed`/`tryRunGroup`, matching the length and shape of
  Python's and JS's own equivalent bullets.

## 0.0.2

- Fixed `pubspec.yaml`'s `repository` field: pointed at this package's
  own subdirectory
  (`https://github.com/pawan-vyas/verdict-rules/tree/main/dart/packages/verdict_rules`)
  rather than the repository root. There is no root `pubspec.yaml` --
  the package lives in a subdirectory of a polyglot monorepo -- so
  pana's "provide a valid pubspec.yaml" check could never find one at
  the bare repository URL and always failed.
- `topics` is now `rules-engine`, `rule-evaluation`, `eligibility`,
  `decision-engine`, `async` -- pub.dev caps this field at 5 entries,
  so it carries a curated subset of the full keyword set this project
  uses everywhere else (see
  `docs/maintenance/discoverability-metadata.md`). Dropped `policy`:
  it names a rule together with what happens when it's enforced, and
  this package only ever evaluates, never acts on the result.
- **The structural-typing example swaps `hasQuorum`/`quorum` for
  `isBusinessHours`/`hour`** -- kept distinct from
  [`extending/new-rule-shape/`](https://github.com/pawan-vyas/verdict-rules/blob/dart-v0.0.2/docs/extending/new-rule-shape/README.md)'s
  own `quorum`/`ThresholdRule` vocabulary now that both ship in the
  same repository.
- **`doc/quickstart.md`'s complete example now nests a composite**
  (`AndRule` containing an `OrRule`, one branch of which is itself a
  further `AndRule`) and demonstrates `runGroup` alongside a passing
  and a failing call, rather than repeating the same flat two-rule
  `AndRule` + `runNamed` shape the package README's own first example
  already covers.
- This git tag also carries this package's own doc set for the
  AI-agent skill, for the first time: `docs/testing/dart.md`,
  `docs/architecture/dart.md`, one file per extending scenario, one
  per sample -- repo-level content the skill fetches on demand, not
  part of the published `.tar.gz` itself.

No library code changed; the package's own runtime behavior is
identical to `0.0.1` throughout.

## 0.0.1

First publish, claiming the name. Correct but minimal: the full type set
and its guarantees, with the tests that prove them, and nothing else yet.

- `Rule` (an `abstract interface class`), `FunctionRule`, `AndRule`,
  `OrRule`, `RulesEngine`, `RuleResult`, `RunResult`.
- Sequential, never concurrent evaluation, so short-circuiting is a real
  contract rather than a best-effort optimisation.
- Vacuous-truth polarity decided per composite: `AndRule([])` passes,
  `OrRule([])` fails.
- Unknown rule names and unknown groups throw rather than returning a
  vacuous pass — emptiness folds to an identity, absence is an error.
- `RulesEngine.tryRunNamed` / `tryRunGroup`, returning null rather than
  throwing when nothing matches — the primitives the throwing forms are built
  on. Null means absent, never failed.
- `RulesEngine.ruleNames` / `groupNames` for enumerating an engine.
- Zero runtime dependencies.

Not yet included: the shared graduation fixture that every language must
pass, and the documentation set. Those arrive before `0.1.0`.
