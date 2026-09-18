namespace VerdictRules;

/// <summary>
/// Holds a set of rules sharing one context type and answers questions
/// about them.
/// </summary>
/// <remarks>
/// The generic sibling of <see cref="RulesEngine"/> — an independent, fresh
/// implementation at a different generic arity, for a cohesive family of
/// rules that genuinely share one <typeparamref name="TContext"/> (every
/// eligibility check that makes up "is this order valid," all reading one
/// <c>OrderContext</c>). The non-generic <see cref="RulesEngine"/> remains
/// the answer for a genuinely heterogeneous set of rules — many different
/// decision points, different natural shapes, addressed by name. Neither
/// form replaces the other.
/// </remarks>
/// <typeparam name="TContext">The context type every held rule shares.</typeparam>
public sealed class RulesEngine<TContext>
{
    /// <summary>Every registered rule, in registration order.</summary>
    private readonly IReadOnlyList<IRule<TContext>> _rules;

    /// <summary>Registered rules, indexed by <see cref="IRule{TContext}.Name"/> for <see cref="TryRunNamedAsync"/>.</summary>
    private readonly Dictionary<string, IRule<TContext>> _byName;

    /// <summary>Registered rules, bucketed by <see cref="IRule{TContext}.Group"/> for <see cref="TryRunGroupAsync"/>.</summary>
    private readonly Dictionary<string, List<IRule<TContext>>> _byGroup;

    /// <summary>Creates an engine over the given rules, in order.</summary>
    /// <param name="rules">Rules this engine holds, indexed by name and group.</param>
    public RulesEngine(IReadOnlyList<IRule<TContext>> rules)
    {
        _rules = rules;
        _byName = new Dictionary<string, IRule<TContext>>(rules.Count, StringComparer.Ordinal);
        _byGroup = new Dictionary<string, List<IRule<TContext>>>(StringComparer.Ordinal);

        foreach (var rule in rules)
        {
            _byName[rule.Name] = rule;
            if (string.IsNullOrEmpty(rule.Group))
            {
                continue;
            }

            if (!_byGroup.TryGetValue(rule.Group!, out var bucket))
            {
                bucket = [];
                _byGroup[rule.Group!] = bucket;
            }

            bucket.Add(rule);
        }
    }

    /// <summary>
    /// Every rule name registered here, in registration order. Exactly the
    /// names <see cref="RunNamedAsync"/> accepts, so a caller who cannot know
    /// in advance whether a rule exists can check rather than catch.
    /// </summary>
    public IReadOnlyCollection<string> RuleNames => _byName.Keys;

    /// <summary>
    /// Every group label carried by at least one rule. Exactly the labels
    /// <see cref="RunGroupAsync"/> accepts — a group is present only because
    /// some rule declared it.
    /// </summary>
    public IReadOnlyCollection<string> GroupNames => _byGroup.Keys;

    /// <summary>Evaluates every registered rule. Never short-circuits.</summary>
    /// <param name="context">The facts every registered rule's predicate reads from.</param>
    /// <param name="cancellationToken">Checked between rules, so a cancellation raised mid-run stops before the next rule starts.</param>
    /// <returns>One aggregate result carrying every rule's own outcome, in registration order.</returns>
    public async Task<RunResult> RunAllAsync(TContext context, CancellationToken cancellationToken = default)
    {
        var results = new List<RuleResult>(_rules.Count);
        foreach (var rule in _rules)
        {
            cancellationToken.ThrowIfCancellationRequested();
            results.Add(await rule.EvaluateAsync(context, cancellationToken).ConfigureAwait(false));
        }

        return new RunResult(results.TrueForAll(r => r.Passed), results);
    }

    /// <summary>
    /// Evaluates one rule by name, or returns <c>null</c> if no such rule exists.
    /// </summary>
    /// <remarks>
    /// This is the primitive; <see cref="RunNamedAsync"/> is a two-line
    /// assertion on top of it. See the non-generic <see cref="RulesEngine"/>'s
    /// own <c>TryRunNamedAsync</c> for the full reasoning behind the
    /// try-prefixed/strict split.
    /// </remarks>
    /// <param name="name">The rule name to look up, matching some <see cref="IRule{TContext}.Name"/>.</param>
    /// <param name="context">The facts the matched rule's predicate reads from.</param>
    /// <param name="cancellationToken">Forwarded to the matched rule's own <see cref="IRule{TContext}.EvaluateAsync"/>.</param>
    /// <returns>The rule's outcome, or <c>null</c> if <paramref name="name"/> matches no rule.</returns>
    public async Task<RuleResult?> TryRunNamedAsync(string name, TContext context, CancellationToken cancellationToken = default)
    {
        if (!_byName.TryGetValue(name, out var rule))
        {
            return null;
        }

        return await rule.EvaluateAsync(context, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Evaluates exactly one rule, looked up by name.</summary>
    /// <param name="name">The rule name to look up, matching some <see cref="IRule{TContext}.Name"/>.</param>
    /// <param name="context">The facts the matched rule's predicate reads from.</param>
    /// <param name="cancellationToken">Forwarded to the matched rule's own <see cref="IRule{TContext}.EvaluateAsync"/>.</param>
    /// <returns>The rule's outcome.</returns>
    /// <exception cref="KeyNotFoundException">No rule has this name.</exception>
    public async Task<RuleResult> RunNamedAsync(string name, TContext context, CancellationToken cancellationToken = default)
    {
        var result = await TryRunNamedAsync(name, context, cancellationToken).ConfigureAwait(false);
        return result ?? throw new KeyNotFoundException($"No rule named '{name}' in this engine");
    }

    /// <summary>
    /// Evaluates a group, or returns <c>null</c> if no such group exists.
    /// </summary>
    /// <param name="group">The group label to look up, matching some <see cref="IRule{TContext}.Group"/>.</param>
    /// <param name="context">The facts every rule in the matched group reads from.</param>
    /// <param name="cancellationToken">Checked between rules, so a cancellation raised mid-run stops before the next rule starts.</param>
    /// <returns>The group's aggregate result, or <c>null</c> if <paramref name="group"/> matches no rule.</returns>
    public async Task<RunResult?> TryRunGroupAsync(string group, TContext context, CancellationToken cancellationToken = default)
    {
        if (!_byGroup.TryGetValue(group, out var rules) || rules.Count == 0)
        {
            return null;
        }

        var results = new List<RuleResult>(rules.Count);
        foreach (var rule in rules)
        {
            cancellationToken.ThrowIfCancellationRequested();
            results.Add(await rule.EvaluateAsync(context, cancellationToken).ConfigureAwait(false));
        }

        return new RunResult(results.TrueForAll(r => r.Passed), results);
    }

    /// <summary>
    /// Evaluates every rule sharing a group label. Never short-circuits.
    /// </summary>
    /// <param name="group">The group label to look up, matching some <see cref="IRule{TContext}.Group"/>.</param>
    /// <param name="context">The facts every rule in the matched group reads from.</param>
    /// <param name="cancellationToken">Checked between rules, so a cancellation raised mid-run stops before the next rule starts.</param>
    /// <returns>The group's aggregate result.</returns>
    /// <exception cref="KeyNotFoundException">No rule carries this label.</exception>
    public async Task<RunResult> RunGroupAsync(string group, TContext context, CancellationToken cancellationToken = default)
    {
        var result = await TryRunGroupAsync(group, context, cancellationToken).ConfigureAwait(false);
        return result ?? throw new KeyNotFoundException($"No rules in group '{group}' in this engine");
    }
}
