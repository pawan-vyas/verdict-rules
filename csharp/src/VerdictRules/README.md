# Verdict — C# SDK

> The C# implementation of Verdict — a small, zero-dependency,
> async-native rule-evaluation engine.

## Install

```sh
dotnet add package VerdictRules
```

```csharp
using VerdictRules;
```

## A first rule

```csharp
using VerdictRules;

static FunctionRule AtLeast(string name, string field, double floor) =>
    new(name, (ctx, cancellationToken) =>
    {
        var value = Convert.ToDouble(ctx[field]);
        return Task.FromResult(
            new RuleResult(name, value >= floor, $"{value} vs {floor}"));
    });

var eligible = new AndRule("eligible", new IRule[]
{
    AtLeast("age_ok", "age", 18),
    AtLeast("score_ok", "score", 60),
});

var engine = new RulesEngine(new IRule[] { eligible });
var verdict = await engine.RunNamedAsync("eligible", new Dictionary<string, object?>
{
    ["age"] = 21,
    ["score"] = 55,
});

Console.WriteLine(verdict.Passed); // False
Console.WriteLine(verdict.Detail); // 'score_ok' failed: 55 vs 60
```

## Shape-based rules, within what C# allows

C# *does* have structural typing — for delegates. Any method or lambda matching
the predicate signature is a rule via `FunctionRule`, with nothing declared and
no type to name. A method group works directly:

```csharp
static Task<RuleResult> HasQuorum(
  IReadOnlyDictionary<string, object?> ctx,
  CancellationToken cancellationToken = default)
    => Task.FromResult(new RuleResult("quorum", ctx.Count >= 3));

var rule = new FunctionRule("quorum", HasQuorum);
```

`FunctionRule`'s predicate parameter is `RulePredicate`, a named delegate for
`Func<IReadOnlyDictionary<string, object?>, CancellationToken, Task<RuleResult>>`
— spelled out once so a field, a stored variable, or a helper that wraps a
predicate never has to repeat that signature. A lambda or method group
converts to it exactly as shown above, as long as it carries both parameters
(the trailing `CancellationToken` needs to be there even though the delegate
gives it a default — matching arity is required for the conversion itself,
unlike calling an existing delegate value, which can omit a trailing optional
parameter as normal). The one case worth knowing: an
already-`Func<...>`-typed value does not implicitly convert to `RulePredicate`
even with an identical signature, because C# delegate types are nominal once a
value has one — wrap it explicitly (`new RulePredicate(existing)`) if that
case comes up.

What C# lacks is structural typing for a *multi-member* interface. An object
that happens to carry `Name`, `Group` and `EvaluateAsync` is not thereby an
`IRule` — a rule shape owning its own name and group must declare `: IRule`
explicitly.

That's why `FunctionRule` carries more weight in this SDK: it is the escape
hatch back to shape-based rules, and most rules should use it rather than
declaring a type.

## Debugging

`RuleResult` and `RunResult` carry `[DebuggerDisplay]`, and `RunResult` a
debugger type proxy that expands straight to the per-rule results. A failing
composite's `Data` is a nested list of sub-results, and stepping through one is
the normal way anyone diagnoses it — so the shape is legible in a watch window
without expanding every level by hand.

SourceLink is enabled and symbols ship as a `.snupkg`, so stepping into the
package lands on real source rather than a decompiler.

## Absence returns null, not a thrown error

`RunNamedAsync`/`RunGroupAsync` throw `KeyNotFoundException` on an unknown
name or group. When absence *is* expected, `TryRunNamedAsync`/`TryRunGroupAsync`
return `null` instead:

```csharp
var result = await engine.TryRunGroupAsync("beta_checks", ctx);
var allowed = result?.Passed ?? true;   // absent means "no constraint here"
```

`null` means **absent, never failed** — a rule that exists and fails still
returns a `RuleResult` with `Passed` false. These are the primitives; the
throwing forms are assertions on top of them.

**The fallback only applies to absence.** A group that exists always reports
its real verdict, so `?? true` does not mean "sometimes true" — a failing group
is still a failure whatever default you choose. If you test code using this,
the case worth covering is a *present, failing* group rather than the absent
one everybody thinks of first.

## What it guarantees

- **Sequential evaluation, never concurrent.** Composites use a plain
  `foreach` with `await`, never `Task.WhenAll`. Short-circuiting only means
  something if later work never *starts* — and because the returned boolean is
  identical either way, getting this wrong is silent.
- **Vacuous truth has a polarity.** `AndRule([])` passes, `OrRule([])` fails.
  Deliberately asymmetric.
- **Emptiness is not absence.** An empty composite folds to its identity; an
  unknown rule name or group throws `KeyNotFoundException`. A group exists only
  because some rule declared it, so a lookup matching nothing can only be a
  mistake — and a misspelled group silently approving is the worst failure an
  eligibility check can have. Use `RuleNames` / `GroupNames` to check
  membership, or `TryRunNamedAsync` / `TryRunGroupAsync` where your own
  domain has an answer for absence — both return `null` instead of throwing.
- **`RuleResult.Data` is opaque** — only what actually ran, never padded, never
  flattened.
- **Zero runtime dependencies.**

## Where to go next

| Doc | For |
| --- | --- |
| [`docs/quickstart.md`](https://github.com/pawan-vyas/verdict-rules/blob/csharp-v0.0.2/csharp/src/VerdictRules/docs/quickstart.md) | The quickstart — core concepts and a full worked example |
| [`docs/architecture/`](https://github.com/pawan-vyas/verdict-rules/blob/csharp-v0.0.2/docs/architecture/README.md) | Why it's shaped this way, in depth — type structure, the execution model |
| [`docs/extending/`](https://github.com/pawan-vyas/verdict-rules/blob/csharp-v0.0.2/docs/extending/README.md) | Building on top of it from your own code, with no changes here |
| [`docs/maintenance/`](https://github.com/pawan-vyas/verdict-rules/blob/csharp-v0.0.2/docs/maintenance/README.md) | Changing this package itself |
| [`docs/testing/`](https://github.com/pawan-vyas/verdict-rules/blob/csharp-v0.0.2/docs/testing/README.md) | How the test suite is organized, and what a change needs to prove |
| [`docs/samples/`](https://github.com/pawan-vyas/verdict-rules/blob/csharp-v0.0.2/docs/samples/README.md) | Worked examples — dynamic discounts, fee waivers, tier promotions, moderation routing, data-driven rule sets |
