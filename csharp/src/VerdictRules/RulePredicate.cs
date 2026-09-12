namespace VerdictRules;

/// <summary>
/// The shape any rule predicate has: given the current facts, decide whether
/// one named condition passed.
/// </summary>
/// <remarks>
/// <para>
/// A named delegate rather than the bare
/// <c>Func&lt;IReadOnlyDictionary&lt;string, object?&gt;, Task&lt;RuleResult&gt;&gt;</c>
/// it stands for — spelling that generic signature out at every field, stored
/// variable, or helper parameter is exactly the ceremony <see cref="FunctionRule"/>
/// exists to avoid. A lambda or method group converts to
/// <see cref="RulePredicate"/> exactly as it would to the bare <c>Func&lt;...&gt;</c>;
/// nothing changes at any call site this library's own docs show, including
/// method-group usage (<c>new FunctionRule("check", MyCheck)</c>).
/// </para>
/// <para>
/// One real difference to know about: C# delegate types are nominal once a
/// value already carries one, not structural the way a bare lambda converting
/// *to* a delegate type is. A variable already typed as
/// <c>Func&lt;IReadOnlyDictionary&lt;string, object?&gt;, Task&lt;RuleResult&gt;&gt;</c>
/// does not implicitly convert to <see cref="RulePredicate"/> even though the
/// signatures are identical — wrap it explicitly
/// (<c>new RulePredicate(existingFunc)</c>) if that case comes up. A raw lambda
/// or method group is unaffected either way.
/// </para>
/// </remarks>
public delegate Task<RuleResult> RulePredicate(IReadOnlyDictionary<string, object?> context);
