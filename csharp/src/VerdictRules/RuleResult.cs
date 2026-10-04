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
/// <param name="decidedByIndices"><inheritdoc cref="DecidedByIndices" path="/summary/node()" /></param>
[DebuggerDisplay("{DebuggerDisplay,nq}")]
public sealed class RuleResult(
    string ruleName, bool passed, string detail = "", object? data = null,
    IReadOnlyList<RuleResult>? subResults = null, IReadOnlyList<int>? decidedByIndices = null)
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
    // Copied, not aliased: IReadOnlyList is a read-only view, not an
    // immutable collection -- a caller passing a List<T> keeps a mutable
    // handle to the same object, and could otherwise append this very result
    // to it, making the result contain itself. Every traversal below
    // recurses through that.
    public IReadOnlyList<RuleResult> SubResults { get; } = subResults is null ? [] : [.. subResults];

    /// <summary>
    /// Positions within <see cref="SubResults"/> of the children that explain
    /// <i>this</i> result's own verdict. Call <see cref="GetDecidedBy"/>
    /// instead unless building a result by hand.
    /// </summary>
    /// <remarks>
    /// Positions rather than the child results themselves, so that the stored
    /// object graph is a genuine tree. Holding the same children under two
    /// properties makes it a DAG, and every tree-shaped walk -- a serializer,
    /// a structured logger -- expands a shared node once per path, so
    /// serialized size doubles per nesting level. This is also why
    /// <see cref="GetDecidedBy"/> is a method: a get-only collection property
    /// returning the children would be serialized, which is the same
    /// duplication by another route.
    /// </remarks>
    public IReadOnlyList<int> DecidedByIndices { get; } =
        CheckedIndices(decidedByIndices, subResults?.Count ?? 0, ruleName);

    /// <summary>
    /// Copies the indices and rejects any that name a child this result does
    /// not have -- the one way this shape can be wrong, so it fails at
    /// construction rather than from whichever caller reads
    /// <see cref="GetDecidedBy"/> first. The objects form admitted no
    /// equivalent check: nothing stopped it naming a result that was never a
    /// child of the result carrying it.
    /// </summary>
    private static IReadOnlyList<int> CheckedIndices(
        IReadOnlyList<int>? indices, int subResultCount, string ruleName)
    {
        if (indices is null)
        {
            return [];
        }

        var copied = indices.ToList();
        var outOfRange = copied.Where(i => i < 0 || i >= subResultCount).ToList();
        if (outOfRange.Count > 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(decidedByIndices),
                $"[{string.Join(", ", outOfRange)}] out of range for {subResultCount} "
                + $"sub-result(s) on rule '{ruleName}'.");
        }

        return copied;
    }

    /// <summary>
    /// Which of <see cref="SubResults"/> explain <i>this</i> result's own
    /// verdict -- a one-level, non-recursive fact, fixed by whatever built
    /// this result. Empty for a leaf or a vacuous composite.
    /// </summary>
    /// <remarks>
    /// This is <b>not</b> the same question <see cref="GetFailingLeaves"/>
    /// answers (recursively, the terminal failures): a failed
    /// <see cref="NotRule{TContext}"/>'s deciding child actually
    /// <i>passed</i> -- correct for "why did this fail," but not a chain to
    /// walk expecting <see cref="GetFailingLeaves"/>-equivalence. Never
    /// recurse through this expecting to land on the same set
    /// <see cref="GetFailingLeaves"/> would: it steps into a passing child at
    /// a <see cref="NotRule{TContext}"/> boundary and keeps going from there,
    /// which answers a different question than such a chain-walk would be
    /// trying to ask.
    /// </remarks>
    /// <returns>The deciding children, in evaluation order.</returns>
    public IReadOnlyList<RuleResult> GetDecidedBy() =>
        DecidedByIndices.Select(i => SubResults[i]).ToList();

    /// <summary>
    /// Every leaf result reachable from this one, in evaluation order --
    /// this result itself when it has no sub-results.
    /// </summary>
    /// <remarks>
    /// A method rather than a property, for two reasons that point the same
    /// way. It walks the whole subtree and allocates a fresh list on every
    /// call, which the Framework Design Guidelines put on the method side of
    /// the line ("the operation is orders of magnitude slower than a field
    /// set", and "the member returns an array"). And a get-only collection
    /// property is walked by reflection-based serializers and structured
    /// loggers: a leaf's own leaves list is itself, so such a walker
    /// recurses until it gives up. <c>[JsonIgnore]</c> cannot fix that here
    /// -- read-only collection properties are serialized even when
    /// <c>IgnoreReadOnlyProperties</c> is set, and the per-member attribute
    /// is not in-box for <c>netstandard2.1</c>, which this package targets.
    /// </remarks>
    /// <remarks>
    /// <b>Each leaf's own <see cref="Passed"/> is its own outcome, not a
    /// contribution to this result's verdict.</b> A
    /// <see cref="NotRule{TContext}"/> above a leaf inverts it, and an
    /// <see cref="OrRule{TContext}"/> can pass despite a failed branch, so
    /// rendering a checklist straight from this can show every item green on
    /// a result that failed. Use <see cref="GetFailingLeaves"/> to explain a
    /// verdict; use this to enumerate what ran.
    /// </remarks>
    /// <returns>The leaves, in evaluation order.</returns>
    public IReadOnlyList<RuleResult> GetLeaves() =>
        SubResults.Count == 0 ? [this] : SubResults.SelectMany(s => s.GetLeaves()).ToList();

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
    /// This is an independent recursion, not a filter over <see cref="GetLeaves"/>.
    /// </remarks>
    /// <returns>The failing leaves, in evaluation order.</returns>
    public IReadOnlyList<RuleResult> GetFailingLeaves()
    {
        if (Passed)
        {
            return [];
        }

        var childFailures = SubResults.SelectMany(s => s.GetFailingLeaves()).ToList();
        return childFailures.Count == 0 ? [this] : childFailures;
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
