namespace VerdictRules;

/// <summary>
/// Holds a set of rules and answers questions about them.
/// </summary>
/// <remarks>
/// Distinct from a composite: a composite returns one verdict and stops early,
/// whereas the engine's run modes are diagnostic and never short-circuit. They
/// exist to produce a full picture, not the fastest path to one boolean.
/// </remarks>
public sealed class RulesEngine
{
    private readonly IReadOnlyList<IRule> _rules;
    private readonly Dictionary<string, IRule> _byName;
    private readonly Dictionary<string, List<IRule>> _byGroup;

    /// <summary>Creates an engine over the given rules, in order.</summary>
    public RulesEngine(IReadOnlyList<IRule> rules)
    {
        _rules = rules;
        _byName = new Dictionary<string, IRule>(StringComparer.Ordinal);
        _byGroup = new Dictionary<string, List<IRule>>(StringComparer.Ordinal);

        foreach (var rule in rules)
        {
            _byName[rule.Name] = rule;
            if (string.IsNullOrEmpty(rule.Group))
            {
                continue;
            }

            if (!_byGroup.TryGetValue(rule.Group!, out var bucket))
            {
                bucket = new List<IRule>();
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
    public async Task<RunResult> RunAllAsync(IReadOnlyDictionary<string, object?> context)
    {
        var results = new List<RuleResult>();
        foreach (var rule in _rules)
        {
            results.Add(await rule.EvaluateAsync(context).ConfigureAwait(false));
        }

        return new RunResult(results.TrueForAll(r => r.Passed), results);
    }

    /// <summary>
    /// Evaluates one rule by name, or returns <c>null</c> if no such rule exists.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is the primitive; <see cref="RunNamedAsync"/> is a two-line
    /// assertion on top of it. The distinction matters when absence is an
    /// expected, legitimate state rather than a mistake — a rule set that
    /// varies per tenant, an optional group behind a feature flag, a name
    /// carried in configuration a given deployment has not adopted yet.
    /// </para>
    /// <para>
    /// In those cases the caller decides what absence means, because the
    /// engine cannot: for one consumer a missing rule means "nothing to
    /// enforce, pass", for another "skip this and do not count it", for a
    /// third "the configuration is wrong, fail loudly". A single library
    /// default would be right for one of them and wrong for the rest.
    /// </para>
    /// <para>
    /// <c>null</c> means <i>absent</i>, never <i>failed</i> — a rule that
    /// exists and fails still returns a <see cref="RuleResult"/> with
    /// <see cref="RuleResult.Passed"/> false.
    /// </para>
    /// </remarks>
    public async Task<RuleResult?> TryRunNamedAsync(string name, IReadOnlyDictionary<string, object?> context)
    {
        if (!_byName.TryGetValue(name, out var rule))
        {
            return null;
        }

        return await rule.EvaluateAsync(context).ConfigureAwait(false);
    }

    /// <summary>Evaluates exactly one rule, looked up by name.</summary>
    /// <remarks>
    /// The strict form, and the one to reach for by default: if a name is not
    /// expected to be absent, an absent name is a bug worth hearing about
    /// immediately. Use <see cref="TryRunNamedAsync"/> when absence is a state
    /// your own domain has an answer for.
    /// </remarks>
    /// <exception cref="KeyNotFoundException">No rule has this name.</exception>
    public async Task<RuleResult> RunNamedAsync(string name, IReadOnlyDictionary<string, object?> context)
    {
        var result = await TryRunNamedAsync(name, context).ConfigureAwait(false);
        if (result is null)
        {
            throw new KeyNotFoundException($"No rule named '{name}' in this engine");
        }

        return result;
    }

    /// <summary>
    /// Evaluates a group, or returns <c>null</c> if no such group exists.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is the primitive; <see cref="RunGroupAsync"/> is a two-line
    /// assertion on top of it. See <see cref="TryRunNamedAsync"/> for when
    /// reaching for it is right — the short version is that the engine cannot
    /// know whether an absent group means "no constraint applies here" or "the
    /// configuration is broken", and only the caller can.
    /// </para>
    /// <para>
    /// <c>null</c> means <i>absent</i>, never <i>vacuously passed</i>. A group
    /// exists only because some rule declared it, so an empty-but-real group is
    /// not representable, and a lookup matching nothing can only be a typo or a
    /// stale name. Returning a passing <see cref="RunResult"/> here would mean
    /// a misspelled group silently approves.
    /// </para>
    /// </remarks>
    public async Task<RunResult?> TryRunGroupAsync(string group, IReadOnlyDictionary<string, object?> context)
    {
        if (!_byGroup.TryGetValue(group, out var rules) || rules.Count == 0)
        {
            return null;
        }

        var results = new List<RuleResult>();
        foreach (var rule in rules)
        {
            results.Add(await rule.EvaluateAsync(context).ConfigureAwait(false));
        }

        return new RunResult(results.TrueForAll(r => r.Passed), results);
    }

    /// <summary>
    /// Evaluates every rule sharing a group label. Never short-circuits.
    /// </summary>
    /// <remarks>
    /// The strict form, and the one to reach for by default. Use
    /// <see cref="TryRunGroupAsync"/> when absence is a state your own domain
    /// has an answer for. This is the one place the package is strict:
    /// emptiness folds to an identity, absence is an error.
    /// </remarks>
    /// <exception cref="KeyNotFoundException">No rule carries this label.</exception>
    public async Task<RunResult> RunGroupAsync(string group, IReadOnlyDictionary<string, object?> context)
    {
        var result = await TryRunGroupAsync(group, context).ConfigureAwait(false);
        if (result is null)
        {
            throw new KeyNotFoundException($"No rules in group '{group}' in this engine");
        }

        return result;
    }
}
