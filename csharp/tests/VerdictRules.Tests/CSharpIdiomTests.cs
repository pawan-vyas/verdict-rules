using Xunit;

namespace VerdictRules.Tests;

/// <summary>
/// Coverage with no Python counterpart, on purpose — capabilities or
/// constraints that only exist in C#: <see cref="System.Threading.CancellationToken"/>
/// propagation, structural typing for delegates only (not multi-member
/// interfaces), and an explicit <see cref="IRule"/> implementation. <c>RuleTests.cs</c>
/// and <c>EngineTests.cs</c> are the files to audit against Python's own
/// <c>test_rule.py</c>/<c>test_engine.py</c>; this one isn't part of that mirror.
/// </summary>
public class FunctionRuleIdiomTests
{
    /// <summary>A plain static method — no lambda, no type declared.</summary>
    private static Task<RuleResult> HasQuorum(IReadOnlyDictionary<string, object?> ctx, CancellationToken cancellationToken = default) =>
        Task.FromResult(new RuleResult("quorum", ctx.Count >= 3));

    [Fact]
    public async Task AMethodGroupIsARuleWithNothingDeclared()
    {
        // C#'s structural typing for delegates: HasQuorum was never declared
        // to be anything rule-shaped, and matching the signature is enough.
        // This is the same property Python's Protocol and TypeScript's
        // interfaces give for whole objects; C# gives it for functions.
        var rule = new FunctionRule("quorum", HasQuorum);

        var result = await new AndRule("composed", new IRule[] { rule })
            .EvaluateAsync(new Dictionary<string, object?> { ["a"] = 1, ["b"] = 2, ["c"] = 3 });

        Assert.True(result.Passed);
    }

    [Fact]
    public async Task ReturnsWhateverThePredicateReturnsUnchanged()
    {
        var rule = new FunctionRule("r", (_, _) =>
            Task.FromResult(new RuleResult("r", true, "why", new { K = 1 })));

        var result = await rule.EvaluateAsync(Rules.Empty);

        Assert.True(result.Passed);
        Assert.Equal("why", result.Detail);
        Assert.NotNull(result.Data);
    }
}

public class AndRuleIdiomTests
{
    [Fact]
    public async Task CancellationStopsBeforeTheNextSubRuleEvenMidRun()
    {
        // The token is cancelled by "b" itself, mid-run -- proving the check
        // happens between every iteration, not just once before the loop
        // starts. A naive "check once at entry" implementation would still
        // let "c" run here.
        var log = new List<string>();
        using var cts = new CancellationTokenSource();
        var rule = new AndRule("all", new IRule[]
        {
            Rules.Counting("a", true, log),
            new FunctionRule("b", (_, _) =>
            {
                cts.Cancel();
                log.Add("b");
                return Task.FromResult(new RuleResult("b", true));
            }),
            Rules.Counting("c", true, log),
        });

        await Assert.ThrowsAsync<OperationCanceledException>(() => rule.EvaluateAsync(Rules.Empty, cts.Token));

        Assert.Equal(new[] { "a", "b" }, log);
    }
}

public class OrRuleIdiomTests
{
    [Fact]
    public async Task CancellationStopsBeforeTheNextSubRuleEvenMidRun()
    {
        // See AndRuleIdiomTests's own version of this test for why the token
        // is cancelled mid-run rather than up front.
        var log = new List<string>();
        using var cts = new CancellationTokenSource();
        var rule = new OrRule("any", new IRule[]
        {
            Rules.Counting("a", false, log),
            new FunctionRule("b", (_, _) =>
            {
                cts.Cancel();
                log.Add("b");
                return Task.FromResult(new RuleResult("b", false));
            }),
            Rules.Counting("c", false, log),
        });

        await Assert.ThrowsAsync<OperationCanceledException>(() => rule.EvaluateAsync(Rules.Empty, cts.Token));

        Assert.Equal(new[] { "a", "b" }, log);
    }
}

public class RunModeIdiomTests
{
    [Fact]
    public async Task RunAllStopsBeforeTheNextRuleEvenMidRun()
    {
        // Never-short-circuits is RunAllAsync's whole point, but cancellation
        // is deliberately the one thing that still stops it early -- see
        // AndRuleIdiomTests's own version of this test for why the token is
        // cancelled mid-run rather than up front.
        var log = new List<string>();
        using var cts = new CancellationTokenSource();
        var engine = new RulesEngine(new IRule[]
        {
            Rules.Counting("a", true, log),
            new FunctionRule("b", (_, _) =>
            {
                cts.Cancel();
                log.Add("b");
                return Task.FromResult(new RuleResult("b", true));
            }),
            Rules.Counting("c", true, log),
        });

        await Assert.ThrowsAsync<OperationCanceledException>(() => engine.RunAllAsync(Rules.Empty, cts.Token));

        Assert.Equal(new[] { "a", "b" }, log);
    }
}

/// <summary>
/// A rule shape owning its own Name and Group must declare <c>: IRule</c>,
/// because C# has no structural typing for multi-member interfaces.
/// Delegates are a different story — see <see cref="FunctionRuleIdiomTests"/>,
/// where a plain method group is a rule with nothing declared.
/// </summary>
public class CustomRuleTests
{
    private sealed class Custom : IRule
    {
        public string Name => "custom";
        public string? Group => null;

        public Task<RuleResult> EvaluateAsync(IReadOnlyDictionary<string, object?> context, CancellationToken cancellationToken = default) =>
            Task.FromResult(new RuleResult(Name, true));
    }

    [Fact]
    public async Task AnExplicitImplementationComposesLikeAnyOther()
    {
        var result = await new AndRule("composed", new IRule[] { new Custom() }).EvaluateAsync(Rules.Empty);
        Assert.True(result.Passed);
    }
}
