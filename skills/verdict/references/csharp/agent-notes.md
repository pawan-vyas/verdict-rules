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
public delegate Task<RuleResult> RulePredicate<TContext>(TContext context, CancellationToken cancellationToken = default);

// cancellationToken is checked before any rule runs and again between rules, on every
// composite and engine run method; an already-cancelled token evaluates nothing at all
// (0.3.2+). Pass a real token when the caller has one.

// Dict-context (IRule's own closure) -- non-generic specializations of the generic forms below:
new FunctionRule(name, predicate, group: null)       // wraps a plain async predicate (dict-context)
new AndRule(name, rules, group: null)                // passes only if every sub-rule passes
new OrRule(name, rules, group: null)                 // passes as soon as one does
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
var typedEngine = new RulesEngine<OrderContext>(rules);
await typedEngine.RunAllAsync(context, cancellationToken);  // same run methods as above, TContext in place of the dictionary

new RuleResult(ruleName, passed, detail: "", data: null)   // immutable classes, construct directly
new RunResult(passed, results)
```

## Which run mode

| Need | Reach for |
| --- | --- |
| One fast pass/fail verdict | A composite's own `EvaluateAsync()` |
| Every rule's own outcome (a status page, an audit trail) | `engine.RunAllAsync()` |
| One named rule/group; absence would be a bug | The strict lookup — throws |
| One named rule/group; absence is expected, your domain decides what it means | The non-raising `Try*` lookup |
