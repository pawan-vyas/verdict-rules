using System.Diagnostics;

namespace VerdictRules;

/// <summary>
/// Stops at the first sub-rule whose own <see cref="RuleResult.Passed"/>
/// equals <see langword="stopOn"/> -- the shape <see cref="AndRule{TContext}"/>
/// and <see cref="OrRule{TContext}"/> both are.
/// </summary>
/// <remarks>
/// Wraps <see cref="SequentialEvaluator{TContext}"/> internally; a caller
/// never needs to know that type exists unless it needs more than this
/// covers (a running-history-dependent decision, which this type's own
/// <c>decider</c> cannot express -- compose <see cref="SequentialEvaluator{TContext}"/>
/// directly for that instead).
/// </remarks>
/// <typeparam name="TContext">The context type every sub-rule shares.</typeparam>
[DebuggerDisplay("{DebuggerDisplay,nq}")]
public sealed class ShortCircuitEvaluator<TContext>
{
    /// <summary>What <see cref="EvaluateAsync"/> was constructed to stop on, kept for display only.</summary>
    private readonly bool _stopOn;

    /// <summary>The <see cref="SequentialEvaluator{TContext}"/> this type composes rather than reimplements.</summary>
    private readonly SequentialEvaluator<TContext> _inner;

    /// <param name="stopOn">
    /// The <see cref="RuleResult.Passed"/> value that ends evaluation early.
    /// The empty-list result (<c>!stopOn</c>) is not a second, independently-set
    /// argument -- it is the same fact the exhaustion case already computes,
    /// so stating it twice could only ever agree or silently contradict it.
    /// </param>
    public ShortCircuitEvaluator(bool stopOn)
    {
        _stopOn = stopOn;
        _inner = new SequentialEvaluator<TContext>(
            decider: (latest, soFar, total) =>
            {
                if (latest.Passed == stopOn)
                {
                    return stopOn;
                }

                return soFar.Count == total ? !stopOn : null;
            },
            vacuousResult: !stopOn);
    }

    /// <inheritdoc cref="SequentialEvaluator{TContext}.EvaluateAsync" />
    /// <remarks>
    /// Same result <see cref="SequentialEvaluator{TContext}.EvaluateAsync"/>
    /// would produce, except <see cref="RuleResult.DecidedBy"/> is recomputed
    /// here rather than trusting that type's own generic rule. That generic
    /// rule can't distinguish "found the trigger, which happened to be the
    /// last item evaluated" from "genuinely exhausted every item without ever
    /// finding it" using count alone -- a composite failing on its own *last*
    /// sub-rule has <c>SubResults.Count == rules.Count</c> exactly like a
    /// genuine full pass does. This type's own <c>stopOn</c> is the one extra
    /// fact that tells them apart.
    /// </remarks>
    public async Task<RuleResult> EvaluateAsync(
        string name, IReadOnlyList<IRule<TContext>> rules, TContext context, CancellationToken cancellationToken = default)
    {
        var result = await _inner.EvaluateAsync(name, rules, context, cancellationToken).ConfigureAwait(false);
        var last = result.SubResults.Count > 0 ? result.SubResults[^1] : null;
        IReadOnlyList<RuleResult> decidedBy = last is not null && last.Passed == _stopOn
            ? [last]                  // the trigger was found -- regardless of position
            : result.SubResults;      // exhausted without ever finding it (or vacuous) -- every evaluated child explains it
        return new RuleResult(result.RuleName, result.Passed, result.Detail, result.Data, result.SubResults, decidedBy);
    }

    /// <inheritdoc />
    public override string ToString() => $"ShortCircuitEvaluator<{typeof(TContext).Name}> (stopOn: {_stopOn})";

    /// <summary>What a debugger shows without expanding the object.</summary>
    private string DebuggerDisplay => ToString();
}
