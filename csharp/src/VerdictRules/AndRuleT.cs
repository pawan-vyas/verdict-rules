namespace VerdictRules;

/// <summary>
/// Composite that passes only if every sub-rule passes, generic over the
/// context every sub-rule shares.
/// </summary>
/// <remarks>
/// Short-circuits on the first failing sub-rule. Every sub-rule must be
/// <see cref="IRule{TContext}"/> for the exact same
/// <typeparamref name="TContext"/>.
/// </remarks>
/// <typeparam name="TContext">The context type every sub-rule shares.</typeparam>
/// <param name="name">See <see cref="Name"/>.</param>
/// <param name="rules">Sub-rules, evaluated in this order.</param>
/// <param name="group">See <see cref="Group"/>.</param>
public sealed class AndRule<TContext>(string name, IReadOnlyList<IRule<TContext>> rules, string? group = null) : IRule<TContext>
{
    /// <summary>Sub-rules, evaluated in order until one fails or all pass.</summary>
    private readonly IReadOnlyList<IRule<TContext>> _rules = rules;

    /// <inheritdoc />
    public string Name { get; } = name;

    /// <inheritdoc />
    public string? Group { get; } = group;

    /// <inheritdoc />
    public async Task<RuleResult> EvaluateAsync(TContext context, CancellationToken cancellationToken = default)
    {
        var subResults = new List<RuleResult>(_rules.Count);
        // Sequential, not Task.WhenAll.
        foreach (var rule in _rules)
        {
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
