<!-- Title: Mutation Survivors -- C# SDK -->
# Mutation survivors -- C# SDK

> C#'s own per-language record required by
> [`mutation-testing.md`](mutation-testing.md) -- every mutant
> [Stryker.NET](https://stryker-mutator.io/docs/stryker-net/introduction/)
> found that the test suite didn't catch, and what happened to it.

## Current status: seven mutants documented below, everything else killed

The most recent run (`csharp/src/VerdictRules/` via
[`scripts/run_mutation_csharp.sh`](../../scripts/run_mutation_csharp.sh),
Stryker.NET 5.0.0, 144 mutants across the package's eight source files, 110
of them scored -- the other 34 are `CompileError` or `Ignored` and excluded
from the denominator by Stryker itself) started from a first pass against
the suite as it stood before this work: 18 surviving mutants plus 38 more
never even reached by the dry run (`NoCoverage`), 56 undetected out of 110
scored (49.09%). 49 of those 56 were real gaps; each got a new test that
kills it, and a re-run confirmed 103/110 scored mutants detected (93.64%).
The remaining 7 survivors are documented below -- not as classically
equivalent, but as a narrower, investigated case explained in its own
section beneath the table.

The 49 real gaps, grouped by what exposed them, and the tests that now kill them:

| Survivor | Mutation | Killed by |
| :-- | :-- | :-- |
| `AndRuleT.cs` line 50, `OrRuleT.cs` line 50, `RulesEngineT.cs` lines 74 and 141 | `cancellationToken.ThrowIfCancellationRequested();` deleted from each composite's own per-iteration loop check | `GenericsTests.cs`: `AndRuleOfTChecksCancellationBetweenSubRulesEvenWhenASubRuleDoesNotCheckItself`, `OrRuleOfTChecksCancellationBetweenSubRulesEvenWhenASubRuleDoesNotCheckItself`, `RulesEngineOfTRunAllChecksCancellationBetweenRulesEvenWhenARuleDoesNotCheckItself`, `RulesEngineOfTRunGroupChecksCancellationBetweenRulesEvenWhenARuleDoesNotCheckItself` |
| `AndRuleT.cs` lines 55-57 and 56 | The failure-detail ternary forced to always take the "with detail" branch, and its "no detail" string literal blanked | `GenericsTests.cs`: `AndRuleOfTFormatsAFailureWithNoDetailDifferentlyFromOneWithDetail` |
| `OrRuleT.cs` line 52 | `subResults.Add(result);` deleted from `OrRule<TContext>.EvaluateAsync`'s loop | `GenericsTests.cs`: `OrRuleOfTAccumulatesEverySubResultEvenWhenAllFail` |
| `OrRuleT.cs` line 65 (x3, one of them `NoCoverage`) | The group-suffix ternary in `OrRule<TContext>.ToString()`, and the `string.IsNullOrEmpty(Group)` check driving it, each mutated independently -- never exercised with a group set at all | `DiagnosticsTests.cs`: `OrRuleShowsItsGroupWhenPresent`, `GenericOrRuleShowsItsGroupWhenPresent` (the non-generic test alone also kills the generic mutants, since `OrRule.ToString()` forwards to the generic implementation) |
| `RulesEngineT.cs` line 114 | `` $"No rule named '{name}' in this engine" `` blanked | `GenericsTests.cs`: `RulesEngineOfTUnknownNameRaisesKeyNotFound`, extended to assert `ex.Message` |
| `RulesEngineT.cs` line 159 | `` $"No rules in group '{group}' in this engine" `` blanked | `GenericsTests.cs`: `RulesEngineOfTUnknownGroupRaisesKeyNotFound` (new -- no generic test had ever exercised an unknown group before) |
| `RuleResult.cs` lines 41-42 (x2 survived, x2 `NoCoverage`) | The `Passed ? "PASS" : "FAIL"` ternary inside each branch of `ToString()` forced to a constant outcome, and the "FAIL" literal in each branch never reached at all | `DiagnosticsTests.cs`: `RuleResultShowsThePassOutcomeWithNoDetail`, `RuleResultShowsTheFailOutcomeWithNoDetail`, `RuleResultShowsTheFailOutcomeWithDetail`, `RuleResultShowsThePassOutcomeWithDetail` (all four combinations of `Passed` x "has detail"; the previous suite covered only two) |
| `RuleResult.cs` lines 40, 46-48 (`NoCoverage`, ~21 sub-mutants across the `Detail.Length == 0` check, both `PASS`/`FAIL` branches, the em-dash separator, and the sub-result-count suffix) | `ToString()`'s equality check, and every string/conditional inside the private `DebuggerDisplay` property, never reached by any test | `DiagnosticsTests.cs`: the four `ToString()` tests above, plus `RuleResultAndRunResultDebuggerDisplayTests`' `RuleResultDebuggerDisplayOmitsDetailWhenEmpty`, `RuleResultDebuggerDisplayShowsDetailWhenPresent`, `RuleResultDebuggerDisplayShowsSubResultCountWhenDataIsSubResults`, `RuleResultDebuggerDisplayOmitsSubResultCountWhenDataIsNotSubResults`, reading the private property via `DebuggerDisplayReflection` (`TestHelpers.cs`) |
| `RunResult.cs` lines 29-35 (`NoCoverage`, 12 sub-mutants across `ToString()` and the private `DebuggerDisplay` property, including the `!r.Passed` failing-count predicate) | `ToString()` and `DebuggerDisplay` never reached by any test | `DiagnosticsTests.cs`: `RunResultShowsThePassOutcomeAndRuleCount`, `RunResultShowsTheFailOutcomeAndRuleCount`, and `RuleResultAndRunResultDebuggerDisplayTests`' `RunResultDebuggerDisplayCountsFailuresSeparatelyFromTheTotal` (a mixed pass/fail `RunResult`, proving the "N failing" count comes from `!r.Passed` rather than being copied from the total) and `RunResultDebuggerDisplayShowsNoFailingWhenEveryRulePassed` |

The cancellation survivors existed because every prior test proving "a
composite stops checking cancellation between iterations" used
`FunctionRule`/`FunctionRule<TContext>` sub-rules exclusively, and
`FunctionRule<TContext>.EvaluateAsync` checks `ThrowIfCancellationRequested`
on its own entry (line 41 of `FunctionRuleT.cs`) -- so deleting the
composite's own per-iteration check was invisible: the sub-rule's own
guard threw at the same moment regardless, with the same observable
result (the next sub-rule's body never ran). A real third-party
`IRule<TContext>` is under no such obligation. `TestHelpers.cs` gained
`NonCheckingRule<TContext>`, a test double that deliberately skips its
own cancellation check, to prove the composite's own guard -- not the
sub-rule's cooperation -- is what stops it.

## The seven `ConfigureAwait(false)` mutants

| Location | Mutation |
| :-- | :-- |
| `AndRuleT.cs` line 51 | `.ConfigureAwait(false)` -> `.ConfigureAwait(true)` |
| `OrRuleT.cs` line 51 | `.ConfigureAwait(false)` -> `.ConfigureAwait(true)` |
| `RulesEngineT.cs` line 75 (`RunAllAsync`) | `.ConfigureAwait(false)` -> `.ConfigureAwait(true)` |
| `RulesEngineT.cs` line 102 (`TryRunNamedAsync`) | `.ConfigureAwait(false)` -> `.ConfigureAwait(true)` |
| `RulesEngineT.cs` line 113 (`RunNamedAsync`) | `.ConfigureAwait(false)` -> `.ConfigureAwait(true)` |
| `RulesEngineT.cs` line 142 (`TryRunGroupAsync`) | `.ConfigureAwait(false)` -> `.ConfigureAwait(true)` |
| `RulesEngineT.cs` line 158 (`RunGroupAsync`) | `.ConfigureAwait(false)` -> `.ConfigureAwait(true)` |

`ConfigureAwait(false)` on every library `await` is a documented
convention of this package (`csharp/AGENTS.md`), kept to avoid a deadlock
risk in a caller with a single-threaded `SynchronizationContext` (classic
ASP.NET, WinForms, WPF). Flipping it to `true` changes **no value any
consumer can observe through this package's public contract**: not a
single `RuleResult`/`RunResult` field, not an exception's type or
message, not evaluation order, not which sub-rules run before a
cancellation or a short-circuit. Every one of those is already pinned by
the rest of this suite, run with and without each of these seven
mutations individually re-introduced by hand to confirm it -- none of
them moved.

The only thing `ConfigureAwait` can possibly change is which
`SynchronizationContext`, if any, a continuation is marshalled back
through -- not a value this package's contract describes, but a real
difference in principle, so this was investigated rather than assumed
equivalent on that basis alone: a test was built with a custom
`SynchronizationContext` that counts its own `Post` invocations, paired
with a sub-rule that genuinely suspends (`await Task.Yield()` or
`await Task.Delay(...)`, never completing synchronously), to observe
whether a composite's own continuation gets re-marshalled. That test
**did** catch several of these mutants when each was re-introduced by
hand and run in isolation. But the same test, unchanged, produced a
different `Post` count across separate runs of the *same, unmutated*
code -- confirmed by five consecutive runs that were stable, followed by
a later run (after unrelated test classes were added to the same file,
with no change to the test itself or to the code under test) that
settled on a different, but then-again-stable, count. The CLR's
await-continuation machinery has an internal fast path
(`SynchronizationContextAwaitTaskContinuation`) that skips re-posting a
continuation when the resuming thread already carries the exact captured
context instance -- whether that fast path applies for a given
`ConfigureAwait` site depends on how many other `async` frames and
thread-pool hops happen to be in flight around it at that moment, which
this investigation found to be sensitive to code completely unrelated to
`verdict`'s own logic (an unrelated test class added to the same
compilation unit shifted thread-pool warm-up timing enough to flip the
result). A regression test whose pass/fail depends on that is not testing
this package's behavior; it is testing incidental CLR scheduling state,
and would fail intermittently in CI for reasons having nothing to do with
a real regression -- which is a materially worse outcome than a mutant
nobody is chasing. Given that (a) no documented, consumer-observable
contract differs, and (b) the one signal that can distinguish them in
principle was built, confirmed to work, and then confirmed to be
non-deterministic under conditions no different from ordinary test-suite
growth, these seven are treated as equivalent for this project's
purposes -- a narrower claim than "no input could ever distinguish them"
(one can, in a lab setting, with low-level `SynchronizationContext`
introspection), but one this investigation backs with more than
inconvenience.

If a future run finds another mutant genuinely indistinguishable from the
original (no input would make the mutated and original code disagree),
or revisits these seven with a deterministic way to pin
`SynchronizationContext` marshalling, add it as a new row or section
above instead of silently skipping it -- see
[`mutation-testing.md`](mutation-testing.md#a-surviving-mutant-needs-one-of-two-things)
for what qualifies.
