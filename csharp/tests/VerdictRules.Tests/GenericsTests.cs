using Xunit;

namespace VerdictRules.Tests;

/// <summary>
/// Tests for <c>IRule&lt;TContext&gt;</c>'s generic context parameter and its
/// arity-coexistence relationship with the non-generic <see cref="IRule"/>.
/// </summary>
/// <remarks>
/// Unlike Python/JS/TS, C#'s generics are fully reified and enforced by the
/// compiler with zero erasure, so this file proves real, independent things
/// no dynamic-language test can: the two interfaces genuinely coexist rather
/// than one masquerading as the other, a typed context runs through every
/// generic primitive identically to how a dict-context one runs through the
/// non-generic primitives, and <c>CancellationToken</c> propagation --
/// already proven for the non-generic composites in
/// <c>CSharpIdiomTests.cs</c> -- holds for the independent generic
/// implementations too, since they are fresh code, not wrappers. Has no
/// counterpart to port to another language's own suite.
/// </remarks>
public sealed record OrderContext(double Total, bool IsMember);

public class ArityCoexistenceTests
{
    /// <summary>
    /// <see cref="IRule"/> is a closed specialization of
    /// <see cref="IRule{TContext}"/>, not an unrelated type -- any
    /// dict-context rule is directly assignable to
    /// <c>IRule&lt;IReadOnlyDictionary&lt;string, object?&gt;&gt;</c> with no
    /// cast or adapter.
    /// </summary>
    [Fact]
    public void ARuleIsDirectlyAssignableToItsClosedGenericInterface()
    {
        IRule dictRule = new FunctionRule("r1", (_, _) => Task.FromResult(new RuleResult("r1", true)));
        IRule<IReadOnlyDictionary<string, object?>> asGeneric = dictRule;
        Assert.Same(dictRule, asGeneric);
    }

    /// <summary>
    /// An existing <c>: IRule</c> implementation keeps compiling and
    /// satisfying <see cref="IRule{TContext}"/> unchanged -- the whole point
    /// of closing the generic to a concrete type rather than the reverse
    /// (open-generic-inherits-non-generic) direction.
    /// </summary>
    [Fact]
    public async Task AnExplicitIRuleImplementationSatisfiesTheGenericInterfaceToo()
    {
        IRule<IReadOnlyDictionary<string, object?>> rule = new ExplicitDictRule();
        var result = await rule.EvaluateAsync(new Dictionary<string, object?>());
        Assert.True(result.Passed);
    }

    private sealed class ExplicitDictRule : IRule
    {
        public string Name => "explicit";
        public string? Group => null;
        public Task<RuleResult> EvaluateAsync(IReadOnlyDictionary<string, object?> context, CancellationToken cancellationToken = default) =>
            Task.FromResult(new RuleResult(Name, true));
    }
}

/// <summary>A typed, non-dict context runs through every generic primitive.</summary>
public class TypedContextEndToEndTests
{
    private static Task<RuleResult> OrderTotalMet(OrderContext context, CancellationToken ct = default) =>
        Task.FromResult(new RuleResult("order_total_met", context.Total >= 50.0));

    private static Task<RuleResult> IsMember(OrderContext context, CancellationToken ct = default) =>
        Task.FromResult(new RuleResult("is_member", context.IsMember));

    [Fact]
    public async Task FunctionRuleOfTEvaluatesATypedContext()
    {
        var rule = new FunctionRule<OrderContext>("order_total_met", OrderTotalMet);
        var result = await rule.EvaluateAsync(new OrderContext(75.0, false));
        Assert.True(result.Passed);
    }

    [Fact]
    public async Task AndRuleOfTComposesTypedSubRules()
    {
        var rule = new AndRule<OrderContext>("eligible", new IRule<OrderContext>[]
        {
            new FunctionRule<OrderContext>("order_total_met", OrderTotalMet),
            new FunctionRule<OrderContext>("is_member", IsMember),
        });
        var result = await rule.EvaluateAsync(new OrderContext(75.0, true));
        Assert.True(result.Passed);
    }

    [Fact]
    public async Task AndRuleOfTShortCircuitsOnATypedContextToo()
    {
        var log = new List<string>();
        var tracked = new FunctionRule<OrderContext>("tracked", (_, _) =>
        {
            log.Add("tracked");
            return Task.FromResult(new RuleResult("tracked", true));
        });
        var rule = new AndRule<OrderContext>("eligible", new IRule<OrderContext>[]
        {
            new FunctionRule<OrderContext>("order_total_met", OrderTotalMet),
            tracked,
        });

        await rule.EvaluateAsync(new OrderContext(10.0, false)); // fails order_total_met first

        Assert.Empty(log); // 'tracked' never reached -- short-circuiting survives typed contexts
    }

    [Fact]
    public async Task OrRuleOfTComposesTypedSubRules()
    {
        var rule = new OrRule<OrderContext>("eligible", new IRule<OrderContext>[]
        {
            new FunctionRule<OrderContext>("order_total_met", OrderTotalMet),
            new FunctionRule<OrderContext>("is_member", IsMember),
        });
        var result = await rule.EvaluateAsync(new OrderContext(10.0, true));
        Assert.True(result.Passed);
    }

    [Fact]
    public async Task RulesEngineOfTRunAllEvaluatesATypedContext()
    {
        var engine = new RulesEngine<OrderContext>(new IRule<OrderContext>[]
        {
            new FunctionRule<OrderContext>("order_total_met", OrderTotalMet),
            new FunctionRule<OrderContext>("is_member", IsMember),
        });
        var result = await engine.RunAllAsync(new OrderContext(75.0, true));
        Assert.True(result.Passed);
        Assert.Equal(2, result.Results.Count);
    }

    [Fact]
    public async Task RulesEngineOfTRunNamedEvaluatesATypedContext()
    {
        var engine = new RulesEngine<OrderContext>(new IRule<OrderContext>[]
        {
            new FunctionRule<OrderContext>("order_total_met", OrderTotalMet),
        });
        var result = await engine.RunNamedAsync("order_total_met", new OrderContext(75.0, false));
        Assert.True(result.Passed);
    }

    [Fact]
    public async Task RulesEngineOfTRunGroupEvaluatesATypedContext()
    {
        var engine = new RulesEngine<OrderContext>(new IRule<OrderContext>[]
        {
            new FunctionRule<OrderContext>("order_total_met", OrderTotalMet, "checkout"),
        });
        var result = await engine.RunGroupAsync("checkout", new OrderContext(75.0, false));
        Assert.True(result.Passed);
    }

    [Fact]
    public async Task RulesEngineOfTTryFormsWorkWithATypedContext()
    {
        var engine = new RulesEngine<OrderContext>(new IRule<OrderContext>[]
        {
            new FunctionRule<OrderContext>("order_total_met", OrderTotalMet),
        });
        var present = await engine.TryRunNamedAsync("order_total_met", new OrderContext(75.0, false));
        var absent = await engine.TryRunNamedAsync("nope", new OrderContext(75.0, false));
        Assert.NotNull(present);
        Assert.True(present!.Passed);
        Assert.Null(absent);
    }

    [Fact]
    public async Task RulesEngineOfTUnknownNameRaisesKeyNotFound()
    {
        var engine = new RulesEngine<OrderContext>(new IRule<OrderContext>[]
        {
            new FunctionRule<OrderContext>("order_total_met", OrderTotalMet),
        });
        await Assert.ThrowsAsync<KeyNotFoundException>(() => engine.RunNamedAsync("missing", new OrderContext(0, false)));
    }
}

/// <summary>
/// CancellationToken propagation for the generic composites -- proven
/// separately from the non-generic idiom test because AndRule&lt;TContext&gt;/
/// OrRule&lt;TContext&gt; are independent implementations, not wrappers
/// around the non-generic classes, so nothing guarantees they inherited the
/// same behavior without their own test.
/// </summary>
public class GenericCancellationTests
{
    [Fact]
    public async Task AndRuleOfTStopsBeforeTheNextSubRuleEvenMidRun()
    {
        using var cts = new CancellationTokenSource();
        var log = new List<string>();

        var rule = new AndRule<OrderContext>("and1", new IRule<OrderContext>[]
        {
            new FunctionRule<OrderContext>("a", (_, _) =>
            {
                log.Add("a");
                cts.Cancel();
                return Task.FromResult(new RuleResult("a", true));
            }),
            new FunctionRule<OrderContext>("b", (_, _) =>
            {
                log.Add("b");
                return Task.FromResult(new RuleResult("b", true));
            }),
        });

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => rule.EvaluateAsync(new OrderContext(0, false), cts.Token));

        Assert.Equal(new[] { "a" }, log); // 'b' never reached -- cancellation observed between sub-rules
    }

    [Fact]
    public async Task RulesEngineOfTRunAllStopsBeforeTheNextRuleEvenMidRun()
    {
        using var cts = new CancellationTokenSource();
        var log = new List<string>();

        var engine = new RulesEngine<OrderContext>(new IRule<OrderContext>[]
        {
            new FunctionRule<OrderContext>("a", (_, _) =>
            {
                log.Add("a");
                cts.Cancel();
                return Task.FromResult(new RuleResult("a", true));
            }),
            new FunctionRule<OrderContext>("b", (_, _) =>
            {
                log.Add("b");
                return Task.FromResult(new RuleResult("b", true));
            }),
        });

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => engine.RunAllAsync(new OrderContext(0, false), cts.Token));

        Assert.Equal(new[] { "a" }, log);
    }
}
