# AGENTS.md — JavaScript/TypeScript SDK

JS/TS-specific rules, on top of the repo-root [`../AGENTS.md`](../AGENTS.md).
Read that first.

## The guarantees, in JS/TS terms

- **Sequential evaluation.** `AndRule`/`OrRule` delegate to a shared
  `ShortCircuitEvaluator`, itself wrapping `SequentialEvaluator`'s plain
  `for…of` loop with `await`. **Never `Promise.all`.** A custom composite
  composes the same evaluator rather than hand-rolling the loop.
  Short-circuiting only means something if
  later work never *starts*, and the returned boolean is identical either way
  — so this is the one mistake here that passes its own tests.
- **Vacuous truth.** `AndRule([])` passes, `OrRule([])` fails.
- **Emptiness is not absence.** Empty composites fold to their identity;
  unknown rule names and unknown groups throw from `runNamed`/`runGroup`, and
  return `undefined` from `tryRunNamed`/`tryRunGroup`. The `try` forms are the
  **primitives** — the throwing ones are assertions on top, so there is one
  lookup path rather than two that can drift.

  A lookup that **matches** always reports its real verdict, so a caller's
  fallback can never mask a failure. When testing code that uses one, cover the
  *present but failing* case — testing only the absent one looks complete and
  misses the direction where a bug is silent.
- **A predicate returns a `PredicateOutcome`.** `{ passed }` plus an
  optional `detail`/`data` — never a `RuleResult`, which throws. The
  `FunctionRule` wrapping it owns the name and builds the result, so a
  predicate cannot name itself something the rule disagrees with.
- **`RuleResult.data` is opaque.** Never read or written by verdict — it
  carries whatever a predicate attached, unchanged. A composite's children
  live in `subResults`, which holds only what actually ran: never padded to
  the full sub-rule list, never flattened into the parent's level.
  `decidedByIndices` stores the *positions* of the children explaining the
  verdict — positions, not the children themselves, so the stored graph stays
  a tree and a result stays serializable. Everything else is a getter computed
  on access: `decidedBy` indexes those positions into `subResults` and stops
  after one level; `leaves` and `failingLeaves` recurse. `failingLeaves` is an
  **independent recursion, never a filter over `leaves`** — a passing result
  has none, even when a short-circuited branch failed on the way to that pass.
  An out-of-range index is rejected by the constructor with a `RangeError`.

## Layout

`js/` is a **workspace root, not a package** — its `package.json` is private,
carries no `version`, and exists only to declare
`workspaces: ["packages/*", "examples/*"]` and fan scripts out across them.
Everything published lives in `packages/<name>/`, with its manifest, README,
changelog, sources and tests together in that one directory; `examples/*` are
workspaces too, each `private` so nothing there can be published by accident.

A second distribution is therefore a new directory under `packages/`, matched
by the existing glob — nothing at the root is edited to add one. npm keeps a
single `package-lock.json` at the workspace root covering every workspace, so
that file stays here and never inside a package.

Run scripts from the root (`npm test` fans out to every workspace) or scope one
with `npm test -w verdict-rules`.

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

## Distribution shape is effectively permanent

The `exports` map and the files it points at cannot change freely once
published: a consumer's `require()` or `<script src>` that worked at `0.0.1`
has to keep working. Three formats ship from one source tree via
`scripts/build.mjs` — ESM, CJS, and an ES2019 IIFE global for a plain
`<script>` tag or a CDN URL. `unpkg`/`jsdelivr`/`browser` fields point at the
global build. Deliberately no minified build: for a library this size, the
transfer-size saving is mostly erased by gzip/brotli, and it isn't worth
trading against a permanent, visible ding on every consumer's own
supply-chain scan of a package whose whole pitch is decision/eligibility
logic — exactly the audience likely to run one.

`tsc` emits declarations only; esbuild owns every `.js` in `dist/`. Declaration
maps are off because `src/` is not published — esbuild's JS maps embed
`sourcesContent` and are self-contained, which those would not be.

`src/` not shipping matters for more than declaration maps: a Python skill
eval opens the installed package's own source to double-check a signature
already fully documented in
[`references/python/agent-notes.md`](../skills/verdict/references/python/agent-notes.md)
— worth watching for in this language's own
[`evals/`](../skills/verdict-workspace/evals/README.md) too (full
reasoning under
[`csharp/AGENTS.md`](../csharp/AGENTS.md#worth-watching-once-evals-exist-here-no-local-source-to-peek-at)).
What such an agent finds here is worth being precise about, since "the
source ships too" is not quite true for this package: `files` in
`package.json` excludes `src/` outright, so the original, per-file
TypeScript with its own comments never reaches a consumer's `node_modules`.
What *does* ship is `dist/index.js`/`index.cjs` — bundled by esbuild, not
minified (nothing in this package is) — plus the full `.d.ts` declarations.
An agent checking "the real source" here finds a bundled, single-file
artifact rather than this repository's own tree, and the `.d.ts` file alone
is arguably a more targeted way to confirm an exact signature than either.
Sits between Python/Dart (full original source, freely readable) and C# (no
local source at all) on that spectrum.

## Errors are typed, not string-matched

JavaScript has no built-in lookup-error type, so the package exports
`UnknownLookupError` with `kind` and `key` fields. Never throw a bare `Error`
for something a caller might reasonably catch — that forces message matching,
and rewording a message then becomes a breaking change.

## Before calling a change done

```bash
cd js && npm run typecheck && npm run build && npm test
```

Each of those fans out across every workspace. To scope one package:

```bash
npm run build -w verdict-rules
```

## Tests prove behaviour, not just booleans

Short-circuiting is proven with a call log, never the final boolean.
Vacuous-truth polarities and unknown-lookup throws each get their own test.
See the repo-root [`docs/testing/`](../docs/testing/README.md).
