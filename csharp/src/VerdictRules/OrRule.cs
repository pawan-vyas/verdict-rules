using System.Diagnostics;
using System.Linq;

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

    /// <summary>
    /// The sole sub-result that decided a passed <see cref="OrRule"/>'s own
    /// outcome, or <see langword="null"/> when <paramref name="result"/> failed
    /// (or has no sub-results at all, which only a vacuous fail ever does).
    /// </summary>
    /// <remarks>
    /// Because evaluation stops the moment the outcome is decided,
    /// <c>result.SubResults[^1]</c> is always the one sub-result that decided
    /// it -- the sole pass for a passed <see cref="OrRule"/>, since everything
    /// before it failed. Reads only <see cref="RuleResult.SubResults"/>, so
    /// nesting composes for free. Not gated behind any check that
    /// <paramref name="result"/> actually came from an <see cref="OrRule"/> --
    /// passing the wrong family's result in is wrong at the call site,
    /// visibly, not a silent misread.
    /// </remarks>
    /// <param name="result">A result produced by evaluating an <see cref="OrRule"/>.</param>
    public static RuleResult? Passed(RuleResult result) =>
        result.SubResults.Count > 0 && result.SubResults[^1].Passed ? result.SubResults[^1] : null;

    /// <summary>
    /// Every sub-result that failed on the way to <paramref name="result"/>'s
    /// own outcome -- every sub-result when it failed, or every one except
    /// the decisive pass (see <see cref="Passed"/>) when it passed.
    /// </summary>
    /// <param name="result">A result produced by evaluating an <see cref="OrRule"/>.</param>
    public static IReadOnlyList<RuleResult> Failing(RuleResult result) =>
        Passed(result) is null ? result.SubResults : result.SubResults.Take(result.SubResults.Count - 1).ToList();
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
