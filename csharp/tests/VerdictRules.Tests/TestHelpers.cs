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
        new(name, (_, _) => Task.FromResult(new PredicateOutcome(true, data: data)), group);

    /// <summary>A rule that always fails.</summary>
    public static FunctionRule Fail(string name, string? group = null, string detail = "") =>
        new(name, (_, _) => Task.FromResult(new PredicateOutcome(false, detail)), group);

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
            return Task.FromResult(new PredicateOutcome(passes));
        }, group);

    /// <summary>An empty context — most tests here don't care what's in it.</summary>
    public static readonly IReadOnlyDictionary<string, object?> Empty =
        new Dictionary<string, object?>();
}

/// <summary>
/// A rule that, unlike <see cref="FunctionRule{TContext}"/>, deliberately does
/// <b>not</b> check its own <see cref="CancellationToken"/> on entry. Exists to
/// prove a composite's own per-iteration cancellation check is load-bearing in
/// its own right, rather than merely redundant with what a well-behaved
/// sub-rule already does — a real third-party <c>IRule&lt;TContext&gt;</c> is
/// under no obligation to check cancellation itself, so a test built only from
/// <see cref="FunctionRule{TContext}"/> sub-rules (which always double-check)
/// cannot distinguish "the composite checks between iterations" from "the
/// composite got lucky because every sub-rule happened to check too."
/// </summary>
internal sealed class NonCheckingRule<TContext>(string name, bool passes, List<string> log, string? group = null) : IRule<TContext>
{
    /// <inheritdoc />
    public string Name { get; } = name;

    /// <inheritdoc />
    public string? Group { get; } = group;

    /// <inheritdoc />
    public Task<RuleResult> EvaluateAsync(TContext context, CancellationToken cancellationToken = default)
    {
        log.Add(Name);
        return Task.FromResult(new RuleResult(Name, passes));
    }
}

/// <summary>
/// Reads a type's private <c>DebuggerDisplay</c> property by reflection — the
/// only way to observe it, since the debugger is the sole intended caller and
/// the property is deliberately not part of the public surface.
/// </summary>
internal static class DebuggerDisplayReflection
{
    /// <summary>The formatted text the debugger would show for <paramref name="instance"/>.</summary>
    public static string Of(object instance)
    {
        var property = instance.GetType().GetProperty(
            "DebuggerDisplay",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"{instance.GetType()} has no private DebuggerDisplay property.");
        return (string)property.GetValue(instance)!;
    }
}
