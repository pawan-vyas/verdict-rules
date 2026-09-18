namespace VerdictRules;

/// <summary>
/// Wraps a plain asynchronous predicate as an <see cref="IRule{TContext}"/>.
/// </summary>
/// <remarks>
/// The generic sibling of <see cref="FunctionRule"/> — an independent,
/// fresh implementation at a different generic arity, not a wrapper around
/// the non-generic type. <typeparamref name="TContext"/> is inferred from
/// the wrapped predicate's own parameter type, so
/// <c>new FunctionRule&lt;OrderContext&gt;("x", predicate)</c> rarely needs
/// the type argument spelled out explicitly when <paramref name="predicate"/>
/// is already typed.
/// </remarks>
/// <typeparam name="TContext">The type this rule's predicate reads from.</typeparam>
/// <param name="name">See <see cref="Name"/>.</param>
/// <param name="predicate">The wrapped predicate <see cref="EvaluateAsync"/> delegates to.</param>
/// <param name="group">See <see cref="Group"/>.</param>
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
    public Task<RuleResult> EvaluateAsync(TContext context, CancellationToken cancellationToken = default) =>
        _predicate(context, cancellationToken);
}
