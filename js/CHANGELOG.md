# Changelog

Version provenance for the JavaScript/TypeScript SDK. The repo-root
`CHANGELOG.md` is the cross-language record.

## 0.0.1

First publish, claiming the name. Correct but minimal: the full type set and
its guarantees, with the tests that prove them, and nothing else yet.

- `Rule` (a structural `interface`), `FunctionRule`, `AndRule`, `OrRule`,
  `RulesEngine`, `RuleResult`, `RunResult`.
- Sequential, never concurrent evaluation, so short-circuiting is a real
  contract rather than a best-effort optimisation.
- Vacuous-truth polarity decided per composite: `AndRule([])` passes,
  `OrRule([])` fails.
- Unknown rule names and unknown groups throw rather than returning a vacuous
  pass — emptiness folds to an identity, absence is an error.
- `RulesEngine.ruleNames` / `groupNames` for checking before calling.
- ESM only, Node 18+, zero runtime dependencies, types included.

Not yet included: the shared graduation fixture that every language must pass,
and the documentation set. Those arrive before `0.1.0`.
