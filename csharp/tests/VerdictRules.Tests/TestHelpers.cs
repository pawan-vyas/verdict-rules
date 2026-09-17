namespace VerdictRules.Tests;

/// <summary>
/// Test-rule builders shared across the suite. <see cref="Pass"/>/<see cref="Fail"/>
/// mirror Python's own local `_pass`/`_fail` helpers in `test_rule.py` and
/// `test_engine.py`; <see cref="Counting"/> is the call-log rule every
/// short-circuit proof in this suite (and in `CSharpIdiomTests.cs`) is built on.
/// </summary>
internal static class Rules
{
    /// <summary>A rule that always passes.</summary>
    public static FunctionRule Pass(string name, string? group = null, object? data = null) =>
        new(name, (_, _) => Task.FromResult(new RuleResult(name, true, data: data)), group);

    /// <summary>A rule that always fails.</summary>
    public static FunctionRule Fail(string name, string? group = null, string detail = "") =>
        new(name, (_, _) => Task.FromResult(new RuleResult(name, false, detail)), group);

    /// <summary>
    /// A rule that records every evaluation, so short-circuiting can be proven
    /// by what actually ran rather than by the final boolean alone — a port
    /// that evaluated concurrently would return the same boolean and fail
    /// only here.
    /// </summary>
    public static FunctionRule Counting(string name, bool passes, List<string> log, string? group = null) =>
        new(name, (_, _) =>
        {
            log.Add(name);
            return Task.FromResult(new RuleResult(name, passes));
        }, group);

    /// <summary>An empty context — most tests here don't care what's in it.</summary>
    public static readonly IReadOnlyDictionary<string, object?> Empty =
        new Dictionary<string, object?>();
}
