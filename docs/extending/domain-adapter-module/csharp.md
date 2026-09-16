<!-- Title: Extending — Keep Your Own Domain Out Of Verdict (C#) -->
# Keep your own domain out of verdict: the C# SDK

> The concept and the diagram are in [`README.md`](README.md) — read
> that first. This page is the concrete C# code, illustrating a
> rate-limiting adapter (one of the two illustrations the spec names),
> and — since the point of the boundary is that `VerdictRules` itself
> is replaceable behind it — a second implementation of the identical
> contract that doesn't use `VerdictRules` at all.

```csharp
using VerdictRules;

/// <summary>This adapter's own domain type -- verdict never sees it directly,
/// only hands it back as RuleResult.Data's opaque payload.</summary>
sealed record RateLimitStatus(string Window, int Used, int Quota);

/// <summary>The contract every call site depends on. Nothing here mentions
/// VerdictRules.</summary>
interface RateLimiter
{
    Task<IReadOnlyList<RateLimitStatus>> Check(IReadOnlyDictionary<string, object?> context, IReadOnlyDictionary<string, int> windows);
}

/// <summary>The only place in this codebase that references VerdictRules.</summary>
sealed class VerdictRateLimiter : RateLimiter
{
    public async Task<IReadOnlyList<RateLimitStatus>> Check(IReadOnlyDictionary<string, object?> context, IReadOnlyDictionary<string, int> windows)
    {
        IRule RuleFor(string window, int quota) => new FunctionRule(
            $"{window}_under_quota",
            ctx =>
            {
                var used = (int)ctx[$"{window}_used"]!;
                var status = new RateLimitStatus(window, used, quota);
                return Task.FromResult(new RuleResult(
                    $"{window}_under_quota",
                    used < quota,
                    data: status));
            });

        var combined = new AndRule("rate_limits", windows.Select(w => RuleFor(w.Key, w.Value)).ToArray());
        var result = await combined.EvaluateAsync(context);
        var subResults = (IReadOnlyList<RuleResult>)result.Data!;
        return subResults.Select(r => (RateLimitStatus)r.Data!).ToArray();
    }
}

/// <summary>A hand-rolled replacement for VerdictRateLimiter -- same contract,
/// zero VerdictRules. Adding this is a new class; nothing above changed to
/// make room for it.</summary>
sealed class SimpleRateLimiter : RateLimiter
{
    public Task<IReadOnlyList<RateLimitStatus>> Check(IReadOnlyDictionary<string, object?> context, IReadOnlyDictionary<string, int> windows)
    {
        IReadOnlyList<RateLimitStatus> result = windows
            .Select(w => new RateLimitStatus(w.Key, (int)context[$"{w.Key}_used"]!, w.Value))
            .ToArray();
        return Task.FromResult(result);
    }
}
```

The composition root — the one place that decides which implementation
is actually running — is a single line:

```csharp
// Before: wired to the VerdictRules-backed implementation.
RateLimiter rateLimiter = new VerdictRateLimiter();

// After: swapped for the hand-rolled one. One line, here, changes.
rateLimiter = new SimpleRateLimiter();

// Every call site in the codebase, unaffected either way:
var statuses = await rateLimiter.Check(
    new Dictionary<string, object?> { ["minute_used"] = 3, ["hour_used"] = 40 },
    new Dictionary<string, int> { ["minute"] = 5, ["hour"] = 100 });
foreach (var s in statuses) Console.WriteLine(s);
// RateLimitStatus { Window = minute, Used = 3, Quota = 5 }
// RateLimitStatus { Window = hour, Used = 40, Quota = 100 }
```

That last line is the actual proof: it's identical before and after the
swap. Nothing that calls `rateLimiter.Check(...)` knows or cares which
class it's holding. `RateLimiter` here is a nominal contract — both
classes say `: RateLimiter` explicitly, since C# has no free structural
typing for a multi-member interface — but the boundary still holds
exactly the same way: nothing about `SimpleRateLimiter` names or depends
on `VerdictRateLimiter`, so the swap costs one assignment, not a
rewrite.

## Related

- [`README.md`](README.md) — the language-agnostic scenario this page
  implements.
