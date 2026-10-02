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
    /// <see cref="RuleResult.FailingLeaves"/> instead.
    /// </summary>
    [Fact]
    public async Task OneFailureYieldsFail()
    {
        var rule = new AndRule("and1", new IRule[] { Rules.Pass("a"), Rules.Fail("b", detail: "bad") });
        var result = await rule.EvaluateAsync(Rules.Empty);
        Assert.False(result.Passed);
        var failingLeaf = Assert.Single(result.FailingLeaves);
        Assert.Equal("b", failingLeaf.RuleName);
        Assert.Equal("bad", failingLeaf.Detail);
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
    /// <see cref="RuleResult.FailingLeaves"/> instead.
    /// </summary>
    [Fact]
    public async Task AllFailYieldsFail()
    {
        var rule = new OrRule("or1", new IRule[] { Rules.Fail("a"), Rules.Fail("b") });
        var result = await rule.EvaluateAsync(Rules.Empty);
        Assert.False(result.Passed);
        Assert.Equal(string.Empty, result.Detail);
        Assert.Equal(new[] { "a", "b" }, result.FailingLeaves.Select(r => r.RuleName));
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

    /// <summary>Mirrors <c>test_negated_returns_the_inner_result</c>.</summary>
    [Fact]
    public async Task NegatedReturnsTheInnerResult()
    {
        var rule = new NotRule("not1", Rules.Fail("inner"));
        var result = await rule.EvaluateAsync(Rules.Empty);
        var inner = NotRule.Negated(result);
        Assert.Equal("inner", inner.RuleName);
        Assert.False(inner.Passed);
    }

    /// <summary>Mirrors <c>test_a_failed_not_rule_has_itself_as_its_own_failing_leaf</c>.</summary>
    [Fact]
    public async Task AFailedNotRuleHasItselfAsItsOwnFailingLeaf()
    {
        var rule = new NotRule("not1", Rules.Pass("inner"));
        var result = await rule.EvaluateAsync(Rules.Empty);
        Assert.Equal(new[] { result }, result.FailingLeaves);
    }

    /// <summary>
    /// Mirrors <c>test_negation_nested_in_a_passing_sibling_is_still_the_whole_failure</c>
    /// -- a nested negation pins the recursion more precisely than a
    /// top-level one: <c>all(a, not(b))</c> with both <c>a</c> and
    /// <c>b</c> passing, <c>not(b)</c> fails, and the outer
    /// <see cref="AndRule"/>'s <see cref="RuleResult.FailingLeaves"/> has
    /// to be exactly <c>[not(b)]</c>.
    /// </summary>
    [Fact]
    public async Task NegationNestedInAPassingSiblingIsStillTheWholeFailure()
    {
        var notB = new NotRule("not1", Rules.Pass("b"));
        var rule = new AndRule("and1", new IRule[] { Rules.Pass("a"), notB });
        var result = await rule.EvaluateAsync(Rules.Empty);
        var notBResult = result.SubResults[1];
        Assert.Equal(new[] { notBResult }, result.FailingLeaves);
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
/// — at multiple levels on multiple branches, to prove <c>Leaves</c>/
/// <c>FailingLeaves</c> report correctly at a scale none of the other
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
            result.Leaves.Select(l => l.RuleName));
        // A passing root has no failing leaves, full stop -- even though
        // c1 failed internally three branches deep, on the way to c's
        // own pass via c2.
        Assert.Empty(result.FailingLeaves);
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
        Assert.Equal(new[] { a3Result }, result.FailingLeaves);
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
        Assert.Equal(new[] { "w", "x", "p" }, result.Leaves.Select(l => l.RuleName));
        Assert.Equal(new[] { "not1" }, result.FailingLeaves.Select(l => l.RuleName));
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
        var failing = result.FailingLeaves;
        Assert.Equal(new[] { "a", "notB", "c", "notD" }, failing.Select(l => l.RuleName));
        Assert.Same(failing[0], result.SubResults[0]);
        Assert.Same(failing[1], result.SubResults[1]);
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
        Assert.Equal(new[] { "a3", "b3", "empty_or" }, result.Leaves.Select(l => l.RuleName));
        Assert.Equal(new[] { "empty_or" }, result.FailingLeaves.Select(l => l.RuleName));
    }
}
