namespace VerdictRules;

/// <inheritdoc cref="AndRule{TContext}" />
public sealed class AndRule(string name, IReadOnlyList<IRule> rules, string? group = null) : IRule
{
    /// <summary>The generic composite this type is a closed specialization of.</summary>
    private readonly AndRule<IReadOnlyDictionary<string, object?>> _inner = new(name, rules, group);

    /// <inheritdoc />
    public string Name => _inner.Name;

    /// <inheritdoc />
    public string? Group => _inner.Group;

    /// <inheritdoc cref="AndRule{TContext}.EvaluateAsync" />
    public Task<RuleResult> EvaluateAsync(IReadOnlyDictionary<string, object?> context, CancellationToken cancellationToken = default) =>
        _inner.EvaluateAsync(context, cancellationToken);
}
