namespace VerdictRules;

/// <summary>
/// Composite that passes as soon as any sub-rule passes.
/// </summary>
/// <remarks>
/// Short-circuits on the first passing sub-rule. An empty list <b>fails</b>
/// vacuously: nothing to pass on. The opposite of <see cref="AndRule"/>, and
/// the asymmetry is the point.
/// </remarks>
public sealed class OrRule : IRule
{
    private readonly IReadOnlyList<IRule> _rules;

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public string? Group { get; }

    /// <summary>Creates a composite from ordered sub-rules.</summary>
    public OrRule(string name, IReadOnlyList<IRule> rules, string? group = null)
    {
        Name = name;
        _rules = rules;
        Group = group;
    }

    /// <inheritdoc />
    public async Task<RuleResult> EvaluateAsync(IReadOnlyDictionary<string, object?> context)
    {
        var subResults = new List<RuleResult>();
        // Sequential, for the same reason as AndRule.
        foreach (var rule in _rules)
        {
            var result = await rule.EvaluateAsync(context).ConfigureAwait(false);
            subResults.Add(result);
            if (result.Passed)
            {
                return new RuleResult(Name, passed: true, data: subResults);
            }
        }

        return new RuleResult(Name, passed: false, detail: "no sub-rule passed", data: subResults);
    }
}
