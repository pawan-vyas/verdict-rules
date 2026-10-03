using System.Diagnostics;
using System.Linq;

namespace VerdictRules;

/// <summary>
/// Aggregate outcome of running a whole set of rules through the engine.
/// </summary>
/// <param name="passed"><inheritdoc cref="Passed" path="/summary/node()" /></param>
/// <param name="results"><inheritdoc cref="Results" path="/summary/node()" /></param>
[DebuggerDisplay("{DebuggerDisplay,nq}")]
[DebuggerTypeProxy(typeof(RunResultDebugView))]
public sealed class RunResult(bool passed, IReadOnlyList<RuleResult> results)
{
    /// <summary>True only if every rule in <see cref="Results"/> passed.</summary>
    public bool Passed { get; } = passed;

    /// <summary>
    /// One <see cref="RuleResult"/> per rule evaluated, in evaluation order.
    /// </summary>
    /// <remarks>
    /// A short-circuited composite still contributes exactly one entry here for
    /// itself; its own sub-results are nested inside its
    /// <see cref="RuleResult.SubResults"/> rather than flattened into this
    /// list. See <see cref="GetLeaves"/> for the flattened view across every
    /// rule this run evaluated.
    /// </remarks>
    // Copied for the same reason RuleResult copies its own children.
    public IReadOnlyList<RuleResult> Results { get; } = [.. results];

    /// <summary>
    /// Every leaf result reachable from <see cref="Results"/>, in evaluation
    /// order -- a one-line forwarder over each result's own
    /// <see cref="RuleResult.GetLeaves"/>.
    /// </summary>
    /// <remarks>
    /// A method rather than a property for the same reasons as
    /// <see cref="RuleResult.GetLeaves"/> -- see its own remarks.
    /// </remarks>
    /// <returns>The leaves, in evaluation order.</returns>
    public IReadOnlyList<RuleResult> GetLeaves() => [.. Results.SelectMany(r => r.GetLeaves())];

    /// <summary>
    /// Every failing leaf across every rule this run evaluated.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A forwarder to each result's own
    /// <see cref="RuleResult.GetFailingLeaves"/>, <b>not</b> a filter over
    /// <see cref="GetLeaves"/>. Filtering disagrees with the per-result answer
    /// in both directions, because a result's verdict is not a function of its
    /// leaves' verdicts.
    /// </para>
    /// <para>
    /// A failed <see cref="NotRule{TContext}"/> wraps a child that
    /// <i>passed</i>, so it is its own failing leaf -- filtering finds a
    /// passing leaf and reports no failure on a failed run. A passed
    /// <see cref="OrRule{TContext}"/> can hold a failed branch it recovered
    /// from -- filtering reports that branch as a failure on a passing run.
    /// </para>
    /// </remarks>
    /// <returns>The failing leaves, in evaluation order.</returns>
    public IReadOnlyList<RuleResult> GetFailingLeaves() => [.. Results.SelectMany(r => r.GetFailingLeaves())];

    /// <inheritdoc />
    public override string ToString() =>
        $"{(Passed ? "PASS" : "FAIL")} ({Results.Count} rule(s))";

    /// <summary>What a debugger shows without expanding the object.</summary>
    private string DebuggerDisplay =>
        $"{(Passed ? "PASS" : "FAIL")} — {Results.Count} rule(s), "
        + $"{Results.Count(r => !r.Passed)} failing";

    /// <summary>Makes a debugger expand straight to the per-rule results.</summary>
    /// <param name="result">The run this proxy presents to the debugger.</param>
    private sealed class RunResultDebugView(RunResult result)
    {
        /// <summary>The run this proxy presents to the debugger.</summary>
        private readonly RunResult _result = result;

        /// <summary>Every per-rule result, expanded directly rather than behind another property.</summary>
        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public RuleResult[] Results => [.. _result.Results];
    }
}
