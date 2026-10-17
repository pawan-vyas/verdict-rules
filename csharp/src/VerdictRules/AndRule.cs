using System.Diagnostics;

namespace VerdictRules;

/// <inheritdoc cref="AndRule{TContext}" />
[DebuggerDisplay("{DebuggerDisplay,nq}")]
[DebuggerTypeProxy(typeof(AndRuleDebugView))]
public sealed class AndRule(string name, IReadOnlyList<IRule> rules, string? group = null)
    : IRule, ICompositeRule<IReadOnlyDictionary<string, object?>>
{
    /// <summary>The generic composite this type is a closed specialization of.</summary>
    private readonly AndRule<IReadOnlyDictionary<string, object?>> _inner = new(name, rules, group);

    /// <inheritdoc />
    public string Name => _inner.Name;

    /// <inheritdoc />
    public string? Group => _inner.Group;

    /// <inheritdoc />
    /// <remarks>
    /// The element type is <see cref="IRule{TContext}" />, not <see cref="IRule" />:
    /// <see cref="IRule" /> derives <em>from</em> the closed generic, so the
    /// inner composite genuinely holds the base type and a caller may have
    /// passed one that is not an <see cref="IRule" />. A walk written against
    /// <see cref="ICompositeRule{TContext}" /> reaches both arities regardless.
    /// </remarks>
    public IReadOnlyList<IRule<IReadOnlyDictionary<string, object?>>> SubRules => _inner.SubRules;

    /// <inheritdoc cref="AndRule{TContext}.EvaluateAsync" />
    public Task<RuleResult> EvaluateAsync(IReadOnlyDictionary<string, object?> context, CancellationToken cancellationToken = default) =>
        _inner.EvaluateAsync(context, cancellationToken);

    /// <inheritdoc />
    public override string ToString() => _inner.ToString();

    /// <summary>What a debugger shows without expanding the object.</summary>
    private string DebuggerDisplay => ToString();
}

/// <summary>Makes a debugger expand an <see cref="AndRule"/> straight to its sub-rules.</summary>
/// <param name="rule">The composite this proxy presents to the debugger.</param>
internal sealed class AndRuleDebugView(AndRule rule)
{
    /// <summary>The composite this proxy presents to the debugger.</summary>
    private readonly AndRule _rule = rule;

    /// <summary>Every sub-rule, expanded directly rather than behind another property.</summary>
    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    public IRule<IReadOnlyDictionary<string, object?>>[] SubRules => [.. _rule.SubRules];
}
