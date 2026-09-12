# Changelog

Version provenance for the Dart SDK. The repo-root `CHANGELOG.md` is the
cross-language record; pub.dev requires this file to live alongside the
package, so it mirrors this language's own entries.

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
