namespace VerdictRules;

/// <summary>
/// Composite that passes as soon as any sub-rule passes, over the dict context.
/// </summary>
/// <param name="name">See <see cref="Name"/>.</param>
/// <param name="rules">Sub-rules, evaluated in this order.</param>
/// <param name="group">See <see cref="Group"/>.</param>
public sealed class OrRule(string name, IReadOnlyList<IRule> rules, string? group = null) : IRule
{
    /// <summary>The generic composite this type is a closed specialization of.</summary>
    private readonly OrRule<IReadOnlyDictionary<string, object?>> _inner = new(name, rules, group);

    /// <inheritdoc />
    public string Name => _inner.Name;

    /// <inheritdoc />
    public string? Group => _inner.Group;

    /// <inheritdoc cref="OrRule{TContext}.EvaluateAsync" />
    public Task<RuleResult> EvaluateAsync(IReadOnlyDictionary<string, object?> context, CancellationToken cancellationToken = default) =>
        _inner.EvaluateAsync(context, cancellationToken);
}
