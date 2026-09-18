namespace VerdictRules;

/// <summary>
/// The shape any rule predicate has: given the current facts, decide whether
/// one named condition passed.
/// </summary>
/// <remarks>
/// A lambda or method group converts to <see cref="RulePredicate"/> the same
/// way it would to the equivalent bare <c>Func&lt;...&gt;</c>. A variable
/// already typed as
/// <c>Func&lt;IReadOnlyDictionary&lt;string, object?&gt;, CancellationToken, Task&lt;RuleResult&gt;&gt;</c>
/// does not implicitly convert to <see cref="RulePredicate"/> — wrap it
/// explicitly (<c>new RulePredicate(existingFunc)</c>).
/// </remarks>
/// <param name="context">The facts this predicate reads from.</param>
/// <param name="cancellationToken">
/// Forwarded from whichever <see cref="RulesEngine"/> run method or composite
/// triggered this evaluation.
/// </param>
/// <returns>The outcome of the one condition this predicate decides.</returns>
public delegate Task<RuleResult> RulePredicate(IReadOnlyDictionary<string, object?> context, CancellationToken cancellationToken = default);
