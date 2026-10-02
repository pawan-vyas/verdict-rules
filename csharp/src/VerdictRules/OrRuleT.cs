using System.Diagnostics;

namespace VerdictRules;

/// <summary>
/// Composite that passes as soon as any sub-rule passes, generic over the
/// context every sub-rule shares.
/// </summary>
/// <remarks>
/// Short-circuits on the first passing sub-rule. Every sub-rule must be
/// <see cref="IRule{TContext}"/> for the exact same
/// <typeparamref name="TContext"/>. Composes a single, shared
/// <see cref="ShortCircuitEvaluator{TContext}"/> rather than implementing
/// evaluation itself -- see that type and <see cref="SequentialEvaluator{TContext}"/>
/// for the one place cancellation/short-circuit/<see cref="RuleResult.SubResults"/>/
/// vacuous-truth are actually implemented.
/// </remarks>
/// <typeparam name="TContext">The context type every sub-rule shares.</typeparam>
/// <param name="name"><inheritdoc cref="IRule{TContext}.Name" path="/summary/node()" /></param>
/// <param name="rules">Sub-rules, evaluated in this order.</param>
/// <param name="group"><inheritdoc cref="IRule{TContext}.Group" path="/summary/node()" /></param>
[DebuggerDisplay("{DebuggerDisplay,nq}")]
[DebuggerTypeProxy(typeof(OrRuleDebugView<>))]
public sealed class OrRule<TContext>(string name, IReadOnlyList<IRule<TContext>> rules, string? group = null) : IRule<TContext>
{
    /// <summary>What an empty <see cref="OrRule{TContext}"/> evaluates to -- pinned, not wired into construction.</summary>
    public const bool VacuousResult = false;

    /// <summary>The one true implementation this composite forwards to.</summary>
    private static readonly ShortCircuitEvaluator<TContext> Evaluator = new(stopOn: true);

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
    public Task<RuleResult> EvaluateAsync(TContext context, CancellationToken cancellationToken = default) =>
        Evaluator.EvaluateAsync(Name, _rules, context, cancellationToken);

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
