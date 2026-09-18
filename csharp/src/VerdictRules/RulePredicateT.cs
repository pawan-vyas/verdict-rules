namespace VerdictRules;

/// <summary>
/// The shape any rule predicate has, generic over the context it reads
/// from: given the current facts, decide whether one named condition
/// passed.
/// </summary>
/// <typeparam name="TContext">The type this predicate reads from.</typeparam>
/// <param name="context">The facts this predicate reads from.</param>
/// <param name="cancellationToken">
/// Forwarded from whichever <see cref="RulesEngine{TContext}"/> run method or
/// composite triggered this evaluation.
/// </param>
/// <returns>The outcome of the one condition this predicate decides.</returns>
public delegate Task<RuleResult> RulePredicate<TContext>(TContext context, CancellationToken cancellationToken = default);
