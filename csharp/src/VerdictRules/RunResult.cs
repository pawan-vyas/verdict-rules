using System.Diagnostics;
using System.Linq;

namespace VerdictRules;

/// <summary>
/// Aggregate outcome of running a whole set of rules through the engine.
/// </summary>
[DebuggerDisplay("{DebuggerDisplay,nq}")]
[DebuggerTypeProxy(typeof(RunResultDebugView))]
public sealed class RunResult
{
    /// <summary>True only if every rule in <see cref="Results"/> passed.</summary>
    public bool Passed { get; }

    /// <summary>
    /// One <see cref="RuleResult"/> per rule evaluated, in evaluation order.
    /// </summary>
    /// <remarks>
    /// A short-circuited composite still contributes exactly one entry here for
    /// itself; its own sub-results are nested inside its
    /// <see cref="RuleResult.Data"/> rather than flattened into this list.
    /// </remarks>
    public IReadOnlyList<RuleResult> Results { get; }

    /// <summary>Creates an aggregate result.</summary>
    public RunResult(bool passed, IReadOnlyList<RuleResult> results)
    {
        Passed = passed;
        Results = results;
    }

    /// <inheritdoc />
    public override string ToString() =>
        $"{(Passed ? "PASS" : "FAIL")} ({Results.Count} rule(s))";

    private string DebuggerDisplay =>
        $"{(Passed ? "PASS" : "FAIL")} — {Results.Count} rule(s), "
        + $"{Results.Count(r => !r.Passed)} failing";

    /// <summary>
    /// Makes a debugger expand straight to the per-rule results rather than to
    /// this type's own properties, which is what anyone inspecting a run
    /// actually wants to see.
    /// </summary>
    private sealed class RunResultDebugView
    {
        private readonly RunResult _result;

        public RunResultDebugView(RunResult result) => _result = result;

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public RuleResult[] Results => [.. _result.Results];
    }
}
