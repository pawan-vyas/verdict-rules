<!-- Title: Extending — Nesting Composites Arbitrarily (C#) -->
# Nesting composites arbitrarily: the C# SDK

> The concept and the diagram are in [`README.md`](README.md) — read
> that first. This page is the concrete C# code.

```csharp
using VerdictRules;

record AccountContext(
    string AccountStatus,
    bool IsPremiumMember,
    string? PromoCode,
    double Spend,
    double SpendThreshold);

static Task<PredicateOutcome> IsActiveAccount(AccountContext context, CancellationToken cancellationToken = default) =>
    Task.FromResult(new PredicateOutcome(context.AccountStatus == "active"));

static Task<PredicateOutcome> IsPremiumMember(AccountContext context, CancellationToken cancellationToken = default) =>
    Task.FromResult(new PredicateOutcome(context.IsPremiumMember));

static Task<PredicateOutcome> HasPromoCode(AccountContext context, CancellationToken cancellationToken = default) =>
    Task.FromResult(new PredicateOutcome(!string.IsNullOrEmpty(context.PromoCode)));

static Task<PredicateOutcome> MeetsSpendThreshold(AccountContext context, CancellationToken cancellationToken = default) =>
    Task.FromResult(new PredicateOutcome(context.Spend >= context.SpendThreshold));

// Nesting doesn't care what built its sub-rules -- each of the four leaves
// here is a plain FunctionRule, but any IRule (a custom shape, another
// composite) would compose exactly the same way.
var qualifies = new AndRule<AccountContext>("qualifies", new IRule<AccountContext>[]
{
    new FunctionRule<AccountContext>("is_active_account", IsActiveAccount),
    new OrRule<AccountContext>("has_a_valid_reason", new IRule<AccountContext>[]
    {
        new FunctionRule<AccountContext>("is_premium_member", IsPremiumMember),
        new FunctionRule<AccountContext>("has_promo_code", HasPromoCode),
        new FunctionRule<AccountContext>("meets_spend_threshold", MeetsSpendThreshold),
    }),
});
```

```csharp
var context = new AccountContext("active", false, "SAVE10", 20, 100);
var result = await qualifies.EvaluateAsync(context);
result.Passed;
// true
result.Data;
// [RuleResult(RuleName: "is_active_account", Passed: true, ...),
//  RuleResult(RuleName: "has_a_valid_reason", Passed: true, ...,
//             Data: [RuleResult(RuleName: "is_premium_member", Passed: false, ...),
//                    RuleResult(RuleName: "has_promo_code", Passed: true, ...)])]
// meets_spend_threshold never ran -- has_a_valid_reason short-circuited
// once has_promo_code passed, exactly as a plain, unnested OrRule would
```

## Related

- [`README.md`](README.md) — the language-agnostic scenario this page
  implements.
