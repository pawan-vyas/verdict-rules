namespace VerdictRules;

/// <summary>
/// The contract every dict-context rule satisfies — a closed specialization
/// of <see cref="IRule{TContext}"/> over
/// <see cref="IReadOnlyDictionary{TKey, TValue}"/>.
/// </summary>
public interface IRule : IRule<IReadOnlyDictionary<string, object?>>
{
}
