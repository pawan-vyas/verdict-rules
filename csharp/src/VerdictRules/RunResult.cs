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
    /// <see cref="RuleResult.Data"/> rather than flattened into this list.
    /// </remarks>
    public IReadOnlyList<RuleResult> Results { get; } = results;

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
