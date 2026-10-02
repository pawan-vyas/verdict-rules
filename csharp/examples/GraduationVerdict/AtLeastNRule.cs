using VerdictRules;

namespace GraduationVerdict;

/// <summary>
/// Passes if at least <paramref name="minimum"/> of the given sub-rules pass.
/// </summary>
/// <remarks>
/// Not part of verdict-rules itself; see docs/extending/new-rule-shape/.
/// Composes <see cref="SequentialEvaluator{TContext}"/> directly (not
/// <see cref="ShortCircuitEvaluator{TContext}"/>, whose own decider
/// hardcodes "the trigger value is the result" and doesn't fit this
/// rule's own policy): the decider short-circuits as soon as the minimum
/// is mathematically decided either way — once enough sub-rules have
/// passed to guarantee the minimum is met, or once too many have failed
/// for the minimum to be reachable even if every remaining sub-rule
/// passed — so a sub-rule after that point never runs.
/// </remarks>
public sealed class AtLeastNRule(string name, IReadOnlyList<IRule> rules, int minimum, string? group = null) : IRule
{
    public string Name { get; } = name;

    public string? Group { get; } = group;

    /// <summary>How many sub-rules must pass. Exposed for StructuralInvariants' own short-circuit-timing proof.</summary>
    public int Minimum { get; } = minimum;

    private readonly SequentialEvaluator<IReadOnlyDictionary<string, object?>> _evaluator = new(
        decider: (latest, soFar, total) =>
        {
            var passed = soFar.Count(r => r.Passed);
            if (passed >= minimum) return true;
            var remaining = total - soFar.Count;
            if (passed + remaining < minimum) return false;
            return soFar.Count == total ? false : null;
        },
        vacuousResult: minimum <= 0);

    public async Task<RuleResult> EvaluateAsync(
        IReadOnlyDictionary<string, object?> context,
        CancellationToken cancellationToken = default)
    {
        var result = await _evaluator.EvaluateAsync(Name, rules, context, cancellationToken);
        var passedCount = result.SubResults.Count(r => r.Passed);
        return new RuleResult(
            result.RuleName,
            result.Passed,
            $"{passedCount} of {rules.Count} passed, needed {Minimum}",
            result.Data,
            result.SubResults,
            result.DecidedBy);
    }
}
