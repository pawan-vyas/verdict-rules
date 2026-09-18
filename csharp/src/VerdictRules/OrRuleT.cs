namespace VerdictRules;

/// <summary>
/// Composite that passes as soon as any sub-rule passes, generic over the
/// context every sub-rule shares.
/// </summary>
/// <remarks>
/// Short-circuits on the first passing sub-rule. Every sub-rule must be
/// <see cref="IRule{TContext}"/> for the exact same
/// <typeparamref name="TContext"/>.
/// </remarks>
/// <typeparam name="TContext">The context type every sub-rule shares.</typeparam>
/// <param name="name">See <see cref="Name"/>.</param>
/// <param name="rules">Sub-rules, evaluated in this order.</param>
/// <param name="group">See <see cref="Group"/>.</param>
public sealed class OrRule<TContext>(string name, IReadOnlyList<IRule<TContext>> rules, string? group = null) : IRule<TContext>
{
    /// <summary>Sub-rules, evaluated in order until one passes or all fail.</summary>
    private readonly IReadOnlyList<IRule<TContext>> _rules = rules;

    /// <inheritdoc />
    public string Name { get; } = name;

    /// <inheritdoc />
    public string? Group { get; } = group;

    /// <inheritdoc />
    public async Task<RuleResult> EvaluateAsync(TContext context, CancellationToken cancellationToken = default)
    {
        // Checked here as well as in the loop, for the same reason as
        // AndRule<TContext>: an empty list must not pass cancellation over.
        cancellationToken.ThrowIfCancellationRequested();

        var subResults = new List<RuleResult>(_rules.Count);
        // Sequential, for the same reason as AndRule<TContext>.
        foreach (var rule in _rules)
        {
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
