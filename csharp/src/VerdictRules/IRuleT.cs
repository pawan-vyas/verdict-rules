namespace VerdictRules;

/// <summary>
/// The contract every rule satisfies, generic over the context it reads from.
/// </summary>
/// <typeparam name="TContext">
/// The type this rule's predicate reads from. <see cref="IRule"/> is the
/// closed specialization over <see cref="IReadOnlyDictionary{TKey, TValue}"/>.
/// </typeparam>
public interface IRule<TContext>
{
    /// <summary>
    /// Unique identifier for this rule, used for engine lookups and to
    /// attribute a <see cref="RuleResult"/> back to its source.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Optional group label. Rules sharing one can be run together.
    /// </summary>
    string? Group { get; }

    /// <summary>Evaluates this rule against <paramref name="context"/>.</summary>
    /// <param name="context">The facts this rule's predicate reads from.</param>
    /// <param name="cancellationToken">
    /// Observed between sub-rules by every composite and by
    /// <see cref="RulesEngine{TContext}"/>'s own run methods.
    /// </param>
    /// <returns>The outcome, attributed back to this rule by <see cref="Name"/>.</returns>
    Task<RuleResult> EvaluateAsync(TContext context, CancellationToken cancellationToken = default);
}
