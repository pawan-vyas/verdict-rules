using System.Diagnostics;

namespace VerdictRules;

/// <summary>
/// Wraps a plain asynchronous predicate as an <see cref="IRule{TContext}"/>.
/// </summary>
/// <remarks>
/// <typeparamref name="TContext"/> is inferred from the wrapped predicate's
/// own parameter type.
/// </remarks>
/// <typeparam name="TContext">The type this rule's predicate reads from.</typeparam>
/// <param name="name"><inheritdoc cref="IRule{TContext}.Name" path="/summary/node()" /></param>
/// <param name="predicate">The wrapped predicate <see cref="EvaluateAsync"/> delegates to.</param>
/// <param name="group"><inheritdoc cref="IRule{TContext}.Group" path="/summary/node()" /></param>
[DebuggerDisplay("{DebuggerDisplay,nq}")]
public sealed class FunctionRule<TContext>(string name, RulePredicate<TContext> predicate, string? group = null) : IRule<TContext>
{
    /// <summary>The wrapped predicate, run by <see cref="EvaluateAsync"/>.</summary>
    private readonly RulePredicate<TContext> _predicate = predicate;

    /// <inheritdoc />
    public string Name { get; } = name;

    /// <inheritdoc />
    public string? Group { get; } = group;

    /// <summary>
    /// Runs the wrapped predicate and builds the <see cref="RuleResult"/>
    /// from whatever it reports -- this is the one place that owns
    /// <see cref="Name"/>, never the predicate itself.
    /// </summary>
    /// <param name="context">Forwarded to the wrapped predicate as-is.</param>
    /// <param name="cancellationToken">Forwarded to the wrapped predicate as-is.</param>
    /// <returns>
    /// A <see cref="RuleResult"/> attributed to <see cref="Name"/>, carrying
    /// the wrapped predicate's own <see cref="PredicateOutcome.Passed"/>,
    /// <see cref="PredicateOutcome.Detail"/>, and <see cref="PredicateOutcome.Data"/>.
    /// </returns>
    /// <exception cref="OperationCanceledException">
    /// <paramref name="cancellationToken"/> is already cancelled, in which case
    /// the predicate is not invoked at all.
    /// </exception>
    public async Task<RuleResult> EvaluateAsync(TContext context, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var outcome = await _predicate(context, cancellationToken).ConfigureAwait(false);
        return new RuleResult(Name, outcome.Passed, outcome.Detail, outcome.Data);
    }

    /// <inheritdoc />
    public override string ToString() =>
        $"FunctionRule \"{Name}\"" + (string.IsNullOrEmpty(Group) ? string.Empty : $" ({Group})");

    /// <summary>What a debugger shows without expanding the object.</summary>
    private string DebuggerDisplay => ToString();
}
