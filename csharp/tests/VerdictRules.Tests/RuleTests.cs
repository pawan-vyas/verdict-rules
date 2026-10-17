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
            return Task.FromResult(new PredicateOutcome(true));
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

    /// <summary>
    /// Mirrors <c>test_one_failure_yields_fail</c> -- an <see cref="AndRule"/>
    /// no longer formats "why" into its own <see cref="RuleResult.Detail"/>;
    /// the failing sub-rule's name and detail are reachable through
    /// <see cref="RuleResult.GetFailingLeaves"/>/<see cref="RuleResult.GetDecidedBy"/>
    /// instead.
    /// </summary>
    [Fact]
    public async Task OneFailureYieldsFail()
    {
        var rule = new AndRule("and1", new IRule[] { Rules.Pass("a"), Rules.Fail("b", detail: "bad") });
        var result = await rule.EvaluateAsync(Rules.Empty);
        Assert.False(result.Passed);
        var failingLeaf = Assert.Single(result.GetFailingLeaves());
        Assert.Equal("b", failingLeaf.RuleName);
        Assert.Equal("bad", failingLeaf.Detail);
        var decisive = Assert.Single(result.GetDecidedBy());
        Assert.Equal("b", decisive.RuleName);
        Assert.Equal("bad", decisive.Detail);
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
                return Task.FromResult(new PredicateOutcome(true));
            }),
        });

        await rule.EvaluateAsync(Rules.Empty);

        Assert.Empty(log); // never reached -- 'a' already failed
    }

    /// <summary>Mirrors <c>test_data_carries_sub_results_up_to_failure</c>, ported to <c>SubResults</c>.</summary>
    [Fact]
    public async Task SubResultsCarriesSubResultsUpToFailure()
    {
        var rule = new AndRule("and1", new IRule[] { Rules.Pass("a"), Rules.Fail("b"), Rules.Pass("c") });
        var result = await rule.EvaluateAsync(Rules.Empty);
        Assert.Equal(new[] { "a", "b" }, result.SubResults.Select(r => r.RuleName));
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

    /// <summary>
    /// Mirrors <c>test_all_fail_yields_fail</c> -- an <see cref="OrRule"/> no
    /// longer formats "why" into its own <see cref="RuleResult.Detail"/>;
    /// every sub-rule's own failure is reachable through
    /// <see cref="RuleResult.GetFailingLeaves"/> instead.
    /// </summary>
    [Fact]
    public async Task AllFailYieldsFail()
    {
        var rule = new OrRule("or1", new IRule[] { Rules.Fail("a"), Rules.Fail("b") });
        var result = await rule.EvaluateAsync(Rules.Empty);
        Assert.False(result.Passed);
        Assert.Equal(string.Empty, result.Detail);
        Assert.Equal(new[] { "a", "b" }, result.GetFailingLeaves().Select(r => r.RuleName));
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
                return Task.FromResult(new PredicateOutcome(false));
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

/// <summary>Mirrors Python's <c>TestNotRule</c>.</summary>
public class NotRuleTests
{
    /// <summary>Mirrors <c>test_passes_when_the_inner_rule_fails</c>.</summary>
    [Fact]
    public async Task PassesWhenTheInnerRuleFails()
    {
        var rule = new NotRule("not1", Rules.Fail("inner"));
        var result = await rule.EvaluateAsync(Rules.Empty);
        Assert.True(result.Passed);
    }

    /// <summary>Mirrors <c>test_fails_when_the_inner_rule_passes</c>.</summary>
    [Fact]
    public async Task FailsWhenTheInnerRulePasses()
    {
        var rule = new NotRule("not1", Rules.Pass("inner"));
        var result = await rule.EvaluateAsync(Rules.Empty);
        Assert.False(result.Passed);
    }

    /// <summary>Mirrors <c>test_sub_results_truthfully_carries_the_one_inner_result</c>.</summary>
    [Fact]
    public async Task SubResultsTruthfullyCarriesTheOneInnerResult()
    {
        var rule = new NotRule("not1", Rules.Pass("inner"));
        var result = await rule.EvaluateAsync(Rules.Empty);
        var inner = Assert.Single(result.SubResults);
        Assert.Equal("inner", inner.RuleName);
    }

    /// <summary>Mirrors <c>test_passes_the_context_through_to_the_inner_rule</c>.</summary>
    [Fact]
    public async Task PassesTheContextThroughToTheInnerRule()
    {
        IReadOnlyDictionary<string, object?>? seen = null;
        var rule = new NotRule("not1", new FunctionRule("inner", (ctx, _) =>
        {
            seen = ctx;
            return Task.FromResult(new PredicateOutcome(true));
        }));

        await rule.EvaluateAsync(new Dictionary<string, object?> { ["user_id"] = 7 });

        Assert.NotNull(seen);
        Assert.Equal(7, seen!["user_id"]);
    }

    /// <summary>
    /// Mirrors <c>test_decided_by_is_the_inner_result_in_both_directions</c> --
    /// unconditional, unlike <see cref="RuleResult.GetFailingLeaves"/>'s own
    /// self-as-leaf rule: "inner passed" is genuinely why a failing
    /// <see cref="NotRule"/> failed, not an inconsistency to paper over.
    /// </summary>
    [Fact]
    public async Task DecidedByIsTheInnerResultInBothDirections()
    {
        var failing = await new NotRule("not1", Rules.Fail("inner")).EvaluateAsync(Rules.Empty);
        var failingDecisive = Assert.Single(failing.GetDecidedBy());
        Assert.Equal("inner", failingDecisive.RuleName);

        var passing = await new NotRule("not2", Rules.Pass("inner")).EvaluateAsync(Rules.Empty);
        var passingDecisive = Assert.Single(passing.GetDecidedBy());
        Assert.Equal("inner", passingDecisive.RuleName);
    }

    /// <summary>Mirrors <c>test_a_failed_not_rule_has_itself_as_its_own_failing_leaf</c>.</summary>
    [Fact]
    public async Task AFailedNotRuleHasItselfAsItsOwnFailingLeaf()
    {
        var rule = new NotRule("not1", Rules.Pass("inner"));
        var result = await rule.EvaluateAsync(Rules.Empty);
        Assert.Equal(new[] { result }, result.GetFailingLeaves());
    }

    /// <summary>
    /// Mirrors <c>test_negation_nested_in_a_passing_sibling_is_still_the_whole_failure</c>
    /// -- a nested negation pins the recursion more precisely than a
    /// top-level one: <c>all(a, not(b))</c> with both <c>a</c> and
    /// <c>b</c> passing, <c>not(b)</c> fails, and the outer
    /// <see cref="AndRule"/>'s <see cref="RuleResult.GetFailingLeaves"/> has
    /// to be exactly <c>[not(b)]</c>.
    /// </summary>
    [Fact]
    public async Task NegationNestedInAPassingSiblingIsStillTheWholeFailure()
    {
        var notB = new NotRule("not1", Rules.Pass("b"));
        var rule = new AndRule("and1", new IRule[] { Rules.Pass("a"), notB });
        var result = await rule.EvaluateAsync(Rules.Empty);
        var notBResult = result.SubResults[1];
        Assert.Equal(new[] { notBResult }, result.GetFailingLeaves());
    }

    /// <summary>
    /// The shape a caller rendering a checklist from <c>GetLeaves()</c> gets
    /// wrong: <c>all</c> fails because of the negation, yet every leaf below
    /// it passed. <c>GetLeaves()</c> enumerates what ran; only
    /// <c>GetFailingLeaves()</c> explains a verdict. Reported from downstream
    /// use.
    /// </summary>
    [Fact]
    public async Task AFailedResultCanHaveEveryLeafPassing()
    {
        var rule = new AndRule("all", new IRule[]
        {
            Rules.Pass("a"),
            new NotRule("not(b)", Rules.Pass("b")),
        });

        var result = await rule.EvaluateAsync(Rules.Empty);

        Assert.False(result.Passed);
        Assert.Equal(new[] { true, true }, result.GetLeaves().Select(l => l.Passed));
        Assert.Equal(new[] { "not(b)" }, result.GetFailingLeaves().Select(l => l.RuleName));
    }

    /// <summary>Mirrors <c>test_repr_shows_the_name</c>.</summary>
    [Fact]
    public void ToStringShowsTheName()
    {
        var rule = new NotRule("not1", Rules.Pass("inner"));
        Assert.Equal("NotRule \"not1\"", rule.ToString());
    }

    /// <summary>Mirrors <c>test_repr_shows_the_group_when_present</c>.</summary>
    [Fact]
    public void ToStringShowsTheGroupWhenPresent()
    {
        var rule = new NotRule("not1", Rules.Pass("inner"), group: "g1");
        Assert.Equal("NotRule \"not1\" (g1)", rule.ToString());
    }

    /// <summary>Mirrors <c>test_does_not_catch_the_inner_rule_s_exception</c>.</summary>
    [Fact]
    public async Task DoesNotCatchTheInnerRulesException()
    {
        var rule = new NotRule("flaky-not", new FunctionRule("flaky", (_, _) => throw new InvalidOperationException("boom")));
        await Assert.ThrowsAsync<InvalidOperationException>(() => rule.EvaluateAsync(Rules.Empty));
    }
}

/// <summary>
/// Mirrors Python's <c>TestDecidedBy</c> in <c>test_composition.py</c> --
/// <see cref="RuleResult.GetDecidedBy"/>, the one-level, non-recursive
/// explanation for a composite's own verdict. Supersedes the removed
/// <c>AndRule.Failed</c>/<c>Passing</c> and <c>OrRule.Passed</c>/<c>Failing</c>
/// static methods: those were static methods a caller could apply to the
/// wrong family's result and get a plausible, silently wrong answer --
/// confirmed with concrete cases from a real adopter review, not
/// hypothetical. <see cref="RuleResult.GetDecidedBy"/> closes that
/// structurally: there is no second method to reach for, every result
/// carries its own correctly-populated field.
/// </summary>
public class DecidedByTests
{
    /// <summary>Mirrors <c>test_and_rule_failing_early_names_just_the_decisive_failure</c>.</summary>
    [Fact]
    public async Task AndRuleFailingEarlyNamesJustTheDecisiveFailure()
    {
        var rule = new AndRule("and1", new IRule[] { Rules.Pass("a"), Rules.Fail("b"), Rules.Pass("c"), Rules.Pass("d") });
        var result = await rule.EvaluateAsync(Rules.Empty);
        Assert.Equal(new[] { "b" }, result.GetDecidedBy().Select(r => r.RuleName));
    }

    /// <summary>
    /// Mirrors <c>test_and_rule_failing_on_its_last_item_still_names_just_that_one</c>
    /// -- the case a first attempt at this got wrong: <c>SubResults.Count == rules.Count</c>
    /// holds here exactly like it does for a genuine full pass, so a rule
    /// based on count alone can't tell them apart -- position in the list is
    /// irrelevant to blame; <see cref="ShortCircuitEvaluator{TContext}"/>'s
    /// own <c>stopOn</c> is what actually distinguishes them.
    /// </summary>
    [Fact]
    public async Task AndRuleFailingOnItsLastItemStillNamesJustThatOne()
    {
        var rule = new AndRule("and1", new IRule[] { Rules.Pass("a"), Rules.Pass("b"), Rules.Fail("c") });
        var result = await rule.EvaluateAsync(Rules.Empty);
        Assert.Equal(new[] { "c" }, result.GetDecidedBy().Select(r => r.RuleName));
    }

    /// <summary>Mirrors <c>test_and_rule_fully_passing_names_every_sub_result</c>.</summary>
    [Fact]
    public async Task AndRuleFullyPassingNamesEverySubResult()
    {
        var rule = new AndRule("and1", new IRule[] { Rules.Pass("a"), Rules.Pass("b"), Rules.Pass("c") });
        var result = await rule.EvaluateAsync(Rules.Empty);
        Assert.Equal(new[] { "a", "b", "c" }, result.GetDecidedBy().Select(r => r.RuleName));
    }

    /// <summary>Mirrors <c>test_and_rule_vacuous_pass_names_nothing</c>.</summary>
    [Fact]
    public async Task AndRuleVacuousPassNamesNothing()
    {
        var rule = new AndRule("and1", Array.Empty<IRule>());
        var result = await rule.EvaluateAsync(Rules.Empty);
        Assert.Empty(result.GetDecidedBy());
    }

    /// <summary>Mirrors <c>test_or_rule_passing_early_names_just_the_decisive_pass</c>.</summary>
    [Fact]
    public async Task OrRulePassingEarlyNamesJustTheDecisivePass()
    {
        var rule = new OrRule("or1", new IRule[] { Rules.Fail("a"), Rules.Pass("b"), Rules.Fail("c") });
        var result = await rule.EvaluateAsync(Rules.Empty);
        Assert.Equal(new[] { "b" }, result.GetDecidedBy().Select(r => r.RuleName));
    }

    /// <summary>
    /// Mirrors <c>test_or_rule_all_fail_names_every_sub_result</c> -- mirrors
    /// the <see cref="AndRule"/> last-item case with the opposite polarity:
    /// <see cref="OrRule"/>'s all-fail verdict is only known once every item
    /// is seen, genuinely collective, not attributable to the last one alone.
    /// </summary>
    [Fact]
    public async Task OrRuleAllFailNamesEverySubResult()
    {
        var rule = new OrRule("or1", new IRule[] { Rules.Fail("a"), Rules.Fail("b"), Rules.Fail("c") });
        var result = await rule.EvaluateAsync(Rules.Empty);
        Assert.Equal(new[] { "a", "b", "c" }, result.GetDecidedBy().Select(r => r.RuleName));
    }

    /// <summary>Mirrors <c>test_or_rule_vacuous_fail_names_nothing</c>.</summary>
    [Fact]
    public async Task OrRuleVacuousFailNamesNothing()
    {
        var rule = new OrRule("or1", Array.Empty<IRule>());
        var result = await rule.EvaluateAsync(Rules.Empty);
        Assert.Empty(result.GetDecidedBy());
    }

    /// <summary>
    /// No shipped composite ever reads a leaf's own <see cref="RuleResult.GetDecidedBy"/>
    /// (every assertion above reads a composite's), so this is the only place
    /// that proves <c>FunctionRule</c>'s own leaf result -- built via
    /// <c>new RuleResult(Name, outcome.Passed, outcome.Detail, outcome.Data)</c>,
    /// never passing a <c>decidedBy</c> argument at all -- actually falls
    /// through to <see cref="RuleResult.GetDecidedBy"/>'s <c>decidedBy ?? []</c>
    /// default rather than surfacing the constructor's raw <see langword="null"/>
    /// default untouched.
    /// </summary>
    [Fact]
    public async Task ALeafResultDefaultsDecidedByToEmptyRatherThanNull()
    {
        var result = await Rules.Pass("leaf").EvaluateAsync(Rules.Empty);
        Assert.NotNull(result.GetDecidedBy());
        Assert.Empty(result.GetDecidedBy());
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

/// <summary>
/// A genuinely wide, deep tree mixing every rule kind — <see cref="AndRule"/>,
/// <see cref="OrRule"/>, <see cref="NotRule"/>, and a bare <see cref="FunctionRule"/>
/// — at multiple levels on multiple branches, to prove <c>GetLeaves()</c>/
/// <c>GetFailingLeaves()</c> report correctly at a scale none of the other
/// tests here exercise.
/// <code>
/// root = AndRule("root", [a, b, c])
///   a = AndRule("a", [a1, a2, a3])
///     a1 = FunctionRule (leaf)
///     a2 = OrRule("a2", [a2x (fails), a2y (passes)])
///     a3 = NotRule("a3", a3Inner)
///   b = FunctionRule (leaf)
///   c = OrRule("c", [c1, c2])
///     c1 = AndRule("c1", [c1x (fails), c1y])
///     c2 = NotRule("c2", c2Inner)
/// </code>
/// </summary>
public class MixedCompositeTreeTests
{
    [Fact]
    public async Task LeavesFlattenAcrossEveryRuleKindEvenWhenEverythingPasses()
    {
        var a1 = Rules.Pass("a1");
        var a2 = new OrRule("a2", new IRule[] { Rules.Fail("a2x"), Rules.Pass("a2y") });
        var a3 = new NotRule("a3", Rules.Fail("a3-inner")); // inner fails -> passes
        var a = new AndRule("a", new IRule[] { a1, a2, a3 });

        var b = Rules.Pass("b");

        var c1 = new AndRule("c1", new IRule[] { Rules.Fail("c1x"), Rules.Pass("c1y") }); // short-circuits, fails
        var c2 = new NotRule("c2", Rules.Fail("c2-inner")); // inner fails -> passes
        var c = new OrRule("c", new IRule[] { c1, c2 });

        var root = new AndRule("root", new IRule[] { a, b, c });
        var result = await root.EvaluateAsync(Rules.Empty);

        Assert.True(result.Passed);
        // c1y never ran at all (c1 short-circuited on c1x) -- absent, not
        // present-and-passing.
        Assert.Equal(
            new[] { "a1", "a2x", "a2y", "a3-inner", "b", "c1x", "c2-inner" },
            result.GetLeaves().Select(l => l.RuleName));
        // A passing root has no failing leaves, full stop -- even though
        // c1 failed internally three branches deep, on the way to c's
        // own pass via c2.
        Assert.Empty(result.GetFailingLeaves());

        // DecidedBy traced at every composite node in this tree: a full pass
        // is genuinely collective (every top-level member), while each
        // individual OrRule/NotRule names only its own one-level trigger.
        Assert.Equal(new[] { "a", "b", "c" }, result.GetDecidedBy().Select(r => r.RuleName));
        var aResult = result.SubResults[0];
        Assert.Equal(new[] { "a1", "a2", "a3" }, aResult.GetDecidedBy().Select(r => r.RuleName));
        var a2Result = aResult.SubResults[1];
        Assert.Equal(new[] { "a2y" }, a2Result.GetDecidedBy().Select(r => r.RuleName));
        var a3Result = aResult.SubResults[2];
        Assert.Equal(new[] { "a3-inner" }, a3Result.GetDecidedBy().Select(r => r.RuleName));
        var cResult = result.SubResults[2];
        Assert.Equal(new[] { "c2" }, cResult.GetDecidedBy().Select(r => r.RuleName)); // c short-circuited, passing on c2
        var c1Result = cResult.SubResults[0];
        Assert.Equal(new[] { "c1x" }, c1Result.GetDecidedBy().Select(r => r.RuleName)); // c1 failed early on c1x
        var c2Result = cResult.SubResults[1];
        Assert.Equal(new[] { "c2-inner" }, c2Result.GetDecidedBy().Select(r => r.RuleName));
    }

    [Fact]
    public async Task FailingLeavesPinpointsTheExactFailureThroughMultipleLevels()
    {
        var a1 = Rules.Pass("a1");
        var a2 = new OrRule("a2", new IRule[] { Rules.Fail("a2x"), Rules.Pass("a2y") });
        var a3 = new NotRule("a3", Rules.Pass("a3-inner")); // inner passes -> fails
        var a = new AndRule("a", new IRule[] { a1, a2, a3 });

        // b/c are never evaluated at all -- root short-circuits on 'a'.
        var b = Rules.Pass("b");
        var c = new OrRule("c", new IRule[]
        {
            new AndRule("c1", new IRule[] { Rules.Fail("c1x"), Rules.Pass("c1y") }),
            new NotRule("c2", Rules.Fail("c2-inner")),
        });

        var root = new AndRule("root", new IRule[] { a, b, c });
        var result = await root.EvaluateAsync(Rules.Empty);

        Assert.False(result.Passed);
        Assert.Equal(new[] { "a" }, result.SubResults.Select(r => r.RuleName)); // b, c never ran
        var a3Result = result.SubResults[0].SubResults[2];
        Assert.Equal("a3", a3Result.RuleName);
        // The one true failure, three levels deep (root -> a -> a3), with
        // a1/a2 (passing siblings of a3) contributing nothing and b/c
        // (never evaluated) not appearing at all.
        Assert.Equal(new[] { a3Result }, result.GetFailingLeaves());

        // root stopped the instant 'a' failed -- the sole item evaluated,
        // named directly, not "all evaluated" (there's only one, but the
        // reasoning is "decided before exhaustion", not "exhausted").
        Assert.Equal(new[] { "a" }, result.GetDecidedBy().Select(r => r.RuleName));
        var aResult = result.SubResults[0];
        // 'a' itself failed on its own *last* evaluated sub-rule (a3) --
        // the exact case a first pass at this design got wrong: a1/a2
        // passed before it, but position is irrelevant to blame.
        Assert.Equal(new[] { "a3" }, aResult.GetDecidedBy().Select(r => r.RuleName));
        // a3 is a NotRule: unconditional, regardless of direction -- its
        // own inner child (a3-inner, which passed) is what explains it.
        Assert.Equal(new[] { "a3-inner" }, a3Result.GetDecidedBy().Select(r => r.RuleName));
    }

    /// <summary>
    /// Stacks everything that could plausibly go wrong at once: an
    /// earlier <see cref="AndRule"/> sibling that passes despite an
    /// internal failure (ordering independence — the real failure comes
    /// later), <see cref="NotRule"/> wrapping a genuine <see cref="OrRule"/>
    /// rather than a bare leaf, that wrapped <see cref="OrRule"/>
    /// short-circuiting internally, and the outer <see cref="AndRule"/>
    /// <i>also</i> short-circuiting — two independent prunings at
    /// different depths in the same tree.
    /// </summary>
    [Fact]
    public async Task NotRuleWrappingAShortCircuitedCompositeWithAnEarlierPassingSibling()
    {
        var innerOr = new OrRule("inner_or", new IRule[] { Rules.Fail("w"), Rules.Pass("x") }); // passes

        var innerOrForNot = new OrRule("inner_or_for_not", new IRule[] { Rules.Pass("p"), Rules.Pass("q") }); // short-circuits
        var notResult = new NotRule("not1", innerOrForNot); // passed -> fails

        var z = Rules.Pass("z"); // never reached

        var root = new AndRule("root", new IRule[] { innerOr, notResult, z });
        var result = await root.EvaluateAsync(Rules.Empty);

        Assert.False(result.Passed);
        Assert.Equal(new[] { "w", "x", "p" }, result.GetLeaves().Select(l => l.RuleName));
        Assert.Equal(new[] { "not1" }, result.GetFailingLeaves().Select(l => l.RuleName));

        // root stopped on 'not1' -- the second of three sub-rules, decided
        // before exhaustion ('z' never ran), so just the one, not all.
        Assert.Equal(new[] { "not1" }, result.GetDecidedBy().Select(r => r.RuleName));
        var innerOrResult = result.SubResults[0];
        Assert.Equal(new[] { "x" }, innerOrResult.GetDecidedBy().Select(r => r.RuleName)); // OrRule passed early
        var notResultResult = result.SubResults[1];
        // NotRule unconditional: names its own inner child
        // (inner_or_for_not, which itself short-circuited on 'p' and
        // passed) -- one level only, not drilled through to 'p' itself.
        Assert.Equal(new[] { "inner_or_for_not" }, notResultResult.GetDecidedBy().Select(r => r.RuleName));
    }

    [Fact]
    public async Task OrRuleAllFailInterleavesRealLeavesAndNotFallbacksInOrder()
    {
        var rule = new OrRule("root", new IRule[]
        {
            Rules.Fail("a"),
            new NotRule("notB", Rules.Pass("b")),
            Rules.Fail("c"),
            new NotRule("notD", Rules.Pass("d")),
        });
        var result = await rule.EvaluateAsync(Rules.Empty);

        Assert.False(result.Passed);
        var failing = result.GetFailingLeaves();
        Assert.Equal(new[] { "a", "notB", "c", "notD" }, failing.Select(l => l.RuleName));
        Assert.Same(failing[0], result.SubResults[0]);
        Assert.Same(failing[1], result.SubResults[1]);

        // All-fail is genuinely collective -- every sub-rule ran and none
        // triggered the stop condition, mirroring the plain OrRule all-fail
        // case even though two of the four children here are NotRules.
        Assert.Equal(new[] { "a", "notB", "c", "notD" }, result.GetDecidedBy().Select(r => r.RuleName));
        Assert.Equal(new[] { "b" }, result.SubResults[1].GetDecidedBy().Select(r => r.RuleName)); // notB: inner 'b' passed
        Assert.Equal(new[] { "d" }, result.SubResults[3].GetDecidedBy().Select(r => r.RuleName)); // notD likewise
    }

    [Fact]
    public async Task AGenuinelyVacuousCompositeNestedInsideALargerFailingTree()
    {
        var innerOr = new OrRule("inner_or", new IRule[] { Rules.Fail("a3"), Rules.Pass("b3") });
        var emptyOr = new OrRule("empty_or", Array.Empty<IRule>());
        var z = Rules.Pass("z");

        var root = new AndRule("root", new IRule[] { innerOr, emptyOr, z });
        var result = await root.EvaluateAsync(Rules.Empty);

        Assert.False(result.Passed);
        Assert.Equal(new[] { "a3", "b3", "empty_or" }, result.GetLeaves().Select(l => l.RuleName));
        Assert.Equal(new[] { "empty_or" }, result.GetFailingLeaves().Select(l => l.RuleName));

        // root stopped on the vacuous empty_or ('z' never ran).
        Assert.Equal(new[] { "empty_or" }, result.GetDecidedBy().Select(r => r.RuleName));
        var innerOrResult = result.SubResults[0];
        Assert.Equal(new[] { "b3" }, innerOrResult.GetDecidedBy().Select(r => r.RuleName)); // OrRule passed early
        var emptyOrResult = result.SubResults[1];
        Assert.Empty(emptyOrResult.GetDecidedBy()); // vacuous -- nothing decided it
    }
}
