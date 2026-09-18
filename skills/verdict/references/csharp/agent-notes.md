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

The NuGet package ID is **`VerdictRules`** (PascalCase, no hyphen) —
NuGet package IDs commonly follow .NET's own PascalCase convention,
unlike npm's `verdict-rules`. There is no naming split the way Python
has (`pip install verdict-rules` → `import verdict`): the NuGet ID and
the namespace are the same word.

`net10.0` and `netstandard2.1`, trimmable and AOT-compatible on `net10.0`.

## The API, in one screen

```csharp
public interface IRule<TContext> {                   // nominal -- must `: IRule<TContext>` explicitly
    string Name { get; }
    string? Group { get; }
    Task<RuleResult> EvaluateAsync(TContext context, CancellationToken cancellationToken = default);
}
public interface IRule : IRule<IReadOnlyDictionary<string, object?>> { }  // the dict-context closure
public delegate Task<RuleResult> RulePredicate<TContext>(TContext context, CancellationToken cancellationToken = default);

// Dict-context (IRule's own closure) -- non-generic, independent classes:
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

// Typed context (e.g. OrderContext) -- the generic siblings, an independent
// implementation from the dict-context forms above, not a wrapper:
new FunctionRule<OrderContext>(name, predicate, group: null)
new AndRule<OrderContext>(name, rules, group: null)
new OrRule<OrderContext>(name, rules, group: null)
var typedEngine = new RulesEngine<OrderContext>(rules);
await typedEngine.RunAllAsync(context, cancellationToken);  // same run methods as above, TContext in place of the dictionary
```

`cancellationToken` defaults to `default` everywhere above and is checked
between rules by every composite and by `RulesEngine`'s own run methods —
pass a real token when the caller has one (a request-scoped
`HttpContext.RequestAborted`, for instance); omit it entirely otherwise.

`RuleResult` and `RunResult` are plain immutable classes, not interfaces
to satisfy — construct them directly:
`new RuleResult(ruleName, passed, detail: "", data: null)` and
`new RunResult(passed, results)`.

**A typed, non-dict context** reaches for the generic siblings shown
above — a different generic arity, an independent implementation from
the dict-context forms, not a wrapper. `TContext` is usually inferred
from the predicate's own parameter type, so the type argument is rarely
spelled out at the constructor call site. Every sub-rule inside one
`AndRule<TContext>`/`OrRule<TContext>` must implement `IRule<TContext>`
for the exact same `TContext` — the compiler rejects mixing contexts.

## Which run mode

| Need | Reach for |
| --- | --- |
| One fast pass/fail verdict | A composite's own `EvaluateAsync()` |
| Every rule's own outcome (a status page, an audit trail) | `engine.RunAllAsync()` |
| One named rule/group; absence would be a bug | The strict lookup — throws |
| One named rule/group; absence is expected, your domain decides what it means | The non-raising `Try*` lookup |

## Testing what matters

`docs/testing/` in the source repository is the full checklist (see
`references/REPOSITORY-MAP.md`). The parts that are easy to skip:

- Prove short-circuiting with a **call log**, not the final boolean. A
  composite that evaluates everything still returns the right answer.
- For a rule set **built from stored/config data at runtime** rather
  than hand-written, hand-picked fixtures stop scaling as the
  configuration space grows — reach for property-based testing (e.g.
  FsCheck) or an oracle/differential approach (an independent,
  deliberately simpler reference implementation checked against many
  random configurations) instead of adding fixtures one at a time as
  bugs are found.
