# AGENTS.md — Dart SDK

Dart-specific rules, on top of the repo-root `AGENTS.md`. Read that first;
this file only adds what is particular to this language.

## The guarantees, in Dart terms

- **Sequential evaluation.** `AndRule`/`OrRule` use a plain `for` loop with
  `await`. **Never `Future.wait`.** Short-circuiting only means something if
  later work never *starts*, and the returned boolean is identical either way
  — so this is the one mistake here that passes its own tests.
- **Vacuous truth.** `AndRule([])` passes, `OrRule([])` fails.
- **Emptiness is not absence.** Empty composites fold to their identity;
  unknown rule names and unknown groups throw `ArgumentError` from
  `runNamed`/`runGroup`, and return null from `tryRunNamed`/`tryRunGroup`.
  The `try` forms are the **primitives** — the throwing ones are assertions on
  top, so there is one lookup path rather than two that can drift.

  A lookup that **matches** always reports its real verdict, so a caller's
  fallback can never mask a failure. When testing code that uses one, cover the
  *present but failing* case — testing only the absent one looks complete and
  misses the direction where a bug is silent.
- **`RuleResult.data`** holds only what actually ran. Never padded to the full
  sub-rule list, never flattened into the parent's level.

## Be precise about structural typing

Do not write that "Dart has no structural typing." It does, for **function
types** — any function matching the predicate signature is a rule through
`FunctionRule`, with nothing declared. A tear-off works directly.

What Dart lacks is structural typing for a **multi-member interface**: an
object carrying `name`, `group` and `evaluate` is not thereby a `Rule`, where
Python's `Protocol` and TypeScript's structural interfaces would accept it.
That narrow difference is the honest statement.

## Layout

`dart/` holds *distributions* rather than being one: the published package is
`packages/verdict_rules/`, with its pubspec, README, changelog, `lib/`, `test/`
and `example/` together. A second package is a new sibling directory under
`packages/` and nothing existing moves.

**There is deliberately no root `pubspec.yaml` yet.** Pub workspaces are the
matching convention, but they require an SDK floor of `^3.6.0`, and this
package targets `>=3.0.0` to stay reachable. Raising the floor to gain shared
resolution across a single package would cost reach and buy nothing. The
directory layout is adopted now — it is the part that makes a second package
additive — and the workspace root is added the day a second package actually
exists, which is also the day the shared lockfile starts paying for itself.

Until then, run every command from inside the package directory rather than
from `dart/`.

## Conventions

- `Rule` is an `abstract interface class` — implemented, never extended.
- Context is `Map<String, Object?>`, never `Map<String, dynamic>`. `dynamic`
  disables type checking; `Object?` still accepts a map straight out of
  `jsonDecode` without a cast.
- lowerCamelCase throughout: `runAll`, `runNamed`, `runGroup`, `ruleNames`,
  `groupNames`, `ruleName`.
- **Zero runtime dependencies.** `dev_dependencies` are fine.
- Public API lives in `lib/verdict_rules.dart`; everything else is `lib/src/`.
- Keep `example/` runnable. pub.dev scores its presence and surfaces it on the
  package page, so it is the first code most readers see — not an afterthought.

## Before calling a change done

```
cd dart/packages/verdict_rules && dart analyze && dart test
```

`dart analyze` must report no issues — `analysis_options.yaml` enables
strict casts, inference and raw types deliberately.

For a release, also `dart pub publish --dry-run` and expect zero warnings.

## Tests prove behaviour, not just booleans

Short-circuiting is proven with a call log, never with the final boolean.
Vacuous-truth polarities and unknown-lookup throws each get their own test.
See the repo-root `docs/testing.md`.
