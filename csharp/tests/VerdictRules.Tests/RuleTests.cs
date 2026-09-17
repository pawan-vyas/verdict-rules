using Xunit;

namespace VerdictRules.Tests;

/// <summary>
/// A strict, 1:1 port of Python's <c>test_rule.py</c> — <see cref="FunctionRule"/>,
/// <see cref="AndRule"/>, <see cref="OrRule"/>. Every test here has an exact
/// Python counterpart, same class, same assertion. C#-specific coverage
/// (<see cref="System.Threading.CancellationToken"/> propagation, structural
/// typing for delegates, explicit <see cref="IRule"/> implementations) lives
/// in <c>CSharpIdiomTests.cs</c> instead, so this file stays auditable
/// against Python's own suite test-for-test.
/// </summary>
public class FunctionRuleTests
{
    /// <summary>Mirrors <c>test_wraps_a_passing_predicate</c>.</summary>
    [Fact]
    public async Task WrapsAPassingPredicate()
    {
        var rule = Rules.Pass("r1");
        var result = await rule.EvaluateAsync(Rules.Empty);
        Assert.True(result.Passed);
        Assert.Equal("r1", result.RuleName);
    }

    /// <summary>Mirrors <c>test_wraps_a_failing_predicate</c>.</summary>
    [Fact]
    public async Task WrapsAFailingPredicate()
    {
        var rule = Rules.Fail("r1", detail: "nope");
        var result = await rule.EvaluateAsync(Rules.Empty);
        Assert.False(result.Passed);
        Assert.Equal("nope", result.Detail);
    }

    /// <summary>Mirrors <c>test_predicate_receives_the_context</c>.</summary>
    [Fact]
    public async Task PredicateReceivesTheContext()
    {
        IReadOnlyDictionary<string, object?>? seen = null;
        var rule = new FunctionRule("r1", (ctx, _) =>
        {
            seen = ctx;
            return Task.FromResult(new RuleResult("r1", true));
        });

        await rule.EvaluateAsync(new Dictionary<string, object?> { ["user_id"] = 42 });

        Assert.NotNull(seen);
        Assert.Equal(42, seen!["user_id"]);
    }

    /// <summary>Mirrors <c>test_carries_name_and_group</c>.</summary>
    [Fact]
    public void CarriesNameAndGroup()
    {
        var rule = Rules.Pass("r1", group: "g1");
        Assert.Equal("r1", rule.Name);
        Assert.Equal("g1", rule.Group);
    }

    /// <summary>Mirrors <c>test_group_defaults_to_none</c>.</summary>
    [Fact]
    public void GroupDefaultsToNull()
    {
        var rule = Rules.Pass("r1");
        Assert.Null(rule.Group);
    }
}

/// <summary>Mirrors Python's <c>TestAndRule</c>.</summary>
public class AndRuleTests
{
    /// <summary>Mirrors <c>test_all_pass_yields_pass</c>.</summary>
    [Fact]
    public async Task AllPassYieldsPass()
    {
        var rule = new AndRule("and1", new IRule[] { Rules.Pass("a"), Rules.Pass("b") });
        var result = await rule.EvaluateAsync(Rules.Empty);
        Assert.True(result.Passed);
        Assert.Equal("and1", result.RuleName);
    }

    /// <summary>Mirrors <c>test_one_failure_yields_fail</c>.</summary>
    [Fact]
    public async Task OneFailureYieldsFail()
    {
        var rule = new AndRule("and1", new IRule[] { Rules.Pass("a"), Rules.Fail("b", detail: "bad") });
        var result = await rule.EvaluateAsync(Rules.Empty);
        Assert.False(result.Passed);
        Assert.Contains("b", result.Detail);
        Assert.Contains("bad", result.Detail);
    }

    /// <summary>Mirrors <c>test_short_circuits_after_first_failure</c>.</summary>
    [Fact]
    public async Task ShortCircuitsAfterFirstFailure()
    {
        var log = new List<string>();
        var rule = new AndRule("and1", new IRule[]
        {
            Rules.Fail("a"),
            new FunctionRule("c", (_, _) =>
            {
                log.Add("c");
                return Task.FromResult(new RuleResult("c", true));
            }),
        });

        await rule.EvaluateAsync(Rules.Empty);

        Assert.Empty(log); // never reached -- 'a' already failed
    }

    /// <summary>Mirrors <c>test_data_carries_sub_results_up_to_failure</c>.</summary>
    [Fact]
    public async Task DataCarriesSubResultsUpToFailure()
    {
        var rule = new AndRule("and1", new IRule[] { Rules.Pass("a"), Rules.Fail("b"), Rules.Pass("c") });
        var result = await rule.EvaluateAsync(Rules.Empty);
        var data = Assert.IsType<List<RuleResult>>(result.Data);
        Assert.Equal(new[] { "a", "b" }, data.Select(r => r.RuleName));
    }

    /// <summary>Mirrors <c>test_empty_rule_list_vacuously_passes</c>.</summary>
    [Fact]
    public async Task EmptyRuleListVacuouslyPasses()
    {
        var rule = new AndRule("and1", Array.Empty<IRule>());
        var result = await rule.EvaluateAsync(Rules.Empty);
        Assert.True(result.Passed);
    }
}

/// <summary>Mirrors Python's <c>TestOrRule</c>.</summary>
public class OrRuleTests
{
    /// <summary>Mirrors <c>test_any_pass_yields_pass</c>.</summary>
    [Fact]
    public async Task AnyPassYieldsPass()
    {
        var rule = new OrRule("or1", new IRule[] { Rules.Fail("a"), Rules.Pass("b") });
        var result = await rule.EvaluateAsync(Rules.Empty);
        Assert.True(result.Passed);
    }

    /// <summary>Mirrors <c>test_all_fail_yields_fail</c>.</summary>
    [Fact]
    public async Task AllFailYieldsFail()
    {
        var rule = new OrRule("or1", new IRule[] { Rules.Fail("a"), Rules.Fail("b") });
        var result = await rule.EvaluateAsync(Rules.Empty);
        Assert.False(result.Passed);
        Assert.Equal("no sub-rule passed", result.Detail);
    }

    /// <summary>Mirrors <c>test_short_circuits_after_first_pass</c>.</summary>
    [Fact]
    public async Task ShortCircuitsAfterFirstPass()
    {
        var log = new List<string>();
        var rule = new OrRule("or1", new IRule[]
        {
            Rules.Pass("a"),
            new FunctionRule("c", (_, _) =>
            {
                log.Add("c");
                return Task.FromResult(new RuleResult("c", false));
            }),
        });

        await rule.EvaluateAsync(Rules.Empty);

        Assert.Empty(log); // never reached -- 'a' already passed
    }

    /// <summary>Mirrors <c>test_empty_rule_list_vacuously_fails</c>.</summary>
    [Fact]
    public async Task EmptyRuleListVacuouslyFails()
    {
        var rule = new OrRule("or1", Array.Empty<IRule>());
        var result = await rule.EvaluateAsync(Rules.Empty);
        Assert.False(result.Passed);
    }
}

/// <summary>
/// Mirrors Python's <c>TestExceptionPropagation</c> in <c>test_rule.py</c> —
/// <see cref="AndRule"/>/<see cref="OrRule"/> catch nothing either; a
/// sub-rule's own exception propagates straight out of <c>EvaluateAsync</c>.
/// See <c>docs/extending/isolating-flaky-predicates/</c> for the wrapper a
/// consumer opts into if the opposite is wanted.
/// </summary>
public class RuleExceptionPropagationTests
{
    /// <summary>Mirrors <c>test_and_rule_does_not_catch_a_sub_rule_s_exception</c>.</summary>
    [Fact]
    public async Task AndRuleDoesNotCatchASubRulesException()
    {
        var rule = new AndRule("and1", new IRule[]
        {
            Rules.Pass("a"),
            new FunctionRule("flaky", (_, _) => throw new InvalidOperationException("boom")),
        });

        await Assert.ThrowsAsync<InvalidOperationException>(() => rule.EvaluateAsync(Rules.Empty));
    }

    /// <summary>Mirrors <c>test_or_rule_does_not_catch_a_sub_rule_s_exception</c>.</summary>
    [Fact]
    public async Task OrRuleDoesNotCatchASubRulesException()
    {
        var rule = new OrRule("or1", new IRule[]
        {
            Rules.Fail("a"),
            new FunctionRule("flaky", (_, _) => throw new InvalidOperationException("boom")),
        });

        await Assert.ThrowsAsync<InvalidOperationException>(() => rule.EvaluateAsync(Rules.Empty));
    }
}
