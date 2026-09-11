# AGENTS.md — JavaScript/TypeScript SDK

JS/TS-specific rules, on top of the repo-root `AGENTS.md`. Read that first.

## The guarantees, in TS terms

- **Sequential evaluation.** `AndRule`/`OrRule` use a plain `for…of` loop with
  `await`. **Never `Promise.all`.** Short-circuiting only means something if
  later work never *starts*, and the returned boolean is identical either way
  — so this is the one mistake here that passes its own tests.
- **Vacuous truth.** `AndRule([])` passes, `OrRule([])` fails.
- **Emptiness is not absence.** Empty composites fold to their identity;
  unknown rule names and unknown groups throw.
- **`RuleResult.data`** holds only what actually ran. Never padded, never
  flattened into the parent's level.

## Conventions

- `Rule` stays a **structural `interface`**, never an abstract class. The point
  is that a plain object of the right shape is already a rule — the same
  property Python's `Protocol` gives. Do not add a base class or a registration
  step.
- `Context` is `Record<string, unknown>`, never `any`.
- ESM only (`"type": "module"`). Node 18+.
- **Zero runtime dependencies.** `devDependencies` are fine.
- `exports` is declared as a map from the start, so a future
  `verdict-rules/extensions` subpath is additive rather than a restructure.
- `strict`, plus `noUncheckedIndexedAccess` and `exactOptionalPropertyTypes`.

## Before calling a change done

```
cd js && npm run typecheck && npm run build && npm test
```

## Tests prove behaviour, not just booleans

Short-circuiting is proven with a call log, never the final boolean.
Vacuous-truth polarities and unknown-lookup throws each get their own test.
See the repo-root `docs/testing.md`.
