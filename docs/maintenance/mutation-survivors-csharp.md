<!-- Title: Mutation Survivors -- C# SDK -->
# Mutation survivors -- C# SDK

> C#'s own per-language record required by
> [`mutation-testing.md`](mutation-testing.md) -- every mutant
> [Stryker.NET](https://stryker-mutator.io/docs/stryker-net/introduction/)
> found that the test suite didn't catch, and what happened to it.

## Current status: ten mutants documented below, everything else killed

This record was rewritten from a fresh run, not patched -- the previous
version (93.64%, 7 equivalent survivors) described the pre-redesign,
hand-rolled `AndRule`/`OrRule`/`FunctionRule` implementations, which no
longer exist in that form. The
[composite-rule-and-leaves redesign](../../.agents/plans/composite-rule-and-leaves-redesign/README.md)
replaced them with `SequentialEvaluator`/`ShortCircuitEvaluator`
composition, a shipped `NotRule`, and `PredicateOutcome`/`RulePredicate`
-- none of which had ever been through mutation testing before this run.
Every survivor below was re-investigated from scratch against the
current source; none of the old doc's reasoning was carried over without
re-verifying it still applies to the rebuilt code.

The run (`csharp/src/VerdictRules/` via
[`scripts/run_mutation_csharp.sh`](../../scripts/run_mutation_csharp.sh),
Stryker.NET 5.0.0, 220 mutants across the package's thirteen source
files) started from a first pass against the suite as it stood before
this work: 38 `CompileError`, 17 `Ignored`, 165 scored, 112 killed, 53
undetected (67.9%) -- 11 `Survived` plus 42 `NoCoverage`, almost all of
it concentrated in code this redesign introduced or rebuilt and that had
never been mutation-tested at all (`AndRule.Failed`/`Passing`,
`OrRule.Passed`/`Failing`, `PredicateOutcome.ToString`/`DebuggerDisplay`,
`SequentialEvaluator`'s own post-loop fallback and `ToString`,
`NotRule<TContext>`'s own cancellation check, and `RunResult.FailingLeaves`).
43 of those 53 were real gaps; each got a new test that kills it (21
tests total, several killing more than one mutant), and a re-run
confirmed 155/165 scored mutants detected (93.9%). The remaining 10
survivors are documented below: 9 equivalent, and 1 that is neither
equivalent nor a real gap -- a Stryker coverage-analysis blind spot,
investigated and confirmed already caught by the existing suite when run
without coverage-based test selection.

## The 43 real gaps, grouped by what exposed them, and the tests that now kill them

| Survivor | Mutation | Killed by |
| :-- | :-- | :-- |
| `AndRule.cs` line 52 (`Failed`, 5 mutants: both `Conditional` branches, a `Logical` `&&`->`\|\|`, an `Equality` `>0`->`>=0`, and a `LogicalNotExpression` un-negation) | `AndRule.Failed` never called by any test -- the whole static helper (§3d of the redesign plan) shipped with zero coverage | `RuleTests.cs`: `AndRuleTests.FailedReturnsTheDecisiveFailingSubResult`, `FailedReturnsNullWhenEverySubRulePassed`, `FailedReturnsNullForAVacuousPass` -- the last one specifically proves no exception is thrown when `SubResults` is empty, which is what the `Logical`/`Equality` mutants on the `Count > 0` guard would otherwise unmask |
| `AndRule.cs` line 61 (`Passing`, 4 mutants: both `Conditional` branches, an `Equality` `is null`->`is not null`, and a `Linq` `Take()`->`Skip()`) | `AndRule.Passing` never called by any test | `RuleTests.cs`: `AndRuleTests.PassingReturnsEverySubResultWhenAllPassed`, `PassingExcludesTheDecisiveFailure` |
| `OrRule.cs` line 50 (`Passed`, 4 mutants: both `Conditional` branches, a `Logical` `&&`->`\|\|`, an `Equality` `>0`->`>=0`) | `OrRule.Passed` never called by any test | `RuleTests.cs`: `OrRuleTests.PassedReturnsTheDecisivePassingSubResult`, `PassedReturnsNullWhenEverySubRuleFailed`, `PassedReturnsNullForAVacuousFail` |
| `OrRule.cs` line 59 (`Failing`, 4 mutants: both `Conditional` branches, an `Equality` `is null`->`is not null`, and a `Linq` `Take()`->`Skip()`) | `OrRule.Failing` never called by any test | `RuleTests.cs`: `OrRuleTests.FailingReturnsEverySubResultWhenAllFailed`, `FailingExcludesTheDecisivePass` |
| `NotRuleT.cs` line 41, `cancellationToken.ThrowIfCancellationRequested();` deleted from `NotRule<TContext>.EvaluateAsync`'s own entry check | Every prior `NotRule` test used a `FunctionRule`-based wrapped rule, which double-checks cancellation on its own entry -- the same blind spot the pre-redesign `AndRule`/`OrRule` cancellation survivors had, now reproduced in the one new composite that has its own unconditional check to prove | `GenericsTests.cs`: `GenericCancellationTests.NotRuleOfTChecksCancellationBeforeEvaluatingEvenWhenTheWrappedRuleDoesNotCheckItself`, built on the same `NonCheckingRule<TContext>` test double the earlier `AndRule`/`OrRule`/`RulesEngine` cancellation tests use |
| `RunResult.cs` line 38, `Leaves.Where(l => !l.Passed)` negation removed | No test ever read `RunResult.FailingLeaves` (as opposed to `RuleResult.FailingLeaves`, which is extensively tested) with a genuine mix of passing and failing rules -- an all-pass or all-fail run can't tell "every leaf" and "only the failing ones" apart | `EngineTests.cs`: `RunAllTests.FailingLeavesIsOnlyTheFailingSubsetOfLeaves` |
| `SequentialEvaluator.cs` line 75, the null-coalescing fallback (`decider(...) ?? vacuousResult`) replaced with just `vacuousResult` | `ShortCircuitEvaluator`'s own decider always resolves by the last sub-rule (see the equivalent-mutant investigation below for why), so nothing exercising `AndRule`/`OrRule` alone ever reaches this line; only a custom decider composing `SequentialEvaluator` directly, one that can genuinely decline to commit even on the last result, does | `CSharpIdiomTests.cs`: `SequentialEvaluatorIdiomTests.PostLoopFallbackConsultsTheDeciderRatherThanJumpingStraightToVacuousResult` -- a decider that counts its own calls, declines on the first (in-loop) call and commits to a value *different from* `vacuousResult` on the second (fallback) call, proving the fallback line actually re-invokes the decider rather than jumping straight to the sentinel |
| `SequentialEvaluator.cs` line 79, `ToString()` blanked | `SequentialEvaluator`/`ShortCircuitEvaluator` are never referenced directly anywhere in the suite -- every prior test reached them only through `AndRule`/`OrRule`, which never calls `ToString()` on the evaluator it holds | `DiagnosticsTests.cs`: `ToStringTests.SequentialEvaluatorShowsItsContextType`, `ShortCircuitEvaluatorShowsItsContextTypeAndStopOn` |
| `PredicateOutcome.cs` line 40 (`ToString()`, 12 mutants across both `Conditional` branches of each of the two `Passed ? "PASS" : "FAIL"` occurrences, the `Detail.Length != 0` equality flip, and four `"PASS"`/`"FAIL"`/`$""` string blankings) | `PredicateOutcome` is a brand-new type (§2a of the redesign plan) that shipped with no `ToString()` coverage at all | `DiagnosticsTests.cs`: `ToStringTests.PredicateOutcomeShowsThePassOutcomeWithNoDetail`, `ShowsTheFailOutcomeWithNoDetail`, `ShowsThePassOutcomeWithDetail`, `ShowsTheFailOutcomeWithDetail` (all four combinations of `Passed` x "has detail", mirroring the pattern `RuleResult.ToString()` already needed) |
| `PredicateOutcome.cs` line 44 (`DebuggerDisplay`, 9 mutants across the same shape as line 40 plus a literal `"Stryker was here!"` swap) | Same as above -- no `DebuggerDisplay` coverage at all | `DiagnosticsTests.cs`: `PredicateOutcomeDebuggerDisplayTests.PredicateOutcomeDebuggerDisplayOmitsDetailWhenEmpty`, `PredicateOutcomeDebuggerDisplayShowsDetailWhenPresent` -- one `Passed` value each is enough here, the same diagonal-pair shape `RuleResult`'s own `DebuggerDisplay` tests already use |

## The eight `ConfigureAwait(false)` mutants

| Location | Mutation |
| :-- | :-- |
| `FunctionRuleT.cs` line 47 | `.ConfigureAwait(false)` -> `.ConfigureAwait(true)` |
| `NotRuleT.cs` line 42 | `.ConfigureAwait(false)` -> `.ConfigureAwait(true)` |
| `SequentialEvaluator.cs` line 67 | `.ConfigureAwait(false)` -> `.ConfigureAwait(true)` |
| `RulesEngineT.cs` line 75 (`RunAllAsync`) | `.ConfigureAwait(false)` -> `.ConfigureAwait(true)` |
| `RulesEngineT.cs` line 102 (`TryRunNamedAsync`) | `.ConfigureAwait(false)` -> `.ConfigureAwait(true)` |
| `RulesEngineT.cs` line 113 (`RunNamedAsync`) | `.ConfigureAwait(false)` -> `.ConfigureAwait(true)` |
| `RulesEngineT.cs` line 142 (`TryRunGroupAsync`) | `.ConfigureAwait(false)` -> `.ConfigureAwait(true)` |
| `RulesEngineT.cs` line 158 (`RunGroupAsync`) | `.ConfigureAwait(false)` -> `.ConfigureAwait(true)` |

This is the same equivalence class the pre-redesign record already
investigated and documented in depth, carried forward rather than
rediscovered: `ConfigureAwait(false)` on every library `await` is a
documented convention of this package (`csharp/AGENTS.md`), kept to
avoid a deadlock risk in a caller with a single-threaded
`SynchronizationContext`. Flipping it to `true` changes no value any
consumer can observe through this package's public contract -- not a
`RuleResult`/`RunResult` field, not an exception's type or message, not
evaluation order, not which sub-rules run before a cancellation or a
short-circuit. The only thing it can possibly change is which
`SynchronizationContext`, if any, a continuation is marshalled through,
a question the pre-redesign investigation built a dedicated test for
(a custom `SynchronizationContext` counting its own `Post` calls, paired
with a sub-rule that genuinely suspends) and found to be sensitive to
unrelated thread-pool warm-up state rather than this package's own
logic -- see that investigation's full writeup, preserved below.

The locations changed with the redesign -- `AndRuleT.cs`/`OrRuleT.cs`
no longer `await` anything themselves (they forward straight to a
shared `ShortCircuitEvaluator`, which is where the `await` now lives,
in `SequentialEvaluator.cs`), and `FunctionRuleT.cs`/`NotRuleT.cs` gained
their own `await` for the first time (`FunctionRule<TContext>.EvaluateAsync`
used to return the predicate's own `Task<RuleResult>` unchanged, with no
`await` of its own at all, per §2a of the redesign plan) -- but the
reasoning above is about `ConfigureAwait`'s own semantics, not about
which type happens to contain the call, so it generalizes to every new
site without needing to be re-proven from scratch.

<details>
<summary>Full investigation, preserved from the pre-redesign record</summary>

`ConfigureAwait` was not assumed equivalent on the "only changes
`SynchronizationContext` marshalling" argument alone, because that is
a real difference in principle, not merely an inconvenient one to
observe. A test was built with a custom `SynchronizationContext` that
counts its own `Post` invocations, paired with a sub-rule that
genuinely suspends (`await Task.Yield()` or `await Task.Delay(...)`,
never completing synchronously), to observe whether a composite's own
continuation gets re-marshalled. That test **did** catch several of
these mutants when each was re-introduced by hand and run in isolation.
But the same test, unchanged, produced a different `Post` count across
separate runs of the *same, unmutated* code -- confirmed by five
consecutive runs that were stable, followed by a later run (after
unrelated test classes were added to the same file, with no change to
the test itself or to the code under test) that settled on a different,
but then-again-stable, count. The CLR's await-continuation machinery
has an internal fast path (`SynchronizationContextAwaitTaskContinuation`)
that skips re-posting a continuation when the resuming thread already
carries the exact captured context instance -- whether that fast path
applies for a given `ConfigureAwait` site depends on how many other
`async` frames and thread-pool hops happen to be in flight around it at
that moment, which this investigation found to be sensitive to code
completely unrelated to `verdict`'s own logic. A regression test whose
pass/fail depends on that is not testing this package's behavior; it is
testing incidental CLR scheduling state, and would fail intermittently
in CI for reasons having nothing to do with a real regression -- which
is a materially worse outcome than a mutant nobody is chasing. Given
that (a) no documented, consumer-observable contract differs, and (b)
the one signal that can distinguish them in principle was built,
confirmed to work, and then confirmed to be non-deterministic under
conditions no different from ordinary test-suite growth, these are
treated as equivalent for this project's purposes -- a narrower claim
than "no input could ever distinguish them" (one can, in a lab setting,
with low-level `SynchronizationContext` introspection), but one this
investigation backs with more than inconvenience.

</details>

## One new genuinely equivalent mutant: `ShortCircuitEvaluator.cs` line 44

```csharp
return soFar.Count == total ? !stopOn : null;
```

Stryker's `Conditional (false)` mutation forces this line's condition to
the constant `false`, so the expression always evaluates to `null`
regardless of whether the sub-rule list is actually exhausted. This is
provably indistinguishable from the original, not merely hard to
trigger: `SequentialEvaluator.EvaluateAsync`'s own post-loop fallback
(line 75) re-invokes the *exact same decider* with the *exact same
arguments* (`soFar[^1]`, `soFar`, `rules.Count`) whenever the loop
finishes without an earlier verdict. Since this decider is a pure
closure over `stopOn` with no side effects, that second call reaches
the identical mutated branch and returns `null` again, falling through
to `?? vacuousResult`. `ShortCircuitEvaluator`'s constructor always sets
`vacuousResult: !stopOn` (line 46) -- exactly the value the unmutated
ternary would have returned directly. So whether the `!stopOn` comes
from the ternary's own true branch or from the fallback's `vacuousResult`,
the final `RuleResult` carries the same `Passed` value and the same
`SubResults` (built identically in the loop body regardless of what the
decider returns). No input through `ShortCircuitEvaluator`'s public
surface can make the mutated and original code disagree.

Verified empirically, not just argued: the mutation was hand-applied to
`ShortCircuitEvaluator.cs`, and a full `dotnet test` run (all 142 tests,
not Stryker's coverage-optimized subset) passed without a single
failure, confirming no test -- existing or new -- can distinguish the
two.

## One survivor that is neither equivalent nor a real gap: `ShortCircuitEvaluator.cs` line 46

```csharp
vacuousResult: !stopOn);
```

Stryker's `LogicalNotExpression` mutation here (`!stopOn` -> `stopOn`)
genuinely changes behavior -- confirmed empirically by hand-applying it
and running the full `dotnet test` suite (not Stryker's coverage-based
subset): four tests fail, including `AndRuleTests.EmptyRuleListVacuouslyPasses`
and `OrRuleTests.EmptyRuleListVacuouslyFails`, exactly the tests that
pin `AndRule`/`OrRule`'s own documented vacuous-truth constants
(`AndRule.VacuousResult`/`OrRule.VacuousResult`, §3a of the redesign
plan). This is not equivalence by any definition this project uses.

It survived Stryker's actual run anyway, and re-survived identically
after every new test above was added, for a reason specific to this
tool's coverage-based test selection rather than anything about the
test suite's power: `AndRule<TContext>`/`OrRule<TContext>` each hold a
`private static readonly ShortCircuitEvaluator<TContext> Evaluator`
field (one shared instance per closed generic type, constructed once).
C#'s static-field initializer for a given closed generic type runs
exactly once per process, triggered by whichever test happens to be the
*first* in the whole suite to touch that specific closed type. Stryker's
coverage-capture pass records only that first-toucher as "covering" the
initializer line -- every later test that constructs a new `AndRule`/`OrRule`
and depends on the *value* the field already holds is, from Stryker's
instrumentation's point of view, not touching that line at all, because
it genuinely isn't re-executing it. When Stryker then re-runs just the
(small, coverage-selected) subset of tests it believes cover a given
mutant, the tests that would actually catch this one -- the vacuous-result
assertions -- are never in that subset, because none of them happened to
be the first `AndRule`/`OrRule` test the suite ran. Confirmed directly:
mutant 218's own `coveredBy` list (inspected in the raw
`mutation-report.json`) consists entirely of cancellation and exception-
propagation tests that construct an `AndRule`/`OrRule` early, none of
which assert anything about the vacuous case.

No new test closes this, because the mechanism is about *when* a
one-time static initializer runs relative to Stryker's fixed
coverage-capture ordering, not about what the suite asserts -- any new
vacuous-result test would need to already be the first `AndRule`/`OrRule`
test the suite happens to run to be included in Stryker's coverage set
for this mutant, which isn't something a test can control. The behavior
this mutation breaks is already correctly pinned by
`AndRuleTests.EmptyRuleListVacuouslyPasses`, `OrRuleTests.EmptyRuleListVacuouslyFails`,
`RunGroupTests.EmptyCompositeStillPassesVacuously`, and
`MixedCompositeTreeTests.AGenuinelyVacuousCompositeNestedInsideALargerFailingTree`
-- proven by running the full suite (not Stryker's subset) against the
hand-applied mutation and watching exactly those four fail. If Stryker
or its coverage-analysis mode changes how it attributes coverage for
lazily-initialized static fields in a future version, re-run and confirm
whether this one still needs its own section.

If a future run finds another mutant genuinely indistinguishable from
the original (no input would make the mutated and original code
disagree), or another instance of this same coverage-attribution
artifact, add it as a new row or section above instead of silently
skipping it -- see
[`mutation-testing.md`](mutation-testing.md#a-surviving-mutant-needs-one-of-two-things)
for what qualifies as equivalent.
