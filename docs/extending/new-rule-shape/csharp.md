<!-- Title: Extending — A Genuinely New Rule Shape (C#) -->
# A genuinely new rule shape: the C# SDK

> The concept, the diagram, and the trade-off are in
> [`README.md`](README.md) — read that first. This page is the concrete
> C# code.

```csharp
using VerdictRules;

/// <summary>
/// Passes if at least <c>minimum</c> of the given sub-rules pass.
/// </summary>
/// <remarks>
/// Not part of verdict itself -- a consumer-defined combinator, exactly
/// as free to exist as AndRule/OrRule are, with no changes needed on
/// verdict's side to support it.
/// </remarks>
sealed class ThresholdRule<TContext> : IRule<TContext>
{
    private readonly IReadOnlyList<IRule<TContext>> _rules;
    private readonly int _minimum;

    public ThresholdRule(string name, IReadOnlyList<IRule<TContext>> rules, int minimum, string? group = null)
    {
        Name = name;
        Group = group;
        _rules = rules;
        _minimum = minimum;
    }

    public string Name { get; }
    public string? Group { get; }

    public async Task<RuleResult> EvaluateAsync(TContext context, CancellationToken cancellationToken = default)
    {
        var subResults = new List<RuleResult>();
        foreach (var rule in _rules)
        {
            subResults.Add(await rule.EvaluateAsync(context, cancellationToken));
        }
        var passedCount = subResults.Count(r => r.Passed);
        return new RuleResult(
            Name,
            passedCount >= _minimum,
            $"{passedCount} of {_rules.Count} passed, needed {_minimum}",
            subResults);
    }
}
```

`ThresholdRule<TContext>` can now be handed to a `RulesEngine`, nested
inside an `AndRule`, or hold an `AndRule` as one of its own sub-rules —
every existing piece of this package already knows how to run it,
because nothing anywhere checks `is FunctionRule` or similar;
`: IRule<TContext>` is the only contract that matters. Unlike Python's
structural `Protocol`, C# requires the explicit interface declaration —
see [`../../architecture/csharp.md`](../../architecture/csharp.md) for
why that's a real, nominal-typing requirement here, not just syntax.

The same case the spec's own diagram shows — 2 of 3 needed, the third
sub-rule fails:

```csharp
static Task<RuleResult> Rule1(Dictionary<string, object?> context, CancellationToken cancellationToken = default) =>
    Task.FromResult(new RuleResult("rule_1", true));

static Task<RuleResult> Rule2(Dictionary<string, object?> context, CancellationToken cancellationToken = default) =>
    Task.FromResult(new RuleResult("rule_2", true));

static Task<RuleResult> Rule3(Dictionary<string, object?> context, CancellationToken cancellationToken = default) =>
    Task.FromResult(new RuleResult("rule_3", false));

var atLeastTwo = new ThresholdRule<Dictionary<string, object?>>("at_least_two", new IRule<Dictionary<string, object?>>[]
{
    new FunctionRule<Dictionary<string, object?>>("rule_1", Rule1),
    new FunctionRule<Dictionary<string, object?>>("rule_2", Rule2),
    new FunctionRule<Dictionary<string, object?>>("rule_3", Rule3),
}, minimum: 2);

var result = await atLeastTwo.EvaluateAsync(new Dictionary<string, object?>());
Console.WriteLine($"{result.Passed} {result.Detail}");
// True 2 of 3 passed, needed 2
```

`ThresholdRule<TContext>` binds every direct sub-rule to the same
`TContext` — but a sub-rule can be a
[`ProjectingRule`](../reusing-a-rule-across-contexts/csharp.md), which
itself satisfies `IRule<TContext>` while its wrapped rule reads a
narrower, different type internally. The combinator stays bound to one
context; what its sub-rules actually read does not have to match:

```csharp
var verifiedRule = new FunctionRule<UserFlag>("is_verified_user", IsVerifiedUser); // reads UserFlag
var checkoutVerified = new ProjectingRule<OrderContext, UserFlag>(
    verifiedRule, ctx => new UserFlag(ctx.IsVerified));

var qualifies = new ThresholdRule<OrderContext>("qualifies", new IRule<OrderContext>[]
{
    checkoutVerified, // reads UserFlag internally, via the projection
    new FunctionRule<OrderContext>("has_promo_code", HasPromoCode), // reads OrderContext directly
}, minimum: 2);
```

## Related

- [`README.md`](README.md) — the language-agnostic scenario this page
  implements.
- [`../reusing-a-rule-across-contexts/csharp.md`](../reusing-a-rule-across-contexts/csharp.md) —
  `ProjectingRule` itself, used above to mix a sub-rule reading a
  narrower context into a `ThresholdRule<TContext>` bound to a wider
  one.
