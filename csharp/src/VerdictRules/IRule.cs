namespace VerdictRules;

/// <summary>
/// The contract every rule satisfies.
/// </summary>
/// <remarks>
/// <para>
/// C# <i>does</i> have structural typing — for delegates. Any method or lambda
/// matching the predicate signature is accepted by <see cref="FunctionRule"/>
/// with nothing declared and no type to name, which is the same "if it has the
/// shape, it is a rule" property Python's <c>Protocol</c> and TypeScript's
/// structural interfaces give. A method group works directly:
/// <c>new FunctionRule("check", MyCheck)</c>.
/// </para>
/// <para>
/// What C# lacks is structural typing for a <i>multi-member</i> interface. An
/// object that happens to carry <c>Name</c>, <c>Group</c> and
/// <c>EvaluateAsync</c> is not thereby an <see cref="IRule"/>; a rule shape
/// that owns its own name and group has to declare <c>: IRule</c>. Python and
/// TypeScript accept such an object as-is. That is the real, narrow difference
/// between the SDKs — and it is why <see cref="FunctionRule"/> carries more
/// weight here: it is the escape hatch back to shape-based rules, and most
/// rules should use it rather than declaring a type.
/// </para>
/// </remarks>
public interface IRule
{
    /// <summary>
    /// Unique identifier for this rule, used for engine lookups and to
    /// attribute a <see cref="RuleResult"/> back to its source.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Optional group label. Rules sharing one can be run together.
    /// </summary>
    string? Group { get; }

    /// <summary>Evaluates this rule against <paramref name="context"/>.</summary>
    Task<RuleResult> EvaluateAsync(IReadOnlyDictionary<string, object?> context);
}
