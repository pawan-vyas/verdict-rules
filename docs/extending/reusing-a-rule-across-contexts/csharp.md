<!-- Title: Extending — Reusing a Typed Rule Across Contexts (C#) -->
# Reusing a typed rule across contexts: the C# SDK

> The concept, the diagram, and the trade-off are in
> [`README.md`](README.md) — read that first. This page is the concrete
> C# code.

```csharp
using VerdictRules;

/// <summary>
/// Adapts an IRule&lt;TInner&gt; to run inside a composite built on TOuter.
/// </summary>
/// <remarks>
/// Not part of verdict-rules itself -- a consumer-defined adapter, exactly
/// as free to exist as a new rule shape is, with no changes needed on
/// verdict-rules' side to support it.
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
```

Written once against its own narrow context, the same rule now projects
into two unrelated composites:

```csharp
public sealed record UserFlag(bool IsVerified);

Task<RuleResult> IsVerifiedUser(UserFlag ctx, CancellationToken ct = default) =>
    Task.FromResult(new RuleResult("is_verified_user", ctx.IsVerified));

public sealed record OrderContext(decimal Total, bool IsVerified);
public sealed record SignupContext(string Email, bool IsVerified);

var verifiedRule = new FunctionRule<UserFlag>("is_verified_user", IsVerifiedUser);

var checkoutVerified = new ProjectingRule<OrderContext, UserFlag>(
    verifiedRule, ctx => new UserFlag(ctx.IsVerified));
var signupVerified = new ProjectingRule<SignupContext, UserFlag>(
    verifiedRule, ctx => new UserFlag(ctx.IsVerified));

var checkout = new AndRule<OrderContext>("eligible", [checkoutVerified]);
var signup = new AndRule<SignupContext>("eligible", [signupVerified]);

var result = await checkout.EvaluateAsync(new OrderContext(75m, true));
result.Passed; // true -- delegated straight through to IsVerifiedUser
```

`ProjectingRule<TOuter, TInner>` satisfies `IRule<TOuter>` structurally
(well-formedly — via the explicit interface declaration C# nominal
typing requires), the same way every other rule in this package does —
nothing about `AndRule<TContext>` or `RulesEngine<TContext>` needed to
change to accept it.

## Related

- [`README.md`](README.md) — the language-agnostic spec this page
  implements.
- [`../new-rule-shape/csharp.md`](../new-rule-shape/csharp.md) — the
  same pattern (a consumer-defined type satisfying `IRule` explicitly)
  applied to combination logic instead of context adaptation.
