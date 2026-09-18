namespace VerdictRules;

/// <summary>
/// Composite that passes as soon as any sub-rule passes.
/// </summary>
/// <remarks>
/// Short-circuits on the first passing sub-rule. An empty list fails
/// vacuously.
/// </remarks>
/// <param name="name">See <see cref="Name"/>.</param>
/// <param name="rules">Sub-rules, evaluated in this order.</param>
/// <param name="group">See <see cref="Group"/>.</param>
public sealed class OrRule(string name, IReadOnlyList<IRule> rules, string? group = null) : IRule
{
    /// <summary>Sub-rules, evaluated in order until one passes or all fail.</summary>
    private readonly IReadOnlyList<IRule> _rules = rules;

    /// <inheritdoc />
    public string Name { get; } = name;

    /// <inheritdoc />
    public string? Group { get; } = group;

    /// <inheritdoc />
    public async Task<RuleResult> EvaluateAsync(IReadOnlyDictionary<string, object?> context, CancellationToken cancellationToken = default)
    {
        var subResults = new List<RuleResult>(_rules.Count);
        // Sequential, for the same reason as AndRule.
        foreach (var rule in _rules)
        {
            // Checked between sub-rules -- see AndRule's own EvaluateAsync.
            cancellationToken.ThrowIfCancellationRequested();
            var result = await rule.EvaluateAsync(context, cancellationToken).ConfigureAwait(false);
            subResults.Add(result);
            if (result.Passed)
            {
                return new RuleResult(Name, passed: true, data: subResults);
            }
        }

        return new RuleResult(Name, passed: false, detail: "no sub-rule passed", data: subResults);
    }
}
