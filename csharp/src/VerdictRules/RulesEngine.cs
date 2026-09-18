namespace VerdictRules;

/// <summary>
/// Holds a set of dict-context rules and answers questions about them.
/// </summary>
/// <remarks>
/// A specialization of <see cref="RulesEngine{TContext}"/> over
/// <see cref="IReadOnlyDictionary{TKey, TValue}"/> by composition, for the same
/// reason as <see cref="AndRule"/>: name/group indexing, run-mode semantics,
/// lookup strictness and the cancellation contract are defined once, in the
/// generic engine, and reached through here. The engine's run modes never
/// short-circuit.
/// </remarks>
/// <param name="rules">Rules this engine holds, indexed by name and group.</param>
public sealed class RulesEngine(IReadOnlyList<IRule> rules)
{
    /// <summary>The generic engine this type is a closed specialization of.</summary>
    private readonly RulesEngine<IReadOnlyDictionary<string, object?>> _inner =
        new(rules);

    /// <inheritdoc cref="RulesEngine{TContext}.RuleNames" />
    public IReadOnlyCollection<string> RuleNames => _inner.RuleNames;

    /// <inheritdoc cref="RulesEngine{TContext}.GroupNames" />
    public IReadOnlyCollection<string> GroupNames => _inner.GroupNames;

    /// <inheritdoc cref="RulesEngine{TContext}.RunAllAsync" />
    public Task<RunResult> RunAllAsync(IReadOnlyDictionary<string, object?> context, CancellationToken cancellationToken = default) =>
        _inner.RunAllAsync(context, cancellationToken);

    /// <inheritdoc cref="RulesEngine{TContext}.TryRunNamedAsync" />
    public Task<RuleResult?> TryRunNamedAsync(string name, IReadOnlyDictionary<string, object?> context, CancellationToken cancellationToken = default) =>
        _inner.TryRunNamedAsync(name, context, cancellationToken);

    /// <inheritdoc cref="RulesEngine{TContext}.RunNamedAsync" />
    public Task<RuleResult> RunNamedAsync(string name, IReadOnlyDictionary<string, object?> context, CancellationToken cancellationToken = default) =>
        _inner.RunNamedAsync(name, context, cancellationToken);

    /// <inheritdoc cref="RulesEngine{TContext}.TryRunGroupAsync" />
    public Task<RunResult?> TryRunGroupAsync(string group, IReadOnlyDictionary<string, object?> context, CancellationToken cancellationToken = default) =>
        _inner.TryRunGroupAsync(group, context, cancellationToken);

    /// <inheritdoc cref="RulesEngine{TContext}.RunGroupAsync" />
    public Task<RunResult> RunGroupAsync(string group, IReadOnlyDictionary<string, object?> context, CancellationToken cancellationToken = default) =>
        _inner.RunGroupAsync(group, context, cancellationToken);
}
