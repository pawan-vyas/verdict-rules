namespace VerdictRules;

/// <summary>
/// The contract every dict-context rule satisfies — a closed specialization
/// of <see cref="IRule{TContext}"/> over
/// <see cref="IReadOnlyDictionary{TKey, TValue}"/>, not an independent type.
/// </summary>
/// <remarks>
/// <para>
/// C# <i>does</i> have structural typing — for delegates. Any method or lambda
/// matching the predicate signature is accepted by <see cref="FunctionRule"/>
/// with nothing declared and no type to name, the "if it has the shape, it is
/// a rule" property. A method group works directly:
/// <c>new FunctionRule("check", MyCheck)</c>.
/// </para>
/// <para>
/// What C# lacks is structural typing for a <i>multi-member</i> interface. An
/// object that happens to carry <c>Name</c>, <c>Group</c> and
/// <c>EvaluateAsync</c> is not thereby an <see cref="IRule"/>; a rule shape
/// that owns its own name and group has to declare <c>: IRule</c> explicitly.
/// That is why <see cref="FunctionRule"/> carries more weight here: it is the
/// escape hatch back to shape-based rules, and most rules should use it
/// rather than declaring a type.
/// </para>
/// <para>
/// <c>IRule</c> and <c>IRule&lt;TContext&gt;</c> coexist as independent types
/// differentiated by generic arity, the same relationship
/// <see cref="IComparer{T}"/> has to the non-generic <c>IComparer</c> in the
/// BCL — not the direction <c>IEnumerable&lt;T&gt; : IEnumerable</c> takes,
/// which only works because that interface's type parameter sits in
/// covariant/output position. <c>TContext</c> here is in input position
/// (a parameter to <see cref="IRule{TContext}.EvaluateAsync"/>), so the
/// only sound relationship is this one: closing the open generic to a
/// concrete type. This is fully additive — every existing <c>: IRule</c>
/// implementation keeps compiling unchanged, because its own
/// <c>EvaluateAsync(IReadOnlyDictionary&lt;string, object?&gt;, ...)</c>
/// already satisfies <see cref="IRule{TContext}"/> once <c>TContext</c> is
/// closed to that same dictionary type.
/// </para>
/// </remarks>
public interface IRule : IRule<IReadOnlyDictionary<string, object?>>
{
}
