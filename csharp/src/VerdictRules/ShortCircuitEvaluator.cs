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
    public Task<RuleResult> EvaluateAsync(
        string name, IReadOnlyList<IRule<TContext>> rules, TContext context, CancellationToken cancellationToken = default) =>
        _inner.EvaluateAsync(name, rules, context, cancellationToken);

    /// <inheritdoc />
    public override string ToString() => $"ShortCircuitEvaluator<{typeof(TContext).Name}> (stopOn: {_stopOn})";

    /// <summary>What a debugger shows without expanding the object.</summary>
    private string DebuggerDisplay => ToString();
}
