namespace VerdictRules;

/// <summary>
/// Wraps a plain asynchronous predicate as an <see cref="IRule"/>.
/// </summary>
/// <remarks>
/// The shape most rules should be: no new type, no ceremony. C# has no free
/// structural typing for a multi-member interface like <see cref="IRule"/>,
/// so this is the escape hatch that keeps most rules from needing one.
/// </remarks>
public sealed class FunctionRule : IRule
{
    private readonly RulePredicate _predicate;

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public string? Group { get; }

    /// <summary>Creates a rule from a predicate.</summary>
    public FunctionRule(
        string name,
        RulePredicate predicate,
        string? group = null)
    {
        Name = name;
        _predicate = predicate;
        Group = group;
    }

    /// <summary>
    /// Runs the wrapped predicate and returns whatever it returns, unchanged.
    /// </summary>
    public Task<RuleResult> EvaluateAsync(IReadOnlyDictionary<string, object?> context) =>
        _predicate(context);
}
