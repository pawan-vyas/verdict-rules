<!-- Title: Sample — Data-Driven Rule Sets (C#) -->
# Sample: Data-Driven Rule Sets

> The problem, the design, and what a solution must demonstrate are
> language-agnostic and live in [`README.md`](README.md) — read that
> first. This page is the C# implementation of it.

## The naive way (and why it breaks down)

The obvious first implementation compiles every configured condition
straight into an `if`/`else if` ladder in application code:

```csharp
static bool MatchesConditionGrant(string? category, IReadOnlyList<string> groups)
{
    if (category == "Manager" && groups.Contains("Headquarters"))
    {
        return true;
    }
    if (category == "Analyst" && (groups.Contains("Support") || groups.Contains("Headquarters")))
    {
        return true;
    }
    return false;
}
```

This is, functionally, a two-row configuration table hand-transcribed
into source code — every new condition an admin wants is a developer
task, a PR, and a deploy. See the spec for the rest of what this shape
gets wrong.

## The `verdict` way

```csharp
using VerdictRules;

/// <summary>One stored row describing a single, independently-editable
/// rule.</summary>
sealed record ConfiguredRow(int Id, string Field, string Operator, object? Value);

static class Operators
{
    // Dispatch on the operator string via a table, not an if/else-if
    // ladder -- adding an operator is one more entry, never an edit to
    // the ones already there.
    internal static readonly IReadOnlyDictionary<string, Func<object?, object?, bool>> Table =
        new Dictionary<string, Func<object?, object?, bool>>
        {
            ["gte"] = (actual, value) => actual is IComparable c && value is not null && c.CompareTo(value) >= 0,
            ["eq"] = (actual, value) => Equals(actual, value),
            ["in"] = (actual, value) => value is IEnumerable<string> set && actual is string s && set.Contains(s),
        };
}

// Turn one stored row into an IRule -- the only place that knows how.
static IRule RuleFor(ConfiguredRow row)
{
    Task<RuleResult> Predicate(IReadOnlyDictionary<string, object?> context)
    {
        context.TryGetValue(row.Field, out var actual);
        var passed = Operators.Table[row.Operator](actual, row.Value);
        return Task.FromResult(new RuleResult($"row:{row.Id}", passed));
    }
    return new FunctionRule($"row:{row.Id}", Predicate);
}

// Stand-in for a real DB read -- the only I/O in this whole pattern.
// A real implementation queries storage instead of returning a literal.
static Task<IReadOnlyList<ConfiguredRow>> FetchCurrentRows() =>
    Task.FromResult<IReadOnlyList<ConfiguredRow>>(new[]
    {
        new ConfiguredRow(1, "category", "eq", "Manager"),
        new ConfiguredRow(2, "region", "in", new HashSet<string> { "US", "CA", "UK" }),
    });

/// <summary>Build the current rule set fresh and evaluate it -- nothing
/// retained between calls.</summary>
/// <param name="combine">"all" (AndRule -- every row must pass; empty
/// config passes) or "any" (OrRule -- at least one row must pass; empty
/// config fails). Which default is correct is a domain decision, made
/// explicitly here, not inferred -- see the spec's "reading the diagram"
/// note on this.</param>
static async Task<bool> EvaluateAgainstCurrentConfig(IReadOnlyDictionary<string, object?> context, string combine)
{
    var rows = await FetchCurrentRows();
    var rules = rows.Select(RuleFor).ToArray();
    IRule combined = combine == "all" ? new AndRule("combined", rules) : new OrRule("combined", rules);
    var result = await combined.EvaluateAsync(context);
    return result.Passed;
}
```

Against the two rows above — a category grant and a region grant, both
required:

```csharp
await EvaluateAgainstCurrentConfig(new Dictionary<string, object?> { ["category"] = "Manager", ["region"] = "US" }, "all");
// true -- matches both rows

await EvaluateAgainstCurrentConfig(new Dictionary<string, object?> { ["category"] = "Manager", ["region"] = "DE" }, "all");
// false -- row 2 fails; the DE region isn't in the configured set

await EvaluateAgainstCurrentConfig(new Dictionary<string, object?> { ["category"] = "Manager", ["region"] = "DE" }, "any");
// true -- combine="any" only needs one row to pass
```

Editing row 2's `Value` to add `"DE"` — a data change, in whatever
storage `FetchCurrentRows` reads from — changes the second call's
result with no edit to this method, `RuleFor`, or the combinator.

## Related

- [`README.md`](README.md) —
  the language-agnostic spec this page implements.
- [`../../extending/data-driven-rule-construction/`](../../extending/data-driven-rule-construction/README.md) —
  the general scenario this sample is a fuller version of, including how
  two unrelated domains share one engine without coupling to each
  other.
- [`dynamic-discounts/csharp.md`](../dynamic-discounts/csharp.md) — a smaller, single-`AndRule`
  instance of the same idea.
