<!-- Title: Sample — Admin Eligibility Lookup (C#) -->
# Sample: Admin Eligibility Lookup

> The problem, the design, and what a solution must demonstrate are
> language-agnostic and live in [`README.md`](README.md) — read that
> first. This page is the C# implementation of it.

## The naive way (and why it breaks down)

Before reaching for a rule engine at all, the obvious first
implementation is a plain dictionary of configured checks and a lookup
method — no `verdict` in sight yet:

```csharp
static readonly Dictionary<string, List<(string Field, object? Expected)>> EligibilityChecks = new()
{
    ["gold_tier"] = new() { ("spend", 1000.0) },
    ["beta_feature"] = new(), // not filled in yet
};

static bool CheckEligibility(string checkName, IReadOnlyDictionary<string, object?> customer)
{
    if (!EligibilityChecks.TryGetValue(checkName, out var conditions))
    {
        return false; // "not eligible" either way
    }

    return conditions.All(c => customer.TryGetValue(c.Field, out var actual) && Equals(actual, c.Expected));
}
```

A typo and a genuine rejection render identically here, and `All()`
over an empty sequence is `true` — C#'s own vacuous truth, same as
`Enumerable.All` in every LINQ query — so a check the team hasn't
finished configuring yet silently passes. See the spec for the rest of
what this shape gets wrong.

## The `verdict` way

```csharp
using VerdictRules;

// "Eligible"/"NotEligible" come from a check that exists and actually ran.
// "UnknownCheck" and "NotConfigured" are both absence-shaped, and kept
// distinct from each other and from a genuine verdict so a support agent
// never mistakes "we don't know" for "we checked and the answer is no".
enum CheckStatus { Eligible, NotEligible, UnknownCheck, NotConfigured }

sealed record Condition(string Field, object? Expected);

sealed record LookupResult(CheckStatus Status, string Detail);

static class EligibilityChecks
{
    internal static Task<RuleResult> ConditionPredicate(string field, object? expected, IReadOnlyDictionary<string, object?> context)
    {
        context.TryGetValue(field, out var actual);
        return Task.FromResult(new RuleResult(field, Equals(actual, expected), $"{field}={actual}, needs {expected}"));
    }

    /// <summary>One configured check, plus whether it currently has zero
    /// conditions.</summary>
    /// <remarks>
    /// An empty <c>conditions</c> list still produces a valid <see
    /// cref="AndRule"/> -- it just vacuously passes if ever evaluated
    /// directly. <see cref="EligibilityLookup"/> intercepts that case
    /// before evaluation, so the vacuous pass never reaches the caller as
    /// a real "eligible".
    /// </remarks>
    internal static (IRule Rule, bool IsEmpty) BuildCheck(string name, IReadOnlyList<Condition> conditions)
    {
        var conditionRules = conditions
            .Select((c, i) => (IRule)new FunctionRule($"{name}[{i}]", (ctx, cancellationToken) => ConditionPredicate(c.Field, c.Expected, ctx)))
            .ToArray();
        return (new AndRule(name, conditionRules), conditions.Count == 0);
    }
}

/// <summary>Looks up one named eligibility check by name, typed fresh each
/// time.</summary>
/// <remarks>
/// <c>configuredChecks</c> mirrors whatever an admin settings screen
/// already holds: one entry per check, each a list of conditions. A
/// check with an empty list is a real, valid state a row can be in while
/// a team is still filling it in -- not an error.
/// </remarks>
sealed class EligibilityLookup
{
    private readonly RulesEngine _engine;
    private readonly HashSet<string> _unconfigured = new();

    public EligibilityLookup(IReadOnlyDictionary<string, IReadOnlyList<Condition>> configuredChecks)
    {
        var rules = new List<IRule>();
        foreach (var (name, conditions) in configuredChecks)
        {
            var (rule, isEmpty) = EligibilityChecks.BuildCheck(name, conditions);
            rules.Add(rule);
            if (isEmpty)
            {
                _unconfigured.Add(name);
            }
        }
        _engine = new RulesEngine(rules);
    }

    /// <summary>Look up and run one named check. Never throws on a bad
    /// <c>name</c> -- that is the entire point of this class existing
    /// between the raw engine and the screen that renders its
    /// result.</summary>
    public async Task<LookupResult> Check(string name, IReadOnlyDictionary<string, object?> customer)
    {
        if (_unconfigured.Contains(name))
        {
            return new LookupResult(CheckStatus.NotConfigured, $"'{name}' has no conditions configured yet");
        }

        var result = await _engine.TryRunNamedAsync(name, customer);
        if (result is null)
        {
            return new LookupResult(CheckStatus.UnknownCheck, $"no eligibility check named '{name}' exists");
        }

        return new LookupResult(result.Passed ? CheckStatus.Eligible : CheckStatus.NotEligible, result.Detail);
    }
}
```

Run against a small configuration — one real check, one the team hasn't
finished, and one lookup with a typo:

```csharp
var lookup = new EligibilityLookup(new Dictionary<string, IReadOnlyList<Condition>>
{
    ["gold_tier"] = new[] { new Condition("spend", 1000.0) },
    ["beta_feature"] = Array.Empty<Condition>(), // not filled in yet
});

await lookup.Check("gold_tier", new Dictionary<string, object?> { ["spend"] = 1000.0 });
// LookupResult(Status: Eligible, Detail: "")

await lookup.Check("gold_tier", new Dictionary<string, object?> { ["spend"] = 5.0 });
// LookupResult(Status: NotEligible, Detail: "'gold_tier[0]' failed: spend=5, needs 1000")

await lookup.Check("beta_feature", new Dictionary<string, object?> { ["spend"] = 1000.0 });
// LookupResult(Status: NotConfigured, Detail: "'beta_feature' has no conditions configured yet")

await lookup.Check("gold_teir", new Dictionary<string, object?> { ["spend"] = 1000.0 }); // typo
// LookupResult(Status: UnknownCheck, Detail: "no eligibility check named 'gold_teir' exists")
```

The screen can still show "not eligible" for the last two — the page
keeps working, exactly as the naive version intended — but `Status` is
what tells a support agent *which* of the four things actually
happened, rather than one boolean standing in for all of them.

### Why not just wrap `RunNamedAsync` in a `try`/`catch`?

`TryRunNamedAsync` and `RunNamedAsync` share one lookup path — the
strict form is a two-line assertion on top of the lenient one, not a
second implementation. Reaching for `TryRunNamedAsync` directly says
"absence is expected here and I have an answer for it," which is true
on this screen; wrapping the strict form in `try`/`catch` says the
same thing by accident, and reads as "I expect this to throw and I'm
suppressing it" to the next person editing this file. The behavior is
identical either way — the difference is only which one tells the
truth about why.

## Related

- [`README.md`](README.md) —
  the language-agnostic spec this page implements.
- [`../../extending/absence-vs-failure/`](../../extending/absence-vs-failure/README.md) —
  the general absence-vs-emptiness guidance this sample is one concrete
  instance of.
- [`data-driven-rule-sets/csharp.md`](../data-driven-rule-sets/csharp.md) — building the
  `IRule` objects themselves from stored configuration, the same pattern
  `EligibilityLookup` uses to turn each check's conditions into an
  `AndRule`.
