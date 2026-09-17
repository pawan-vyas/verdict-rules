using Xunit;

namespace VerdictRules.Tests;

/// <summary>
/// A strict, 1:1 port of Python's <c>test_engine.py</c> — <see cref="RulesEngine"/>.
/// Every test here has an exact Python counterpart, same class, same
/// assertion. C#-specific coverage lives in <c>CSharpIdiomTests.cs</c>
/// instead, so this file stays auditable against Python's own suite
/// test-for-test.
/// </summary>
public class RunAllTests
{
    /// <summary>Mirrors <c>test_all_passing_rules_yields_passed_true</c>.</summary>
    [Fact]
    public async Task AllPassingRulesYieldsPassedTrue()
    {
        var engine = new RulesEngine(new IRule[] { Rules.Pass("a"), Rules.Pass("b") });
        var result = await engine.RunAllAsync(Rules.Empty);
        Assert.True(result.Passed);
        Assert.Equal(new[] { "a", "b" }, result.Results.Select(r => r.RuleName));
    }

    /// <summary>Mirrors <c>test_one_failing_rule_yields_passed_false</c>.</summary>
    [Fact]
    public async Task OneFailingRuleYieldsPassedFalse()
    {
        var engine = new RulesEngine(new IRule[] { Rules.Pass("a"), Rules.Fail("b") });
        var result = await engine.RunAllAsync(Rules.Empty);
        Assert.False(result.Passed);
    }

    /// <summary>Mirrors <c>test_does_not_short_circuit_unlike_and_rule</c>.</summary>
    [Fact]
    public async Task DoesNotShortCircuitUnlikeAndRule()
    {
        var engine = new RulesEngine(new IRule[] { Rules.Fail("a"), Rules.Pass("b") });
        var result = await engine.RunAllAsync(Rules.Empty);
        Assert.Equal(new[] { "a", "b" }, result.Results.Select(r => r.RuleName));
    }

    /// <summary>Mirrors <c>test_empty_engine_run_all_vacuously_passes</c>.</summary>
    [Fact]
    public async Task EmptyEngineRunAllVacuouslyPasses()
    {
        var engine = new RulesEngine(Array.Empty<IRule>());
        var result = await engine.RunAllAsync(Rules.Empty);
        Assert.True(result.Passed);
        Assert.Empty(result.Results);
    }
}

/// <summary>Mirrors Python's <c>TestRunNamed</c>.</summary>
public class RunNamedTests
{
    /// <summary>Mirrors <c>test_returns_that_rule_s_own_result</c>.</summary>
    [Fact]
    public async Task ReturnsThatRulesOwnResult()
    {
        var engine = new RulesEngine(new IRule[] { Rules.Pass("a"), Rules.Fail("b") });
        var result = await engine.RunNamedAsync("b", Rules.Empty);
        Assert.Equal("b", result.RuleName);
        Assert.False(result.Passed);
    }

    /// <summary>Mirrors <c>test_unknown_name_raises_key_error</c>.</summary>
    [Fact]
    public async Task UnknownNameRaisesKeyNotFound()
    {
        var engine = new RulesEngine(new IRule[] { Rules.Pass("a") });
        await Assert.ThrowsAsync<KeyNotFoundException>(() => engine.RunNamedAsync("missing", Rules.Empty));
    }
}

/// <summary>Mirrors Python's <c>TestRunGroup</c>.</summary>
public class RunGroupTests
{
    /// <summary>Mirrors <c>test_runs_only_matching_group</c>.</summary>
    [Fact]
    public async Task RunsOnlyMatchingGroup()
    {
        var engine = new RulesEngine(new IRule[]
        {
            Rules.Pass("a", group: "g1"),
            Rules.Pass("b", group: "g2"),
            Rules.Fail("c", group: "g1"),
        });

        var result = await engine.RunGroupAsync("g1", Rules.Empty);

        Assert.Equal(new[] { "a", "c" }, result.Results.Select(r => r.RuleName));
        Assert.False(result.Passed);
    }

    /// <summary>Mirrors <c>test_unknown_group_raises</c>.</summary>
    [Fact]
    public async Task UnknownGroupRaises()
    {
        var engine = new RulesEngine(new IRule[] { Rules.Pass("a", group: "g1") });
        await Assert.ThrowsAsync<KeyNotFoundException>(() => engine.RunGroupAsync("no-such-group", Rules.Empty));
    }

    /// <summary>Mirrors <c>test_ungrouped_rules_are_never_matched</c>.</summary>
    [Fact]
    public async Task UngroupedRulesAreNeverMatched()
    {
        var engine = new RulesEngine(new IRule[] { Rules.Pass("a") }); // no group
        await Assert.ThrowsAsync<KeyNotFoundException>(() => engine.RunGroupAsync("g1", Rules.Empty));
    }

    /// <summary>Mirrors <c>test_empty_composite_still_passes_vacuously</c>.</summary>
    [Fact]
    public async Task EmptyCompositeStillPassesVacuously()
    {
        Assert.True((await new AndRule("none", Array.Empty<IRule>()).EvaluateAsync(Rules.Empty)).Passed);
        Assert.False((await new OrRule("none", Array.Empty<IRule>()).EvaluateAsync(Rules.Empty)).Passed);
    }
}

/// <summary>
/// Mirrors Python's <c>TestTryLookups</c> — the non-throwing primitives, and
/// that the strict ones sit on top of them. These exist because the engine
/// cannot know what an absent group means: for one consumer it's "no
/// constraint applies, pass", for another "skip this and don't count it",
/// for a third "the configuration is wrong, fail loudly". A library default
/// would be right for one of them and wrong for the rest.
/// </summary>
public class TryLookupTests
{
    /// <summary>Mirrors <c>test_try_run_group_returns_the_result_when_present</c>.</summary>
    [Fact]
    public async Task TryRunGroupReturnsTheResultWhenPresent()
    {
        var engine = new RulesEngine(new IRule[] { Rules.Pass("a", group: "g1"), Rules.Fail("b", group: "g1") });

        var result = await engine.TryRunGroupAsync("g1", Rules.Empty);

        Assert.NotNull(result);
        Assert.Equal(new[] { "a", "b" }, result!.Results.Select(r => r.RuleName));
        Assert.False(result.Passed);
    }

    /// <summary>Mirrors <c>test_try_run_group_returns_none_when_absent</c>.</summary>
    [Fact]
    public async Task TryRunGroupReturnsNullWhenAbsent()
    {
        var engine = new RulesEngine(new IRule[] { Rules.Pass("a", group: "g1") });
        Assert.Null(await engine.TryRunGroupAsync("no-such-group", Rules.Empty));
    }

    /// <summary>Mirrors <c>test_try_run_named_returns_the_result_when_present</c>.</summary>
    [Fact]
    public async Task TryRunNamedReturnsTheResultWhenPresent()
    {
        var engine = new RulesEngine(new IRule[] { Rules.Pass("a") });
        var result = await engine.TryRunNamedAsync("a", Rules.Empty);
        Assert.NotNull(result);
        Assert.Equal("a", result!.RuleName);
    }

    /// <summary>Mirrors <c>test_try_run_named_returns_none_when_absent</c>.</summary>
    [Fact]
    public async Task TryRunNamedReturnsNullWhenAbsent()
    {
        var engine = new RulesEngine(new IRule[] { Rules.Pass("a") });
        Assert.Null(await engine.TryRunNamedAsync("nope", Rules.Empty));
    }

    /// <summary>Mirrors <c>test_none_means_absent_never_failed</c>.</summary>
    [Fact]
    public async Task NullMeansAbsentNeverFailed()
    {
        var engine = new RulesEngine(new IRule[] { Rules.Fail("present", group: "g1") });

        var failed = await engine.TryRunNamedAsync("present", Rules.Empty);
        Assert.NotNull(failed);
        Assert.False(failed!.Passed);

        Assert.Null(await engine.TryRunNamedAsync("absent", Rules.Empty));
    }

    /// <summary>Mirrors <c>test_strict_forms_are_the_try_forms_plus_an_assertion</c>.</summary>
    [Fact]
    public async Task StrictFormsAreTheTryFormsPlusAnAssertion()
    {
        var engine = new RulesEngine(new IRule[] { Rules.Pass("a", group: "g1") });

        var namedStrict = await engine.RunNamedAsync("a", Rules.Empty);
        var namedTry = await engine.TryRunNamedAsync("a", Rules.Empty);
        Assert.Equal(namedStrict.RuleName, namedTry!.RuleName);

        var groupStrict = await engine.RunGroupAsync("g1", Rules.Empty);
        var groupTry = await engine.TryRunGroupAsync("g1", Rules.Empty);
        Assert.NotNull(groupTry);
        Assert.Equal(groupStrict.Passed, groupTry!.Passed);
        Assert.Equal(groupStrict.Results.Count, groupTry.Results.Count);
    }

    /// <summary>
    /// Mirrors <c>test_fallback_matrix</c> — the full matrix a caller faces:
    /// three states a lookup can be in, against the three things a caller
    /// can decide absence means. The interesting rows are the ones where the
    /// default must <i>not</i> fire — a fallback firing on a
    /// present-but-failing group turns a real rejection into a silent
    /// approval, which is the whole failure this API exists to let callers
    /// avoid.
    /// <code>
    ///   group state       | ?? true | ?? false | strict
    ///   ------------------+---------+----------+---------------
    ///   present, passing  | true    | true     | Passed = true
    ///   present, failing  | false   | false    | Passed = false   &lt;- must not fire
    ///   absent            | true    | false    | throws
    /// </code>
    /// </summary>
    [Theory]
    [InlineData("passing", true, true, false)]
    [InlineData("failing", false, false, false)]
    [InlineData("absent", true, false, true)]
    public async Task FallbackMatrix(string group, bool defaultTrue, bool defaultFalse, bool strictThrows)
    {
        var engine = new RulesEngine(new IRule[] { Rules.Pass("p", group: "passing"), Rules.Fail("f", group: "failing") });

        var result = await engine.TryRunGroupAsync(group, Rules.Empty);

        Assert.Equal(defaultTrue, result?.Passed ?? true);
        Assert.Equal(defaultFalse, result?.Passed ?? false);

        if (strictThrows)
        {
            await Assert.ThrowsAsync<KeyNotFoundException>(() => engine.RunGroupAsync(group, Rules.Empty));
        }
        else
        {
            var strict = await engine.RunGroupAsync(group, Rules.Empty);
            Assert.Equal(defaultTrue, strict.Passed);
            Assert.Equal(defaultFalse, strict.Passed);
        }
    }

    /// <summary>Mirrors <c>test_skipping_counts_only_what_exists</c>.</summary>
    [Fact]
    public async Task SkippingCountsOnlyWhatExists()
    {
        var engine = new RulesEngine(new IRule[] { Rules.Fail("f", group: "failing") });

        var evaluated = new[]
        {
            await engine.TryRunGroupAsync("failing", Rules.Empty),
            await engine.TryRunGroupAsync("absent", Rules.Empty),
        }.Where(r => r is not null).ToList();

        Assert.Single(evaluated);
        Assert.False(evaluated[0]!.Passed);
    }
}

/// <summary>Mirrors Python's <c>TestIntrospection</c> — <c>RuleNames</c>/<c>GroupNames</c> let a caller check instead of catching.</summary>
public class IntrospectionTests
{
    /// <summary>Mirrors <c>test_reports_registered_names_in_order</c>.</summary>
    [Fact]
    public void ReportsRegisteredNamesInOrder()
    {
        var engine = new RulesEngine(new IRule[] { Rules.Pass("a", group: "g1"), Rules.Pass("b", group: "g2"), Rules.Pass("c") });
        Assert.Equal(new[] { "a", "b", "c" }, engine.RuleNames);
        Assert.Equal(new[] { "g1", "g2" }, engine.GroupNames);
    }

    /// <summary>Mirrors <c>test_group_names_is_exactly_what_run_group_accepts</c>.</summary>
    [Fact]
    public async Task GroupNamesIsExactlyWhatRunGroupAccepts()
    {
        var engine = new RulesEngine(new IRule[] { Rules.Pass("a", group: "g1"), Rules.Pass("b") });

        foreach (var group in engine.GroupNames)
        {
            await engine.RunGroupAsync(group, Rules.Empty); // must not throw
        }

        Assert.DoesNotContain("g2", engine.GroupNames);
        await Assert.ThrowsAsync<KeyNotFoundException>(() => engine.RunGroupAsync("g2", Rules.Empty));
    }

    /// <summary>Mirrors <c>test_empty_engine_reports_nothing</c>.</summary>
    [Fact]
    public void EmptyEngineReportsNothing()
    {
        var engine = new RulesEngine(Array.Empty<IRule>());
        Assert.Empty(engine.RuleNames);
        Assert.Empty(engine.GroupNames);
    }
}

/// <summary>Mirrors Python's <c>TestConstruction</c>.</summary>
public class ConstructionTests
{
    /// <summary>
    /// Mirrors <c>test_duplicate_names_last_one_wins_in_by_name_lookup</c>.
    /// Python's version reaches into the engine's own private <c>_by_name</c>
    /// dict directly; C#'s <c>_byName</c> is truly private (not just
    /// convention), so this asserts the same fact through the public API
    /// instead — which rule <c>RunNamedAsync</c> actually returns for a
    /// duplicated name.
    /// </summary>
    [Fact]
    public async Task DuplicateNamesLastOneWinsInByNameLookup()
    {
        var engine = new RulesEngine(new IRule[] { Rules.Pass("a"), Rules.Fail("a") });
        var result = await engine.RunNamedAsync("a", Rules.Empty);
        Assert.False(result.Passed); // the second registration ("a", failing) won
    }
}

/// <summary>
/// Mirrors Python's <c>TestExceptionPropagation</c> in <c>test_engine.py</c> —
/// a predicate's own exception is never caught anywhere in the engine, and
/// propagates exactly as if the caller had invoked the predicate directly.
/// </summary>
public class EngineExceptionPropagationTests
{
    /// <summary>Mirrors <c>test_run_all_does_not_catch_a_predicate_s_exception</c>.</summary>
    [Fact]
    public async Task RunAllDoesNotCatchAPredicatesException()
    {
        var engine = new RulesEngine(new IRule[]
        {
            Rules.Pass("a"),
            new FunctionRule("flaky", (_, _) => throw new TimeoutException("external check unreachable")),
            Rules.Pass("c"),
        });

        await Assert.ThrowsAsync<TimeoutException>(() => engine.RunAllAsync(Rules.Empty));
    }

    /// <summary>Mirrors <c>test_run_group_does_not_catch_a_predicate_s_exception</c>.</summary>
    [Fact]
    public async Task RunGroupDoesNotCatchAPredicatesException()
    {
        var engine = new RulesEngine(new IRule[]
        {
            new FunctionRule("flaky", (_, _) => throw new InvalidOperationException("bad input"), "g"),
        });

        await Assert.ThrowsAsync<InvalidOperationException>(() => engine.RunGroupAsync("g", Rules.Empty));
    }
}
