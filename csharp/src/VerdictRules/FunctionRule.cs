using System.Diagnostics;

namespace VerdictRules;

/// <inheritdoc cref="FunctionRule{TContext}" />
[DebuggerDisplay("{DebuggerDisplay,nq}")]
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

    /// <inheritdoc />
    public override string ToString() => _inner.ToString();

    /// <summary>What a debugger shows without expanding the object.</summary>
    private string DebuggerDisplay => ToString();
}
