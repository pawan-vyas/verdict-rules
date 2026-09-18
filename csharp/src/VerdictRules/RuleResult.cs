using System.Diagnostics;

namespace VerdictRules;

/// <summary>
/// Outcome of evaluating a single <see cref="IRule"/>.
/// </summary>
/// <param name="ruleName">See <see cref="RuleName"/>.</param>
/// <param name="passed">See <see cref="Passed"/>.</param>
/// <param name="detail">See <see cref="Detail"/>.</param>
/// <param name="data">See <see cref="Data"/>.</param>
[DebuggerDisplay("{DebuggerDisplay,nq}")]
public sealed class RuleResult(string ruleName, bool passed, string detail = "", object? data = null)
{
    /// <summary>
    /// Name of the rule this result came from, matching that rule's own
    /// <see cref="IRule{TContext}.Name"/>.
    /// </summary>
    public string RuleName { get; } = ruleName;

    /// <summary>Whether the rule's condition was satisfied.</summary>
    public bool Passed { get; } = passed;

    /// <summary>
    /// Optional human-readable explanation. Empty when there is nothing
    /// beyond the boolean.
    /// </summary>
    public string Detail { get; } = detail;

    /// <summary>
    /// Optional payload; opaque to this package.
    /// </summary>
    /// <remarks>
    /// For composites this holds the sub-results gathered so far.
    /// </remarks>
    public object? Data { get; } = data;

    /// <inheritdoc />
    public override string ToString() =>
        Detail.Length == 0
            ? $"{RuleName}: {(Passed ? "PASS" : "FAIL")}"
            : $"{RuleName}: {(Passed ? "PASS" : "FAIL")} ({Detail})";

    /// <summary>What a debugger shows without expanding the object.</summary>
    private string DebuggerDisplay =>
        $"{RuleName} = {(Passed ? "PASS" : "FAIL")}"
        + (Detail.Length == 0 ? string.Empty : $" — {Detail}")
        + (Data is IReadOnlyList<RuleResult> subs ? $" [{subs.Count} sub-result(s)]" : string.Empty);
}
