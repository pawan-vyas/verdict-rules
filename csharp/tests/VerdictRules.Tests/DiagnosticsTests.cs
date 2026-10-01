using System.Diagnostics;
using System.Reflection;
using Xunit;

namespace VerdictRules.Tests;

/// <summary>
/// <see cref="object.ToString"/> coverage for every rule and engine type,
/// mirroring the debugger-display text each carries (see
/// <see cref="AllPublicTypesCarryDebuggerDisplayTests"/> for the reflection
/// check that nothing new ships without one).
/// </summary>
public class ToStringTests
{
    [Fact]
    public void FunctionRuleShowsItsName()
    {
        var rule = new FunctionRule("over_18", (_, _) => Task.FromResult(new RuleResult("over_18", true)));
        Assert.Equal("FunctionRule \"over_18\"", rule.ToString());
    }

    [Fact]
    public void FunctionRuleShowsItsGroupWhenPresent()
    {
        var rule = new FunctionRule("over_18", (_, _) => Task.FromResult(new RuleResult("over_18", true)), "age");
        Assert.Equal("FunctionRule \"over_18\" (age)", rule.ToString());
    }

    [Fact]
    public void GenericFunctionRuleShowsItsName()
    {
        var rule = new FunctionRule<int>("even", (n, _) => Task.FromResult(new RuleResult("even", n % 2 == 0)));
        Assert.Equal("FunctionRule \"even\"", rule.ToString());
    }

    [Fact]
    public void AndRuleShowsItsNameAndSubRuleCount()
    {
        var rule = new AndRule("all", new IRule[] { Rules.Pass("a"), Rules.Pass("b") });
        Assert.Equal("AndRule \"all\" — 2 sub-rule(s)", rule.ToString());
    }

    [Fact]
    public void AndRuleShowsItsGroupWhenPresent()
    {
        var rule = new AndRule("all", new IRule[] { Rules.Pass("a") }, "checkout");
        Assert.Equal("AndRule \"all\" (checkout) — 1 sub-rule(s)", rule.ToString());
    }

    [Fact]
    public void GenericAndRuleShowsItsNameAndSubRuleCount()
    {
        var rule = new AndRule<int>("all", Array.Empty<IRule<int>>());
        Assert.Equal("AndRule \"all\" — 0 sub-rule(s)", rule.ToString());
    }

    [Fact]
    public void OrRuleShowsItsNameAndSubRuleCount()
    {
        var rule = new OrRule("any", new IRule[] { Rules.Fail("a"), Rules.Pass("b") });
        Assert.Equal("OrRule \"any\" — 2 sub-rule(s)", rule.ToString());
    }

    [Fact]
    public void OrRuleShowsItsGroupWhenPresent()
    {
        var rule = new OrRule("any", new IRule[] { Rules.Pass("a") }, "checkout");
        Assert.Equal("OrRule \"any\" (checkout) — 1 sub-rule(s)", rule.ToString());
    }

    [Fact]
    public void GenericOrRuleShowsItsNameAndSubRuleCount()
    {
        var rule = new OrRule<int>("any", new IRule<int>[] { new FunctionRule<int>("a", (_, _) => Task.FromResult(new RuleResult("a", false))) });
        Assert.Equal("OrRule \"any\" — 1 sub-rule(s)", rule.ToString());
    }

    [Fact]
    public void GenericOrRuleShowsItsGroupWhenPresent()
    {
        var rule = new OrRule<int>("any", Array.Empty<IRule<int>>(), "checkout");
        Assert.Equal("OrRule \"any\" (checkout) — 0 sub-rule(s)", rule.ToString());
    }

    [Fact]
    public void RuleResultShowsThePassOutcomeWithNoDetail()
    {
        var result = new RuleResult("over_18", true);
        Assert.Equal("over_18: PASS", result.ToString());
    }

    [Fact]
    public void RuleResultShowsTheFailOutcomeWithNoDetail()
    {
        var result = new RuleResult("over_18", false);
        Assert.Equal("over_18: FAIL", result.ToString());
    }

    [Fact]
    public void RuleResultShowsTheFailOutcomeWithDetail()
    {
        var result = new RuleResult("over_18", false, "too young");
        Assert.Equal("over_18: FAIL (too young)", result.ToString());
    }

    [Fact]
    public void RuleResultShowsThePassOutcomeWithDetail()
    {
        var result = new RuleResult("over_18", true, "bonus eligible");
        Assert.Equal("over_18: PASS (bonus eligible)", result.ToString());
    }

    [Fact]
    public void RunResultShowsThePassOutcomeAndRuleCount()
    {
        var run = new RunResult(true, new List<RuleResult> { new("a", true) });
        Assert.Equal("PASS (1 rule(s))", run.ToString());
    }

    [Fact]
    public void RunResultShowsTheFailOutcomeAndRuleCount()
    {
        var run = new RunResult(false, new List<RuleResult> { new("a", false), new("b", true) });
        Assert.Equal("FAIL (2 rule(s))", run.ToString());
    }

    [Fact]
    public void RulesEngineShowsRuleAndGroupCounts()
    {
        var engine = new RulesEngine(new IRule[]
        {
            Rules.Pass("a", group: "g1"),
            Rules.Pass("b", group: "g2"),
            Rules.Pass("c"),
        });
        Assert.Equal("RulesEngine — 3 rule(s), 2 group(s)", engine.ToString());
    }

    [Fact]
    public void GenericRulesEngineShowsRuleAndGroupCounts()
    {
        var engine = new RulesEngine<int>(Array.Empty<IRule<int>>());
        Assert.Equal("RulesEngine — 0 rule(s), 0 group(s)", engine.ToString());
    }
}

/// <summary>
/// Every public, concrete type this package ships carries
/// <see cref="DebuggerDisplayAttribute"/> — the guard against a new rule or
/// engine type landing without the same debugger/log-line treatment every
/// existing one has. Interfaces and delegate types are excluded: a
/// <see cref="DebuggerDisplayAttribute"/> describes an instance, and neither
/// has one of its own.
/// </summary>
public class AllPublicTypesCarryDebuggerDisplayTests
{
    [Fact]
    public void EveryPublicConcreteTypeHasADebuggerDisplay()
    {
        var assembly = typeof(RuleResult).Assembly;
        var missing = assembly.GetTypes()
            .Where(t => t.IsPublic)
            .Where(t => !t.IsInterface)
            .Where(t => !typeof(Delegate).IsAssignableFrom(t))
            .Where(t => t.GetCustomAttribute<DebuggerDisplayAttribute>() is null)
            .Select(t => t.Name)
            .ToArray();

        Assert.True(missing.Length == 0, $"Missing [DebuggerDisplay]: {string.Join(", ", missing)}");
    }
}

/// <summary>
/// Direct coverage of <see cref="RuleResult"/>'s and <see cref="RunResult"/>'s
/// own private <c>DebuggerDisplay</c> properties, read via
/// <see cref="DebuggerDisplayReflection"/> — the only types whose debugger
/// text is assembled with its own extra formatting (an em dash, an optional
/// detail, an optional sub-result count) rather than delegating straight to
/// <see cref="object.ToString"/> like every rule and engine type does.
/// </summary>
public class RuleResultAndRunResultDebuggerDisplayTests
{
    [Fact]
    public void RuleResultDebuggerDisplayOmitsDetailWhenEmpty()
    {
        var result = new RuleResult("over_18", true);
        Assert.Equal("over_18 = PASS", DebuggerDisplayReflection.Of(result));
    }

    [Fact]
    public void RuleResultDebuggerDisplayShowsDetailWhenPresent()
    {
        var result = new RuleResult("over_18", false, "too young");
        Assert.Equal("over_18 = FAIL — too young", DebuggerDisplayReflection.Of(result));
    }

    [Fact]
    public void RuleResultDebuggerDisplayShowsSubResultCountWhenDataIsSubResults()
    {
        var subs = new List<RuleResult> { new("a", true), new("b", false) };
        var result = new RuleResult("all", false, data: subs);
        Assert.Equal("all = FAIL [2 sub-result(s)]", DebuggerDisplayReflection.Of(result));
    }

    [Fact]
    public void RuleResultDebuggerDisplayOmitsSubResultCountWhenDataIsNotSubResults()
    {
        var result = new RuleResult("over_18", true, data: 42);
        Assert.Equal("over_18 = PASS", DebuggerDisplayReflection.Of(result));
    }

    [Fact]
    public void RunResultDebuggerDisplayCountsFailuresSeparatelyFromTheTotal()
    {
        var run = new RunResult(false, new List<RuleResult>
        {
            new("a", true),
            new("b", false),
            new("c", false),
        });
        Assert.Equal("FAIL — 3 rule(s), 2 failing", DebuggerDisplayReflection.Of(run));
    }

    [Fact]
    public void RunResultDebuggerDisplayShowsNoFailingWhenEveryRulePassed()
    {
        var run = new RunResult(true, new List<RuleResult> { new("a", true), new("b", true) });
        Assert.Equal("PASS — 2 rule(s), 0 failing", DebuggerDisplayReflection.Of(run));
    }
}
