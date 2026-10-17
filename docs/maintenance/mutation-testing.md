<!-- Title: Mutation Testing -->
# Mutation testing

> Why line coverage alone doesn't prove a test suite catches anything,
> which real tool each language uses, and what a surviving mutant means
> and requires.

## Why this exists

Line coverage proves a test *ran* a line, never that it would notice
the line being wrong. A mutation tool rewrites the real source in small,
deliberate ways (flips a `&&` to `||`, changes `<` to `<=`, deletes a
statement) and reruns the test suite against each mutant. A mutant the
suite still passes against is a **survivor** — concrete evidence of a
code change the existing tests would not catch.

## Per-language tool, each real and already current

No hand-rolled mutator here -- each language already has a maintained
tool built for exactly this:

| Language | Tool | Runs against |
| :-- | :-- | :-- |
| C# | [Stryker.NET](https://stryker-mutator.io/docs/stryker-net/introduction/) (`dotnet-stryker`) | `csharp/src/VerdictRules/` via `dotnet test` |
| Python | [mutmut](https://github.com/boxed/mutmut) | `python/packages/verdict-rules/src/verdict/` via `pytest` |
| JS/TS | [StrykerJS](https://stryker-mutator.io/docs/stryker-js/guides/nodejs/) | `js/packages/verdict-rules/src/` via its TAP runner, consuming `node --test`'s own TAP output (StrykerJS ships no first-class `node:test` plugin, but a dedicated TAP runner plugin has existed since v7.0 and explicitly covers the built-in test runner this way) |
| Dart | [`mutation_test`](https://pub.dev/packages/mutation_test) | `dart/packages/verdict_rules/lib/` via `dart test`, zero-config. A regex-based text mutator rather than an AST-aware one, so its reach is much narrower than the other three -- see [`mutation-survivors-dart.md`](mutation-survivors-dart.md) before reading its score as comparable |

## Not a required CI gate

A mutation run reruns the whole test suite once per mutant -- genuinely
slow, and its own score moves slowly release to release. Each language
has its own `scripts/run_mutation_<lang>.sh` (or equivalent), run
on-demand or on a schedule, never blocking a PR.

## A surviving mutant needs one of two things

1. **A new test that kills it.** The normal outcome -- the mutant
   revealed a real gap in what the suite proves.
2. **Listed as equivalent, one line each**, in that language's own
   `docs/maintenance/mutation-survivors-<lang>.md` -- only when the
   mutant is genuinely **behaviorally indistinguishable** from the
   original (no input exists that would make the mutated and original
   code disagree), not merely "hard to test" or "an edge case nobody
   hit yet." Equivalence is the narrow, provable case; when in doubt, it
   is a real gap and needs a test instead.

## Per-language survivor lists

- [`mutation-survivors-csharp.md`](mutation-survivors-csharp.md)
- [`mutation-survivors-python.md`](mutation-survivors-python.md)
- [`mutation-survivors-js.md`](mutation-survivors-js.md)
- [`mutation-survivors-dart.md`](mutation-survivors-dart.md)

Separate files, not one shared list, for the same reason every other
per-language tracking file in this repo is separate: four branches
documenting survivors in the same language concurrently never contend
for the same file.
