<!-- Title: Extending — Walking a Rule Tree (C#) -->
# Walking a rule tree: the C# SDK

> The concept, the diagram, and the trap are in
> [`README.md`](README.md) — read that first. This page is the concrete
> C# code.

Pattern-match `ICompositeRule<TContext>` and the declaration pattern gives
you a typed `composite` in the same expression. `SubRules` is a property, not
a method, unlike a result's own derived views — those are methods because a
get-only collection property gets serialized, and a rule is never serialized.

```csharp
using VerdictRules;

/// <summary>A consumer-defined composite that also carries its own threshold.</summary>
public sealed class AtLeastNRule<TContext>(
    string name,
    IReadOnlyList<IRule<TContext>> rules,
    int minimum) : ICompositeRule<TContext>
{
    private readonly int _minimum = minimum;

    public string Name { get; } = name;

    public string? Group => null;

    public IReadOnlyList<IRule<TContext>> SubRules { get; } = [.. rules];

    public async Task<RuleResult> EvaluateAsync(TContext context, CancellationToken cancellationToken = default)
    {
        var subResults = new List<RuleResult>();
        foreach (var rule in SubRules)
        {
            subResults.Add(await rule.EvaluateAsync(context, cancellationToken));
        }

        var passed = subResults.Count(r => r.Passed);
        return new RuleResult(
            Name,
            passed >= _minimum,
            subResults: subResults,
            decidedByIndices: [.. subResults.Index().Where(p => p.Item.Passed).Select(p => p.Index)]);
    }
}

public static class RuleTree
{
    /// <summary>Every rule in the tree, parents before children, depth-first.</summary>
    public static IEnumerable<IRule<TContext>> Walk<TContext>(IRule<TContext> rule)
    {
        yield return rule;
        if (rule is ICompositeRule<TContext> composite)
        {
            foreach (var part in composite.SubRules)
            {
                foreach (var found in Walk(part))
                {
                    yield return found;
                }
            }
        }
    }

    public static List<string> LeafNames<TContext>(IRule<TContext> rule) =>
        [.. Walk(rule).Where(r => r is not ICompositeRule<TContext>).Select(r => r.Name)];

    /// <summary>Names used by two *different* rule objects -- a build problem.</summary>
    public static HashSet<string> DuplicateNames<TContext>(IRule<TContext> rule)
    {
        var seen = new Dictionary<string, IRule<TContext>>();
        var duplicates = new HashSet<string>();
        foreach (var found in Walk(rule))
        {
            if (seen.TryGetValue(found.Name, out var first))
            {
                if (!ReferenceEquals(first, found)) duplicates.Add(found.Name);
            }
            else
            {
                seen[found.Name] = found;
            }
        }

        return duplicates;
    }
}
```

Using it. Note the generic arity throughout — see the caution below:

```csharp
using VerdictRules;

using Dict = System.Collections.Generic.IReadOnlyDictionary<string, object?>;

static IRule<Dict> Leaf(string name, bool passed = true) =>
    new FunctionRule<Dict>(name, (_, _) => Task.FromResult(new PredicateOutcome(passed)));

var tree = new AndRule<Dict>("eligible",
[
    Leaf("verified"),
    new OrRule<Dict>("either_path", [Leaf("auto", passed: false), Leaf("manual")]),
    new NotRule<Dict>("not_blocked", Leaf("blocked", passed: false)),
    new AtLeastNRule<Dict>("two_of_three", [Leaf("a"), Leaf("b"), Leaf("c", passed: false)], 2),
]);

Console.WriteLine(string.Join(", ", RuleTree.LeafNames(tree)));
// verified, auto, manual, blocked, a, b, c

Console.WriteLine(RuleTree.DuplicateNames(tree).Count);
// 0

// The custom composite's own parts are reached, so its three leaves are
// listed. A switch over AndRule/OrRule/NotRule would have stopped at
// `two_of_three` and reported four leaves instead of seven.
var verdict = await tree.EvaluateAsync(new Dictionary<string, object?>());
Console.WriteLine($"{verdict.Passed} {verdict.SubResults.Count}");
// True 4
```

> **Caution, and the reason the sample above uses `AndRule<Dict>` rather than
> the non-generic `AndRule`.** `IRule` *derives from*
> `IRule<IReadOnlyDictionary<string, object?>>`, so it is the **more derived**
> type. A custom composite declared `ICompositeRule<Dict>` therefore satisfies
> `IRule<Dict>` but **not** `IRule`, and will not compile into the non-generic
> `AndRule`, whose constructor takes `IReadOnlyList<IRule>`. Two ways out:
> build the tree with the generic arity, as above, or declare your composite
> `: IRule, ICompositeRule<Dict>` so it satisfies both. The generic arity is
> the simpler default once any custom composite is involved.

The non-generic `AndRule` works with a walk written against
`ICompositeRule<TContext>` because `IRule` derives from the closed generic —
so one walk covers both arities, with `TContext` as
`IReadOnlyDictionary<string, object?>`.

## Related

- [`README.md`](README.md) — the concept and the trap this page implements.
- [`../new-rule-shape/csharp.md`](../new-rule-shape/csharp.md) — where
  `AtLeastNRule` comes from, written out in full.
