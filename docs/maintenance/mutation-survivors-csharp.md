<!-- Title: Mutation Survivors -- C# SDK -->
# Mutation survivors -- C# SDK

> The C# package's surviving mutants, each with the proof it is behaviorally
> equivalent or that the suite does catch it. The bar a survivor has to clear
> to be listed instead of fixed is in
> [`mutation-testing.md`](mutation-testing.md#a-surviving-mutant-needs-one-of-two-things);
> the run itself is
> [`../../scripts/run_mutation_csharp.sh`](../../scripts/run_mutation_csharp.sh).

## Score: 174/176 killed (98.9%), two survivors, zero real gaps

[Stryker.NET](https://stryker-mutator.io/docs/stryker-net/introduction/)
against [`csharp/src/VerdictRules/`](../../csharp/src/VerdictRules/), through
the 171-test suite in
[`csharp/tests/VerdictRules.Tests/`](../../csharp/tests/VerdictRules.Tests/).
237 mutants are generated; 41 fail to compile and 20 are dropped by Stryker's
own already-covered-block filter, leaving 176 actually tested.

Both survivors are in
[`ShortCircuitEvaluator.cs`](../../csharp/src/VerdictRules/ShortCircuitEvaluator.cs),
and neither is a gap: one is provably equivalent, the other is caught by four
existing tests that Stryker's test selection does not attribute to it.

## Equivalent: the decider returning `null` instead of the exhausted verdict

Line 45, `Conditional (false)` -- `return soFar.Count == total ? !stopOn : null;`
becomes `return null;`.

The mutated decider stops answering the exhausted-without-trigger case and
falls through. [`SequentialEvaluator`](../../csharp/src/VerdictRules/SequentialEvaluator.cs)'s
post-loop line is `decider(...) ?? vacuousResult`, and this evaluator
constructs its inner one with `vacuousResult: !stopOn` -- the identical
value the deleted branch returned. `SubResults` is the same list either way,
and `ShortCircuitEvaluator` recomputes `DecidedByIndices` itself after the
inner call regardless of which path produced the result, so the one field
that differs between the in-loop and post-loop branches is overwritten
before anything can observe it.

That recomputation is load-bearing to the equivalence. **If
`ShortCircuitEvaluator` ever stops recomputing `DecidedByIndices`, this
mutant becomes distinguishable and needs a test** -- the post-loop branch
names every evaluated child, where the in-loop branch names only the one
that stopped it.

## Caught by four tests, mis-attributed by Stryker: the vacuous polarity

Line 47, `LogicalNotExpression` -- `vacuousResult: !stopOn` becomes
`vacuousResult: stopOn`.

Not equivalent at all: it inverts vacuous truth, so `AndRule([])` would fail
and `OrRule([])` would pass. Reverting it by hand fails four existing tests
(`AndRuleTests.EmptyRuleListVacuouslyPasses`,
`OrRuleTests.EmptyRuleListVacuouslyFails`,
`RunGroupTests.EmptyCompositeStillPassesVacuously`,
`MixedCompositeTreeTests.AGenuinelyVacuousCompositeNestedInsideALargerFailingTree`).

It survives Stryker because of `CoverageBasedTest` selection combined with
this package's own shared-instance idiom: `AndRule`/`OrRule` each hold a
`private static readonly ShortCircuitEvaluator<TContext>` constructed at type
initialization, before any individual test runs, so coverage attributes the
constructor's own line to whichever test first triggered that type's
initialization rather than to the vacuous-truth tests that actually exercise
it. Confirmed deterministic -- re-running with `--disable-bail` produces the
identical survivor set. Python's own run hits the same artifact on the same
construct; see
[`mutation-survivors-python.md`](mutation-survivors-python.md).

Listed here rather than as equivalent, because it is not equivalent. The
suite does catch it; the tool does not credit the tests that do.

## What a previous run got wrong, and what fixed it

An earlier record listed nine `ConfigureAwait(false)` to
`ConfigureAwait(true)` mutants -- one at every await site in the package --
as an equivalence class. They were not equivalent. `ConfigureAwait` changes
only *where* a continuation resumes, never the value computed, so no
assertion on a `RuleResult` can tell them apart; but the difference is real
and matters to a consumer on a platform with a synchronization context, who
deadlocks if a continuation tries to resume on a context their
sync-over-async wrapper is still blocking.

That is testable, and
[`ContextCaptureTests.cs`](../../csharp/tests/VerdictRules.Tests/ContextCaptureTests.cs)
tests it: install a recording `SynchronizationContext`, run each entry point,
and assert nothing was posted back to it. The suite carries its own control
test, because without one every assertion would also pass against a context
nothing consults. Flipping a single `ConfigureAwait(false)` to `true` fails
seven of them.

This moved the score from 92.0% to 98.9% and is the reason to re-derive an
equivalence claim rather than carry it forward: "no test can distinguish
these" and "no test *does* distinguish these" are different statements, and
only the first justifies the listing.

## Related docs

- [`mutation-testing.md`](mutation-testing.md) -- why mutation testing, which
  tool per language, what a survivor obliges.
- [`../testing/csharp.md`](../testing/csharp.md) -- the suite these mutants
  are run against.
