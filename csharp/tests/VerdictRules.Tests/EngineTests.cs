using Xunit;

namespace VerdictRules.Tests;

/// <summary>
/// Helpers shared across the suite.
/// </summary>
internal static class Rules
{
    /// <summary>
    /// A rule that records every evaluation, so short-circuiting can be proven
    /// by what actually ran rather than by the final boolean alone. A port that
    /// evaluated concurrently would return the same boolean and fail only here.
    /// </summary>
    public static FunctionRule Counting(string name, bool passes, List<string> log, string? group = null) =>
        new(name, ctx =>
        {
            log.Add(name);
            return Task.FromResult(new RuleResult(name, passes));
        }, group);

    public static readonly IReadOnlyDictionary<string, object?> Empty =
        new Dictionary<string, object?>();
}

public class FunctionRuleTests
{
    /// <summary>A plain static method — no lambda, no type declared.</summary>
    private static Task<RuleResult> HasQuorum(IReadOnlyDictionary<string, object?> ctx) =>
        Task.FromResult(new RuleResult("quorum", ctx.Count >= 3));

    [Fact]
    public async Task AMethodGroupIsARuleWithNothingDeclared()
    {
        // C#'s structural typing for delegates: HasQuorum was never declared to
        // be anything rule-shaped, and matching the signature is enough. This is
        // the same property Python's Protocol and TypeScript's interfaces give
        // for whole objects; C# gives it for functions.
        var rule = new FunctionRule("quorum", HasQuorum);

        var result = await new AndRule("composed", new IRule[] { rule })
            .EvaluateAsync(new Dictionary<string, object?> { ["a"] = 1, ["b"] = 2, ["c"] = 3 });

        Assert.True(result.Passed);
    }

    [Fact]
    public async Task ReturnsWhateverThePredicateReturnsUnchanged()
    {
        var rule = new FunctionRule("r", _ =>
            Task.FromResult(new RuleResult("r", true, "why", new { K = 1 })));

        var result = await rule.EvaluateAsync(Rules.Empty);

        Assert.True(result.Passed);
        Assert.Equal("why", result.Detail);
        Assert.NotNull(result.Data);
    }
}

public class AndRuleTests
{
    [Fact]
    public async Task PassesWhenEverySubRulePassesAndEvaluatesAllOfThem()
    {
        var log = new List<string>();
        var result = await new AndRule("all", new IRule[]
        {
            Rules.Counting("a", true, log),
            Rules.Counting("b", true, log),
        }).EvaluateAsync(Rules.Empty);

        Assert.True(result.Passed);
        Assert.Equal(new[] { "a", "b" }, log);
    }

    [Fact]
    public async Task ShortCircuitsSoLaterSubRulesNeverRun()
    {
        var log = new List<string>();
        var result = await new AndRule("all", new IRule[]
        {
            Rules.Counting("a", true, log),
            Rules.Counting("b", false, log),
            Rules.Counting("c", true, log),
        }).EvaluateAsync(Rules.Empty);

        Assert.False(result.Passed);
        Assert.Equal(new[] { "a", "b" }, log);
    }

    [Fact]
    public async Task DataHoldsOnlyWhatRanNeverPaddedNeverFlattened()
    {
        var log = new List<string>();
        var result = await new AndRule("outer", new IRule[]
        {
            new AndRule("inner", new IRule[] { Rules.Counting("deep", false, log) }),
            Rules.Counting("never", true, log),
        }).EvaluateAsync(Rules.Empty);

        var top = Assert.IsType<List<RuleResult>>(result.Data);
        Assert.Single(top);
        Assert.Equal("inner", top[0].RuleName);

        var nested = Assert.IsType<List<RuleResult>>(top[0].Data);
        Assert.Equal("deep", nested[0].RuleName);
    }

    [Fact]
    public async Task EmptyPassesVacuously()
    {
        var result = await new AndRule("none", Array.Empty<IRule>()).EvaluateAsync(Rules.Empty);
        Assert.True(result.Passed);
    }
}

public class OrRuleTests
{
    [Fact]
    public async Task ShortCircuitsOnTheFirstPass()
    {
        var log = new List<string>();
        var result = await new OrRule("any", new IRule[]
        {
            Rules.Counting("a", false, log),
            Rules.Counting("b", true, log),
            Rules.Counting("c", true, log),
        }).EvaluateAsync(Rules.Empty);

        Assert.True(result.Passed);
        Assert.Equal(new[] { "a", "b" }, log);
    }

    [Fact]
    public async Task EmptyFailsVacuouslyTheOppositePolarityToAndRule()
    {
        var result = await new OrRule("none", Array.Empty<IRule>()).EvaluateAsync(Rules.Empty);
        Assert.False(result.Passed);
    }
}

public class RunModeTests
{
    [Fact]
    public async Task RunAllNeverShortCircuits()
    {
        var log = new List<string>();
        var engine = new RulesEngine(new IRule[]
        {
            Rules.Counting("a", false, log),
            Rules.Counting("b", false, log),
            Rules.Counting("c", true, log),
        });

        var result = await engine.RunAllAsync(Rules.Empty);

        Assert.False(result.Passed);
        Assert.Equal(3, result.Results.Count);
        Assert.Equal(new[] { "a", "b", "c" }, log);
    }

    [Fact]
    public async Task RunGroupEvaluatesOnlyItsOwnGroup()
    {
        var log = new List<string>();
        var engine = new RulesEngine(new IRule[]
        {
            Rules.Counting("a", true, log, "g1"),
            Rules.Counting("b", true, log, "g2"),
            Rules.Counting("c", false, log, "g1"),
        });

        var result = await engine.RunGroupAsync("g1", Rules.Empty);

        Assert.Equal(new[] { "a", "c" }, result.Results.Select(r => r.RuleName));
        Assert.False(result.Passed);
    }

    [Fact]
    public async Task RunNamedLooksOneRuleUp()
    {
        var engine = new RulesEngine(new IRule[] { Rules.Counting("a", true, new List<string>()) });
        var result = await engine.RunNamedAsync("a", Rules.Empty);
        Assert.Equal("a", result.RuleName);
    }
}

public class EmptinessIsNotAbsenceTests
{
    [Fact]
    public async Task UnknownRuleNameThrows()
    {
        var engine = new RulesEngine(new IRule[] { Rules.Counting("a", true, new List<string>(), "g1") });
        await Assert.ThrowsAsync<KeyNotFoundException>(() => engine.RunNamedAsync("nope", Rules.Empty));
    }

    [Fact]
    public async Task UnknownGroupThrowsRatherThanPassingVacuously()
    {
        // A group exists only because some rule declared it, so a lookup that
        // matches nothing can only be a typo. Returning a pass would mean a
        // misspelled group silently approves.
        var engine = new RulesEngine(new IRule[] { Rules.Counting("a", true, new List<string>(), "g1") });
        await Assert.ThrowsAsync<KeyNotFoundException>(() => engine.RunGroupAsync("no-such-group", Rules.Empty));
    }

    [Fact]
    public async Task ARuleWithNoGroupMakesNoGroupExist()
    {
        var engine = new RulesEngine(new IRule[] { Rules.Counting("a", true, new List<string>()) });
        await Assert.ThrowsAsync<KeyNotFoundException>(() => engine.RunGroupAsync("g1", Rules.Empty));
    }

    [Fact]
    public async Task ButEmptyCompositesStillFoldToTheirIdentity()
    {
        Assert.True((await new AndRule("none", Array.Empty<IRule>()).EvaluateAsync(Rules.Empty)).Passed);
        Assert.False((await new OrRule("none", Array.Empty<IRule>()).EvaluateAsync(Rules.Empty)).Passed);
    }
}

/// <summary>
/// The non-throwing primitives, and that the strict forms sit on top of them.
/// These exist because the engine cannot know what an absent group means: for
/// one consumer it is "no constraint applies", for another "the configuration
/// is broken". A library default would be right for one and wrong for the rest.
/// </summary>
public class TryLookupTests
{
    [Fact]
    public async Task ReturnsTheResultWhenPresent()
    {
        var log = new List<string>();
        var engine = new RulesEngine(new IRule[]
        {
            Rules.Counting("a", true, log, "g1"),
            Rules.Counting("b", false, log, "g1"),
        });

        var group = await engine.TryRunGroupAsync("g1", Rules.Empty);
        Assert.NotNull(group);
        Assert.Equal(new[] { "a", "b" }, group!.Results.Select(r => r.RuleName));
        Assert.False(group.Passed);

        var named = await engine.TryRunNamedAsync("a", Rules.Empty);
        Assert.NotNull(named);
        Assert.Equal("a", named!.RuleName);
    }

    [Fact]
    public async Task ReturnsNullWhenAbsent()
    {
        var engine = new RulesEngine(new IRule[] { Rules.Counting("a", true, new List<string>(), "g1") });
        Assert.Null(await engine.TryRunGroupAsync("no-such-group", Rules.Empty));
        Assert.Null(await engine.TryRunNamedAsync("nope", Rules.Empty));
    }

    [Fact]
    public async Task NullMeansAbsentNeverFailed()
    {
        // Collapsing the two would make a typo indistinguishable from a
        // legitimate rejection.
        var engine = new RulesEngine(new IRule[] { Rules.Counting("present", false, new List<string>()) });

        var failed = await engine.TryRunNamedAsync("present", Rules.Empty);
        Assert.NotNull(failed);
        Assert.False(failed!.Passed);

        Assert.Null(await engine.TryRunNamedAsync("absent", Rules.Empty));
    }

    [Fact]
    public async Task StrictFormsAreTheTryFormsPlusAnAssertion()
    {
        // Asserting the relationship keeps the two from drifting: one lookup
        // path, and the strict form adds only the throw.
        var engine = new RulesEngine(new IRule[] { Rules.Counting("a", true, new List<string>(), "g1") });

        var strict = await engine.RunGroupAsync("g1", Rules.Empty);
        var lenient = await engine.TryRunGroupAsync("g1", Rules.Empty);

        Assert.NotNull(lenient);
        Assert.Equal(strict.Passed, lenient!.Passed);
        Assert.Equal(strict.Results.Count, lenient.Results.Count);
    }

    /// <summary>
    /// The full matrix a caller faces: three states a lookup can be in,
    /// against the three things a caller can decide absence means.
    /// </summary>
    /// <remarks>
    /// The interesting rows are the ones where the default must <i>not</i>
    /// fire. A fallback firing on a present-but-failing group turns a real
    /// rejection into a silent approval, which is the whole failure this API
    /// exists to let callers avoid.
    /// <code>
    ///   group state       | ?? true | ?? false | strict
    ///   ------------------+---------+----------+---------------
    ///   present, passing  | true    | true     | Passed = true
    ///   present, failing  | false   | false    | Passed = false   &lt;- must not fire
    ///   absent            | true    | false    | throws
    /// </code>
    /// </remarks>
    [Theory]
    [InlineData("passing", true, true, false)]
    [InlineData("failing", false, false, false)]
    [InlineData("absent", true, false, true)]
    public async Task FallbackMatrix(string group, bool defaultTrue, bool defaultFalse, bool strictThrows)
    {
        var log = new List<string>();
        var engine = new RulesEngine(new IRule[]
        {
            Rules.Counting("p", true, log, "passing"),
            Rules.Counting("f", false, log, "failing"),
        });

        var result = await engine.TryRunGroupAsync(group, Rules.Empty);

        Assert.Equal(defaultTrue, result?.Passed ?? true);
        Assert.Equal(defaultFalse, result?.Passed ?? false);

        if (strictThrows)
        {
            await Assert.ThrowsAsync<KeyNotFoundException>(
                () => engine.RunGroupAsync(group, Rules.Empty));
        }
        else
        {
            var strict = await engine.RunGroupAsync(group, Rules.Empty);
            Assert.Equal(defaultTrue, strict.Passed);
            Assert.Equal(defaultFalse, strict.Passed);
        }
    }

    [Fact]
    public async Task SkippingCountsOnlyWhatExists()
    {
        var engine = new RulesEngine(new IRule[] { Rules.Counting("f", false, new List<string>(), "failing") });

        var evaluated = new[]
        {
            await engine.TryRunGroupAsync("failing", Rules.Empty),
            await engine.TryRunGroupAsync("absent", Rules.Empty),
        }.Where(r => r is not null).ToList();

        Assert.Single(evaluated);
        Assert.False(evaluated[0]!.Passed);
    }
}

public class IntrospectionTests
{
    [Fact]
    public async Task ReportsExactlyWhatTheLookupsAccept()
    {
        var log = new List<string>();
        var engine = new RulesEngine(new IRule[]
        {
            Rules.Counting("a", true, log, "g1"),
            Rules.Counting("b", true, log, "g2"),
            Rules.Counting("c", true, log),
        });

        Assert.Equal(new[] { "a", "b", "c" }, engine.RuleNames);
        Assert.Equal(new[] { "g1", "g2" }, engine.GroupNames);

        foreach (var group in engine.GroupNames)
        {
            await engine.RunGroupAsync(group, Rules.Empty);
        }
    }

    [Fact]
    public void AnEmptyEngineReportsNothing()
    {
        var engine = new RulesEngine(Array.Empty<IRule>());
        Assert.Empty(engine.RuleNames);
        Assert.Empty(engine.GroupNames);
    }
}

/// <summary>
/// A rule shape owning its own Name and Group must declare <c>: IRule</c>,
/// because C# has no structural typing for multi-member interfaces. Delegates
/// are a different story — see the FunctionRule tests, where a plain method
/// group is a rule with nothing declared.
/// </summary>
public class CustomRuleTests
{
    private sealed class Custom : IRule
    {
        public string Name => "custom";
        public string? Group => null;
        public Task<RuleResult> EvaluateAsync(IReadOnlyDictionary<string, object?> context) =>
            Task.FromResult(new RuleResult(Name, true));
    }

    [Fact]
    public async Task AnExplicitImplementationComposesLikeAnyOther()
    {
        var result = await new AndRule("composed", new IRule[] { new Custom() }).EvaluateAsync(Rules.Empty);
        Assert.True(result.Passed);
    }
}
