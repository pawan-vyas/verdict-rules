# VerdictRules

> A small, zero-dependency, async-native rule-evaluation engine for .NET.
> Compose independently-changing conditions into one explainable pass/fail
> verdict.

The C# SDK of [verdict](https://github.com/pawan-vyas/verdict-rules), which
exists in more than one language with identical execution-model guarantees.

**This is `0.0.1` — correct, but minimal.** The type set and its guarantees are
complete and tested. The worked example, the shared cross-language fixture, and
the full documentation set arrive before `0.1.0`.

## Install

```sh
dotnet add package VerdictRules
```

`net8.0` and `netstandard2.1`. Trimmable and AOT-compatible.

## Use

```csharp
using VerdictRules;

static FunctionRule AtLeast(string name, string field, double floor) =>
    new(name, ctx =>
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

var verdict = await eligible.EvaluateAsync(new Dictionary<string, object?>
{
    ["age"] = 21,
    ["score"] = 55,
});

Console.WriteLine(verdict.Passed); // False
Console.WriteLine(verdict.Detail); // 'score_ok' failed: 55 vs 60
```

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
  eligibility check can have. Use `RuleNames` / `GroupNames` to check rather
  than catch.
- **`RuleResult.Data` is opaque** — only what actually ran, never padded, never
  flattened.
- **Zero runtime dependencies.**

## Shape-based rules, within what C# allows

C# *does* have structural typing — for delegates. Any method or lambda matching
the predicate signature is a rule via `FunctionRule`, with nothing declared and
no type to name. A method group works directly:

```csharp
static Task<RuleResult> HasQuorum(IReadOnlyDictionary<string, object?> ctx) =>
    Task.FromResult(new RuleResult("quorum", ctx.Count >= 3));

var rule = new FunctionRule("quorum", HasQuorum);
```

What C# lacks is structural typing for a *multi-member* interface. An object
that happens to carry `Name`, `Group` and `EvaluateAsync` is not thereby an
`IRule` — a rule shape owning its own name and group must declare `: IRule`.
Python's `Protocol` and TypeScript's structural interfaces accept such an
object as-is.

That narrow difference is why `FunctionRule` carries more weight in this SDK:
it is the escape hatch back to shape-based rules, and most rules should use it
rather than declaring a type.

## Debugging

`RuleResult` and `RunResult` carry `[DebuggerDisplay]`, and `RunResult` a
debugger type proxy that expands straight to the per-rule results. A failing
composite's `Data` is a nested list of sub-results, and stepping through one is
the normal way anyone diagnoses it — so the shape is legible in a watch window
without expanding every level by hand.

SourceLink is enabled and symbols ship as a `.snupkg`, so stepping into the
package lands on real source rather than a decompiler.

## Licence

MIT.
