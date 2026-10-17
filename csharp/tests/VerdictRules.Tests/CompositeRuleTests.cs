using Xunit;

namespace VerdictRules.Tests;

/// <summary>
/// Proves a rule <em>tree</em> can be walked before it is evaluated, and that one
/// walk reaches a composite this package never saw. The result surface answers
/// what ran; this answers what was built.
/// </summary>
public class CompositeRuleTests
{
    /// <summary>
    /// A composite defined entirely outside this package, opting into the
    /// contract. It deliberately does not derive from anything verdict owns --
    /// the point of the test is that satisfying the contract is sufficient.
    /// </summary>
    private sealed class AtLeastOneOf<TContext>(string name, IReadOnlyList<IRule<TContext>> parts)
        : ICompositeRule<TContext>
    {
        public string Name { get; } = name;

        public string? Group => null;

        public IReadOnlyList<IRule<TContext>> SubRules { get; } = [.. parts];

        public async Task<RuleResult> EvaluateAsync(TContext context, CancellationToken cancellationToken = default)
        {
            var subResults = new List<RuleResult>();
            foreach (var part in SubRules)
            {
                subResults.Add(await part.EvaluateAsync(context, cancellationToken));
                if (subResults[^1].Passed)
                {
                    return new RuleResult(Name, true, subResults: subResults, decidedByIndices: [subResults.Count - 1]);
                }
            }

            return new RuleResult(Name, false, subResults: subResults);
        }
    }

    /// <summary>
    /// The whole reason this is a contract and not three properties: one walk,
    /// no knowledge of which composite it is looking at.
    /// </summary>
    private static List<string> WalkLeafNames<TContext>(IRule<TContext> rule)
    {
        var names = new List<string>();
        Visit(rule);
        return names;

        void Visit(IRule<TContext> current)
        {
            if (current is ICompositeRule<TContext> composite)
            {
                foreach (var part in composite.SubRules)
                {
                    Visit(part);
                }

                return;
            }

            names.Add(current.Name);
        }
    }

    [Fact]
    public void AndRuleExposesItsSubRulesInOrder()
    {
        var a = Rules.Pass("a");
        var b = Rules.Fail("b");
        var composite = new AndRule("gate", [a, b]);

        Assert.Equal<object>([a, b], composite.SubRules);
    }

    [Fact]
    public void OrRuleExposesItsSubRulesInOrder()
    {
        var a = Rules.Fail("a");
        var b = Rules.Pass("b");
        var composite = new OrRule<IReadOnlyDictionary<string, object?>>("gate", [a, b]);

        Assert.Equal<object>([a, b], composite.SubRules);
    }

    [Fact]
    public void NotRuleReportsExactlyOneSubRuleUnderTheSameNameAsEveryOtherComposite()
    {
        var inner = Rules.Pass("inner");
        var composite = new NotRule("negated", inner);

        // Named SubRules, not Rule: a walk must not need to know this is a
        // negation to find its one part.
        Assert.Single(composite.SubRules);
        Assert.Same(inner, composite.SubRules[0]);
    }

    [Fact]
    public void AVacuousCompositeReportsNoSubRulesRatherThanRefusing()
    {
        Assert.Empty(new AndRule("empty-and", []).SubRules);
        Assert.Empty(new OrRule("empty-or", []).SubRules);
    }

    [Fact]
    public void ALeafRuleIsNotAComposite()
    {
        // This is what makes "structure or terminal check" answerable without
        // inspecting concrete types.
        Assert.IsNotAssignableFrom<ICompositeRule<IReadOnlyDictionary<string, object?>>>(Rules.Pass("leaf"));
    }

    [Fact]
    public void OneWalkReachesEveryLeafThroughNestedBuiltInComposites()
    {
        var tree = new AndRule("top",
        [
            Rules.Pass("a"),
            new OrRule("either", [Rules.Fail("b"), Rules.Pass("c")]),
            new NotRule("not-d", Rules.Fail("d")),
        ]);

        Assert.Equal(["a", "b", "c", "d"], WalkLeafNames(tree));
    }

    [Fact]
    public void TheSameWalkReachesLeavesInsideAConsumerDefinedComposite()
    {
        // The capability the three built-ins alone cannot prove, and the reason
        // this is a published contract: a combinator verdict has never heard of
        // is walked by exactly the same code.
        var tree = new AndRule<IReadOnlyDictionary<string, object?>>("top",
        [
            Rules.Pass("a"),
            new AtLeastOneOf<IReadOnlyDictionary<string, object?>>("custom",
            [
                Rules.Fail("b"),
                new NotRule("not-c", Rules.Pass("c")),
            ]),
        ]);

        Assert.Equal(["a", "b", "c"], WalkLeafNames(tree));
    }

    [Fact]
    public async Task AConsumerDefinedCompositeStillEvaluatesThroughTheEngine()
    {
        // Satisfying the inspection contract must not cost it anything on the
        // evaluation side: it is still just a rule.
        var custom = new AtLeastOneOf<IReadOnlyDictionary<string, object?>>("custom",
            [Rules.Fail("b"), Rules.Pass("c")]);
        var engine = new RulesEngine<IReadOnlyDictionary<string, object?>>([custom]);

        var verdict = await engine.RunNamedAsync("custom", Rules.Empty);

        Assert.True(verdict.Passed);
        Assert.Equal("c", verdict.GetDecidedBy()[0].RuleName);
    }

    [Fact]
    public void TheSubRuleListIsACopyTakenAtConstruction()
    {
        var parts = new List<IRule>([Rules.Pass("a")]);
        var composite = new AndRule("gate", parts);

        parts.Clear();

        Assert.Single(composite.SubRules);
    }
}
