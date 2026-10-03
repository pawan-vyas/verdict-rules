<!-- Title: Mutation Survivors -- JS/TS -->
# Mutation survivors -- JS/TS

> JS/TS's own per-language record required by
> [`mutation-testing.md`](mutation-testing.md) -- every mutant
> [StrykerJS](https://stryker-mutator.io/docs/stryker-js/) found that the
> test suite didn't catch, and what happened to it.

## Current status: five equivalent mutants, everything else killed

This is JS/TS's **first** mutation run since the composite-rule-and-leaves
redesign (`SequentialEvaluator`/`ShortCircuitEvaluator`/`NotRule`/
`PredicateOutcome`/`RuleResult.decidedBy` -- see
[`src/rule.ts`](../../js/packages/verdict-rules/src/rule.ts) and
[`src/result.ts`](../../js/packages/verdict-rules/src/result.ts)) -- the prior version of this doc described code from before that
redesign and has been replaced in full, not amended.

The run (`js/packages/verdict-rules/src/` via
[`scripts/run_mutation_js.sh`](../../scripts/run_mutation_js.sh), 204
mutants across `engine.ts`, `errors.ts`, `result.ts`, and `rule.ts` --
`index.ts` produced none, being a re-export-only module) found 24
surviving/uncovered mutants on the first pass. 19 were real gaps across 6
root causes; each root cause got one or more new tests that kill every
mutant it covered, and a re-run confirmed 199/204 mutants detected
(97.55%). The remaining 5 survivors are documented below as genuinely
equivalent.

## The 6 real gaps and the tests that now kill them

| Root cause | Mutation | Killed by |
| :-- | :-- | :-- |
| `result.ts:132` -- `buildRuleResult`'s `decidedBy` default | `init.decidedBy ?? []` replaced with `init.decidedBy ?? ["Stryker was here"]` | `rule.test.js`: `decidedBy defaults to empty for a leaf result` |
| `rule.ts:209` -- `SequentialEvaluator`'s own in-loop `decidedBy` ternary | The `soFar.length === rules.length ? soFar : [latest]` ternary, mutated four ways (both branches, the condition itself, and its polarity) | `composition.test.js`: `decided before every sub-rule is evaluated names only the deciding sub-result` and `decided in-loop on exactly the last sub-rule names every evaluated sub-result` |
| `rule.ts:215` -- the post-loop fallback's `lastEvaluated` index | `soFar[soFar.length - 1]` replaced with `soFar[soFar.length + 1]` | `composition.test.js`: `post-loop fallback calls the decider against the real last-evaluated sub-result` |
| `rule.ts:81-88` -- `assertPredicateOutcome`'s `\|\|` guard chain and its message text | Each of the four `\|\|` clauses individually, two of them combined into `&&`, the whole chain collapsed to `false`, and both halves of the thrown message string | `rule.test.js`: `predicate returning null raises TypeError...`, `predicate returning an object without a boolean passed raises TypeError...`, `predicate returning a function raises TypeError even if it carries a boolean passed` |
| `rule.ts:317`/`rule.ts:359` -- `AndRule`/`OrRule`'s pinned `VACUOUS_RESULT` constants | `true` flipped to `false` (and vice versa) | `rule.test.js`: `VACUOUS_RESULT pins the vacuous-pass fact` / `VACUOUS_RESULT pins the vacuous-fail fact` |
| `rule.ts:433` -- `NotRule`'s `util.inspect` custom symbol | The symbol's string literal, and the whole method body (flagged `NoCoverage` -- not even dry-run hit) | `js-idioms.test.js`: `NotRule shows its name` / `NotRule shows its group when present` |

### Why each one existed

**`result.ts:132`** existed because nothing had ever read `decidedBy` on a
plain leaf result -- every existing `decidedBy` test exercised a
composite (`AndRule`, `OrRule`, `NotRule`), never a bare `FunctionRule`
result relying on the default. The sibling C# mutation re-run found the
identical gap in its own leaf type.

**`rule.ts:209`** existed because `AndRule`/`OrRule` are the only shipped
composites, and both route through `ShortCircuitEvaluator`, which always
*recomputes and overwrites* `decidedBy` from its own `stopOn` after
delegating to `SequentialEvaluator` (see `rule.ts:274-286`) -- so no
`AndRule`/`OrRule` test, however thorough, can ever observe
`SequentialEvaluator`'s own generic `decidedBy` computation; it's always
discarded before reaching a public result. Only a test instantiating
`SequentialEvaluator` directly (as the custom-composite extension point
it's designed to be) can exercise it. Two sub-cases needed separate
tests, matching exactly what the sibling C#/Python mutation re-runs
found in their own equivalent code: deciding with items still
unevaluated (`decidedBy` must be just the one sub-result that flipped
it, not everything seen so far) and deciding in-loop but landing exactly
on the last item (indistinguishable from the post-loop fallback by
`soFar.length === total` alone, but reached via a different code path
and required to produce the same "every evaluated child" answer). A
decision on the very first evaluated rule couldn't tell these apart (a
one-element `soFar` looks identical to `[latest]` either way), so both
new tests use three sub-rules and decide on the second or third.

**`rule.ts:215`** existed for the same structural reason as `rule.ts:209`
-- the post-loop fallback's `decidedBy` is always `soFar` regardless of
what `lastEvaluated` an off-by-one hands the decider, so no assertion on
the *final result* could ever have caught an off-by-one there. The new
test instead asserts on a side effect: the decider's own log of what
`latest.ruleName` it was called with, at the exact two points it should
be invoked once per the real last sub-result (once in-loop, once more
by the fallback) -- proving the index, not just the eventual boolean.

**`rule.ts:81-88`** existed because the two pre-existing predicate-guard
tests only ever made the *first* clause (`typeof outcome !== "object"`,
via a string) or the *last* clause (`"ruleName" in outcome`, via a
`RuleResult`-shaped object) pivotal, and neither checked the thrown
message's exact text -- only `err.name`. The middle two clauses
(`outcome === null`, `passed` not a boolean) were never independently
true-while-the-others-are-false, and a mutant collapsing several clauses
together, or hollowing out either half of the message, still produced
*some* `TypeError`, which `{ name: "TypeError" }` alone can't
distinguish from the correct one. A fourth test was needed even after
covering all four clauses with pivotal inputs: every other non-object
value (a string, a number, `undefined`) fails the "not an object" and
the "passed isn't a boolean" clauses simultaneously, so a mutant
deleting the "not an object" clause specifically stayed invisible until
a function -- `typeof` "function", never "object", yet still able to
carry its own arbitrary `passed` property -- gave that one clause a case
where it's pivotal alone.

**`rule.ts:317`/`rule.ts:359`** existed because these constants are no
longer wired into construction since the `ShortCircuitEvaluator`
composition redesign (§3a of the plan) -- they're kept purely as a
pinned, directly-readable guarantee for a reader, which means nothing
exercising actual rule evaluation will ever touch them. Only a test
reading the constant itself can kill a mutant on its literal value.

**`rule.ts:433`** existed because `NotRule` shipped alongside
`AndRule`/`OrRule`/`FunctionRule` but was simply missing from
`js-idioms.test.js`'s `console.log / util.inspect representation`
describe block -- every other rule type has both a `toString()` test and
an `inspect()` test there; `NotRule` had neither for `inspect()`,
leaving its custom-symbol method completely unexercised (StrykerJS's
dry run never even covered the method body, let alone killed a mutant
in it).

None of the 6 gaps were specific to a StrykerJS tooling quirk the way
Python's mutmut run found with `@property`-on-`@dataclass` blind spots
or mutation-tool coverage misattribution on a shared singleton --
StrykerJS's `perTest` coverage analysis correctly attributed every
survivor here to the real tests that exercised it.

## Equivalent mutants

| Mutant | Why it's equivalent |
| :-- | :-- |
| `engine.ts:87` -- `rules === undefined \|\| rules.length === 0` mutated to `rules === undefined \|\| false` | Unchanged since the prior run of this doc. `#byGroup` is a private field, written only in the constructor, which either creates a new one-element array for a group label or pushes onto an existing one -- it never stores an empty array, and nothing outside the constructor ever mutates it. So for every key present in `#byGroup`, `.get(key)` is provably non-empty for the object's entire lifetime; the `rules.length === 0` arm is unreachable through any public API call, for any input. |
| `rule.ts:193` -- `SequentialEvaluator`'s empty-rules branch, `buildRuleResult(name, this.#vacuousResult, { subResults: [] })` mutated to drop `subResults: []` entirely | `buildRuleResult`'s own `RuleResultInit` already defaults `subResults: init.subResults ?? []` (`result.ts:131`). Passing `{ subResults: [] }` and passing `{}` normalize to the identical `[]` through that default -- no input can observe a difference, because both inputs to `buildRuleResult` produce byte-identical output. |
| `rule.ts:248` -- `ShortCircuitEvaluator`'s own decider, `if (soFar.length === total) return !stopOn;` mutated to `if (false) return !stopOn;` | `ShortCircuitEvaluator`'s constructor hardcodes the `SequentialEvaluator` it builds with `vacuousResult: !stopOn` -- the exact same value this branch returns on exhaustion. Suppressing the branch only means the decider returns `undefined` instead of `!stopOn` at that point; `SequentialEvaluator`'s own post-loop fallback then computes `final ?? vacuousResult`, which evaluates to the identical `!stopOn` either way. The post-loop path's `decidedBy` is unconditionally `soFar` in both cases too, and `ShortCircuitEvaluator.evaluate` discards and recomputes `decidedBy` from `result.subResults` regardless (`rule.ts:274-286`), so nothing downstream can observe the difference either. True only because `ShortCircuitEvaluator` is the one caller that ties `vacuousResult` to this exact branch's return value by construction -- a hypothetical caller supplying a different `vacuousResult` to a bare `SequentialEvaluator` with this same decider shape would *not* be equivalent, but no such caller exists in this codebase. |
| `rule.ts:275` (x2) -- `result.subResults.length > 0 ? result.subResults[result.subResults.length - 1] : undefined`, mutated to always take the true branch (`true ? ... : undefined`) and separately to `length >= 0 ? ... : undefined` | `Array.prototype.length` can never be negative, so `length >= 0` is true for every possible array, identical in effect to unconditionally taking the true branch. And when `length === 0` (the one case the original's `> 0` check excludes), `result.subResults[length - 1]` is `result.subResults[-1]` -- a plain JS array has no negative-index support, so this is a property lookup that misses and evaluates to `undefined`, the exact same value the original's `false` branch returns explicitly. Both mutants collapse to the same observable result as the original for every possible `subResults` array, empty or not. |

If a future run finds another mutant genuinely indistinguishable from the
original (no input would make the mutated and original code disagree),
add it as a new row above instead of silently skipping it -- see
[`mutation-testing.md`](mutation-testing.md#a-surviving-mutant-needs-one-of-two-things)
for what qualifies.
