using System.Diagnostics;

namespace VerdictRules;

/// <summary>
/// What a <see cref="RulePredicate{TContext}"/> reports back to the
/// <see cref="FunctionRule{TContext}"/> wrapping it.
/// </summary>
/// <remarks>
/// Deliberately carries no name -- a predicate has no legitimate reason to
/// restate a name that is already fixed, once, on the
/// <see cref="FunctionRule{TContext}"/> wrapping it; nothing else ever
/// attributes a <see cref="RuleResult"/> to the wrong rule. Carries no
/// <c>Group</c> either, and never will: if <see cref="RuleResult"/> ever
/// grows a <c>Group</c> field, it stays exclusively sourced from
/// <see cref="FunctionRule{TContext}.Group"/>, the same way <c>Name</c>
/// already is, rather than reopening a path for a predicate to diverge on
/// it.
/// </remarks>
/// <param name="passed"><inheritdoc cref="Passed" path="/summary/node()" /></param>
/// <param name="detail"><inheritdoc cref="Detail" path="/summary/node()" /></param>
/// <param name="data"><inheritdoc cref="Data" path="/summary/node()" /></param>
[DebuggerDisplay("{DebuggerDisplay,nq}")]
public sealed class PredicateOutcome(bool passed, string detail = "", object? data = null)
{
    /// <summary>Whether the predicate's own condition was satisfied.</summary>
    public bool Passed { get; } = passed;

    /// <summary>
    /// Optional human-readable explanation. Empty when there is nothing
    /// beyond the boolean.
    /// </summary>
    public string Detail { get; } = detail;

    /// <summary>Optional payload; opaque to this package.</summary>
    public object? Data { get; } = data;

    /// <inheritdoc />
    public override string ToString() =>
        Detail.Length == 0 ? (Passed ? "PASS" : "FAIL") : $"{(Passed ? "PASS" : "FAIL")} ({Detail})";

    /// <summary>What a debugger shows without expanding the object.</summary>
    private string DebuggerDisplay =>
        (Passed ? "PASS" : "FAIL") + (Detail.Length == 0 ? string.Empty : $" — {Detail}");
}
