<!-- Title: Sample — Loyalty Tier Promotion (C#) -->
# Sample: Loyalty Tier Promotion

> The problem, the design, and what a solution must demonstrate are
> language-agnostic and live in [`README.md`](README.md) — read that
> first. This page is the C# implementation of it.

## The naive way (and why it breaks down)

The obvious first implementation ends up as *two* methods that have to
be kept in sync by hand — one for the yes/no decision, one for the
customer-facing checklist:

```csharp
static Dictionary<string, bool> GoldChecklist(Customer customer) => new()
{
    ["spend"] = customer.Trailing12MoSpend >= customer.GoldSpendThreshold,
    ["orders"] = customer.Trailing12MoOrders >= customer.GoldOrderThreshold,
    ["returns"] = customer.ReturnRate <= customer.GoldMaxReturnRate,
    ["standing"] = customer.AccountStatus == "active",
};

static bool IsEligibleForGold(Customer customer) => GoldChecklist(customer).Values.All(v => v);
```

Nothing enforces the coupling between the two methods — adding a
fifth requirement to one without the other produces a promotion
decision the UI's own checklist can't explain. See the spec for the
rest of what this shape gets wrong.

## The `verdict` way

```csharp
using VerdictRules;

static Task<RuleResult> MeetsSpendThreshold(IReadOnlyDictionary<string, object?> context)
{
    var passed = (double)context["trailing_12mo_spend"]! >= (double)context["gold_spend_threshold"]!;
    return Task.FromResult(new RuleResult("meets_spend_threshold", passed));
}

static Task<RuleResult> MeetsOrderCount(IReadOnlyDictionary<string, object?> context)
{
    var passed = (double)context["trailing_12mo_orders"]! >= (double)context["gold_order_threshold"]!;
    return Task.FromResult(new RuleResult("meets_order_count", passed));
}

static Task<RuleResult> ReturnRateBelowMax(IReadOnlyDictionary<string, object?> context)
{
    var passed = (double)context["return_rate"]! <= (double)context["gold_max_return_rate"]!;
    return Task.FromResult(new RuleResult("return_rate_below_max", passed));
}

static Task<RuleResult> AccountInGoodStanding(IReadOnlyDictionary<string, object?> context) =>
    Task.FromResult(new RuleResult("account_in_good_standing", (string)context["account_status"]! == "active"));

// Registered as four independent named rules on one engine -- not nested
// in an AndRule -- precisely so RunAllAsync() reports every criterion's
// own outcome, with no short-circuiting hiding a later criterion's result.
var engine = new RulesEngine(new IRule[]
{
    new FunctionRule("meets_spend_threshold", MeetsSpendThreshold),
    new FunctionRule("meets_order_count", MeetsOrderCount),
    new FunctionRule("return_rate_below_max", ReturnRateBelowMax),
    new FunctionRule("account_in_good_standing", AccountInGoodStanding),
});

async Task<RunResult> PromotionChecklist(IReadOnlyDictionary<string, object?> customerContext)
{
    var result = await engine.RunAllAsync(customerContext);
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
var customer = new Dictionary<string, object?>
{
    ["trailing_12mo_spend"] = 6000.0,
    ["gold_spend_threshold"] = 5000.0,
    ["trailing_12mo_orders"] = 20.0,
    ["gold_order_threshold"] = 15.0,
    ["return_rate"] = 0.08,
    ["gold_max_return_rate"] = 0.05,
    ["account_status"] = "active",
};

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
