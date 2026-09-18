<!-- Title: Extending — Wrapping A Predicate (C#) -->
# Wrapping a predicate: the C# SDK

> The concept and why it's the common case are in
> [`README.md`](README.md) — read that first. This page is the concrete
> C# code.

```csharp
using VerdictRules;

static Task<RuleResult> CartMeetsMinimum(IReadOnlyDictionary<string, object?> context, CancellationToken cancellationToken = default)
{
    var total = (double)context["cart_total"]!;
    var minimum = (double)context["minimum_for_offer"]!;
    return Task.FromResult(new RuleResult(
        "cart_meets_minimum",
        total >= minimum,
        $"{total} vs minimum {minimum}"));
}

var rule = new FunctionRule("cart_meets_minimum", CartMeetsMinimum);
```

The wrap itself doesn't care what the predicate's own context looks
like — a predicate already written against a typed context wraps
exactly the same way:

```csharp
record CartContext(double CartTotal, double MinimumForOffer);

static Task<RuleResult> CartMeetsMinimumTyped(CartContext context, CancellationToken cancellationToken = default) =>
    Task.FromResult(new RuleResult(
        "cart_meets_minimum",
        context.CartTotal >= context.MinimumForOffer,
        $"{context.CartTotal} vs minimum {context.MinimumForOffer}"));

var typedRule = new FunctionRule<CartContext>("cart_meets_minimum", CartMeetsMinimumTyped);
```

## Related

- [`README.md`](README.md) — the language-agnostic scenario this page
  implements.
- [`../new-rule-shape/csharp.md`](../new-rule-shape/csharp.md) — the
  next step up, for combination logic `FunctionRule` alone can't express.
