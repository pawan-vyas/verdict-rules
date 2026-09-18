namespace VerdictRules;

/// <summary>
/// The shape any rule predicate has, generic over the context it reads
/// from: given the current facts, decide whether one named condition
/// passed.
/// </summary>
/// <remarks>
/// Same relationship to the non-generic <see cref="RulePredicate"/> that
/// <see cref="IRule{TContext}"/> has to <see cref="IRule"/> — an independent
/// delegate type at a different generic arity, not one wrapping the other.
/// A lambda or method group typed against <typeparamref name="TContext"/>
/// converts to this exactly as one typed against
/// <see cref="IReadOnlyDictionary{TKey, TValue}"/> converts to
/// <see cref="RulePredicate"/>.
/// </remarks>
/// <typeparam name="TContext">The type this predicate reads from.</typeparam>
/// <param name="context">The facts this predicate reads from.</param>
/// <param name="cancellationToken">
/// Forwarded from whichever <see cref="RulesEngine{TContext}"/> run method or
/// composite triggered this evaluation. Observing it is optional — a
/// predicate with nothing cancellable to do may ignore it — but a predicate
/// wrapping real I/O should pass it through to whatever it awaits.
/// </param>
/// <returns>The outcome of the one condition this predicate decides.</returns>
public delegate Task<RuleResult> RulePredicate<TContext>(TContext context, CancellationToken cancellationToken = default);
