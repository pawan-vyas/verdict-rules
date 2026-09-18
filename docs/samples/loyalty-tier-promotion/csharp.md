<!-- Title: Sample — Loyalty Tier Promotion (C#) -->
# Sample: Loyalty Tier Promotion

> The problem, the design, and what a solution must demonstrate are
> language-agnostic and live in [`README.md`](README.md) — read that
> first. This page is the C# implementation of it.

## The naive way (and why it breaks down)

The obvious first implementation is *two* methods, one for the
customer-facing checklist and one for the actual yes/no decision, each
with the four criteria's thresholds written inline:

```csharp
static Dictionary<string, bool> GoldChecklist(Customer customer) => new()
{
    ["spend"] = customer.Trailing12MoSpend >= 5000,
    ["orders"] = customer.Trailing12MoOrders >= 15,
    ["returns"] = customer.ReturnRate <= 0.05,
    ["standing"] = customer.AccountStatus == "active",
};

static bool IsEligibleForGold(Customer customer) =>
    customer.Trailing12MoSpend >= 5000
    && customer.Trailing12MoOrders >= 20
    && customer.ReturnRate <= 0.05
    && customer.AccountStatus == "active";
```

`GoldChecklist` promotes at 15 orders; `IsEligibleForGold` at 20 —
visible by reading the two methods side by side, not a hypothetical
future drift. See the spec for the rest of what this shape gets wrong.

## The `verdict` way

A typed context, not a dictionary — each threshold has exactly one
definition, read by name from every rule that needs it:

```csharp
using VerdictRules;

record LoyaltyContext(
    double Trailing12MoSpend,
    double GoldSpendThreshold,
    double Trailing12MoOrders,
    double GoldOrderThreshold,
    double ReturnRate,
    double GoldMaxReturnRate,
    string AccountStatus);

static Task<RuleResult> MeetsSpendThreshold(LoyaltyContext context, CancellationToken cancellationToken = default) =>
    Task.FromResult(new RuleResult("meets_spend_threshold", context.Trailing12MoSpend >= context.GoldSpendThreshold));

static Task<RuleResult> MeetsOrderCount(LoyaltyContext context, CancellationToken cancellationToken = default) =>
    Task.FromResult(new RuleResult("meets_order_count", context.Trailing12MoOrders >= context.GoldOrderThreshold));

static Task<RuleResult> ReturnRateBelowMax(LoyaltyContext context, CancellationToken cancellationToken = default) =>
    Task.FromResult(new RuleResult("return_rate_below_max", context.ReturnRate <= context.GoldMaxReturnRate));

static Task<RuleResult> AccountInGoodStanding(LoyaltyContext context, CancellationToken cancellationToken = default) =>
    Task.FromResult(new RuleResult("account_in_good_standing", context.AccountStatus == "active"));

// Registered as four independent named rules on one engine -- not nested
// in an AndRule -- precisely so RunAllAsync() reports every criterion's
// own outcome, with no short-circuiting hiding a later criterion's result.
var engine = new RulesEngine<LoyaltyContext>(new IRule<LoyaltyContext>[]
{
    new FunctionRule<LoyaltyContext>("meets_spend_threshold", MeetsSpendThreshold),
    new FunctionRule<LoyaltyContext>("meets_order_count", MeetsOrderCount),
    new FunctionRule<LoyaltyContext>("return_rate_below_max", ReturnRateBelowMax),
    new FunctionRule<LoyaltyContext>("account_in_good_standing", AccountInGoodStanding),
});

async Task<RunResult> PromotionChecklist(LoyaltyContext context)
{
    var result = await engine.RunAllAsync(context);
    // result.Passed is true only if all four passed -- the actual promotion decision.
    // result.Results is one RuleResult per criterion, always all four -- the UI checklist.
    return result;
}
```

This is the one case where reaching for a bare `AndRule` and reaching
for the engine's `RunAllAsync()` produce genuinely different, both-correct
answers depending on what the caller actually needs — see
[`../../architecture/csharp.md`](../../architecture/csharp.md#three-ways-to-run-rules-concretely)
for the general rule of thumb.

A customer meeting three of the four criteria — the same case the
spec's own diagram shows:

```csharp
var customer = new LoyaltyContext(
    Trailing12MoSpend: 6000,
    GoldSpendThreshold: 5000,
    Trailing12MoOrders: 20,
    GoldOrderThreshold: 15,
    ReturnRate: 0.08,
    GoldMaxReturnRate: 0.05,
    AccountStatus: "active");

var checklist = await PromotionChecklist(customer);
checklist.Passed;
// false -- not promoted, the return rate is over the limit
checklist.Results.Select(r => (r.RuleName, r.Passed));
// [("meets_spend_threshold", true), ("meets_order_count", true),
//  ("return_rate_below_max", false), ("account_in_good_standing", true)]
// all four report, not just the one that decided the outcome
```

## Related

- [`README.md`](README.md) —
  the language-agnostic spec this page implements.
- [`content-moderation-routing/csharp.md`](../content-moderation-routing/csharp.md) —
  another `Run*` (this time `RunGroupAsync()`) example, for when a single
  engine needs to serve more than one independent decision.
