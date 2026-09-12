using System.Diagnostics;

namespace VerdictRules;

/// <summary>
/// Outcome of evaluating a single <see cref="IRule"/>.
/// </summary>
/// <remarks>
/// Deliberately plain, immutable data — a rule reports what happened and
/// nothing more. Any domain-specific payload a caller wants to carry alongside
/// the pass/fail outcome rides in <see cref="Data"/>, which this package treats
/// as fully opaque: verdict never inspects or depends on its shape, and that is
/// what keeps the engine reusable across unrelated domains.
/// </remarks>
[DebuggerDisplay("{DebuggerDisplay,nq}")]
public sealed class RuleResult
{
    /// <summary>
    /// Name of the rule this result came from, matching that rule's own
    /// <see cref="IRule.Name"/>, so a caller walking a <see cref="RunResult"/>
    /// can attribute each outcome back to the rule that produced it.
    /// </summary>
    public string RuleName { get; }

    /// <summary>Whether the rule's condition was satisfied.</summary>
    public bool Passed { get; }

    /// <summary>
    /// Optional human-readable explanation — usually why a rule failed. Empty
    /// when there is nothing worth saying beyond the boolean.
    /// </summary>
    public string Detail { get; }

    /// <summary>
    /// Optional, fully opaque payload. Verdict never reads it.
    /// </summary>
    /// <remarks>
    /// For composites this holds the sub-results gathered so far — only the
    /// ones that actually ran, never padded out to the full list, and never
    /// flattened into the parent's own level.
    /// </remarks>
    public object? Data { get; }

    /// <summary>Creates a result.</summary>
    public RuleResult(string ruleName, bool passed, string detail = "", object? data = null)
    {
        RuleName = ruleName;
        Passed = passed;
        Detail = detail;
        Data = data;
    }

    /// <inheritdoc />
    public override string ToString() =>
        Detail.Length == 0
            ? $"{RuleName}: {(Passed ? "PASS" : "FAIL")}"
            : $"{RuleName}: {(Passed ? "PASS" : "FAIL")} ({Detail})";

    /// <summary>
    /// What a debugger shows without expanding the object.
    /// </summary>
    /// <remarks>
    /// Worth having explicitly: a composite's <see cref="Data"/> is a nested
    /// list of sub-results, and stepping through a failing evaluation is the
    /// normal way anyone diagnoses one. Without this the watch window shows a
    /// type name and the shape has to be expanded by hand at every level.
    /// </remarks>
    private string DebuggerDisplay =>
        $"{RuleName} = {(Passed ? "PASS" : "FAIL")}"
        + (Detail.Length == 0 ? string.Empty : $" — {Detail}")
        + (Data is IReadOnlyList<RuleResult> subs ? $" [{subs.Count} sub-result(s)]" : string.Empty);
}

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
        public RuleResult[] Results => _result.Results.ToArray();
    }
}
