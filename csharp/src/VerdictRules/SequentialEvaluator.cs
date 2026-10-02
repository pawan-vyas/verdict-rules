using System.Diagnostics;

namespace VerdictRules;

/// <summary>
/// Evaluates a list of sub-rules sequentially against one context, letting
/// <see cref="StepDecider"/> choose when to stop.
/// </summary>
/// <remarks>
/// Composable -- hold one as a field and delegate to it, the same way
/// <see cref="FunctionRule{TContext}"/> holds a predicate. This is the one
/// true sequential, cancellation-checked, <see cref="RuleResult.SubResults"/>-
/// correct implementation in this package; <see cref="ShortCircuitEvaluator{TContext}"/>
/// is built on top of it rather than reimplementing it, and a custom
/// composite should do the same rather than hand-rolling its own loop.
/// </remarks>
/// <typeparam name="TContext">The context type every sub-rule shares.</typeparam>
/// <param name="decider">Called once per sub-rule evaluated; see <see cref="StepDecider"/>.</param>
/// <param name="vacuousResult">
/// What to return, without evaluating anything, when the sub-rule list is
/// empty. Independent of <paramref name="decider"/> -- not every decider's
/// own exhaustion behavior generalizes to the zero-iteration case (a fixed
/// threshold like "at least N" does not derive this from any single fact
/// the decider closes over).
/// </param>
[DebuggerDisplay("{DebuggerDisplay,nq}")]
public sealed class SequentialEvaluator<TContext>(StepDecider decider, bool vacuousResult)
{
    /// <summary>
    /// Evaluates every rule in <paramref name="rules"/>, in order, against
    /// <paramref name="context"/>, stopping as soon as this evaluator's own
    /// <see cref="StepDecider"/> returns a non-null verdict.
    /// </summary>
    /// <param name="name">The name attributed to the returned <see cref="RuleResult"/>.</param>
    /// <param name="rules">Sub-rules, evaluated in this order.</param>
    /// <param name="context">Forwarded to every sub-rule as-is.</param>
    /// <param name="cancellationToken">
    /// Checked before anything runs -- including when <paramref name="rules"/>
    /// is empty, so an already-cancelled token never returns the vacuous
    /// result silently -- and again before every sub-rule, so cancellation
    /// raised mid-run stops before the next one starts.
    /// </param>
    /// <returns>
    /// A <see cref="RuleResult"/> named <paramref name="name"/>, whose
    /// <see cref="RuleResult.SubResults"/> is exactly the sub-results actually
    /// produced -- every one of them when <paramref name="rules"/> is
    /// exhausted without an early stop, or every one up to and including the
    /// sub-result that triggered an early stop. <see cref="RuleResult.DecidedBy"/>
    /// is the generic default: just the sub-result that flipped the verdict
    /// when the decision landed before exhaustion, or every evaluated
    /// sub-result when it only landed once everything was seen.
    /// </returns>
    public async Task<RuleResult> EvaluateAsync(
        string name, IReadOnlyList<IRule<TContext>> rules, TContext context, CancellationToken cancellationToken = default)
    {
        // Checked here, before the empty-list check below: an already-cancelled
        // token must throw even for an empty rule list, rather than silently
        // returning the vacuous result as though cancellation never happened.
        cancellationToken.ThrowIfCancellationRequested();

        if (rules.Count == 0)
        {
            return new RuleResult(name, vacuousResult, subResults: []);
        }

        var soFar = new List<RuleResult>(rules.Count);
        foreach (var rule in rules)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var latest = await rule.EvaluateAsync(context, cancellationToken).ConfigureAwait(false);
            soFar.Add(latest);
            if (decider(latest, soFar, rules.Count) is { } early)
            {
                // Generic default, correct for a custom decider with no simpler
                // shortcut available: decided with items still unevaluated ->
                // the one sub-result that flipped the verdict; decided only once
                // everything was seen -> every evaluated child. Provably wrong
                // for ShortCircuitEvaluator specifically, which overrides this
                // below using the one extra fact (its own stopOn) a fully
                // generic decider has no access to -- see that type.
                IReadOnlyList<RuleResult> decidedBy = soFar.Count == rules.Count ? soFar : [latest];
                return new RuleResult(name, early, subResults: soFar, decidedBy: decidedBy);
            }
        }

        return new RuleResult(
            name, decider(soFar[^1], soFar, rules.Count) ?? vacuousResult, subResults: soFar, decidedBy: soFar);
    }

    /// <inheritdoc />
    public override string ToString() => $"SequentialEvaluator<{typeof(TContext).Name}>";

    /// <summary>What a debugger shows without expanding the object.</summary>
    private string DebuggerDisplay => ToString();
}
