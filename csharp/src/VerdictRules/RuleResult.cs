using System.Diagnostics;
using System.Linq;

namespace VerdictRules;

/// <summary>
/// Outcome of evaluating a single <see cref="IRule"/>.
/// </summary>
/// <param name="ruleName"><inheritdoc cref="RuleName" path="/summary/node()" /></param>
/// <param name="passed"><inheritdoc cref="Passed" path="/summary/node()" /></param>
/// <param name="detail"><inheritdoc cref="Detail" path="/summary/node()" /></param>
/// <param name="data"><inheritdoc cref="Data" path="/summary/node()" /></param>
/// <param name="subResults"><inheritdoc cref="SubResults" path="/summary/node()" /></param>
[DebuggerDisplay("{DebuggerDisplay,nq}")]
public sealed class RuleResult(
    string ruleName, bool passed, string detail = "", object? data = null, IReadOnlyList<RuleResult>? subResults = null)
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
    /// Optional payload; genuinely opaque to this package. Never written to
    /// by any shipped composite -- a composite's own children live in
    /// <see cref="SubResults"/> instead, never here.
    /// </summary>
    public object? Data { get; } = data;

    /// <summary>
    /// This result's own children, in evaluation order. Empty for a leaf
    /// result -- absence of any sub-result <i>is</i> the leaf signal, the
    /// same idiom <see cref="IRule{TContext}.Group"/> already uses for
    /// "none" on this type's sibling types. A composite rule
    /// (<see cref="AndRule{TContext}"/>, <see cref="OrRule{TContext}"/>,
    /// <see cref="NotRule{TContext}"/>, or a custom composite built the same
    /// way) populates this with exactly the sub-results it actually
    /// evaluated -- never padded, never flattened.
    /// </summary>
    public IReadOnlyList<RuleResult> SubResults { get; } = subResults ?? [];

    /// <summary>
    /// Every leaf result reachable from this one, in evaluation order --
    /// this result itself when it has no sub-results.
    /// </summary>
    public IReadOnlyList<RuleResult> Leaves =>
        SubResults.Count == 0 ? [this] : SubResults.SelectMany(s => s.Leaves).ToList();

    /// <summary>
    /// Every failing leaf that contributed to this result's own failure.
    /// </summary>
    /// <remarks>
    /// Empty when this result passed -- even when an earlier,
    /// short-circuited branch on the way to that pass itself failed. A
    /// failed result with no failing children is itself the leaf (this is
    /// what lets a failed <see cref="NotRule{TContext}"/> -- whose single
    /// child actually passed -- report correctly here, rather than
    /// misleadingly reporting no failing leaves at all on a failed result).
    /// This is an independent recursion, not a filter over <see cref="Leaves"/>.
    /// </remarks>
    public IReadOnlyList<RuleResult> FailingLeaves
    {
        get
        {
            if (Passed)
            {
                return [];
            }

            var childFailures = SubResults.SelectMany(s => s.FailingLeaves).ToList();
            return childFailures.Count == 0 ? [this] : childFailures;
        }
    }

    /// <inheritdoc />
    public override string ToString() =>
        Detail.Length == 0
            ? $"{RuleName}: {(Passed ? "PASS" : "FAIL")}"
            : $"{RuleName}: {(Passed ? "PASS" : "FAIL")} ({Detail})";

    /// <summary>What a debugger shows without expanding the object.</summary>
    private string DebuggerDisplay =>
        $"{RuleName} = {(Passed ? "PASS" : "FAIL")}"
        + (Detail.Length == 0 ? string.Empty : $" — {Detail}")
        + (SubResults.Count == 0 ? string.Empty : $" [{SubResults.Count} sub-result(s)]");
}
