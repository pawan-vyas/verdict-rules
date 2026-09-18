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

/// <summary>
/// The cancellation contract: no rule evaluation begins on an already-cancelled
/// token. Every public method here takes a <see cref="CancellationToken"/>, so
/// a caller handing over a cancelled one expects no predicate to run and an
/// <see cref="OperationCanceledException"/> instead — including on the paths
/// that evaluate nothing at all (an empty composite, an empty engine, a lookup
/// that matches no rule), where a vacuous result would otherwise come back as
/// though the cancellation never happened.
///
/// These check *entry*; <see cref="AndRuleIdiomTests.CancellationStopsBeforeTheNextSubRuleEvenMidRun"/>
/// checks between iterations. Both matter, and an entry check alone would not
/// satisfy that one.
/// </summary>
public class CancellationContractTests
{
    private static CancellationToken Cancelled()
    {
        var cts = new CancellationTokenSource();
        cts.Cancel();
        return cts.Token;
    }

    private sealed record Ctx(int Value);

    [Fact]
    public async Task RunNamedRunsNoPredicateOnACancelledToken()
    {
        var log = new List<string>();
        var engine = new RulesEngine(new IRule[] { Rules.Counting("a", true, log) });

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => engine.RunNamedAsync("a", Rules.Empty, Cancelled()));

        Assert.Empty(log);
    }

    [Fact]
    public async Task TryRunNamedRunsNoPredicateOnACancelledToken()
    {
        var log = new List<string>();
        var engine = new RulesEngine(new IRule[] { Rules.Counting("a", true, log) });

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => engine.TryRunNamedAsync("a", Rules.Empty, Cancelled()));

        Assert.Empty(log);
    }

    [Fact]
    public async Task TryRunGroupThrowsOnACancelledTokenEvenWhenTheGroupIsAbsent() =>
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => new RulesEngine(Array.Empty<IRule>()).TryRunGroupAsync("nope", Rules.Empty, Cancelled()));

    [Fact]
    public async Task TryRunNamedThrowsOnACancelledTokenEvenWhenTheRuleIsAbsent() =>
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => new RulesEngine(Array.Empty<IRule>()).TryRunNamedAsync("nope", Rules.Empty, Cancelled()));

    [Fact]
    public async Task RunAllThrowsOnACancelledTokenEvenWithNoRules() =>
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => new RulesEngine(Array.Empty<IRule>()).RunAllAsync(Rules.Empty, Cancelled()));

    [Fact]
    public async Task AnEmptyAndRuleThrowsOnACancelledTokenRatherThanPassingVacuously() =>
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => new AndRule("none", Array.Empty<IRule>()).EvaluateAsync(Rules.Empty, Cancelled()));

    [Fact]
    public async Task AnEmptyOrRuleThrowsOnACancelledTokenRatherThanFailingVacuously() =>
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => new OrRule("none", Array.Empty<IRule>()).EvaluateAsync(Rules.Empty, Cancelled()));

    [Fact]
    public async Task FunctionRuleRunsNoPredicateOnACancelledToken()
    {
        var log = new List<string>();
        var rule = Rules.Counting("a", true, log);

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => rule.EvaluateAsync(Rules.Empty, Cancelled()));

        Assert.Empty(log);
    }

    // The generic arity holds the implementation and the dict-context one forwards
    // to it, so these repeat the cases above against the generic form directly --
    // proving the contract at its source, not only through the specialization.

    [Fact]
    public async Task GenericRunNamedRunsNoPredicateOnACancelledToken()
    {
        var log = new List<string>();
        var engine = new RulesEngine<Ctx>(new IRule<Ctx>[]
        {
            new FunctionRule<Ctx>("a", (_, _) => { log.Add("a"); return Task.FromResult(new RuleResult("a", true)); }),
        });

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => engine.RunNamedAsync("a", new Ctx(1), Cancelled()));

        Assert.Empty(log);
    }

    [Fact]
    public async Task GenericRunAllThrowsOnACancelledTokenEvenWithNoRules() =>
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => new RulesEngine<Ctx>(Array.Empty<IRule<Ctx>>()).RunAllAsync(new Ctx(1), Cancelled()));

    [Fact]
    public async Task GenericTryRunGroupThrowsOnACancelledTokenEvenWhenTheGroupIsAbsent() =>
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => new RulesEngine<Ctx>(Array.Empty<IRule<Ctx>>()).TryRunGroupAsync("nope", new Ctx(1), Cancelled()));

    [Fact]
    public async Task AnEmptyGenericAndRuleThrowsOnACancelledToken() =>
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => new AndRule<Ctx>("none", Array.Empty<IRule<Ctx>>()).EvaluateAsync(new Ctx(1), Cancelled()));

    [Fact]
    public async Task AnEmptyGenericOrRuleThrowsOnACancelledToken() =>
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => new OrRule<Ctx>("none", Array.Empty<IRule<Ctx>>()).EvaluateAsync(new Ctx(1), Cancelled()));

    [Fact]
    public async Task GenericFunctionRuleRunsNoPredicateOnACancelledToken()
    {
        var log = new List<string>();
        var rule = new FunctionRule<Ctx>("a", (_, _) => { log.Add("a"); return Task.FromResult(new RuleResult("a", true)); });

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => rule.EvaluateAsync(new Ctx(1), Cancelled()));

        Assert.Empty(log);
    }
}
