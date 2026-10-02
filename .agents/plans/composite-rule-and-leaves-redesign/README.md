<!-- Title: SequentialEvaluator, RuleResult.SubResults, and Leaves/FailingLeaves -->
# `SequentialEvaluator`, `RuleResult.SubResults`, and `Leaves`/`FailingLeaves`

> Issue #100's original one-paragraph pitch ("add two read-only
> properties") needed a real architecture decision once an adopter's
> review surfaced the cases it didn't account for. This doc is the
> settled spec — what ships, and why — not a transcript of how the
> design got here. Supersedes #100's own original design; #100 stays
> open as the tracking issue, closed by whichever PR implements this.

## 0 · Status

**Implemented in all four languages** (C#, Python, JS/TS, Dart — each
green on its own full test suite), committed as four separate commits
on this PR branch and pushed. Remaining, in this order: (1) the queued
doc-verbosity audit, sweeping `docs/extending/*/`, `docs/samples/*/`,
and `docs/architecture/` for every language consistently (currently
uneven — C#'s implementation pass already swept its own
`docs/*/csharp.md` tree; Python and Dart deferred the whole tree;
JS/TS fixed only what would otherwise throw); (2) the cross-language
API concept map (`docs/maintenance/api-concepts.yaml`), deliberately
done last, after the docs are actually settled, since redoing it a
second time if the docs pass changes any public-surface shape would
be wasted work.

## 1 · Origin

Issue #100 proposed `RuleResult.Leaves`/`FailingLeaves`: flatten a
composite's nested sub-results, in evaluation order, leaves only. The
original design computed this by checking `Data is List<RuleResult>` —
duck-typing a field documented as opaque.

An adopter (`eps_bloc_maker`, posted to #100 as a GitHub comment,
2026-10-01) reviewed that design against two real stopgap
implementations they'd already hand-written and found it unsound in
five concrete cases: a non-composite rule that sets its own `Data` to
something list-shaped for unrelated reasons; negation, where flattening
can produce an empty `FailingLeaves` on a *failed* result; no way to
tell a composite's own child list apart from a rule's unrelated
`List<RuleResult>` payload; a naive "last sub-result" shortcut getting
nested `AndRule`s wrong; and `OrRule`'s all-fail case needing a
documented convention for which of several failing leaves a caller
picks.

## 2 · `RuleResult.SubResults`

`RuleResult.Data` is documented, in every language, as opaque — never
read by verdict itself. `AndRule`/`OrRule` populating it with their own
sub-results was always a *convention*, not a contract the type system
enforced, and `Leaves` computed by duck-typing `Data` is exactly what
the five cases in §1 break.

`RuleResult` gains `SubResults: IReadOnlyList<RuleResult>` (empty
default — absence *is* the leaf signal, the same idiom `Group: string?`
already uses in this type). `Data` stops carrying composite sub-results
at all and becomes *actually* opaque, structurally enforced rather than
only documented. `Leaves` is simple recursion over `SubResults`;
`FailingLeaves` is its own independent recursion, not a filter over
`Leaves` — validated against real downstream requirements (a consuming
project's blocked-request-reason use case) requiring exactly this
shape: a failed result with no failing children is itself the leaf
(handles negation without `NotRule` needing to misrepresent its own
structure, §3e); a passed result contributes no failing leaves at all,
even if an earlier short-circuited branch failed on the way to that
pass:

```csharp
public IReadOnlyList<RuleResult> Leaves =>
    SubResults.Count == 0 ? [this] : SubResults.SelectMany(s => s.Leaves).ToList();

public IReadOnlyList<RuleResult> FailingLeaves
{
    get
    {
        if (Passed) return [];
        var childFailures = SubResults.SelectMany(s => s.FailingLeaves).ToList();
        return childFailures.Count == 0 ? [this] : childFailures;
    }
}
```

This is additive to the type (new field, new derived properties) but
changes `AndRule`/`OrRule`'s own behavior (they stop writing sub-results
into `Data`) — a real breaking change, accepted deliberately: see §6.

## 2a · `FunctionRule`/`RulePredicate` — closing a name-divergence hole, folded into this PR

Found while working through introspection, confirmed in all four
languages' real source, not hypothetical: `RulePredicate<TContext>`
today is `Func<TContext, CancellationToken, Task<RuleResult>>` — the
predicate constructs the *whole* `RuleResult`, including its own
`RuleName`, independently of the `FunctionRule.Name` the predicate is
wrapped by. Nothing keeps them equal. This repo's own C# example,
`csharp/examples/GraduationVerdict/GraduationCheck.cs:102`, hardcodes
`new RuleResult("cgpa_met", ...)` as a literal inside `CgpaMet`,
completely decoupled from whatever name a `FunctionRule` wrapping it is
constructed with. The same shape is confirmed in the Python, JS/TS, and
Dart sources too — every language's `FunctionRule.evaluate` documents
itself as returning "whatever the predicate returns, unchanged." They
happen to agree everywhere in this repo today — nothing enforces they
must.

The name is already fixed, once, at `FunctionRule` construction — a
predicate re-stating it is never legitimate, not just inconvenient. Fix
is structural, not a runtime check: the predicate stops constructing
`RuleResult` at all.

```csharp
public sealed class PredicateOutcome(bool passed, string detail = "", object? data = null)
{
    public bool Passed { get; } = passed;
    public string Detail { get; } = detail;
    public object? Data { get; } = data;
}

public delegate Task<PredicateOutcome> RulePredicate<TContext>(TContext context, CancellationToken cancellationToken = default);

public sealed class FunctionRule<TContext>(string name, RulePredicate<TContext> predicate, string? group = null) : IRule<TContext>
{
    private readonly RulePredicate<TContext> _predicate = predicate;
    public string Name { get; } = name;
    public string? Group { get; } = group;

    public async Task<RuleResult> EvaluateAsync(TContext context, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var outcome = await _predicate(context, cancellationToken).ConfigureAwait(false);
        return new RuleResult(Name, outcome.Passed, outcome.Detail, outcome.Data);
    }

    public override string ToString() =>
        $"FunctionRule \"{Name}\"" + (string.IsNullOrEmpty(Group) ? "" : $" ({Group})");
}
```

`EvaluateAsync` is no longer a straight pass-through of the predicate's
`Task` (it was deliberately non-`async` before, forwarding the
predicate's own `Task<RuleResult>` unchanged) — it now has to `await`
and transform, since `RuleResult` is built here, the one place that
owns `Name`/`Group`, never by the predicate.

**`PredicateOutcome` carries no `Group` field either, permanently —
closed now, not left open to reopen later.** `RuleResult` doesn't carry
`Group` today (§10, still parked, unmotivated). If that ever changes,
`Group` stays exclusively sourced from `FunctionRule.Group`, the same
way `Name` now is — the predicate never gets a path to diverge on it,
because it never has a path to set it at all.

Generalizes beyond this one fix: any *future* rule-owned field on
`RuleResult` gets this guarantee for free, because the predicate has
lost `RuleResult`-construction privileges entirely, not just its
ability to misname things.

**Scope**: folded into this PR, not deferred — a breaking change to
`RulePredicate<TContext>`'s own signature, larger in blast radius than
§2's (every predicate implementation in every example, sample, doc
snippet, and fixture across all four languages constructs a
`RuleResult` today and will need rewriting to return a
`PredicateOutcome` instead). Accepted: see §6. The doc-verbosity audit
already queued on this PR absorbs the doc-side sweep once every design
decision here is finalized, rather than as a separate pass.

## 3 · `SequentialEvaluator`, `ShortCircuitEvaluator` — composition, not inheritance

A field anyone *can* populate isn't a field everyone *will* populate
correctly. `Rule` is deliberately structural — no base class, no
registration — so there's no closed set of "composite" types to force
correctness onto. The fix is making the *correct* path the *only easy*
path, the way `FunctionRule` already does for leaves: `FunctionRule`
*holds* a predicate delegate and calls it — composition, not
inheritance. The same shape applies to composites, for the same reason,
plus three concrete ones: no base class means no member a reader has to
trace into a different type to understand ("does this concrete type
actually override that, or silently inherit something else?" never
comes up); every dependency is a visible, local constructor argument,
not inherited state; and each piece is unit-testable in isolation,
independent of `AndRule`/`OrRule`/any specific composite existing at
all.

**Three small, independently-public, composed pieces — no base class
anywhere:**

```csharp
/// <summary>
/// Evaluates a list of sub-rules sequentially against one context,
/// letting <paramref name="decider"/> choose when to stop. Composable --
/// hold one as a field and delegate to it, the same way
/// <see cref="FunctionRule{TContext}"/> holds a predicate.
/// </summary>
public delegate bool? StepDecider(RuleResult latest, IReadOnlyList<RuleResult> soFar, int total);

public sealed class SequentialEvaluator<TContext>(StepDecider decider, bool vacuousResult)
{
    public async Task<RuleResult> EvaluateAsync(
        string name, IReadOnlyList<IRule<TContext>> rules, TContext context, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (rules.Count == 0)
            return new RuleResult(name, vacuousResult, subResults: []);

        var soFar = new List<RuleResult>(rules.Count);
        foreach (var rule in rules)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var latest = await rule.EvaluateAsync(context, cancellationToken).ConfigureAwait(false);
            soFar.Add(latest);
            if (decider(latest, soFar, rules.Count) is { } early)
                return new RuleResult(name, early, subResults: soFar);
        }
        return new RuleResult(name, decider(soFar[^1], soFar, rules.Count) ?? vacuousResult, subResults: soFar);
    }
}

/// <summary>
/// Stops at the first sub-rule whose own <see cref="RuleResult.Passed"/>
/// equals <paramref name="stopOn"/> -- the shape <see cref="AndRule{TContext}"/>
/// and <see cref="OrRule{TContext}"/> both are. Wraps
/// <see cref="SequentialEvaluator{TContext}"/> internally; callers never
/// need to know that type exists unless they need more than this covers.
/// </summary>
public sealed class ShortCircuitEvaluator<TContext>
{
    private readonly SequentialEvaluator<TContext> _inner;

    public ShortCircuitEvaluator(bool stopOn)
    {
        _inner = new SequentialEvaluator<TContext>(
            decider: (latest, soFar, total) =>
            {
                if (latest.Passed == stopOn) return stopOn;
                if (soFar.Count == total) return !stopOn;
                return null;
            },
            vacuousResult: !stopOn);
    }

    public Task<RuleResult> EvaluateAsync(
        string name, IReadOnlyList<IRule<TContext>> rules, TContext context, CancellationToken cancellationToken = default) =>
        _inner.EvaluateAsync(name, rules, context, cancellationToken);
}
```

`AndRule<TContext>`/`OrRule<TContext>` are unchanged in every way a
consumer or the diagnostics work (#98) already depends on (construction
syntax, `ToString()`, type identity). Each holds exactly one
`ShortCircuitEvaluator` field:

```csharp
public sealed class AndRule<TContext>(string name, IReadOnlyList<IRule<TContext>> rules, string? group = null) : IRule<TContext>
{
    public const bool VacuousResult = true;   // pinned fact, not wired -- see §3a
    public string Name { get; } = name;
    public string? Group { get; } = group;
    private readonly IReadOnlyList<IRule<TContext>> _rules = rules;
    private static readonly ShortCircuitEvaluator<TContext> Evaluator = new(stopOn: false);

    public Task<RuleResult> EvaluateAsync(TContext context, CancellationToken cancellationToken = default) =>
        Evaluator.EvaluateAsync(Name, _rules, context, cancellationToken);

    public override string ToString() =>
        $"AndRule \"{Name}\"" + (string.IsNullOrEmpty(Group) ? "" : $" ({Group})") + $" — {_rules.Count} sub-rule(s)";
}

public sealed class OrRule<TContext>(string name, IReadOnlyList<IRule<TContext>> rules, string? group = null) : IRule<TContext>
{
    public const bool VacuousResult = false;   // pinned fact, not wired -- see §3a
    public string Name { get; } = name;
    public string? Group { get; } = group;
    private readonly IReadOnlyList<IRule<TContext>> _rules = rules;
    private static readonly ShortCircuitEvaluator<TContext> Evaluator = new(stopOn: true);

    public Task<RuleResult> EvaluateAsync(TContext context, CancellationToken cancellationToken = default) =>
        Evaluator.EvaluateAsync(Name, _rules, context, cancellationToken);

    public override string ToString() =>
        $"OrRule \"{Name}\"" + (string.IsNullOrEmpty(Group) ? "" : $" ({Group})") + $" — {_rules.Count} sub-rule(s)";
}
```

`AtLeastNRule` stays a `docs/extending/new-rule-shape/` worked
example — not shipped, no promotion planned (§7). It composes
`SequentialEvaluator` directly, skipping `ShortCircuitEvaluator`, since
its decision needs the running history, not just the latest result:

```csharp
public sealed class AtLeastNRule<TContext>(
    string name, IReadOnlyList<IRule<TContext>> rules, int minimum, string? group = null) : IRule<TContext>
{
    public string Name { get; } = name;
    public string? Group { get; } = group;
    private readonly IReadOnlyList<IRule<TContext>> _rules = rules;
    private readonly SequentialEvaluator<TContext> _evaluator = new(   // instance, not static -- minimum varies per instance
        decider: (latest, soFar, total) =>
        {
            var passed = soFar.Count(r => r.Passed);
            if (passed >= minimum) return true;
            if (passed + (total - soFar.Count) < minimum) return false;
            return soFar.Count == total ? false : null;
        },
        vacuousResult: minimum <= 0);

    public Task<RuleResult> EvaluateAsync(TContext context, CancellationToken cancellationToken = default) =>
        _evaluator.EvaluateAsync(Name, _rules, context, cancellationToken);
}
```

This proves the lowest layer generalizes past the two built-ins it was
designed around, not just reshapes them. `docs/extending/new-rule-shape/`
gets widened to show `SequentialEvaluator` composed against more than
this one case — a worked set varied enough that "at least N" reads as
one instance of a general pattern, not the only thing the evaluator is
for (§7).

**Resolves a real conflict found during implementation**: the
pre-existing `AtLeastNRule` example in this repo had a test pinning
"never short-circuits, every sub-rule always runs" — predating
`SequentialEvaluator` and written against a dumber, unconditional-loop
implementation. The `decider` above *does* short-circuit once the
threshold is mathematically decided either way (both the `passed >=
minimum` and `passed + remaining < minimum` branches can fire before
exhaustion). That short-circuiting behavior is the settled design,
not the old test's "never short-circuits" expectation — it's a
genuine improvement (fewer unnecessary rule evaluations) consistent
with everything else in this redesign, and the old test gets updated
to match it, not the other way around.

## 3a · Vacuous truth

`SequentialEvaluator` must check `rules.Count == 0` **before** the
loop and return `vacuousResult` directly — not rely on a post-loop
fallback indexing the last evaluated result, which has no last element
when the list started empty. `AndRule([])`/`OrRule([])` must return
their pinned `true`/`false`, never throw.

`ShortCircuitEvaluator` takes only `stopOn` and derives
`vacuousResult: !stopOn` internally — unlike `SequentialEvaluator`,
where `vacuousResult` is a genuinely independent fact
(`AtLeastNRule` needs `minimum <= 0`, not derivable from any single
`stopOn`-shaped value), `ShortCircuitEvaluator`'s own `decider` already
computes `!stopOn` for the non-empty exhaustion case; an empty list is
the same exhaustion with zero iterations, so a second, independently-set
constructor argument could only ever state that same fact correctly or
contradict it — never add real information. Two arguments that must
always agree but aren't enforced to is the same "a value anyone can set
wrong will eventually be set wrong" gap this design exists to close
elsewhere, so it collapses to one. Naming it `stopOn` rather than
`passed` matters too: `passed` reads as a verdict already decided before
`EvaluateAsync` ever runs; `stopOn` reads as what it is, a stopping
condition. `AndRule`/`OrRule` each still carry a
`public const bool VacuousResult` in their own class body — no longer
wired into construction, but kept as the pinned, directly-readable
guarantee (`AndRule([]).Passed == true`, `OrRule([]).Passed == false`),
checked against the real pinned fixture
(`fixtures/graduation_verdict/edge_cases.json`'s `vacuous_pass` /
`unsatisfiable_threshold` cases) and holds.

Cancellation (where it exists — see §4) is checked unconditionally,
before the empty-check, so an already-cancelled token still throws for
an empty composite rather than silently returning the vacuous value —
preserves the exact guarantee the 14 `CancellationContractTests`
already prove today.

A fixed-arity composite (`NotRule`, always exactly one child) never
actually reaches the empty-list branch — its `vacuousResult` argument
is unreachable in practice, but still required since
`SequentialEvaluator`'s constructor needs one; worth a one-line comment
saying so rather than leaving a reader to wonder.

## 3b · `RuleResult` stays a single, unified type

Not split into `LeafResult`/`CompositeResult` variants, for a sharper
reason than "structural typing forces this everywhere." `RunResult`
*is* split out as its own type, cleanly, with no ambiguity — and that
split works specifically because `RunResult`'s role is fixed by *which
call* produced it (`run_all`/`run_group`, never a leaf, no case
analysis needed anywhere).

`RuleResult` can't get the same treatment, because its role isn't
fixed by the call site — it's fixed by *which `Rule` evaluated*, chosen
by domain code the engine doesn't control, and any `Rule` can be
nested inside any other at any depth
(`AndRule([functionRule, anotherAndRule, aCustomComposite])`). A split
type would mean every composite's own `SubResults` list holds a mix of
two incompatible shapes, forcing re-discrimination at every level of
every tree — not ceremony at one boundary, a tax paid recursively,
forever. **A uniform result type is the price of arbitrary-depth
recursive composition, not of structural typing in general** —
`RunResult` gets to be clean because it's the one place in the whole
design that genuinely isn't recursive.

## 3c · Two scope boundaries, both deferred without foreclosing either

**No `IEvaluator<TContext>` interface.** `SequentialEvaluator` stays a
concrete, public class, not an interface a consumer could substitute
their own implementation behind. The value of `SequentialEvaluator` is
specifically that it's the *one* true implementation — sequential,
cancellation-checked, `SubResults`-correct, by construction, which is
exactly what closes off the "a composite author forgets a concern" gap
§3 exists to fix. An open interface reopens that same gap one layer up:
nothing would stop a second implementation from evaluating sub-rules
concurrently, silently breaking short-circuiting — the precise failure
`SKILL.md`'s own Step 2 already names. There's also only one genuine
implementation today (`ShortCircuitEvaluator` delegates to
`SequentialEvaluator` rather than reimplementing it), so there's
nothing real to abstract over yet. The one piece that *does* already
vary per composite — the decision itself — is a plain delegate
(`decider`), which is the right weight for the one axis that changes;
promoting it to a named interface buys naming, not capability.

**`SequentialEvaluator` takes `IReadOnlyList<IRule<TContext>>`, not a
lazy/streamed source.** The motivating case for streaming (large,
data-driven rule sets) doesn't actually need it: the expensive
operation — evaluating a rule's predicate — is already lazy and
short-circuited by the plain loop over a list, regardless of list
size. A streamed source would only help if *constructing* rule objects
were itself expensive per rule, a narrower and more exotic case than
"rules loaded from a database" (which already materializes cheaply
before evaluation starts, per `docs/samples/data-driven-rule-sets/`).
Building it now would also cost something real: C#'s BCL has no
built-in `IEnumerable<T>` → `IAsyncEnumerable<T>` bridge (that's the
`System.Linq.Async` NuGet package, a new dependency this repo's
zero-dependency guarantee doesn't take on lightly), and async
enumeration carries real per-iteration overhead paid by every
evaluation, not just ones that need it. Deferring this doesn't risk a
second release to fix a mistake, but it's not entirely free either: a
scratch sketch of a streamed `AsyncRuleEvaluator<TContext>` (not
committed, cost-estimate only) confirms `decider`'s `total` argument
doesn't port — a streamed source only knows "was that the last one"
after pulling past it, not a count up front. The three existing
`decider` bodies that read `total` (`ShortCircuitEvaluator`'s,
`AtLeastNRule`'s) would need rewriting against an `isLast`-shaped
signature, not reused as-is. A genuine future need still arrives as a
new sibling type, consistent with everything else in this section —
just not a zero-cost one.

## 3d · `AndRule.Failed`/`Passing`, `OrRule.Passed`/`Failing`

`Leaves`/`FailingLeaves` (§2) is the general mechanism — correct for any
composite, any depth, any mix of types. `ShortCircuitEvaluator`'s own
invariant gives `AndRule`/`OrRule` specifically something more precise:
because evaluation stops the moment the outcome is decided,
`SubResults[^1]` is always *the one sub-result that decided it* — the
sole failure for a failed `AndRule` (everything before it passed), or
the sole pass for a passed `OrRule` (everything before it failed). Each
family's own, non-generalized view — no attempt at one shared shape
across both, since And and Or are genuine opposites here and forcing a
common shape is exactly what the singular-helper question above already
ruled out:

```csharp
public static class AndRule
{
    public static RuleResult? Failed(RuleResult result) =>
        result.SubResults.Count > 0 && !result.SubResults[^1].Passed ? result.SubResults[^1] : null;
    public static IReadOnlyList<RuleResult> Passing(RuleResult result) =>
        Failed(result) is null ? result.SubResults : result.SubResults.Take(result.SubResults.Count - 1).ToList();
}

public static class OrRule
{
    public static RuleResult? Passed(RuleResult result) =>
        result.SubResults.Count > 0 && result.SubResults[^1].Passed ? result.SubResults[^1] : null;
    public static IReadOnlyList<RuleResult> Failing(RuleResult result) =>
        Passed(result) is null ? result.SubResults : result.SubResults.Take(result.SubResults.Count - 1).ToList();
}
```

**Found during implementation: `result.SubResults[..^1]` as originally
written here does not compile.** `SubResults` is typed
`IReadOnlyList<RuleResult>`, which has no `Slice`/range-indexer
pattern — C#'s range syntax needs an actual indexer accepting `Range`
or a `Length`+`Slice(int,int)` pair, neither of which
`IReadOnlyList<T>` provides, regardless of what the concrete runtime
type happens to support. Fixed above to `.Take(n).ToList()`. Worth
flagging plainly: this was a real bug in this plan's own reference
code, not hypothetical — caught only because an agent actually tried
to compile it.

Plain static methods over `RuleResult` alone — deliberately not
`IRule<TContext>`-aware, not an instance method on the rule, not gated
behind any interface. Neither derivation reads anything but
`SubResults`, so nesting is already handled: if the decisive sub-result
is itself a composite, `AndRule.Failed(result)?.FailingLeaves` composes
for free. No provenance guard against passing the wrong family's result
in — intentionally: that mistake is visible at the call site
(`AndRule.Failed(orResult)` reads wrong immediately), unlike the
original `Data`-duck-typing bug (§1), which broke on completely
innocent code with no misuse involved. That distinction is why this
doesn't need the weight `SequentialEvaluator`/`ShortCircuitEvaluator`
carry in §3 — those close a gap in code with no misuse; this one only
guards against an actively wrong call.

## 3e · `NotRule` — shipped, not example-only

No `SequentialEvaluator`/`ShortCircuitEvaluator` composed in — one
child, no sequence to iterate, so the machinery would be indirection
for nothing it uses. Still checks cancellation unconditionally before
evaluating, matching `AndRule`/`OrRule`'s own "checked before anything
runs" guarantee rather than trusting the wrapped rule to check it.

```csharp
public sealed class NotRule<TContext>(string name, IRule<TContext> rule, string? group = null) : IRule<TContext>
{
    public string Name { get; } = name;
    public string? Group { get; } = group;

    public async Task<RuleResult> EvaluateAsync(TContext context, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var inner = await rule.EvaluateAsync(context, cancellationToken).ConfigureAwait(false);
        return new RuleResult(Name, !inner.Passed, subResults: [inner]);
    }

    public override string ToString() =>
        $"NotRule \"{Name}\"" + (string.IsNullOrEmpty(Group) ? "" : $" ({Group})");

    public static RuleResult Negated(RuleResult result) => result.SubResults[0];
}
```

`SubResults = [inner]`, truthfully — not discarded. An earlier draft of
this design flattened `NotRule` to a leaf (empty `SubResults`)
specifically to dodge `FailingLeaves` reading misleadingly empty on a
failed `NotRule` (the cause is a pass, not a failure). That dodge is
wrong: `SubResults.Count == 0` has to mean *only* "this is a leaf,"
never also "this is a composite that hid its own structure" — that's
the entire basis §2 gives for `Leaves`/`FailingLeaves` being safe on
the shared `RuleResult` type at all. Breaking that reliability to avoid
one misleading read just relocates the original `Data`-ambiguity
problem (§1) from one property to another. The actual fix is the same
one §3d already established: a dedicated accessor, not generic
`FailingLeaves`, answers "why." `Negated` is non-nullable, unlike
`AndRule.Failed`/`OrRule.Passed` (§3d) — fixed arity means the inner
result is never absent.

**C#-specific completion, found during implementation**: `AndRule`/
`OrRule`/`FunctionRule` all ship as a generic/non-generic pair — the
generic `TContext` type, plus a non-generic wrapper (`IRule`, not
`IRule<TContext>`) for the dict-context ecosystem, since a bare
`NotRule<IReadOnlyDictionary<string, object?>>` doesn't structurally
satisfy the non-generic `IRule` the dict-context world depends on.
`NotRule` needs the same pair for parity with every other composite
in this codebase — a non-generic `NotRule : IRule` wrapper with its
own `Negated`, shaped exactly like the generic one above. Not called
out explicitly before because `AtLeastNRule`/`NoneOfRule` (the other
two composites discussed at this level of detail) never shipped, so
this gap in the spec never surfaced until `NotRule` actually did. Only
a C# concern — Python/JS/Dart have no generic/non-generic split to
begin with (§5).

## 3f · `NoneOfRule` — considered, not shipping

All sub-rules must fail for this to pass. Would share `OrRule`'s own
stop-on-first-pass trigger with an inverted outcome, composing
`SequentialEvaluator` directly (not `ShortCircuitEvaluator`, whose own
`decider` hardcodes "the trigger value *is* the result" — doesn't fit
an inverted polarity). **Resolved: not shipping**, same reasoning as
`AtLeastNRule` (§7) — `NotRule(OrRule(rules))` already produces the
same `Passed`, the same short-circuit point, and (once `FailingLeaves`
is corrected per §2) the same flattened failing leaves, once `NotRule`
ships with its own corrected design. The only gap is one extra
synthetic tree node and a two-step drill-down instead of one-step —
not enough to justify a dedicated type the same way the `AtLeastNRule`
gap wasn't. If a genuine need for the one-step ergonomics shows up
later, it's `docs/extending/new-rule-shape/` material, not a shipped
type.

Naming note, kept for the record since it bears on the general
cross-language safety rule, not just this one rejected type: had this
shipped, it would have been `NoneOfRule`, never the shorter `NoneRule`
— not merely stylistic. Python's `None` is a reserved keyword
(capitalized, unlike the lowercase `and`/`or`/`not`), so `class None`
is a `SyntaxError` there, not just bad style.

## 4 · `RunResult`

`RunResult.Results` was never ambiguous (always a real
`IReadOnlyList<RuleResult>`, never `Data`), so none of §2/§3 applies to
it. `Leaves`/`FailingLeaves` on `RunResult` are one-line forwarders once
`RuleResult.Leaves` exists:

```csharp
public IReadOnlyList<RuleResult> Leaves => Results.SelectMany(r => r.Leaves).ToList();
public IReadOnlyList<RuleResult> FailingLeaves => Leaves.Where(l => !l.Passed).ToList();
```

`RunAllAsync` is **not** built on `SequentialEvaluator`, despite the
surface similarity (`Passed` + a list of results). `SequentialEvaluator`
exists to let a caller decide when to stop early; `RunAllAsync`'s whole
contract is the guaranteed opposite — it never short-circuits
(`RunModeIdiomTests.RunAllStopsBeforeTheNextRuleEvenMidRun` proves it).
It also doesn't build the same shape: `run_all` isn't itself a named
rule answering "did I pass," it builds `RunResult` directly (no
`RuleName`/`Detail`/`SubResults`), which `SequentialEvaluator` doesn't
produce. `RulesEngine([]).RunAllAsync(ctx)` already returns
`Passed: true` for an empty engine — incidental, falling straight out
of `results.TrueForAll(...)`'s own semantics on an empty list, not a
declared polarity the way `AndRule`/`OrRule` need one.

## 5 · Rough shape, other languages

Structural note: **only C# has a `CancellationToken`** — Python's
cancellation is transparent at every `await` via `asyncio`; neither
JS/TS nor Dart ever modeled cancellation at the library level at all.
`SequentialEvaluator` is simpler by one concern in the other three, not
by omission.

- **Python**: `SequentialEvaluator`/`ShortCircuitEvaluator` as plain
  classes (no ABC needed — nothing is inherited). `AndRule`/`OrRule`
  **stay real classes** (not factory functions, even though that's this
  package's own idiom for leaf predicates elsewhere) — they're already
  shipped, public, importable types; changing shape here would be a
  second, unforced break on top of `Data`→`sub_results`. `StepDecider`
  as a `TypeAlias`: `StepDecider: TypeAlias = Callable[[RuleResult, Sequence[RuleResult], int], bool | None]`.
  **§2a's `PredicateOutcome` guarantee is weaker here than in C#,
  found during implementation**: duck typing means `FunctionRule.evaluate`
  reading `outcome.passed`/`.detail`/`.data` can't distinguish a real
  `PredicateOutcome` from a stale predicate that still (incorrectly)
  returns an old-style `RuleResult` object, since the attribute names
  happen to coincide — it would silently keep "working" instead of
  failing to compile the way C# does. `FunctionRule.evaluate` adds an
  explicit `isinstance(outcome, PredicateOutcome)` check, raising
  rather than silently accepting the wrong type — the predicate
  boundary is exactly the externally-authored edge this package's own
  defensive-first conventions already call out for validation.
- **JS/TS**: same two classes, private fields via `#`, matching
  `FunctionRule`'s existing `#predicate`/`#rules` convention. `decider`
  returns `boolean | undefined` (not `| null`) — matches `Rule.group`'s
  existing `string | undefined` idiom in this codebase. `StepDecider`
  as a named type alias: `type StepDecider = (latest: RuleResult, soFar: readonly RuleResult[], total: number) => boolean | undefined;`.
  **Same duck-type gap as Python, found during implementation, worse
  if anything — JS has no runtime type system at all.** `FunctionRule.evaluate`
  guards against a stale predicate returning an old-style `RuleResult`
  (checks for a `ruleName` field, the one thing `PredicateOutcome`
  never has), same reasoning as Python's `isinstance` check above —
  this plan's own silence on the JS case was a gap, not a deliberate
  omission. **No per-closed-type shared static evaluator** — TypeScript
  (like C#) forbids a `static` class member from referencing its own
  class's type parameter, but unlike C#, `SequentialEvaluator`/
  `ShortCircuitEvaluator` are themselves non-generic (`TContext` only
  appears on their `evaluate<TContext>()` method), so one true
  module-level instance is shared across every `AndRule`/`OrRule`
  regardless of `TContext` — arguably cleaner than C#'s
  per-closed-type static, not a compromise.
- **Dart**: same two classes; `SequentialEvaluator`'s `decider` is a
  typed `function` field, same composition shape as the others.
  `StepDecider` as a native `typedef`:
  `typedef StepDecider = bool? Function(RuleResult latest, List<RuleResult> soFar, int total);`.
  No duck-type gap — `RulePredicate<TContext>`'s typedef is a real
  static function-type signature, so a predicate still returning the
  old `RuleResult` shape fails to compile, same guarantee as C#, no
  extra guard needed. **No shared static evaluator at all**: Dart
  additionally forbids a generic class's `static` member from
  referencing that class's own type parameter (stricter than C#/TS
  here), so `AndRule`/`OrRule` each build their own
  `ShortCircuitEvaluator<TContext>` as a plain instance field instead
  — one extra allocation per rule constructed, functionally identical
  since the evaluator is stateless beyond its closed-over `decider`.

`StepDecider` (every language) names the `decider` signature once, used
by `SequentialEvaluator` and anything composing it (`AtLeastNRule`,
custom composites) — not parameterized by `TContext` in any language,
since it only ever touches `RuleResult`/count, never the evaluation
context itself. Named for what it is: invoked once per step (per
sub-rule evaluated), not a one-shot classifier of the whole run.

`StepDecider`/`decider` bodies are early-return `if`s in every language, not
ternary chains — these are genuinely different predicates (met the
minimum / can't reach it / exhausted), not a discriminant ladder in
disguise; see `AGENTS.md`'s own dispatch-rule carve-out for guard
clauses.

## 6 · Breaking change, deliberately

Pre-1.0 (`csharp-v0.4.0` and siblings are the only versions this ever
shipped in) — per this repo's own stated policy, a breaking change
pre-1.0 takes `MINOR`, not a special exception process. What breaks
concretely: any consumer reading `andRuleResult.Data as List<RuleResult>`
today stops working — `Data` is `null`/`None`/`undefined` for every
composite result going forward, full stop.

**Second, larger break, folded into the same release (§2a):**
`RulePredicate<TContext>` stops returning `RuleResult` and returns
`PredicateOutcome` instead. Every predicate implementation in this
repo — every example, every sample, every doc snippet, every
fixture, across all four languages — constructs a `RuleResult`
directly today and needs rewriting. Larger blast radius than the
`Data`/`SubResults` change above, accepted deliberately rather than
deferred to a second breaking release, for the same reason: one
breaking version to adapt to, not two.

**Third break, found during implementation, not originally called out
here — worth being explicit about:** `AndRule`/`OrRule`'s own
`RuleResult.Detail` is empty going forward. The pre-redesign
implementations hand-built a descriptive string on construction
(`"'x' failed: ..."` / `"no sub-rule passed"`); composing
`SequentialEvaluator`/`ShortCircuitEvaluator` has no detail channel —
`StepDecider` returns `bool?`, nothing richer — so that string is
simply gone. This is accepted, not a gap to patch: `FailingLeaves`
(§2) and the family-specific accessors (§3d, §3e) are the intended
replacement, and they're strictly more informative than the old
single string ever was (the full set of failing leaves, not one
hand-picked sentence mentioning the first). Any consumer or doc
reading `andOrResult.Detail` for a human-readable reason needs to
read `FailingLeaves` instead.

## 7 · What this unlocks

- **Mutation-testing survivors can't recur the same way.** The C# run
  found the *same* missing-cancellation-test gap four times (`AndRuleT`,
  `OrRuleT`, `RulesEngineT`×2) because the same logic was copy-pasted
  four times. One implementation means one place to get it right, once.
- **The structural-invariant chaos tests just added to
  `graduation_verdict` (this same branch, prior commit) partially
  become redundant, in a good way** — each language's new invariant
  checker exists specifically because there was no common base to
  trust, and dispatches by concrete rule type accordingly. Once
  `SequentialEvaluator`'s own guarantees are proven once in the core
  library's own suite, a generic version needs no per-type dispatch at
  all. **Explicit open thread, not assumed free**: revisit those tests
  after this ships rather than carrying duplicate coverage indefinitely.
- **Composite-authoring gets closer to the pit-of-success leaf-authoring
  already had** — compose `ShortCircuitEvaluator` or
  `SequentialEvaluator` and forward `EvaluateAsync`, instead of
  independently getting cancellation/short-circuit/`SubResults`/
  vacuous-truth all right by hand. Slightly more boilerplate than
  inheritance would have given (no free `Name`/`Group` forwarding), in
  exchange for zero inheritance anywhere in the design.
- **`docs/extending/new-rule-shape/`'s worked example gets
  dramatically shorter and more honest** — compose the two public
  pieces instead of hand-rolling a loop.
- **Resolved**: `AtLeastNRule` stays example-only, not promoted.
  `SequentialEvaluator` is the real generalization — anyone with a
  genuinely different "at least"-shaped condition composes it directly
  with their own `decider`, the same way `AtLeastNRule` itself does, so
  shipping `AtLeastNRule` as a named type wouldn't add capability, only
  a second, narrower name for something already public. The widened
  `docs/extending/new-rule-shape/` (above) covers this case among
  several varied ones instead.
- **Fewer independent places for the four ports to drift** — one
  reference algorithm (`SequentialEvaluator`'s loop) to port correctly
  per language, not two independently hand-written ones per language
  each needing to agree with three siblings.

## 8 · Resolved before implementation

- **`RunResult` helpers beyond the two forwarders**: none. Nothing
  surfaced through this whole discussion needed more than
  `Leaves`/`FailingLeaves`.
- **`SequentialEvaluator`/`ShortCircuitEvaluator` public from day
  one**: yes. §7's pit-of-success benefit only holds if they're
  public, and the widened `docs/extending/new-rule-shape/` (§3, §7)
  explicitly composes against them — internal-until-asked would
  contradict that doc's own worked examples.
- **Test-fixture additions**: not pre-designed here — resolved
  naturally during implementation (§9), driven by what the corrected
  `FailingLeaves` (§2) and §2a's `PredicateOutcome` change actually
  need pinned, rather than speculated in advance.

## 9 · GitHub tracking

- **#100** — stays open as the tracking issue for this redesign; a
  comment posted summarizing the revised design, crediting the
  `eps_bloc_maker` adopter comment that prompted it, linking this plan.
  Closed by whichever PR implements §2–§6.
- **PR #103** (`diagnostics/debugger-display-all-languages`) — per the
  user's own explicit instruction earlier in this session ("single PR,
  sequenced... no PR-open/review/merge ceremony per item"), this work
  lands on the *same* branch/PR as the #98 diagnostics work and the
  full #101 hardening pass already committed there. PR body updated to
  list `Closes #100` alongside the existing `#98`/`#101`.
  **Not yet implemented** — §0 and this section get updated as code
  actually lands.

## 10 · Parked, not decided, not a priority

**`ICompositeRule<TContext> : IRule<TContext> { IReadOnlyList<IRule<TContext>> Rules { get; } }`.**
Considered as a structural marker so diagnostics/tooling could
introspect *any* composite's configured sub-rules generically. No
present consumer: `AndRule`/`OrRule`'s own `ToString()` already reads
`_rules.Count` directly off their own field via a per-type override,
the same "caller already knows the concrete variant" shape as
everywhere else in this library. Would be designing for a hypothetical
future requirement with nothing in current scope that needs it —
same restraint already applied to promoting `AtLeastNRule` in §7.
Orthogonal to §3d's `Failed`/`Passing` regardless of whether it's ever
built: that mechanism derives purely from `RuleResult`, never touches
`IRule<TContext>`.

**`RuleEvaluation` — a forward-looking `(IRule<TContext> Rule, RuleResult Result)` pairing.**
Name settled (not `RuleContext`, which collides with the existing
`TContext` generic parameter used everywhere) if this is ever built.
Not motivated by anything in current scope; floated, not designed.

**A Tambola/Housie ticket-check sample.** Floated as a `run_all`
showcase — independent win-pattern checks (`early_five`/`top_line`/
`full_house`/etc.) as named rules, pure array/set math, no I/O,
genuinely runnable. Also a candidate for illustrating the
judgment-vs-state boundary explicitly (checking a pattern is verdict;
claiming/awarding a prize first is not, the same split as
`docs/extending/domain-adapter-module/`'s rate-limiter, more vivid),
and a natural motivating case for `Leaves`/`FailingLeaves` once that
ships ("which numbers are still missing for Full House"). Explicitly
not scoped, not scheduled, and not part of #103 — listed here only so
it isn't lost, not as a commitment. Revisit after the architecture work
in §0–§9 actually ships.
