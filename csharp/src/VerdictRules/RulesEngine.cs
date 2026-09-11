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

    /// <summary>Evaluates exactly one rule, looked up by name.</summary>
    /// <exception cref="KeyNotFoundException">No rule has this name.</exception>
    public Task<RuleResult> RunNamedAsync(string name, IReadOnlyDictionary<string, object?> context)
    {
        if (!_byName.TryGetValue(name, out var rule))
        {
            throw new KeyNotFoundException($"No rule named '{name}' in this engine");
        }

        return rule.EvaluateAsync(context);
    }

    /// <summary>
    /// Evaluates every rule sharing a group label. Never short-circuits.
    /// </summary>
    /// <exception cref="KeyNotFoundException">No rule carries this label.</exception>
    /// <remarks>
    /// <para>
    /// An unknown group throws rather than returning a vacuous pass, matching
    /// <see cref="RunNamedAsync"/>. A group exists only because some rule
    /// declared it, so an empty-but-real group is not representable and a
    /// lookup matching nothing can only be a typo or a stale name. Returning a
    /// pass there would mean a misspelled group silently approves.
    /// </para>
    /// <para>
    /// This is the one place the package is strict. Emptiness — a set you were
    /// handed that happened to be empty — still folds to its identity; absence
    /// is an error. Use <see cref="GroupNames"/> to check first if a group may
    /// legitimately be absent.
    /// </para>
    /// </remarks>
    public async Task<RunResult> RunGroupAsync(string group, IReadOnlyDictionary<string, object?> context)
    {
        if (!_byGroup.TryGetValue(group, out var rules) || rules.Count == 0)
        {
            throw new KeyNotFoundException($"No rules in group '{group}' in this engine");
        }

        var results = new List<RuleResult>();
        foreach (var rule in rules)
        {
            results.Add(await rule.EvaluateAsync(context).ConfigureAwait(false));
        }

        return new RunResult(results.TrueForAll(r => r.Passed), results);
    }
}
