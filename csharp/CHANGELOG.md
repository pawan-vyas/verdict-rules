# Changelog

Version provenance for the C# SDK. The repo-root `CHANGELOG.md` is the
cross-language record.

## 0.0.1

First publish, claiming the name. Correct but minimal: the full type set and
its guarantees, with the tests that prove them, and nothing else yet.

- `IRule`, `FunctionRule`, `AndRule`, `OrRule`, `RulesEngine`, `RuleResult`,
  `RunResult`.
- Sequential, never concurrent evaluation, so short-circuiting is a real
  contract rather than a best-effort optimisation.
- Vacuous-truth polarity decided per composite: `AndRule([])` passes,
  `OrRule([])` fails.
- Unknown rule names and unknown groups throw `KeyNotFoundException` rather
  than returning a vacuous pass.
- `RulesEngine.RuleNames` / `GroupNames` for checking before calling.
- `net8.0` and `netstandard2.1`, trimmable, AOT-compatible, zero runtime
  dependencies.
- `[DebuggerDisplay]` and a debugger type proxy so a nested result tree is
  legible while stepping; SourceLink and `.snupkg` symbols so stepping into the
  package reaches real source.

Not yet included: the shared graduation fixture that every language must pass,
and the documentation set. Those arrive before `0.1.0`.
