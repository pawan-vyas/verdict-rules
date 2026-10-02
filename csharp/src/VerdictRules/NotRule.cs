using System.Diagnostics;

namespace VerdictRules;

/// <inheritdoc cref="NotRule{TContext}" />
[DebuggerDisplay("{DebuggerDisplay,nq}")]
[DebuggerTypeProxy(typeof(NotRuleDebugView))]
public sealed class NotRule(string name, IRule rule, string? group = null) : IRule
{
    /// <summary>The generic composite this type is a closed specialization of.</summary>
    private readonly NotRule<IReadOnlyDictionary<string, object?>> _inner = new(name, rule, group);

    /// <inheritdoc />
    public string Name => _inner.Name;

    /// <inheritdoc />
    public string? Group => _inner.Group;

    /// <summary>The negated rule, for <see cref="NotRuleDebugView"/> to reach through <c>_inner</c>.</summary>
    internal IRule<IReadOnlyDictionary<string, object?>> Rule => _inner.Rule;

    /// <inheritdoc cref="NotRule{TContext}.EvaluateAsync" />
    public Task<RuleResult> EvaluateAsync(IReadOnlyDictionary<string, object?> context, CancellationToken cancellationToken = default) =>
        _inner.EvaluateAsync(context, cancellationToken);

    /// <inheritdoc />
    public override string ToString() => _inner.ToString();

    /// <summary>What a debugger shows without expanding the object.</summary>
    private string DebuggerDisplay => ToString();

    /// <inheritdoc cref="NotRule{TContext}.Negated" />
    public static RuleResult Negated(RuleResult result) => result.SubResults[0];
}

/// <summary>Makes a debugger expand a <see cref="NotRule"/> straight to its negated rule.</summary>
/// <param name="rule">The composite this proxy presents to the debugger.</param>
internal sealed class NotRuleDebugView(NotRule rule)
{
    /// <summary>The composite this proxy presents to the debugger.</summary>
    private readonly NotRule _rule = rule;

    /// <summary>The negated rule, expanded directly rather than behind another property.</summary>
    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    public IRule<IReadOnlyDictionary<string, object?>> Rule => _rule.Rule;
}
