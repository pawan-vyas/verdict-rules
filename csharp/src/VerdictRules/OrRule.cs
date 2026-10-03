using System.Diagnostics;

namespace VerdictRules;

/// <inheritdoc cref="OrRule{TContext}" />
[DebuggerDisplay("{DebuggerDisplay,nq}")]
[DebuggerTypeProxy(typeof(OrRuleDebugView))]
public sealed class OrRule(string name, IReadOnlyList<IRule> rules, string? group = null) : IRule
{
    /// <summary>The generic composite this type is a closed specialization of.</summary>
    private readonly OrRule<IReadOnlyDictionary<string, object?>> _inner = new(name, rules, group);

    /// <inheritdoc />
    public string Name => _inner.Name;

    /// <inheritdoc />
    public string? Group => _inner.Group;

    /// <summary>Sub-rules, for <see cref="OrRuleDebugView"/> to reach through <c>_inner</c>.</summary>
    internal IReadOnlyList<IRule<IReadOnlyDictionary<string, object?>>> SubRules => _inner.SubRules;

    /// <inheritdoc cref="OrRule{TContext}.EvaluateAsync" />
    public Task<RuleResult> EvaluateAsync(IReadOnlyDictionary<string, object?> context, CancellationToken cancellationToken = default) =>
        _inner.EvaluateAsync(context, cancellationToken);

    /// <inheritdoc />
    public override string ToString() => _inner.ToString();

    /// <summary>What a debugger shows without expanding the object.</summary>
    private string DebuggerDisplay => ToString();
}

/// <summary>Makes a debugger expand an <see cref="OrRule"/> straight to its sub-rules.</summary>
/// <param name="rule">The composite this proxy presents to the debugger.</param>
internal sealed class OrRuleDebugView(OrRule rule)
{
    /// <summary>The composite this proxy presents to the debugger.</summary>
    private readonly OrRule _rule = rule;

    /// <summary>Every sub-rule, expanded directly rather than behind another property.</summary>
    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    public IRule<IReadOnlyDictionary<string, object?>>[] SubRules => [.. _rule.SubRules];
}
