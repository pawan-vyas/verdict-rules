using VerdictRules;

namespace MarketplaceEligibility;

/// <summary>
/// Adapts an <see cref="IRule{TInner}"/> to run inside a composite built on
/// <typeparamref name="TOuter"/>.
/// </summary>
/// <remarks>
/// Not part of verdict-rules itself. See
/// docs/extending/reusing-a-rule-across-contexts/csharp.md.
/// </remarks>
public sealed class ProjectingRule<TOuter, TInner>(
    IRule<TInner> inner,
    Func<TOuter, TInner> project) : IRule<TOuter>
{
    public string Name { get; } = inner.Name;
    public string? Group { get; } = inner.Group;

    public Task<RuleResult> EvaluateAsync(TOuter context, CancellationToken cancellationToken = default) =>
        inner.EvaluateAsync(project(context), cancellationToken);
}
