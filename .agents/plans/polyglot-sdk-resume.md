<!-- Title: Polyglot SDK Cross-Language Framing -->
# Cross-language framing

> What holds true for **every** language verdict is ported to,
> independent of which one. Trimmed down to exactly that: the
> per-language detail this file used to carry now lives in each SDK's
> own plan, and the process for getting there lives in
> [`../../docs/adding-a-language.md`](../../docs/adding-a-language.md).
> Kept for now rather than deleted, since the claim below is the thing
> every port is judged against and it belongs somewhere durable.

## The core claim to preserve

Architecturally, nothing about verdict changes across languages: the
same `Rule` / `FunctionRule` / `AndRule` / `OrRule` / `RulesEngine` /
`RuleResult` / `RunResult`, and the same execution-model guarantees.

- **Sequential evaluation — never concurrent.** Short-circuiting is a
  real, reliable contract rather than a best-effort optimisation, which
  means composites must use a plain loop and never the language's
  "run these together" primitive (`Promise.all`, `Task.WhenAll`,
  `Future.wait`). The returned boolean is identical either way, so this
  breaks silently.
- **Vacuous-truth polarity, decided per composite shape.** `AndRule([])`
  passes, `OrRule([])` fails. Deliberately asymmetric, easy to get
  backwards.
- **Emptiness is not absence.** An empty composite folds to its
  identity; an unknown rule name or group **raises**. Permissive about
  arithmetic, strict about lookups.
- **`RuleResult.data` stays opaque** — only what actually ran, never
  padded, never flattened.
- **One adapter module per domain.** A consumer's own vocabulary never
  leaks into a `Rule` implementation; exactly one module imports the
  engine directly.

Only the idiom changes per language. **Judge every SDK against whether
it actually preserves this claim, not whether it feels similar** — and
the mechanical form of that judgement is
[`../../fixtures/graduation_verdict/README.md`](../../fixtures/graduation_verdict/README.md),
which a port must pass before it is called `0.1.0`.

## Where the per-language detail went

| | |
| :-- | :-- |
| Process, stages and version milestones | [`../../docs/adding-a-language.md`](../../docs/adding-a-language.md) |
| JavaScript/TypeScript | `js-sdk/PLAN.md` |
| C# | `csharp-sdk/PLAN.md` |
| Dart | `dart-sdk/PLAN.md` |

Those three are named plainly rather than linked: each lives on its own
SDK development branch and does not exist on `main` until that SDK
ships. Links would be broken here and correct nowhere.

## Design source of truth to port from

Read these fresh in each new language's own context rather than
translating any summary of them:
[`../../docs/architecture.md`](../../docs/architecture.md) for why
evaluation is sequential, why `Rule` is structural, and why
`RuleResult.data` stays opaque; and
[`../../docs/extension.md`](../../docs/extension.md) for the extension
recipes, especially Recipe 3's one-adapter-module boundary.
