namespace VerdictRules;

/// <summary>
/// Composite that passes only if every sub-rule passes.
/// </summary>
/// <remarks>
/// Short-circuits on the first failing sub-rule. An empty list passes
/// vacuously.
/// </remarks>
/// <param name="name">See <see cref="Name"/>.</param>
/// <param name="rules">Sub-rules, evaluated in this order.</param>
/// <param name="group">See <see cref="Group"/>.</param>
public sealed class AndRule(string name, IReadOnlyList<IRule> rules, string? group = null) : IRule
{
    /// <summary>Sub-rules, evaluated in order until one fails or all pass.</summary>
    private readonly IReadOnlyList<IRule> _rules = rules;

    /// <inheritdoc />
    public string Name { get; } = name;

    /// <inheritdoc />
    public string? Group { get; } = group;

    /// <inheritdoc />
    public async Task<RuleResult> EvaluateAsync(IReadOnlyDictionary<string, object?> context, CancellationToken cancellationToken = default)
    {
        var subResults = new List<RuleResult>(_rules.Count);
        // Sequential, not Task.WhenAll.
        foreach (var rule in _rules)
        {
            // Checked between sub-rules, not just once at entry.
            cancellationToken.ThrowIfCancellationRequested();
            var result = await rule.EvaluateAsync(context, cancellationToken).ConfigureAwait(false);
            subResults.Add(result);
            if (!result.Passed)
            {
                var detail = string.IsNullOrEmpty(result.Detail)
                    ? $"'{rule.Name}' failed"
                    : $"'{rule.Name}' failed: {result.Detail}";
                return new RuleResult(Name, passed: false, detail: detail, data: subResults);
            }
        }

        return new RuleResult(Name, passed: true, data: subResults);
    }
}
