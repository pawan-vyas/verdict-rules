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
