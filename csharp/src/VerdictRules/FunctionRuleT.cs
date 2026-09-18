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
public sealed class FunctionRule<TContext>(string name, RulePredicate<TContext> predicate, string? group = null) : IRule<TContext>
{
    /// <summary>The wrapped predicate, run unchanged by <see cref="EvaluateAsync"/>.</summary>
    private readonly RulePredicate<TContext> _predicate = predicate;

    /// <inheritdoc />
    public string Name { get; } = name;

    /// <inheritdoc />
    public string? Group { get; } = group;

    /// <summary>
    /// Runs the wrapped predicate and returns whatever it returns, unchanged.
    /// </summary>
    /// <param name="context">Forwarded to the wrapped predicate as-is.</param>
    /// <param name="cancellationToken">Forwarded to the wrapped predicate as-is.</param>
    /// <returns>Whatever the wrapped predicate returns, unchanged.</returns>
    /// <exception cref="OperationCanceledException">
    /// <paramref name="cancellationToken"/> is already cancelled, in which case
    /// the predicate is not invoked at all. Thrown synchronously, as a guard on
    /// a method that is deliberately not <c>async</c>.
    /// </exception>
    public Task<RuleResult> EvaluateAsync(TContext context, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return _predicate(context, cancellationToken);
    }
}
