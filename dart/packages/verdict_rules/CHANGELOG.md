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

## 0.0.2

- Fixed `pubspec.yaml`'s `repository` field: pointed at this package's
  own subdirectory
  (`https://github.com/pawan-vyas/verdict-rules/tree/main/dart/packages/verdict_rules`)
  rather than the repository root. There is no root `pubspec.yaml` --
  the package lives in a subdirectory of a polyglot monorepo -- so
  pana's "provide a valid pubspec.yaml" check could never find one at
  the bare repository URL and always failed. No other code changed;
  the package itself is identical to `0.0.1` otherwise.

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
