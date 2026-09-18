using VerdictRules;

namespace GraduationVerdict;

/// <summary>
/// Passes if at least <paramref name="minimum"/> of the given sub-rules pass.
/// </summary>
/// <remarks>
/// Not part of verdict-rules itself; see docs/extending/new-rule-shape/.
/// Evaluates every sub-rule unconditionally.
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
