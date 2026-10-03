using Xunit;

namespace VerdictRules.Tests;

/// <summary>
/// A rule or engine owns its collections -- a caller that keeps the list it
/// passed in cannot change one afterwards.
/// </summary>
/// <remarks>
/// Every other suite here builds a rule and asserts on what evaluation
/// computed. None asks whether the inputs can move underneath it, which is
/// the gap these cover: <c>IReadOnlyList&lt;T&gt;</c> is a read-only
/// <i>view</i>, not an immutable collection, so a caller passing a
/// <c>List&lt;T&gt;</c> kept a mutable handle to the very object the
/// composite was storing.
/// </remarks>
public class ImmutabilityTests
{
    private static FunctionRule Rule(string name, bool passed = true) =>
        new(name, (_, _) => Task.FromResult(new PredicateOutcome(passed)));

    [Fact]
    public async Task AndRuleDoesNotSeeARuleAddedAfterConstruction()
    {
        var held = new List<IRule> { Rule("a") };
        var and = new AndRule("and1", held);

        held.Add(Rule("injected"));

        Assert.Contains("1 sub-rule(s)", and.ToString());
        Assert.True((await and.EvaluateAsync(new Dictionary<string, object?>())).Passed);
    }

    [Fact]
    public async Task OrRuleDoesNotSeeARuleAddedAfterConstruction()
    {
        var held = new List<IRule> { Rule("a", passed: false) };
        var or = new OrRule("or1", held);

        held.Add(Rule("injected"));

        Assert.Contains("1 sub-rule(s)", or.ToString());
        Assert.False((await or.EvaluateAsync(new Dictionary<string, object?>())).Passed);
    }

    [Fact]
    public void ClearingTheCallersListDoesNotEmptyTheComposite()
    {
        var held = new List<IRule> { Rule("a"), Rule("b") };
        var and = new AndRule("and1", held);

        held.Clear();

        Assert.Contains("2 sub-rule(s)", and.ToString());
    }

    [Fact]
    public async Task TheVerdictCannotChangeAfterConstruction()
    {
        // The sharpest form: a caller who keeps the list could otherwise flip
        // a passing AndRule to failing without touching the rule at all.
        var held = new List<IRule> { Rule("a") };
        var and = new AndRule("and1", held);
        Assert.True((await and.EvaluateAsync(new Dictionary<string, object?>())).Passed);

        held.Add(Rule("sabotage", passed: false));

        Assert.True((await and.EvaluateAsync(new Dictionary<string, object?>())).Passed);
    }

    [Fact]
    public async Task TheEngineDoesNotSeeARuleAddedAfterConstruction()
    {
        var held = new List<IRule> { Rule("a") };
        var engine = new RulesEngine(held);

        held.Add(Rule("injected"));

        Assert.Equal(["a"], engine.RuleNames);
        var run = await engine.RunAllAsync(new Dictionary<string, object?>());
        Assert.Single(run.Results);
    }

    [Fact]
    public async Task AGenericCompositeCopiesToo()
    {
        var held = new List<IRule<int>>
        {
            new FunctionRule<int>("a", (_, _) => Task.FromResult(new PredicateOutcome(true))),
        };
        var and = new AndRule<int>("and1", held);

        held.Add(new FunctionRule<int>("injected", (_, _) => Task.FromResult(new PredicateOutcome(false))));

        Assert.True((await and.EvaluateAsync(0)).Passed);
    }

    [Fact]
    public void RuleResultCopiesItsSubResultsAndDecidedBy()
    {
        var child = new RuleResult("child", true);
        var heldSubs = new List<RuleResult> { child };
        var heldDecided = new List<RuleResult> { child };

        var result = new RuleResult("parent", true, subResults: heldSubs, decidedBy: heldDecided);

        heldSubs.Add(new RuleResult("injected", false));
        heldDecided.Clear();

        Assert.Equal(["child"], result.SubResults.Select(r => r.RuleName));
        Assert.Equal(["child"], result.DecidedBy.Select(r => r.RuleName));
    }

    [Fact]
    public void RunResultCopiesItsResults()
    {
        var held = new List<RuleResult> { new("a", true) };
        var run = new RunResult(true, held);

        held.Add(new RuleResult("injected", false));

        Assert.Equal(["a"], run.Results.Select(r => r.RuleName));
    }

    [Fact]
    public void AResultCannotBeMadeToContainItself()
    {
        // Early-define/late-init: add the result to the very list it was
        // constructed from. Copying severs it, so the traversals terminate
        // rather than recursing until the stack gives out.
        var kids = new List<RuleResult>();
        var result = new RuleResult("p", false, subResults: kids);
        kids.Add(result);

        Assert.Empty(result.SubResults);
        Assert.Equal(["p"], result.GetLeaves().Select(l => l.RuleName));
        Assert.Equal(["p"], result.GetFailingLeaves().Select(l => l.RuleName));
    }

    [Fact]
    public async Task NotRuleHoldsOneRuleSoThereIsNoCollectionToCopy()
    {
        var result = await new NotRule("not_a", Rule("a")).EvaluateAsync(new Dictionary<string, object?>());

        Assert.False(result.Passed);
        Assert.Equal(["a"], result.SubResults.Select(r => r.RuleName));
    }
}
