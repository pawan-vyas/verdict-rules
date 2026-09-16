namespace VerdictRules;

/// <summary>
/// Wraps a plain asynchronous predicate as an <see cref="IRule"/>.
/// </summary>
/// <remarks>
/// The shape most rules should be: no new type, no ceremony. C# has no free
/// structural typing for a multi-member interface like <see cref="IRule"/>,
/// so this is the escape hatch that keeps most rules from needing one.
/// </remarks>
/// <param name="name">See <see cref="Name"/>.</param>
/// <param name="predicate">The wrapped predicate <see cref="EvaluateAsync"/> delegates to.</param>
/// <param name="group">See <see cref="Group"/>.</param>
public sealed class FunctionRule(string name, RulePredicate predicate, string? group = null) : IRule
{
    /// <summary>The wrapped predicate, run unchanged by <see cref="EvaluateAsync"/>.</summary>
    private readonly RulePredicate _predicate = predicate;

    /// <inheritdoc />
    public string Name { get; } = name;

    /// <inheritdoc />
    public string? Group { get; } = group;

    /// <summary>
    /// Runs the wrapped predicate and returns whatever it returns, unchanged.
    /// </summary>
    /// <param name="context">Forwarded to the wrapped predicate as-is.</param>
    /// <returns>Whatever the wrapped predicate returns, unchanged.</returns>
    public Task<RuleResult> EvaluateAsync(IReadOnlyDictionary<string, object?> context) =>
        _predicate(context);
}
