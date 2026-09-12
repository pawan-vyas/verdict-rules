# AGENTS.md — JavaScript/TypeScript SDK

JS/TS-specific rules, on top of the repo-root `AGENTS.md`. Read that first.

## The guarantees, in TS terms

- **Sequential evaluation.** `AndRule`/`OrRule` use a plain `for…of` loop with
  `await`. **Never `Promise.all`.** Short-circuiting only means something if
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
- **`RuleResult.data`** holds only what actually ran. Never padded, never
  flattened into the parent's level.

## Layout

`js/` is a **workspace root, not a package** — its `package.json` is private,
carries no `version`, and exists only to declare `workspaces: ["packages/*"]`
and fan scripts out across them. Everything published lives in
`packages/<name>/`, with its manifest, README, changelog, sources and tests
together in that one directory.

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
has to keep working. Four formats ship from one source tree via
`scripts/build.mjs` — ESM, CJS, an ES2019 IIFE global for a plain `<script>`
tag, and a minified global for CDN URLs. `unpkg`/`jsdelivr`/`browser` fields
point at the global build.

`tsc` emits declarations only; esbuild owns every `.js` in `dist/`. Declaration
maps are off because `src/` is not published — esbuild's JS maps embed
`sourcesContent` and are self-contained, which those would not be.

`src/` not shipping matters for more than declaration maps: a Python skill
eval was seen opening the installed package's own source to double-check a
signature already fully documented in `agent-notes.md` — worth watching for
whether an agent does the same here once this language has its own evals
(full note on `csharp/AGENTS.md`). What such an agent would actually find is
worth being precise about, since "the source ships too" is not quite true
for this package: `files` in `package.json` excludes `src/` outright, so the
original, per-file TypeScript with its own comments never reaches a
consumer's `node_modules`. What *does* ship is `dist/index.js`/`index.cjs` —
bundled by esbuild, not minified (only the two CDN/IIFE builds are) — plus
the full `.d.ts` declarations. An agent checking "the real source" here
would find a bundled, single-file artifact rather than this repository's own
tree, and the `.d.ts` file alone is arguably a more targeted way to confirm
an exact signature than either. Sits between Python/Dart (full original
source, freely readable) and C# (no local source at all) on that spectrum.

## Errors are typed, not string-matched

JavaScript has no built-in lookup-error type, so the package exports
`UnknownLookupError` with `kind` and `key` fields. Never throw a bare `Error`
for something a caller might reasonably catch — that forces message matching,
and rewording a message then becomes a breaking change.

## Before calling a change done

```
cd js && npm run typecheck && npm run build && npm test
```

Each of those fans out across every workspace. To scope one package:

```
npm run build -w verdict-rules
```

## Tests prove behaviour, not just booleans

Short-circuiting is proven with a call log, never the final boolean.
Vacuous-truth polarities and unknown-lookup throws each get their own test.
See the repo-root `docs/testing.md`.
