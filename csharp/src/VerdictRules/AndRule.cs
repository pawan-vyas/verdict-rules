namespace VerdictRules;

/// <summary>
/// Composite that passes only if every sub-rule passes.
/// </summary>
/// <remarks>
/// <para>
/// Short-circuits on the first failing sub-rule: later sub-rules are never
/// evaluated once one has failed, so a caller can rely on this never doing more
/// work — or having more side effects — than the minimum needed to reach a
/// verdict.
/// </para>
/// <para>
/// An empty list <b>passes</b> vacuously: nothing to fail on, and the identity
/// of the fold it performs. The opposite polarity to <see cref="OrRule"/>,
/// which is deliberate and easy to get backwards.
/// </para>
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
    public async Task<RuleResult> EvaluateAsync(IReadOnlyDictionary<string, object?> context)
    {
        var subResults = new List<RuleResult>(_rules.Count);
        // A plain sequential loop, never Task.WhenAll: short-circuiting only
        // means something if later work never *starts*, and concurrent
        // scheduling would already have begun every sub-rule before the first
        // result returns. The returned boolean is identical either way, so this
        // breaks silently.
        foreach (var rule in _rules)
        {
            var result = await rule.EvaluateAsync(context).ConfigureAwait(false);
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
