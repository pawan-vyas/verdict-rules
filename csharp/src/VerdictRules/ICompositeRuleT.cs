namespace VerdictRules;

/// <summary>
/// A rule built from other rules, whose parts can be read without evaluating
/// it.
/// </summary>
/// <remarks>
/// <para>
/// Satisfied by <see cref="AndRule{TContext}" />, <see cref="OrRule{TContext}" />
/// and <see cref="NotRule{TContext}" /> -- and, deliberately, by a consumer's own
/// composite. That is the point of putting this on a contract rather than on the
/// three built-in types: one walk reaches a combinator this package never saw,
/// where a <see langword="switch" /> over concrete types silently walks past it
/// and reports the rules inside it as absent.
/// </para>
/// <para>
/// A leaf rule does not implement this, so "is this structure, or a terminal
/// check" is answerable without inspecting concrete types.
/// </para>
/// <para>
/// This describes a rule tree, before anything is evaluated. It makes no claim
/// about what ran -- that is <see cref="RuleResult.SubResults" />'s job, and the
/// two differ precisely because a composite short-circuits.
/// </para>
/// </remarks>
/// <typeparam name="TContext">
/// The context type every rule in this composite shares, the same single type
/// parameter <see cref="IRule{TContext}" /> carries.
/// </typeparam>
public interface ICompositeRule<TContext> : IRule<TContext>
{
    /// <summary>
    /// The rules this composite is built from, in evaluation order: exactly one
    /// for a negation, and empty for a vacuous <see cref="AndRule{TContext}" /> or
    /// <see cref="OrRule{TContext}" />, which is a valid composite with no parts.
    /// </summary>
    /// <remarks>
    /// A property rather than a method, unlike a result's own derived views,
    /// which are methods because a get-only collection property gets serialized.
    /// A rule is never serialized, so the reason does not apply here -- see
    /// docs/maintenance/api-surface-allowlist.yaml.
    /// </remarks>
    IReadOnlyList<IRule<TContext>> SubRules { get; }
}
