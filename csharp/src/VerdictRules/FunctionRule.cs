namespace VerdictRules;

/// <summary>
/// Wraps a plain asynchronous predicate as an <see cref="IRule"/>.
/// </summary>
/// <param name="name">See <see cref="Name"/>.</param>
/// <param name="predicate">The wrapped predicate <see cref="EvaluateAsync"/> delegates to.</param>
/// <param name="group">See <see cref="Group"/>.</param>
public sealed class FunctionRule(string name, RulePredicate predicate, string? group = null) : IRule
{
    /// <summary>The generic rule this type is a closed specialization of.</summary>
    private readonly FunctionRule<IReadOnlyDictionary<string, object?>> _inner =
        new(
            name,
            new RulePredicate<IReadOnlyDictionary<string, object?>>(predicate.Invoke),
            group);

    /// <inheritdoc />
    public string Name => _inner.Name;

    /// <inheritdoc />
    public string? Group => _inner.Group;

    /// <inheritdoc cref="FunctionRule{TContext}.EvaluateAsync" />
    public Task<RuleResult> EvaluateAsync(IReadOnlyDictionary<string, object?> context, CancellationToken cancellationToken = default) =>
        _inner.EvaluateAsync(context, cancellationToken);
}
