<!-- Title: Mutation Survivors -- JS/TS -->
# Mutation survivors -- JS/TS

> The JS/TS package's surviving mutants, each with the proof it is
> behaviorally equivalent rather than a real gap. The bar a survivor has to
> clear to be listed instead of fixed is in
> [`mutation-testing.md`](mutation-testing.md#a-surviving-mutant-needs-one-of-two-things);
> the run itself is
> [`../../scripts/run_mutation_js.sh`](../../scripts/run_mutation_js.sh).

## Score: 238/243 killed (97.9%), five equivalent mutants, zero real gaps

[StrykerJS](https://stryker-mutator.io/docs/stryker-js/guides/nodejs/) against
[`js/packages/verdict-rules/src/`](../../js/packages/verdict-rules/src/),
through the 144-test suite, consuming `node --test`'s own TAP output.

| Module | Score | Survivors |
| :-- | --: | --: |
| `result.ts` | 100.0% | 0 |
| `errors.ts` | 100.0% | 0 |
| `engine.ts` | 98.3% | 1 (equivalent) |
| `rule.ts` | 96.6% | 4 (equivalent) |

Every survivor is provably equivalent, and all four in `rule.ts` were
confirmed together: applying all of them at once still leaves the suite
green, and each has a proof below rather than only an absence of failures.

## Equivalent: an explicit empty `subResults` on the vacuous branch

`rule.ts`, `ObjectLiteral` -- `new RuleResult(name, this.#vacuousResult, { subResults: [] })`
becomes `new RuleResult(name, this.#vacuousResult, {})`.

`RuleResult`'s constructor reads `[...(init.subResults ?? [])]`, so an
omitted key and an explicit empty array produce the identical field. No
input can make them disagree. Python's run carries the same mutant on the
same branch; see
[`mutation-survivors-python.md`](mutation-survivors-python.md#the-one-equivalent-mutant).

## Equivalent: the decider returning `undefined` instead of the exhausted verdict

`rule.ts`, `ConditionalExpression` -- `if (soFar.length === total) return !stopOn;`
has its condition replaced with `false`, so the branch never runs.

The mutated decider falls through to `return undefined`, and
`SequentialEvaluator`'s post-loop line is `final ?? this.#vacuousResult` --
where `ShortCircuitEvaluator` constructed that inner evaluator with
`!stopOn`, the exact value the skipped branch returned. `subResults` is the
same array either way, and `ShortCircuitEvaluator` recomputes
`decidedByIndices` itself after the inner call, so the one field that
differs between the in-loop and post-loop branches is overwritten before
anything observes it.

That recomputation is load-bearing to the equivalence: **if
`ShortCircuitEvaluator` ever stops recomputing `decidedByIndices`, this
mutant becomes distinguishable and needs a test.** C#'s run carries the
identical mutant with the identical proof.

## Equivalent, because of a JS-specific fact: the empty guard on `last`

`rule.ts`, two mutants on `const last = total > 0 ? result.subResults[total - 1] : undefined;`
-- the condition replaced with `true`, and `>` loosened to `>=`.

Both differ from the original only when `total === 0`, and there the mutated
branch evaluates `result.subResults[-1]`, which in JavaScript is `undefined`
-- the same value the original's `else` supplies. Verified directly rather
than assumed.

**This equivalence does not port.** Dart's `List` and C#'s
`IReadOnlyList<T>` both throw on a negative index, so the same mutation in
those languages is a real failure and their suites catch it. The guard stays
because the code should not depend on a language quirk to be correct, which
is the posture
[`../../AGENTS.md`](../../AGENTS.md) asks for.

## Equivalent: the unreachable half of a defensive group guard

`engine.ts`, `ConditionalExpression` -- in
`if (rules === undefined || rules.length === 0) return undefined;`, the
second clause is replaced with `false`.

`#byGroup` entries are only ever created as `set(rule.group, [rule])`, with
one rule already in them, and only ever grown by `push`. A stored empty
array is unconstructible, so `rules.length === 0` is unreachable and
deleting it changes nothing. The first clause does all the real work: an
absent group yields `undefined`, and removing *that* clause is caught.

Kept rather than deleted, deliberately: the degenerate case stays explicit
at the guard rather than relying on a reader reconstructing the
index-construction argument above. That makes it dead code with a purpose,
which is why it is listed here instead of removed.

## What a previous run missed, and what fixed it

The run against the new result shape scored 94.65% with 13 survivors, 8 of
them in `result.ts`. All eight were real gaps in genuinely new code, and
five tests in
[`test/immutability.test.js`](../../js/packages/verdict-rules/test/immutability.test.js)
close them, taking `result.ts` to 100%:

| Gap | Closed by |
| :-- | :-- |
| Nothing covered a negative `decidedByIndices` entry, so three mutants on the bounds predicate survived -- the lower bound was unverified | `a negative index is rejected, not read from the end` |
| Nothing covered a non-integer entry | `a non-integer index is rejected` |
| The `RangeError` message was never asserted, so three mutants wiping it survived -- including the `join(", ")` separator, invisible to a single-index case | `the error names every offending index, the count, and the rule` |
| Nothing asserted the instances are actually frozen -- `readonly` is erased at runtime, so removing `Object.freeze` was unobservable | `a result instance is frozen, not merely typed readonly` |

The frozen one is worth singling out: a `readonly` field is a compile-time
claim only, so without that test the suite proved nothing about whether a
result a consumer already holds can be changed under them.

## Related docs

- [`mutation-testing.md`](mutation-testing.md) -- why mutation testing, which
  tool per language, what a survivor obliges.
- [`../testing/js.md`](../testing/js.md) -- the suite these mutants are run
  against.
