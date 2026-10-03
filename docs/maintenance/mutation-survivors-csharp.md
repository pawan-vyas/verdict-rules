<!-- Title: Mutation Survivors -- C# SDK -->
# Mutation survivors -- C# SDK

> C#'s own per-language record required by
> [`mutation-testing.md`](mutation-testing.md) -- every mutant
> [Stryker.NET](https://stryker-mutator.io/docs/stryker-net/introduction/)
> found that the test suite didn't catch, and what happened to it.

## Current status: eleven mutants documented below, everything else killed

This record was rewritten from a fresh run, not patched -- the previous
version (93.9%, 10 survivors) described the code as it stood right after the
composite-rule-and-leaves redesign
landed, before `RuleResult.DecidedBy` existed and before the three static
accessor pairs it replaced (`AndRule.Failed`/`Passing`, `OrRule.Passed`/`Failing`)
were removed. Neither `RuleResult.DecidedBy` itself, `SequentialEvaluator`'s
generic computation of it, nor `ShortCircuitEvaluator`'s `stopOn`-aware
override of it (see [`ShortCircuitEvaluator.cs`](../../csharp/src/VerdictRules/ShortCircuitEvaluator.cs))
had ever been through mutation testing
before this run. Every survivor below was re-investigated from scratch
against the current source; the two carried-forward equivalence findings
(the `ConfigureAwait` class and the `ShortCircuitEvaluator.cs` line 46
coverage-attribution artifact) were re-verified against the current code
rather than assumed to still apply unchanged.

The run (`csharp/src/VerdictRules/` via
[`scripts/run_mutation_csharp.sh`](../../scripts/run_mutation_csharp.sh),
Stryker.NET 5.0.0, 214 mutants across the package's fourteen source files)
started from a first pass against the suite as it stood before this work: 35
`CompileError`, 18 `Ignored`, 161 scored, 147 killed, 14 undetected (91.3%).
Of those 14, two were real gaps, both concentrated in exactly the code this
mutation run exists to exercise for the first time:

- `RuleResult.cs`'s own `DecidedBy` default (`decidedBy ?? []`) had never
  been read on a bare leaf result anywhere in the suite -- every existing
  `DecidedBy` assertion reads a *composite's* result (`AndRule`/`OrRule`/
  `NotRule`), never a plain `FunctionRule` leaf's.
- `SequentialEvaluator.cs`'s generic `DecidedBy` computation for an
  early, in-loop decision (`soFar.Count == rules.Count ? soFar : [latest]`)
  had never been exercised directly through `SequentialEvaluator` with a
  custom decider that decides early with sub-rules still unevaluated, nor
  with one that decides in-loop exactly on the last item -- the only
  existing direct-`SequentialEvaluator` test exercises its *post-loop
  fallback*, a different branch entirely.

Each got a new test that kills it (two tests total). A re-run confirmed
150/161 scored mutants detected (93.2%). The remaining 11 survivors are
documented below: 9 in one `ConfigureAwait` equivalence class, and 2 that
are a single, already-understood Stryker coverage-attribution artifact
carried forward from the prior run and re-confirmed against the current code.

## The two real gaps, and the tests that now kill them

| Survivor | Mutation | Killed by |
| :-- | :-- | :-- |
| `RuleResult.cs` line 72, `decidedBy ?? []` with the `?? []` removed (leaving the bare, nullable `decidedBy` parameter assigned directly) | No test ever read `DecidedBy` on a leaf result -- `FunctionRule<TContext>.EvaluateAsync` (`FunctionRuleT.cs` line 48) constructs its `RuleResult` without passing `decidedBy` at all, relying entirely on this default, and nothing exercised that path directly | `RuleTests.cs`: `DecidedByTests.ALeafResultDefaultsDecidedByToEmptyRatherThanNull` -- evaluates a bare `Rules.Pass("leaf")` and asserts `DecidedBy` is non-null and empty, rather than assuming the default is only ever reached through a composite's own vacuous case (which already has its own coverage) |
| `SequentialEvaluator.cs` line 81, `soFar.Count == rules.Count ? soFar : [latest]`, both `Conditional` branches (force-`true`, force-`false`) | The only existing direct-`SequentialEvaluator` test (`SequentialEvaluatorIdiomTests.PostLoopFallbackConsultsTheDeciderRatherThanJumpingStraightToVacuousResult`) exercises the *post-loop fallback* at line 87, a separate line that unconditionally sets `decidedBy: soFar` -- it never reaches this line at all, since its decider always declines (`null`) during the loop itself | `CSharpIdiomTests.cs`: `SequentialEvaluatorIdiomTests.EarlyDecisionWithItemsStillUnevaluatedNamesOnlyTheTriggeringSubResult` (a decider that commits in-loop with sub-rules still unevaluated -- kills the force-`true` mutant, which would wrongly report every sub-result seen so far instead of just the trigger) and `EarlyDecisionThatLandsExactlyOnTheLastItemNamesEveryEvaluatedSubResult` (a decider that commits in-loop on exactly the last sub-rule, `soFar.Count == rules.Count` by coincidence rather than via the fallback -- kills the force-`false` mutant, which would wrongly report only the last item instead of every evaluated sub-result) |

## The nine `ConfigureAwait(false)` mutants

| Location | Mutation |
| :-- | :-- |
| `FunctionRuleT.cs` line 47 | `.ConfigureAwait(false)` -> `.ConfigureAwait(true)` |
| `NotRuleT.cs` line 50 | `.ConfigureAwait(false)` -> `.ConfigureAwait(true)` |
| `SequentialEvaluator.cs` line 70 | `.ConfigureAwait(false)` -> `.ConfigureAwait(true)` |
| `ShortCircuitEvaluator.cs` line 64 | `.ConfigureAwait(false)` -> `.ConfigureAwait(true)` |
| `RulesEngineT.cs` line 75 (`RunAllAsync`) | `.ConfigureAwait(false)` -> `.ConfigureAwait(true)` |
| `RulesEngineT.cs` line 102 (`TryRunNamedAsync`) | `.ConfigureAwait(false)` -> `.ConfigureAwait(true)` |
| `RulesEngineT.cs` line 113 (`RunNamedAsync`) | `.ConfigureAwait(false)` -> `.ConfigureAwait(true)` |
| `RulesEngineT.cs` line 142 (`TryRunGroupAsync`) | `.ConfigureAwait(false)` -> `.ConfigureAwait(true)` |
| `RulesEngineT.cs` line 158 (`RunGroupAsync`) | `.ConfigureAwait(false)` -> `.ConfigureAwait(true)` |

This is the same equivalence class the pre-redesign record first
investigated in depth, carried forward rather than rediscovered:
`ConfigureAwait(false)` on every library `await` is a documented convention
of this package (`csharp/AGENTS.md`), kept to avoid a deadlock risk in a
caller with a single-threaded `SynchronizationContext`. Flipping it to
`true` changes no value any consumer can observe through this package's
public contract -- not a `RuleResult`/`RunResult` field, not an exception's
type or message, not evaluation order, not which sub-rules run before a
cancellation or a short-circuit. The only thing it can possibly change is
which `SynchronizationContext`, if any, a continuation is marshalled
through, a question a dedicated investigation (preserved below) built a
real test for and found to be sensitive to unrelated thread-pool warm-up
state rather than this package's own logic.

`ShortCircuitEvaluator.cs` line 64 is the one genuinely new site in this
list: `ShortCircuitEvaluator<TContext>` previously had no `EvaluateAsync` of
its own at all (`AndRule`/`OrRule` called straight through to
`SequentialEvaluator`) -- it gained one specifically to recompute
`DecidedBy` after delegating (§3d of the redesign plan), and that new
method's own `await _inner.EvaluateAsync(...)` carries a `ConfigureAwait(false)`
like every other `await` in this package. The reasoning above is about
`ConfigureAwait`'s own semantics, not about which type happens to contain
the call, so it applies here without needing to be re-proven from scratch --
confirmed anyway, the same way as every other site: no `DecidedBy`,
`Passed`, `SubResults`, `Detail`, or `Data` on the returned `RuleResult`
depends on which continuation context resumes the `await`.

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

## Two survivors, one mechanism: `ShortCircuitEvaluator.cs` lines 44 and 46

Both re-verified against the current source by hand-applying the mutation
and running the full `dotnet test` suite (143 tests, not Stryker's
coverage-optimized subset) -- the same method the prior record used, redone
rather than assumed.

### Line 44, genuinely equivalent

```csharp
return soFar.Count == total ? !stopOn : null;
```

Stryker's `Conditional (false)` mutation forces this line's condition to
the constant `false`, so the expression always evaluates to `null`
regardless of whether the sub-rule list is actually exhausted. This is
provably indistinguishable from the original, not merely hard to
trigger: `SequentialEvaluator.EvaluateAsync`'s own post-loop fallback
(line 87) re-invokes the *exact same decider* with the *exact same
arguments* (`soFar[^1]`, `soFar`, `rules.Count`) whenever the loop
finishes without an earlier verdict. Since this decider is a pure
closure over `stopOn` with no side effects, that second call reaches
the identical mutated branch and returns `null` again, falling through
to `?? vacuousResult`. `ShortCircuitEvaluator`'s constructor always sets
`vacuousResult: !stopOn` (line 46) -- exactly the value the unmutated
ternary would have returned directly. So whether the `!stopOn` comes
from the ternary's own true branch or from the fallback's `vacuousResult`,
the final `RuleResult`'s `Passed` carries the same value and `SubResults`
is built identically in the loop body regardless of what the decider
returns. `ShortCircuitEvaluator.EvaluateAsync` separately recomputes
`DecidedBy` from `result.SubResults` after delegating (lines 65-69), never
from whatever the decider itself returned, so this mutation can't leak
into `DecidedBy` either. No input through `ShortCircuitEvaluator`'s public
surface can make the mutated and original code disagree.

Verified empirically again this run: hand-applied to `ShortCircuitEvaluator.cs`,
full `dotnet test` (143 tests) passed without a single failure.

### Line 46, neither equivalent nor a real gap -- the same coverage-attribution artifact as before

```csharp
vacuousResult: !stopOn);
```

Stryker's `LogicalNotExpression` mutation here (`!stopOn` -> `stopOn`)
genuinely changes behavior -- confirmed empirically again this run by
hand-applying it and running the full `dotnet test` suite (not Stryker's
coverage-based subset): the same four tests fail as before,
`AndRuleTests.EmptyRuleListVacuouslyPasses`, `OrRuleTests.EmptyRuleListVacuouslyFails`,
`RunGroupTests.EmptyCompositeStillPassesVacuously`, and
`MixedCompositeTreeTests.AGenuinelyVacuousCompositeNestedInsideALargerFailingTree`
-- exactly the tests that pin `AndRule`/`OrRule`'s own documented
vacuous-truth constants (`AndRule.VacuousResult`/`OrRule.VacuousResult`,
§3a of the redesign plan). This is not equivalence by any definition this
project uses.

It survived Stryker's actual run anyway, for the same reason documented
previously: `AndRule<TContext>`/`OrRule<TContext>` each hold a
`private static readonly ShortCircuitEvaluator<TContext> Evaluator` field
(one shared instance per closed generic type, constructed once). C#'s
static-field initializer for a given closed generic type runs exactly once
per process, triggered by whichever test happens to be the *first* in the
whole suite to touch that specific closed type. Stryker's coverage-capture
pass records only that first-toucher as "covering" the initializer line --
every later test that constructs a new `AndRule`/`OrRule` and depends on the
*value* the field already holds is, from Stryker's instrumentation's point
of view, not touching that line at all, because it genuinely isn't
re-executing it. When Stryker then re-runs just the (small,
coverage-selected) subset of tests it believes cover a given mutant, the
tests that would actually catch this one -- the vacuous-result assertions
-- are never in that subset, because none of them happened to be the first
`AndRule`/`OrRule` test the suite ran.

No new test closes this, because the mechanism is about *when* a
one-time static initializer runs relative to Stryker's fixed
coverage-capture ordering, not about what the suite asserts -- any new
vacuous-result test would need to already be the first `AndRule`/`OrRule`
test the suite happens to run to be included in Stryker's coverage set
for this mutant, which isn't something a test can control. The behavior
this mutation breaks is already correctly pinned by the four tests named
above, re-proven by hand-applying the mutation and watching exactly those
four fail against the full suite. If Stryker or its coverage-analysis mode
changes how it attributes coverage for lazily-initialized static fields in
a future version, re-run and confirm whether this one still needs its own
section.

If a future run finds another mutant genuinely indistinguishable from
the original (no input would make the mutated and original code
disagree), or another instance of this same coverage-attribution
artifact, add it as a new row or section above instead of silently
skipping it -- see
[`mutation-testing.md`](mutation-testing.md#a-surviving-mutant-needs-one-of-two-things)
for what qualifies as equivalent.
