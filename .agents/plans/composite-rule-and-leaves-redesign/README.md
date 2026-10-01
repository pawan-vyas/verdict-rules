<!-- Title: SequentialEvaluator, RuleResult.SubResults, and Leaves/FailingLeaves -->
# `SequentialEvaluator`, `RuleResult.SubResults`, and `Leaves`/`FailingLeaves`

> Issue #100's original one-paragraph pitch ("add two read-only
> properties") needed a real architecture decision once an adopter's
> review surfaced the cases it didn't account for. This doc is the
> settled spec — what ships, and why — not a transcript of how the
> design got here. Supersedes #100's own original design; #100 stays
> open as the tracking issue, closed by whichever PR implements this.

## 0 · Status

**Design settled. No implementation yet — see §8 for what's still open
before any code lands.**

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
only documented. `Leaves`/`FailingLeaves` are then simple recursion over
`SubResults`:

```csharp
public IReadOnlyList<RuleResult> Leaves =>
    SubResults.Count == 0 ? [this] : SubResults.SelectMany(s => s.Leaves).ToList();
public IReadOnlyList<RuleResult> FailingLeaves => Leaves.Where(l => !l.Passed).ToList();
```

This is additive to the type (new field, new derived properties) but
changes `AndRule`/`OrRule`'s own behavior (they stop writing sub-results
into `Data`) — a real breaking change, accepted deliberately: see §6.

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
/// letting <paramref name="decide"/> choose when to stop. Composable --
/// hold one as a field and delegate to it, the same way
/// <see cref="FunctionRule{TContext}"/> holds a predicate.
/// </summary>
public sealed class SequentialEvaluator<TContext>(
    Func<RuleResult, IReadOnlyList<RuleResult>, int, bool?> decide,
    bool vacuousResult)
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
            if (decide(latest, soFar, rules.Count) is { } early)
                return new RuleResult(name, early, subResults: soFar);
        }
        return new RuleResult(name, decide(soFar[^1], soFar, rules.Count) ?? vacuousResult, subResults: soFar);
    }
}

/// <summary>
/// Stops at the first sub-rule whose own <see cref="RuleResult.Passed"/>
/// equals <paramref name="passed"/> -- the shape <see cref="AndRule{TContext}"/>
/// and <see cref="OrRule{TContext}"/> both are. Wraps
/// <see cref="SequentialEvaluator{TContext}"/> internally; callers never
/// need to know that type exists unless they need more than this covers.
/// </summary>
public sealed class ShortCircuitEvaluator<TContext>
{
    private readonly SequentialEvaluator<TContext> _inner;

    public ShortCircuitEvaluator(bool passed, bool vacuousResult)
    {
        _inner = new SequentialEvaluator<TContext>(
            decide: (latest, soFar, total) =>
            {
                if (latest.Passed == passed) return passed;
                if (soFar.Count == total) return !passed;
                return null;
            },
            vacuousResult: vacuousResult);
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
    public string Name { get; } = name;
    public string? Group { get; } = group;
    private readonly IReadOnlyList<IRule<TContext>> _rules = rules;
    private static readonly ShortCircuitEvaluator<TContext> Evaluator = new(passed: false, vacuousResult: true);

    public Task<RuleResult> EvaluateAsync(TContext context, CancellationToken cancellationToken = default) =>
        Evaluator.EvaluateAsync(Name, _rules, context, cancellationToken);

    public override string ToString() =>
        $"AndRule \"{Name}\"" + (string.IsNullOrEmpty(Group) ? "" : $" ({Group})") + $" — {_rules.Count} sub-rule(s)";
}

public sealed class OrRule<TContext>(string name, IReadOnlyList<IRule<TContext>> rules, string? group = null) : IRule<TContext>
{
    public string Name { get; } = name;
    public string? Group { get; } = group;
    private readonly IReadOnlyList<IRule<TContext>> _rules = rules;
    private static readonly ShortCircuitEvaluator<TContext> Evaluator = new(passed: true, vacuousResult: false);

    public Task<RuleResult> EvaluateAsync(TContext context, CancellationToken cancellationToken = default) =>
        Evaluator.EvaluateAsync(Name, _rules, context, cancellationToken);

    public override string ToString() =>
        $"OrRule \"{Name}\"" + (string.IsNullOrEmpty(Group) ? "" : $" ({Group})") + $" — {_rules.Count} sub-rule(s)";
}
```

`AtLeastNRule`/`NotRule` (today example-only,
`docs/extending/new-rule-shape/`) compose `SequentialEvaluator`
directly, skipping `ShortCircuitEvaluator`, since their decision needs
the running history, not just the latest result:

```csharp
public sealed class AtLeastNRule<TContext>(
    string name, IReadOnlyList<IRule<TContext>> rules, int minimum, string? group = null) : IRule<TContext>
{
    public string Name { get; } = name;
    public string? Group { get; } = group;
    private readonly IReadOnlyList<IRule<TContext>> _rules = rules;
    private readonly SequentialEvaluator<TContext> _evaluator = new(   // instance, not static -- minimum varies per instance
        decide: (latest, soFar, total) =>
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
designed around, not just reshapes them.

## 3a · Vacuous truth

`SequentialEvaluator` must check `rules.Count == 0` **before** the
loop and return `vacuousResult` directly — not rely on a post-loop
fallback indexing the last evaluated result, which has no last element
when the list started empty. `AndRule([])`/`OrRule([])` must return
their pinned `true`/`false`, never throw.

`ShortCircuitEvaluator` takes `passed` and `vacuousResult` as two
*separate* constructor arguments rather than deriving the second from
the first. `AndRule`'s/`OrRule`'s own construction states both facts
side by side, in the same line, in their own class body — the single
most pinned fact in the library (`AndRule([]).Passed == true`,
`OrRule([]).Passed == false`) stays a literal anyone reading `AndRule`
sees directly, never a value recomputed from a sibling parameter.
`AtLeastNRule` states its own, non-derivable polarity the same way:
`vacuousResult: minimum <= 0`. Checked against the real pinned fixture
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
(`decide`), which is the right weight for the one axis that changes;
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
second release to fix a mistake: `decide`'s own shape is already
decoupled from how rules are sourced, so a genuine future need would
arrive as a new sibling type reusing the same `decide` functions
`AndRule`/`OrRule`/`AtLeastNRule` already have, not a breaking change
to any of them.

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
  second, unforced break on top of `Data`→`sub_results`.
- **JS/TS**: same two classes, private fields via `#`, matching
  `FunctionRule`'s existing `#predicate`/`#rules` convention. `decide`
  returns `boolean | undefined` (not `| null`) — matches `Rule.group`'s
  existing `string | undefined` idiom in this codebase.
- **Dart**: same two classes; `SequentialEvaluator`'s `decide` is a
  typed `function` field, same composition shape as the others.

`Decide`/`decide` bodies are early-return `if`s in every language, not
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
- **Open question, not decided here**: `AtLeastNRule` drops from ~25
  hand-rolled lines to a much smaller composed version. Worth asking,
  separately, whether it's general enough to promote from
  example-only into the shipped library. Not this PR's call to make
  unprompted.
- **Fewer independent places for the four ports to drift** — one
  reference algorithm (`SequentialEvaluator`'s loop) to port correctly
  per language, not two independently hand-written ones per language
  each needing to agree with three siblings.

## 8 · Before writing a line of code: the "helpers" discussion

- Does `Leaves`/`FailingLeaves` need a convenience singular form (the
  original #100 motivation — "a server records refused privileged
  changes in an audit log, keyed by the refusing rule's stable id" —
  implies a common need for exactly *one* id, and `OrRule`'s own
  all-fail case can produce several)? `FirstFailingLeaf`? Leave callers
  to write `.FailingLeaves.FirstOrDefault()` themselves?
  `docs/testing/README.md`'s own "Related" table already suggests
  documenting evaluation order as the convention (per the adopter
  comment's case 5) — does that need a named helper, or just a stated
  convention?
- Any helper needed on `RunResult` beyond the two forwarders in §4?
- Should `SequentialEvaluator`/`ShortCircuitEvaluator` be public from
  day one in every language, or internal until a real consumer asks?
  §7's "pit of success for custom composites" benefit only holds if
  they're public.
- Test-fixture additions: new `leaves`/`failing_leaves` expectation
  rows in `fixtures/graduation_verdict/students.json` (mirroring how
  `rules_evaluated`/`groups` are already pinned), per the adopter
  comment's own suggested fixture-row table (single failing
  `FunctionRule`, nested `all(a, all(b,c))`, `any(a,b)` both-fail,
  negation, passing composite).
- Scope of the `fixtures/graduation_verdict/README.md` contract update
  needed once `leaves`/`failing_leaves` are pinned there.

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
