using System.Diagnostics;

namespace VerdictRules;

/// <summary>
/// Composite that passes exactly when its one wrapped rule fails, generic
/// over the context the wrapped rule reads from.
/// </summary>
/// <remarks>
/// No <see cref="SequentialEvaluator{TContext}"/>/<see cref="ShortCircuitEvaluator{TContext}"/>
/// composed in -- one child, no sequence to iterate, so that machinery would
/// be indirection for nothing it uses. Still checks cancellation
/// unconditionally before evaluating, matching <see cref="AndRule{TContext}"/>/
/// <see cref="OrRule{TContext}"/>'s own "checked before anything runs"
/// guarantee rather than trusting the wrapped rule to check it itself.
/// </remarks>
/// <typeparam name="TContext">The context type the wrapped rule reads from.</typeparam>
/// <param name="name"><inheritdoc cref="IRule{TContext}.Name" path="/summary/node()" /></param>
/// <param name="rule">The single rule this negates.</param>
/// <param name="group"><inheritdoc cref="IRule{TContext}.Group" path="/summary/node()" /></param>
[DebuggerDisplay("{DebuggerDisplay,nq}")]
[DebuggerTypeProxy(typeof(NotRuleDebugView<>))]
public sealed class NotRule<TContext>(string name, IRule<TContext> rule, string? group = null) : IRule<TContext>
{
    /// <summary>
    /// The negated rule, exposed for the non-generic <see cref="NotRule"/>
    /// wrapper's own debugger proxy to reach through <c>_inner</c> without
    /// widening this type's public surface.
    /// </summary>
    internal IRule<TContext> Rule { get; } = rule;

    /// <inheritdoc />
    public string Name { get; } = name;

    /// <inheritdoc />
    public string? Group { get; } = group;

    /// <inheritdoc />
    /// <remarks>
    /// <see cref="RuleResult.GetDecidedBy"/> is the one inner result
    /// unconditionally, in both directions -- "inner passed" is genuinely
    /// why a failing <see cref="NotRule{TContext}"/> failed, not an
    /// inconsistency with <see cref="RuleResult.GetFailingLeaves"/>'s own
    /// self-as-leaf rule (they answer different questions; see
    /// <see cref="RuleResult.GetDecidedBy"/>).
    /// </remarks>
    public async Task<RuleResult> EvaluateAsync(TContext context, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var inner = await Rule.EvaluateAsync(context, cancellationToken).ConfigureAwait(false);
        return new RuleResult(Name, !inner.Passed, subResults: [inner], decidedByIndices: [0]);
    }

    /// <inheritdoc />
    public override string ToString() =>
        $"NotRule \"{Name}\"" + (string.IsNullOrEmpty(Group) ? string.Empty : $" ({Group})");

    /// <summary>What a debugger shows without expanding the object.</summary>
    private string DebuggerDisplay => ToString();
}

/// <summary>Makes a debugger expand a <see cref="NotRule{TContext}"/> straight to its negated rule.</summary>
/// <param name="rule">The composite this proxy presents to the debugger.</param>
internal sealed class NotRuleDebugView<TContext>(NotRule<TContext> rule)
{
    /// <summary>The composite this proxy presents to the debugger.</summary>
    private readonly NotRule<TContext> _rule = rule;

    /// <summary>The negated rule, expanded directly rather than behind another property.</summary>
    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    public IRule<TContext> Rule => _rule.Rule;
}
