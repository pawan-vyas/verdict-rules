<!-- Title: Sample — Shipping Fee Waiver (C#) -->
# Sample: Shipping Fee Waiver

> The problem, the design, and what a solution must demonstrate are
> language-agnostic and live in [`README.md`](README.md) — read that
> first. This page is the C# implementation of it.

## The naive way (and why it breaks down)

The obvious first implementation is a conditional ladder, one `if` per
qualifying path:

```csharp
interface IPromoCodeService
{
    Task<bool> Validate(string? code);
}

async Task<bool> ShipsFree(Order order, IPromoCodeService promoCodeService)
{
    if (await promoCodeService.Validate(order.PromoCode))
    {
        return true;
    }
    if (order.IsPremiumMember)
    {
        return true;
    }
    if (order.Total >= order.FreeShippingThreshold)
    {
        return true;
    }
    return false;
}
```

Adding a fourth path means opening this method and inserting another
`if` among the ones already there, at risk to the branches already
correct and already shipped. Here that cost is concrete: the
promo-code check is an external service call, and it happens to run
first, so it runs on **every single order** — including a $500 order
from a non-member with no promo code, which qualifies on total alone
and never needed the network round trip at all. See the spec for the
rest of what this shape gets wrong.

## The `verdict-rules` way

```csharp
using VerdictRules;

interface IPromoCodeService
{
    Task<bool> Validate(string? code);
}

static Task<RuleResult> OrderTotalOverThreshold(IReadOnlyDictionary<string, object?> context, CancellationToken cancellationToken = default)
{
    var passed = (double)context["orderTotal"]! >= (double)context["freeShippingThreshold"]!;
    return Task.FromResult(new RuleResult("order_total_over_threshold", passed));
}

static Task<RuleResult> HasPremiumMembership(IReadOnlyDictionary<string, object?> context, CancellationToken cancellationToken = default) =>
    Task.FromResult(new RuleResult("has_premium_membership", (bool)context["isPremiumMember"]!));

static async Task<RuleResult> HasValidPromoCode(IReadOnlyDictionary<string, object?> context, CancellationToken cancellationToken = default)
{
    // The expensive path: only reached if both cheaper checks above failed.
    var service = (IPromoCodeService)context["promoCodeService"]!;
    var isValid = await service.Validate((string?)context["promoCode"]);
    return new RuleResult("has_valid_promo_code", isValid);
}

var shipsFree = new OrRule("ships_free", new IRule[]
{
    new FunctionRule("order_total_over_threshold", OrderTotalOverThreshold),
    new FunctionRule("has_premium_membership", HasPremiumMembership),
    new FunctionRule("has_valid_promo_code", HasValidPromoCode), // cheapest-last, on purpose
});
```

The comment on the last rule is the whole fix, made visible: cost order
is now a stated decision at the point it matters, not something the
next person to edit this file has to reconstruct from scratch.

Proving the skip, not just asserting it — a call-counting fake stands
in for the real service:

```csharp
sealed class FakePromoService : IPromoCodeService
{
    public int Calls { get; private set; }
    public Task<bool> Validate(string? code)
    {
        Calls += 1;
        return Task.FromResult(code == "SAVE10");
    }
}

var promoService = new FakePromoService();
var order = new Dictionary<string, object?>
{
    ["orderTotal"] = 120.0,
    ["freeShippingThreshold"] = 50.0,
    ["isPremiumMember"] = false,
    ["promoCode"] = null,
    ["promoCodeService"] = promoService,
};

var result = await shipsFree.EvaluateAsync(order);
result.Passed;
promoService.Calls;
// true, 0 -- order_total alone already qualifies; the promo service is never called
```

## Related

- [`README.md`](README.md) —
  the language-agnostic spec this page implements.
- [`dynamic-discounts/csharp.md`](../dynamic-discounts/csharp.md) — the `AndRule`
  mirror image (every condition must pass, not just one).
- [`../../architecture/csharp.md`](../../architecture/csharp.md#execution-model-concretely) —
  why short-circuiting only means something because evaluation is
  sequential, never concurrent.
