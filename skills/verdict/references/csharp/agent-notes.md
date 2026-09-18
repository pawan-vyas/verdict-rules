# C# — agent notes

Short by design. Everything about *what verdict is* lives in
`references/docs/`, which is the repository's own documentation rather
than a summary that could drift from it. This file carries only what is
specific to the C# SDK, and to writing C# that uses it.

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
```

`cancellationToken` defaults to `default` everywhere above and is checked
between rules by every composite and by `RulesEngine`'s own run methods —
pass a real token when the caller has one (a request-scoped
`HttpContext.RequestAborted`, for instance); omit it entirely otherwise.

`RuleResult` and `RunResult` are plain immutable classes, not interfaces
to satisfy — construct them directly:
`new RuleResult(ruleName, passed, detail: "", data: null)` and
`new RunResult(passed, results)`.

**A typed, non-dict context** reaches for the generic siblings instead:
`FunctionRule<OrderContext>`, `AndRule<OrderContext>`,
`OrRule<OrderContext>`, `RulesEngine<OrderContext>` — same names,
different generic arity, an independent implementation from the
dict-context forms rather than a wrapper. `TContext` is usually inferred
from the predicate's own parameter type, so the type argument is rarely
spelled out at the constructor call site. Every sub-rule inside one
`AndRule<TContext>`/`OrRule<TContext>` must implement `IRule<TContext>`
for the exact same `TContext` — the compiler rejects mixing contexts.

## Mistakes that show up in generated C# specifically

- **`Task.WhenAll` in a composite.** It starts every sub-rule's task
  before the first result returns and destroys the short-circuit
  guarantee. `AndRule`/`OrRule` evaluate sub-rules in a plain `foreach`
  loop with `await`, one at a time — the returned boolean is identical
  either way, so this is the one mistake here that passes its own tests.
- **Forgetting `: IRule`.** C# *does* have structural typing — for
  delegates. Any method or lambda matching `RulePredicate` is a rule
  through `FunctionRule`, with nothing declared and no type to name; a
  method group works directly, as `new FunctionRule("quorum", HasQuorum)`
  — as long as `HasQuorum` itself carries both parameters (a defaulted
  `CancellationToken cancellationToken = default` is enough; method-group
  and lambda conversion to a delegate type require matching arity
  exactly, unlike calling an existing delegate value). What C# lacks is
  structural typing for a *multi-member* interface: an
  object carrying `Name`, `Group` and `EvaluateAsync` is not thereby an
  `IRule`, where Python's `Protocol` and TypeScript's structural
  `interface` would accept it as-is. A rule shape owning its own name
  and group must declare `: IRule` explicitly. That's why `FunctionRule`
  carries more weight in this SDK than in Python/JS — it's the escape
  hatch back to shape-based rules, and most rules should use it rather
  than declaring a type.
- **A predicate returning a bare `bool`.** `FunctionRule`'s predicate
  must return `Task<RuleResult>`, not `true`/`false`.
- **An already-`Func<...>`-typed value not converting to
  `RulePredicate`.** C# delegate types are nominal once a value has one
  — a raw lambda or method group converts to `RulePredicate` exactly as
  if it were a bare `Func<...>`, but a variable already typed as
  `Func<IReadOnlyDictionary<string, object?>, CancellationToken, Task<RuleResult>>`
  needs an explicit `new RulePredicate(existingFunc)`.
- **Catching `KeyNotFoundException` where a `Try*` method should be
  reached for instead.** Unlike JS's dedicated `UnknownLookupError` or
  Python's `KeyError`, C#'s unknown-lookup failure is the same generic
  exception the BCL throws for countless unrelated dictionary-lookup
  failures elsewhere, so catching it by type here is far less precise
  than in JS or Python. Checking `engine.RuleNames.Contains(name)` (or
  `GroupNames`) before calling is clearer, and is exactly what those two
  properties exist for.
- **`object`/`dynamic` instead of `object?`** for a context value's
  declared type. `dynamic` disables compile-time type checking
  entirely; `object?` still accepts a value straight out of
  `System.Text.Json` deserialization with an explicit cast, while
  keeping real type checking everywhere else the value is used.
- **`RuleName` set to something other than the rule's own `Name`.** A
  caller walking a `RunResult` attributes outcomes by that field.
- **Missing `ConfigureAwait(false)`** inside a custom `IRule`
  implementation meant to be reusable library code, not application
  code — this package's own source does it on every internal `await`.
- **Assuming `IRule<TContext> : IRule`.** The inheritance runs the other
  way: `IRule : IRule<IReadOnlyDictionary<string, object?>>`. A generic
  `IRule<TContext>` value cannot be passed anywhere a bare `IRule` is
  expected unless `TContext` is that same dictionary type.
- **Mixing sub-rules of different `TContext`s inside one
  `AndRule<TContext>`/`OrRule<TContext>`.** The compiler rejects this —
  reuse a rule across two shapes via an explicit projecting adapter
  (`docs/extending/reusing-a-rule-across-contexts/csharp.md`) instead of
  trying to loosen the composite's own type parameter.

## Testing what matters

[`references/docs/testing/`](../../../../docs/testing/README.md) (fetch it) is the full checklist,
with [`references/docs/testing/csharp.md`](../../../../docs/testing/csharp.md)
naming which test proves which contract. The parts that are easy to
skip:

- Prove short-circuiting with a **call log**, not the final boolean. A
  composite that evaluates everything still returns the right answer.
- Give each **vacuous-truth polarity** its own test. They are asymmetric.
- Test both halves of a lookup: the strict form throwing **and** the
  `Try` form returning `null`.
- If code uses a fallback, test the **present-but-failing** case — not
  just the absent one. That is the direction where a bug is silent.
- For a rule set **built from stored/config data at runtime** rather
  than hand-written, hand-picked fixtures stop scaling as the
  configuration space grows — reach for property-based testing (e.g.
  FsCheck) or an oracle/differential approach (an independent,
  deliberately simpler reference implementation checked against many
  random configurations) instead of adding fixtures one at a time as
  bugs are found.

## Fetching the deeper documents

Determine the actual installed version first — not a range from the
manifest — using this ecosystem's own tooling, then:

```bash
scripts/fetch-docs.sh csharp=<version>
```

If the tag doesn't exist, re-check the version before assuming the release
is missing. See [`commands/verdict-fetch-docs.md`](../../commands/verdict-fetch-docs.md).
