# C# — agent notes

What is specific to the C# SDK, and to writing C# that uses it. The
engine's own general guarantees are in `SKILL.md`, not repeated here.

## Install and import

```sh
dotnet add package VerdictRules
```

```csharp
using VerdictRules;
```

## The API, in one screen

```csharp
public interface IRule<TContext> {                   // nominal -- must `: IRule<TContext>` explicitly
    string Name { get; }
    string? Group { get; }
    Task<RuleResult> EvaluateAsync(TContext context, CancellationToken cancellationToken = default);
}
public interface IRule : IRule<IReadOnlyDictionary<string, object?>> { }  // the dict-context closure
public delegate Task<PredicateOutcome> RulePredicate<TContext>(TContext context, CancellationToken cancellationToken = default);

// cancellationToken is checked before any rule runs and again between rules, on every
// composite and engine run method; an already-cancelled token evaluates nothing at all.
// Pass a real token when the caller has one.

// Dict-context (IRule's own closure) -- non-generic specializations of the generic forms below:
new FunctionRule(name, predicate, group: null)       // wraps a plain async predicate (dict-context)
new AndRule(name, rules, group: null)                // passes only if every sub-rule passes
new OrRule(name, rules, group: null)                 // passes as soon as one does
new NotRule(name, rule, group: null)                 // passes exactly when the one wrapped rule fails
var engine = new RulesEngine(rules);
await engine.RunAllAsync(context, cancellationToken);          // every rule, never short-circuits
await engine.RunNamedAsync(name, context, cancellationToken);  // one rule; throws KeyNotFoundException if absent
await engine.RunGroupAsync(group, context, cancellationToken); // one group; throws KeyNotFoundException if absent
await engine.TryRunNamedAsync(name, context, cancellationToken);  // -> RuleResult?
await engine.TryRunGroupAsync(group, context, cancellationToken); // -> RunResult?
engine.RuleNames, engine.GroupNames                   // IReadOnlyCollection<string> of what exists

// Typed context (e.g. OrderContext) -- the generic forms, where the behaviour is
// implemented; the dict-context ones above forward to them. TContext is usually
// inferred from the predicate, so the type argument is rarely spelled out:
new FunctionRule<OrderContext>(name, predicate, group: null)
new AndRule<OrderContext>(name, rules, group: null)
new OrRule<OrderContext>(name, rules, group: null)
new NotRule<OrderContext>(name, rule, group: null)
var typedEngine = new RulesEngine<OrderContext>(rules);
await typedEngine.RunAllAsync(context, cancellationToken);  // same run methods as above, TContext in place of the dictionary

new PredicateOutcome(passed, detail: "", data: null)  // what a predicate returns; no RuleName on it
new RuleResult(ruleName, passed, detail: "", data: null, subResults: null, decidedBy: null)
new RunResult(passed, results)                       // all three are immutable
```

A predicate reports a `PredicateOutcome`; the `FunctionRule` wrapping it
owns the name and builds the `RuleResult`:

```csharp
static Task<PredicateOutcome> OverEighteen(
    IReadOnlyDictionary<string, object?> ctx, CancellationToken ct = default) =>
    Task.FromResult(new PredicateOutcome((int)ctx["age"]! >= 18, $"age {ctx["age"]}"));

var rule = new FunctionRule("over_18", OverEighteen);
```

## Reading a result

**`Leaves`/`FailingLeaves` are methods here, not properties** -- this SDK
is the only one that spells them that way. Each walks the subtree and
allocates, and a get-only collection property would be traversed by any
reflection-based serializer or structured logger:

```csharp
result.SubResults            // this result's own children, exactly what it evaluated
result.DecidedBy             // which of those explain this result's own verdict
result.GetLeaves()           // every leaf reachable from here, flattened
result.GetFailingLeaves()    // the leaves explaining a failure
```

`RunResult` exposes `GetLeaves()`/`GetFailingLeaves()` too, flattened
across every rule the run evaluated.

Key an audit trail on a leaf's own `RuleName`, never a composite's:

```csharp
var verdict = await graduates.EvaluateAsync(student, cancellationToken);
if (!verdict.Passed)
{
    logger.LogWarning("refused by {Rule}", verdict.GetFailingLeaves()[0].RuleName);
}
```

Composites short-circuit, so these hold only what was evaluated: a failed
`AndRule` has exactly one failing leaf, and a passing `OrRule` has none
even when an earlier branch failed on the way to that pass.

## Which run mode

| Need | Reach for |
| --- | --- |
| One fast pass/fail verdict | A composite's own `EvaluateAsync()` |
| Every rule's own outcome (a status page, an audit trail) | `engine.RunAllAsync()` |
| One named rule/group; absence would be a bug | The strict lookup — throws |
| One named rule/group; absence is expected, your domain decides what it means | The non-raising `Try*` lookup |
