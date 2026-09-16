<!-- Title: Extending — Deciding What A Missing Rule Set Means (C#) -->
# Deciding what a missing rule set means: the C# SDK

> The concept, the table, and the diagram are in
> [`README.md`](README.md) — read that first. This page is the concrete
> C# code.

```csharp
using VerdictRules;

static Task<RuleResult> IsBetaTester(IReadOnlyDictionary<string, object?> context) =>
    Task.FromResult(new RuleResult("is_beta_tester", context.TryGetValue("beta_tester", out var v) && v is true));

var engine = new RulesEngine([new FunctionRule("is_beta_tester", IsBetaTester, "beta_checks")]);

var result = await engine.TryRunGroupAsync("beta_checks", new Dictionary<string, object?> { ["beta_tester"] = true });
// RunResult(Passed: true, Results: [RuleResult(RuleName: "is_beta_tester", Passed: true, ...)])

await engine.TryRunGroupAsync("no_such_group", new Dictionary<string, object?>());
// null -- the group was never registered
```

The four situations from the spec, as four different ways to consume
that same `null`:

```csharp
// 1. Absence means "no constraint applies"
var result = await engine.TryRunGroupAsync(group, context);
var allowed = result is not null ? result.Passed : true;

// 2. Absence means "the configuration is wrong"
allowed = result is not null ? result.Passed : false;

// 3. Absence means "skip it" -- contributes nothing either way
var checks = result is not null ? new[] { result } : Array.Empty<RunResult>();

// 4. Absence is genuinely unexpected -- say so immediately
result = await engine.RunGroupAsync(group, context); // throws
```

`TryRunGroupAsync` is the primitive `RunGroupAsync` is built on, not the
other way around:

```csharp
public async Task<RunResult> RunGroupAsync(string group, IReadOnlyDictionary<string, object?> context)
{
    var result = await TryRunGroupAsync(group, context);
    if (result is null)
    {
        throw new KeyNotFoundException($"No rules in group '{group}' in this engine");
    }
    return result;
}
```

There is one lookup path. The strict form is a two-line assertion on top
of the lenient one, rather than a second implementation that could drift
from it.

If you only need to enumerate what exists, `engine.RuleNames` and
`engine.GroupNames` report exactly the lookups that will not throw.

## Related

- [`README.md`](README.md) — the language-agnostic scenario this page
  implements.
