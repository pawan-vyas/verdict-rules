using System.Diagnostics;

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
/// <param name="name"><inheritdoc cref="IRule{TContext}.Name" path="/summary/node()" /></param>
/// <param name="rules">Sub-rules, evaluated in this order.</param>
/// <param name="group"><inheritdoc cref="IRule{TContext}.Group" path="/summary/node()" /></param>
[DebuggerDisplay("{DebuggerDisplay,nq}")]
[DebuggerTypeProxy(typeof(OrRuleDebugView<>))]
public sealed class OrRule<TContext>(string name, IReadOnlyList<IRule<TContext>> rules, string? group = null) : IRule<TContext>
{
    /// <summary>Sub-rules, evaluated in order until one passes or all fail.</summary>
    private readonly IReadOnlyList<IRule<TContext>> _rules = rules;

    /// <summary>
    /// Same sub-rules as <see cref="_rules"/>, exposed for the non-generic
    /// <see cref="OrRule"/> wrapper's own debugger proxy to reach through
    /// <c>_inner</c> without widening this type's public surface.
    /// </summary>
    internal IReadOnlyList<IRule<TContext>> SubRules => _rules;

    /// <inheritdoc />
    public string Name { get; } = name;

    /// <inheritdoc />
    public string? Group { get; } = group;

    /// <inheritdoc />
    public async Task<RuleResult> EvaluateAsync(TContext context, CancellationToken cancellationToken = default)
    {
        // Checked here as well as in the loop below: an already-cancelled token
        // must evaluate nothing, including when there is nothing to evaluate and
        // the loop would otherwise fall straight through to a vacuous `false`.
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

    /// <inheritdoc />
    public override string ToString() =>
        $"OrRule \"{Name}\""
        + (string.IsNullOrEmpty(Group) ? string.Empty : $" ({Group})")
        + $" — {_rules.Count} sub-rule(s)";

    /// <summary>What a debugger shows without expanding the object.</summary>
    private string DebuggerDisplay => ToString();
}

/// <summary>Makes a debugger expand an <see cref="OrRule{TContext}"/> straight to its sub-rules.</summary>
/// <param name="rule">The composite this proxy presents to the debugger.</param>
internal sealed class OrRuleDebugView<TContext>(OrRule<TContext> rule)
{
    /// <summary>The composite this proxy presents to the debugger.</summary>
    private readonly OrRule<TContext> _rule = rule;

    /// <summary>Every sub-rule, expanded directly rather than behind another property.</summary>
    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    public IRule<TContext>[] SubRules => [.. _rule.SubRules];
}
