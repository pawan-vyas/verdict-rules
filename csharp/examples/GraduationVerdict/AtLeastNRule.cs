using VerdictRules;

namespace GraduationVerdict;

/// <summary>
/// Passes if at least <paramref name="minimum"/> of the given sub-rules pass.
/// </summary>
/// <remarks>
/// Same shape as verdict-rules' docs/extending/new-rule-shape/
/// (<c>ThresholdRule</c>) -- not part of verdict-rules itself, a
/// consumer-defined combinator for a requirement <see cref="AndRule"/>/
/// <see cref="OrRule"/> can't express directly. Evaluates every sub-rule
/// unconditionally (no short-circuit is possible for a threshold count),
/// unlike <see cref="AndRule"/>/<see cref="OrRule"/>.
/// </remarks>
public sealed class AtLeastNRule(string name, IReadOnlyList<IRule> rules, int minimum, string? group = null) : IRule
{
    public string Name { get; } = name;

    public string? Group { get; } = group;

    public async Task<RuleResult> EvaluateAsync(
        IReadOnlyDictionary<string, object?> context,
        CancellationToken cancellationToken = default)
    {
        var subResults = new List<RuleResult>();
        foreach (var rule in rules)
        {
            subResults.Add(await rule.EvaluateAsync(context, cancellationToken));
        }
        var passedCount = subResults.Count(r => r.Passed);
        return new RuleResult(
            Name,
            passedCount >= minimum,
            $"{passedCount} of {rules.Count} passed, needed {minimum}",
            subResults);
    }
}
