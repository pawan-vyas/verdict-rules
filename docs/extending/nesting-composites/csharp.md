<!-- Title: Extending — Nesting Composites Arbitrarily (C#) -->
# Nesting composites arbitrarily: the C# SDK

> The concept and the diagram are in [`README.md`](README.md) — read
> that first. This page is the concrete C# code.

```csharp
using VerdictRules;

static Task<RuleResult> IsActiveAccount(IReadOnlyDictionary<string, object?> context) =>
    Task.FromResult(new RuleResult("is_active_account", (string)context["account_status"]! == "active"));

static Task<RuleResult> IsPremiumMember(IReadOnlyDictionary<string, object?> context) =>
    Task.FromResult(new RuleResult("is_premium_member", (bool)context["is_premium_member"]!));

static Task<RuleResult> HasPromoCode(IReadOnlyDictionary<string, object?> context) =>
    Task.FromResult(new RuleResult("has_promo_code", context.TryGetValue("promo_code", out var v) && v is string { Length: > 0 }));

static Task<RuleResult> MeetsSpendThreshold(IReadOnlyDictionary<string, object?> context) =>
    Task.FromResult(new RuleResult("meets_spend_threshold", (double)context["spend"]! >= (double)context["spend_threshold"]!));

// Nesting doesn't care what built its sub-rules -- each of the four leaves
// here is a plain FunctionRule, but any IRule (a custom shape, another
// composite) would compose exactly the same way.
var qualifies = new AndRule("qualifies", new IRule[]
{
    new FunctionRule("is_active_account", IsActiveAccount),
    new OrRule("has_a_valid_reason", new IRule[]
    {
        new FunctionRule("is_premium_member", IsPremiumMember),
        new FunctionRule("has_promo_code", HasPromoCode),
        new FunctionRule("meets_spend_threshold", MeetsSpendThreshold),
    }),
});
```

```csharp
var context = new Dictionary<string, object?>
{
    ["account_status"] = "active",
    ["is_premium_member"] = false,
    ["promo_code"] = "SAVE10",
    ["spend"] = 20.0,
    ["spend_threshold"] = 100.0,
};
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
