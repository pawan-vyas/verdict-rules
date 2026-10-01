<!-- Title: Mutation Survivors -- JS/TS -->
# Mutation survivors -- JS/TS

> JS/TS's own per-language record required by
> [`mutation-testing.md`](mutation-testing.md) -- every mutant
> [StrykerJS](https://stryker-mutator.io/docs/stryker-js/) found that the
> test suite didn't catch, and what happened to it.

## Current status: one equivalent mutant, everything else killed

The most recent run (`js/packages/verdict-rules/src/` via
[`scripts/run_mutation_js.sh`](../../scripts/run_mutation_js.sh), 121
mutants across `engine.ts`, `errors.ts`, and `rule.ts` -- `index.ts` and
`result.ts` produced none, being re-exports and a type-only module) found
13 surviving mutants. 12 were real gaps; each got a new test that kills
it, and a re-run confirmed 120/121 mutants detected (99.17%). The one
remaining survivor is documented below as genuinely equivalent.

The 12 real survivors and the tests that now kill them:

| Survivor | Mutation | Killed by |
| :-- | :-- | :-- |
| `errors.ts` line 23-25 (x6) | `kind === "rule"` and the two message string literals it selects between, each mutated independently | `js-idioms.test.js`: `is catchable by type and inspectable by field`, extended to assert `err.message` for both the `"rule"` and `"group"` kind |
| `engine.ts` line 24 (x2) | `rule.group === ""` and the `""` literal itself, in the constructor's group-index guard | `engine.test.js`: `a rule with an empty-string group is treated as ungrouped` |
| `rule.ts` line 100 | `` `'${rule.name}' failed` `` replaced with `` `` `` -- `AndRule`'s failure detail when the failing sub-rule carries none | `rule.test.js`: `failure detail omits the colon when the sub-rule provides none` |
| `rule.ts` line 142 and 146 | `OrRule.evaluate`'s `subResults` initial value and the `subResults.push(result)` call that populates it | `rule.test.js`: `data carries every evaluated sub-result, including the one that passed` |
| `rule.ts` line 163 | `` (this.group ? ` (${this.group})` : "") `` in `OrRule.toString()`, never covered at all (`NoCoverage`, not even a dry-run hit) | `js-idioms.test.js`: `OrRule shows its group when present` |

The `errors.ts` survivors existed because every existing test asserted
`err.kind` and `err.key` but never `err.message` -- a mutant that flipped
the `kind === "rule"` check, or hollowed out either message string, still
produced an object with the right `kind`/`key`, so nothing distinguished
it from the original.

The `engine.ts` line 24 survivors existed because the constructor's guard
reads `rule.group === undefined || rule.group === ""`, deliberately
treating an empty-string group the same as no group at all -- the same
falsy check Python's own `if rule.group:` already applies (see
`python/packages/verdict-rules/src/verdict/engine.py`) -- but nothing on
the JS side had ever exercised a rule constructed with `group: ""`.

The `rule.ts` line 100 survivor existed because the one existing
`AndRule` failure-detail test always gave the failing sub-rule a
`detail`, so the plain `` `'${rule.name}' failed` `` branch (no sub-rule
detail) was only ever exercised alongside the detail-bearing branch,
never checked for its own exact text.

The `rule.ts` line 142/146 survivors existed because `AndRule` already
had a "data carries sub-results up to failure" test proving its `data`
field is populated along the way, but `OrRule` had no equivalent --
nothing ever inspected `result.data` on an `OrRule`, so both the initial
array value and the line that pushes each evaluated sub-result onto it
could be mutated without any test noticing.

The `rule.ts` line 163 survivor existed because `FunctionRule` and
`AndRule` each had a "shows its group when present" test for their own
`toString()`/`util.inspect` output, but `OrRule` only had the no-group
case -- the group-formatting branch of `OrRule.toString()` was never
even covered by the dry run, let alone by a mutant-killing assertion.

## Equivalent mutant

| Mutant | Why it's equivalent |
| :-- | :-- |
| `engine.ts:87` -- `rules === undefined \|\| rules.length === 0` mutated to `rules === undefined \|\| false` | `#byGroup` is a private field, written only in the constructor, which either creates a new one-element array for a group label or pushes onto an existing one -- it never stores an empty array, and nothing outside the constructor ever mutates it. So for every key present in `#byGroup`, `.get(key)` is provably non-empty for the object's entire lifetime; the `rules.length === 0` arm is unreachable through any public API call, for any input. No test can distinguish the mutated code from the original because no input reaches the branch where they'd disagree. |

If a future run finds another mutant genuinely indistinguishable from the
original (no input would make the mutated and original code disagree),
add it as a new row above instead of silently skipping it -- see
[`mutation-testing.md`](mutation-testing.md#a-surviving-mutant-needs-one-of-two-things)
for what qualifies.
