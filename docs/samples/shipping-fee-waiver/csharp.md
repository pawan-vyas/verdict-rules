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

A typed context, not a dictionary — and unlike the other typed samples,
one of its fields isn't order data at all: `PromoCodeService` is an
injected dependency, typed as an interface rather than `object`, so
every rule reading it gets the same compile-time checking as the plain
fields:

```csharp
using VerdictRules;

interface IPromoCodeService
{
    Task<bool> Validate(string? code);
}

record ShippingContext(
    double OrderTotal,
    double FreeShippingThreshold,
    bool IsPremiumMember,
    string? PromoCode,
    IPromoCodeService PromoCodeService);

static Task<RuleResult> OrderTotalOverThreshold(ShippingContext context, CancellationToken cancellationToken = default) =>
    Task.FromResult(new RuleResult("order_total_over_threshold", context.OrderTotal >= context.FreeShippingThreshold));

static Task<RuleResult> HasPremiumMembership(ShippingContext context, CancellationToken cancellationToken = default) =>
    Task.FromResult(new RuleResult("has_premium_membership", context.IsPremiumMember));

static async Task<RuleResult> HasValidPromoCode(ShippingContext context, CancellationToken cancellationToken = default)
{
    // The expensive path: only reached if both cheaper checks above failed.
    var isValid = await context.PromoCodeService.Validate(context.PromoCode);
    return new RuleResult("has_valid_promo_code", isValid);
}

var shipsFree = new OrRule<ShippingContext>("ships_free", new IRule<ShippingContext>[]
{
    new FunctionRule<ShippingContext>("order_total_over_threshold", OrderTotalOverThreshold),
    new FunctionRule<ShippingContext>("has_premium_membership", HasPremiumMembership),
    new FunctionRule<ShippingContext>("has_valid_promo_code", HasValidPromoCode), // cheapest-last, on purpose
});
```

The comment on the last rule is the whole fix, made visible: cost order
is now a stated decision at the point it matters, not something the
next person to edit this file has to reconstruct from scratch.

Proving the skip, not just asserting it — a call-counting fake stands
in for the real service, and satisfies `IPromoCodeService` the same way
the real implementation would:

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
var order = new ShippingContext(120, 50, false, null, promoService);

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
