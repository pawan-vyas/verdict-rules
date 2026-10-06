<!-- Title: Verdict Examples (JS/TS) -->
# Verdict — Examples: JS/TS

> Full, tested mini-projects behind verdict's shared fixtures — real code
> with its own test suite and docs, not just markdown snippets. Each one is
> this language's port of a scenario whose problem, design, and expected
> outcomes live in [`../../fixtures/README.md`](../../fixtures/README.md),
> data only; every language ships the same two under its own `examples/`.

| Project | What it demonstrates |
| --- | --- |
| [`graduation_verdict/`](graduation_verdict/README.md) | The full breadth of the engine at once — `FunctionRule`/`AndRule`/`OrRule`, a custom `Rule` shape, all three `RulesEngine` run modes, and data-driven rule construction — against a graduation-eligibility policy. Also doubles as an integration/e2e regression test for verdict itself; see the shared [fixture contract](../../fixtures/graduation_verdict/README.md) for how to extend the curriculum. |
| [`marketplace_eligibility/`](marketplace_eligibility/README.md) | `Rule<TContext>`'s generic-context design — two typed contexts sharing no fields, one rule reused across both via a projecting adapter, and a dict-context catalog coexisting in the same codebase. See the shared [fixture contract](../../fixtures/marketplace_eligibility/README.md) for the data every language's own port asserts against. |

Run either with `npm test --workspace=@verdict-rules/example-graduation-verdict`; both are npm workspaces of `js/`.
