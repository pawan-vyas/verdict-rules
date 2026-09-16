<!-- Title: Sample — Dynamic Discount Eligibility (C#) -->
# Sample: Dynamic Discount Eligibility

> The problem, the design, and what a solution must demonstrate are
> language-agnostic and live in [`README.md`](README.md) — read that
> first. This page is the C# implementation of it.

## The naive way (and why it breaks down)

The obvious first implementation is a nested `if` chain, checked
straight against the cart, with the current campaign's numbers baked
directly into the code:

```csharp
static bool QualifiesForPromo(IReadOnlyDictionary<string, object?> cart)
{
    if ((double)cart["total"]! >= 100)
    {
        if ((string)cart["region"]! is "US" or "CA")
        {
            if ((bool)cart["is_first_purchase"]!)
            {
                return true;
            }
        }
    }
    return false;
}
```

This is fine for exactly as long as the campaign's numbers never
change — see the spec for the three specific ways it stops being fine.

## The `verdict` way

```csharp
using VerdictRules;

static Task<RuleResult> CartMeetsMinimum(IReadOnlyDictionary<string, object?> context)
{
    var total = (double)context["cart_total"]!;
    var minimum = (double)context["promo_minimum"]!;
    return Task.FromResult(new RuleResult(
        "cart_meets_minimum", total >= minimum, $"cart_total={total}, needs >= {minimum}"));
}

static Task<RuleResult> IsEligibleRegion(IReadOnlyDictionary<string, object?> context)
{
    var region = (string)context["region"]!;
    var eligible = (HashSet<string>)context["eligible_regions"]!;
    return Task.FromResult(new RuleResult(
        "is_eligible_region", eligible.Contains(region),
        $"region=\"{region}\" not in [{string.Join(", ", eligible.OrderBy(r => r))}]"));
}

static Task<RuleResult> IsFirstPurchase(IReadOnlyDictionary<string, object?> context) =>
    Task.FromResult(new RuleResult("is_first_purchase", (bool)context["is_first_purchase"]!));

// Built once. The AndRule and the engine below both hold these same three
// objects -- nothing is declared twice, and nothing here needs to know in
// advance which of the two call sites below will use it.
var minimumRule = new FunctionRule("cart_meets_minimum", CartMeetsMinimum);
var regionRule = new FunctionRule("is_eligible_region", IsEligibleRegion);
var firstPurchaseRule = new FunctionRule("is_first_purchase", IsFirstPurchase);

var qualifiesForPromo = new AndRule("qualifies_for_promo", new IRule[] { minimumRule, regionRule, firstPurchaseRule });
var engine = new RulesEngine(new IRule[] { minimumRule, regionRule, firstPurchaseRule });

/// <summary>The checkout gate: the cheapest possible yes/no, stopping at the
/// first failing condition. Nothing past this point needs to know *which*
/// condition failed -- only whether the discount applies.</summary>
async Task<bool> CheckCartFast(IReadOnlyDictionary<string, object?> cart, IReadOnlyDictionary<string, object?> campaign)
{
    var context = cart.Concat(campaign).ToDictionary(kv => kv.Key, kv => kv.Value);
    return (await qualifiesForPromo.EvaluateAsync(context)).Passed;
}

/// <summary>The support-facing view: every condition, always -- even if more
/// than one failed at once. Never the fast path's job, so it doesn't share
/// the fast path's short-circuiting.
///
/// <c>campaign</c> (e.g. promo_minimum: 100, eligible_regions: {US, CA}) is
/// whatever the checkout code already reads the active campaign's configured
/// values from -- nothing here cares where it came from.</summary>
async Task<RunResult> WhyNot(IReadOnlyDictionary<string, object?> cart, IReadOnlyDictionary<string, object?> campaign)
{
    var context = cart.Concat(campaign).ToDictionary(kv => kv.Key, kv => kv.Value);
    return await engine.RunAllAsync(context);
    // result.Passed is true only if every condition passed; result.Results
    // is one entry per condition, always all three, regardless of how many
    // failed -- this is what a "why not?" screen actually needs.
}
```

Run against a qualifying cart, then one that fails on two conditions at
once:

```csharp
var cart = new Dictionary<string, object?> { ["cart_total"] = 120.0, ["region"] = "US", ["is_first_purchase"] = true };
var campaign = new Dictionary<string, object?> { ["promo_minimum"] = 100.0, ["eligible_regions"] = new HashSet<string> { "US", "CA" } };

await CheckCartFast(cart, campaign);
// true

var result = await WhyNot(cart, campaign);
result.Passed;
// true -- every condition passed

var badCart = new Dictionary<string, object?> { ["cart_total"] = 50.0, ["region"] = "MX", ["is_first_purchase"] = true };
await CheckCartFast(badCart, campaign);
// false -- stops at the first failure

result = await WhyNot(badCart, campaign);
result.Passed;
// false
result.Results.Select(r => r.Passed);
// [false, false, true] -- both failures visible, not just the first
```

Swapping the campaign's minimum from `100` to `75`, or adding `"MX"` to
`eligible_regions`, is now a data change passed into either method —
no edit to `CartMeetsMinimum`/`IsEligibleRegion`/`IsFirstPurchase`,
the `AndRule`, or the engine.

## Related

- [`README.md`](README.md) —
  the language-agnostic spec this page implements.
- [`shipping-fee-waiver/csharp.md`](../shipping-fee-waiver/csharp.md) — the `OrRule`
  mirror image of this same idea (any one qualifying path, not all).
- [`data-driven-rule-sets/csharp.md`](../data-driven-rule-sets/csharp.md) — building
  the rules themselves from stored config, not just their values.
