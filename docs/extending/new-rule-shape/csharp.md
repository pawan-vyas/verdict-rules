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
sealed class ThresholdRule : IRule
{
    private readonly IReadOnlyList<IRule> _rules;
    private readonly int _minimum;

    public ThresholdRule(string name, IReadOnlyList<IRule> rules, int minimum, string? group = null)
    {
        Name = name;
        Group = group;
        _rules = rules;
        _minimum = minimum;
    }

    public string Name { get; }
    public string? Group { get; }

    public async Task<RuleResult> EvaluateAsync(IReadOnlyDictionary<string, object?> context, CancellationToken cancellationToken = default)
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

`ThresholdRule` can now be handed to a `RulesEngine`, nested inside an
`AndRule`, or hold an `AndRule` as one of its own sub-rules — every
existing piece of this package already knows how to run it, because
nothing anywhere checks `is FunctionRule` or similar; `: IRule` is the
only contract that matters. Unlike Python's structural `Protocol`, C#
requires the explicit `: IRule` declaration — see
[`../../architecture/csharp.md`](../../architecture/csharp.md) for why
that's a real, nominal-typing requirement here, not just syntax.

The same case the spec's own diagram shows — 2 of 3 needed, the third
sub-rule fails:

```csharp
static Task<RuleResult> Rule1(IReadOnlyDictionary<string, object?> context, CancellationToken cancellationToken = default) =>
    Task.FromResult(new RuleResult("rule_1", true));

static Task<RuleResult> Rule2(IReadOnlyDictionary<string, object?> context, CancellationToken cancellationToken = default) =>
    Task.FromResult(new RuleResult("rule_2", true));

static Task<RuleResult> Rule3(IReadOnlyDictionary<string, object?> context, CancellationToken cancellationToken = default) =>
    Task.FromResult(new RuleResult("rule_3", false));

var atLeastTwo = new ThresholdRule("at_least_two", new IRule[]
{
    new FunctionRule("rule_1", Rule1),
    new FunctionRule("rule_2", Rule2),
    new FunctionRule("rule_3", Rule3),
}, minimum: 2);

var result = await atLeastTwo.EvaluateAsync(new Dictionary<string, object?>());
Console.WriteLine($"{result.Passed} {result.Detail}");
// True 2 of 3 passed, needed 2
```

## Related

- [`README.md`](README.md) — the language-agnostic scenario this page
  implements.
