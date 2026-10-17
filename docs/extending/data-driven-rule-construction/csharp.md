<!-- Title: Extending — Building Rule Sets From Stored Configuration (C#) -->
# Building rule sets from stored configuration: the C# SDK

> The concept and the testing implication are in
> [`README.md`](README.md) — read that first. This page is the concrete
> C# code.

```csharp
using VerdictRules;

static FunctionRule MakeRule(RuleConfig config)
{
    // The predicate never restates config.Name -- FunctionRule below is the
    // one place that owns it, so the two can never drift apart.
    Task<PredicateOutcome> Predicate(IReadOnlyDictionary<string, object?> context, CancellationToken cancellationToken = default)
    {
        context.TryGetValue(config.Field, out var actual);
        var passed = Equals(actual, config.Expected);
        return Task.FromResult(new PredicateOutcome(passed));
    }
    return new FunctionRule(config.Name, Predicate);
}

// Stands in for a real config source for this example.
static IReadOnlyList<RuleConfig> LoadRuleConfigs() =>
[
    new RuleConfig("is_manager", "role", "manager"),
    new RuleConfig("in_headquarters", "office", "HQ"),
];

var configuredRules = LoadRuleConfigs().Select(MakeRule).ToArray();
var combinedRule = new AndRule("combined", configuredRules);

// RuleConfig would normally live in its own file; kept here, after the
// top-level statements above, only because C# requires type declarations
// to follow them in a single top-level-statements Program.cs.
sealed record RuleConfig(string Name, string Field, object? Expected);
```

```csharp
await combinedRule.EvaluateAsync(new Dictionary<string, object?> { ["role"] = "manager", ["office"] = "HQ" });
// Passed: true

var refused = await combinedRule.EvaluateAsync(
    new Dictionary<string, object?> { ["role"] = "manager", ["office"] = "Remote" });
refused.GetFailingLeaves()[0].RuleName;
// in_headquarters -- the config-driven name, carried through
```

An empty `LoadRuleConfigs()` produces an empty `AndRule`, which
vacuously passes.

## Related

- [`README.md`](README.md) — the language-agnostic scenario this page
  implements.
