<!-- Title: Extending — Isolating A Flaky Predicate (C#) -->
# Isolating a flaky predicate: the C# SDK

> The concept and the reasoning are in [`README.md`](README.md) — read
> that first. This page is the concrete C# code.

```csharp
using VerdictRules;

// Turn a predicate's own exception into a failing RuleResult, instead of
// letting it propagate out of the run that contains it.
static FunctionRule Defensive(string name, RulePredicate predicate)
{
    async Task<RuleResult> Wrapped(IReadOnlyDictionary<string, object?> context, CancellationToken cancellationToken = default)
    {
        try
        {
            return await predicate(context, cancellationToken);
        }
        catch (Exception exc)
        {
            return new RuleResult(name, false, exc.Message);
        }
    }
    return new FunctionRule(name, Wrapped);
}

// Stands in for a real network call that can time out.
static Task<RuleResult> CheckPromoCodeAgainstExternalService(IReadOnlyDictionary<string, object?> context, CancellationToken cancellationToken = default)
{
    if (context.TryGetValue("simulate_timeout", out var t) && t is true)
    {
        throw new TimeoutException("promo-validation service did not respond");
    }
    return Task.FromResult(new RuleResult("promo_code_valid", (string?)context["promo_code"] == "SAVE10"));
}

var rule = Defensive("promo_code_valid", CheckPromoCodeAgainstExternalService);
```

```csharp
await rule.EvaluateAsync(new Dictionary<string, object?> { ["promo_code"] = "SAVE10" });
// RuleResult(RuleName: "promo_code_valid", Passed: true, ...)

await rule.EvaluateAsync(new Dictionary<string, object?> { ["promo_code"] = "SAVE10", ["simulate_timeout"] = true });
// RuleResult(RuleName: "promo_code_valid", Passed: false,
//            Detail: "promo-validation service did not respond")
```

The same timeout against the **unwrapped** predicate propagates instead
of returning a result — this is what every other rule shares a
`RunAllAsync`/`RunGroupAsync` with, unless it's wrapped too:

```csharp
var unwrapped = new FunctionRule("promo_code_valid", CheckPromoCodeAgainstExternalService);
await unwrapped.EvaluateAsync(new Dictionary<string, object?> { ["promo_code"] = "SAVE10", ["simulate_timeout"] = true });
// throws TimeoutException: promo-validation service did not respond
```

Now a timeout in the wrapped check reports as `Passed: false, Detail: "..."`
— one entry in `RunResult.Results`, same as any other failing rule — and
every other rule in that `RunAllAsync`/`RunGroupAsync` still runs and still
reports.

## Related

- [`README.md`](README.md) — the language-agnostic scenario this page
  implements.
