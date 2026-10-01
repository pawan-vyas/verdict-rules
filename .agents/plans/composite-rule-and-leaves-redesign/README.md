<!-- Title: CompositeRule, RuleResult.SubResults, and Leaves/FailingLeaves -->
# `CompositeRule`, `RuleResult.SubResults`, and `Leaves`/`FailingLeaves`

> Issue #100's original one-paragraph pitch ("add two read-only
> properties") turned out to need a real architecture decision once an
> adopter's review surfaced the cases it didn't account for. This doc
> is the settled design from that discussion, the rough shape in all
> four languages, and the open threads still to resolve before any
> code lands. Supersedes #100's own original design; #100 stays open
> as the tracking issue, closed by whichever PR implements this.

## 0 · Status

**Design settled, discussed, and agreed in conversation (this doc is
the record of it). No implementation yet — see §5, "before writing a
line of code," for what's still open.**

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

## 2 · Why duck-typing `Data` can't be fixed, only replaced

`RuleResult.Data` is documented, in every language, as opaque — never
read by verdict itself. `AndRule`/`OrRule` populating it with their own
sub-results was always a *convention*, not a contract the type system
enforced. `Leaves` as originally scoped would have been the first
place verdict's own code read `Data` semantically, and the only way to
do that after the fact (since a `RuleResult` carries no reference back
to the `Rule` that produced it) is runtime shape-checking — which is
exactly what the five cases above broke.

A flag (`IsComposite: bool`) was considered and rejected: it's a
second, independently-settable fact that can disagree with `Data`,
with nothing in the type system tying the two together. Pure
config-knob shape — metadata *about* a field, not intrinsic to it.

**Settled: a real field, not a flag.** `RuleResult` gains
`SubResults: IReadOnlyList<RuleResult>` (empty default — absence *is*
the leaf signal, the same idiom `Group: string?` already uses in this
type). `Data` stops carrying composite sub-results at all and becomes
*actually* opaque, for the first time structurally enforced rather
than only documented. This is additive to the type (new field, new
derived properties) but changes `AndRule`/`OrRule`'s own behavior
(they stop writing sub-results into `Data`) — a real breaking change,
accepted deliberately: see §4.

## 3 · Why `SubResults` alone isn't enough: `CompositeRule`

A field anyone *can* populate isn't a field everyone *will* populate
correctly. `Rule` is deliberately structural — no base class, no
registration — so there's no closed set of "composite" types to force
correctness onto. A custom composite (an `AtLeastNRule`, a `NotRule`)
implementing `IRule<TContext>` from scratch is exactly as free to
forget `SubResults`, botch short-circuit cancellation checks, or get
vacuous truth backwards as any hand-rolled code can be wrong — that's
not a gap specific to this feature, it's the same trust already
extended to a `FunctionRule` predicate's own correctness.

What can change: make the *correct* path the *only easy* path, the
way `FunctionRule` already does for leaves. `CompositeRule<TContext>`
owns sequential evaluation, cancellation checking (where that concept
exists), and `RuleResult` construction in exactly one place. A
composite built by extending it cannot construct an inconsistent
result — there's only one call site that builds one, and it always
populates `SubResults` from what it actually evaluated.

`ShortCircuitCompositeRule<TContext>` is the "stop at the first result
where `Passed == X`" specialization `AndRule`/`OrRule` both are —
literally the same algorithm, differing by one constructor boolean.
`AndRule`/`OrRule` become one-line declarations in every language.
`AtLeastNRule`/`NotRule` (today example-only, `docs/extending/new-rule-shape/`)
extend `CompositeRule` directly, since their decision needs the running
history, not just the latest result — proving the base generalizes
past the two built-ins it was designed around, not just reshaping them.

## 3a · Vacuous truth under `CompositeRule` — the bug this design caught

First draft of the post-loop fallback assumed `soFar` was always
non-empty and indexed `soFar[^1]` unconditionally — `AndRule([])` and
`OrRule([])` would have **thrown** instead of returning their pinned
`true`/`false`. Caught by tracing the empty-`Rules` case through the
sketch before writing any real code, not after.

**Fix:** vacuous truth is its own required, explicit declaration —
`abstract bool VacuousResult { get; }` — checked *before* the loop, so
`Rules.Count == 0` never reaches the per-iteration logic at all.
`ShortCircuitCompositeRule` seals `Decide` (the genuine algorithm)
but deliberately leaves `VacuousResult` abstract — `AndRule`/`OrRule`
each state their own polarity as a plain literal (`AndRule: true`,
`OrRule: false`) rather than deriving it from `decideOn`, so the fact
is visible on the concrete type itself, not something a reader has to
trace into the base to find (refined after the initial sketch — see
§5 for the full reasoning). `AtLeastNRule` states its own:
`minimum <= 0`. Checked against the real pinned fixture
(`fixtures/graduation_verdict/edge_cases.json`'s `vacuous_pass` /
`unsatisfiable_threshold` cases) and holds.

Cancellation (where it exists — see §4) is checked *unconditionally*,
before the empty-check, so an already-cancelled token still throws for
an empty composite rather than silently returning the vacuous value —
preserves the exact guarantee the 14 `CancellationContractTests`
already prove today.

A fixed-arity composite (`NotRule`, always exactly one child) has an
unreachable `VacuousResult` — should assert/throw rather than return
an arbitrary value, so it can't be mistaken for a meaningful answer.

## 3b · Why `RuleResult` can't be split into `LeafResult`/`CompositeResult`

Considered and rejected, for a sharper reason than "structural typing
forces this everywhere." `RunResult` already *is* split out as its own
type, cleanly, with no ambiguity — and that split is free specifically
because `RunResult`'s role is fixed by *which call* produced it
(`run_all`/`run_group`, never a leaf, no case analysis needed
anywhere).

`RuleResult` can't get the same treatment, because its role isn't
fixed by the call site — it's fixed by *which `Rule` evaluated*, chosen
by domain code the engine doesn't control, and any `Rule` can be
nested inside any other at any depth
(`AndRule([functionRule, anotherAndRule, aCustomComposite])`). A split
type would mean every composite's own `SubResults`/children list holds
a mix of two incompatible shapes, forcing re-discrimination at every
level of every tree — not ceremony at one boundary, a tax paid
recursively, forever, to protect against a mistake `CompositeRule`
already closes off for anyone following the documented path.
**A uniform result type is the price of arbitrary-depth recursive
composition, not of structural typing in general** — `RunResult` gets
to be clean because it's the one place in the whole design that
genuinely isn't recursive.

## 4 · `RunResult` — free, not a second design problem

`RunResult.Results` was never ambiguous (always a real
`IReadOnlyList<RuleResult>`, never `Data`), so none of §2/§3 applies to
it. `RunResult` is semantically the never-short-circuiting sibling of
what a composite's own result represents (`Passed` = "every child
passed" either way; `Results`/`SubResults` = the children either way;
the only real difference is the short-circuit guarantee). `Leaves`/
`FailingLeaves` on `RunResult` are one-line forwarders once
`RuleResult.Leaves` exists:

```csharp
public IReadOnlyList<RuleResult> Leaves => Results.SelectMany(r => r.Leaves).ToList();
public IReadOnlyList<RuleResult> FailingLeaves => Leaves.Where(l => !l.Passed).ToList();
```

## 5 · Rough shape, all four languages

Full code for each is in conversation (not reproduced in full here —
this is the recap, not the spec). Structural note: **only C# has a
`CancellationToken`** — Python's cancellation is transparent at every
`await` via `asyncio`; neither JS/TS nor Dart ever modeled cancellation
at the library level at all. `CompositeRule` is simpler by one concern
in the other three, not by omission.

- **C#**: `CompositeRule<TContext>` (abstract class, `protected
  abstract bool VacuousResult { get; }` / `protected abstract bool?
  Decide(RuleResult latest, IReadOnlyList<RuleResult> soFar)`) →
  `ShortCircuitCompositeRule<TContext>` seals **only** `Decide`, via one
  `decideOn` ctor bool — `VacuousResult` stays abstract at this layer,
  deliberately. `AndRule<TContext>`/`OrRule<TContext>` each state their
  own `VacuousResult` explicitly (`true`/`false` literal) rather than
  having it derived from `decideOn` — a conscious choice: `Decide` is a
  real algorithm worth sharing (duplicating it risks the exact
  mutation-testing lesson from §7), `VacuousResult` is a single,
  maximally-load-bearing fact that should be visible on the concrete
  type itself, not require tracing into the base to answer "does
  `AndRule([])` pass?". The residual risk (the literal and `decideOn`
  drifting apart) is accepted — backstopped by the 14
  `CancellationContractTests` plus the two pinned fixture rows
  (`vacuous_pass`/`unsatisfiable_threshold`), and a trivial literal is
  far less likely to be miscopied than branching logic would be.
  `Decide` bodies are early-return `if`s, not ternary chains —
  deliberate, for readability (these are genuinely different
  predicates, not a discriminant ladder in disguise; see `AGENTS.md`'s
  own dispatch-rule carve-out for guard clauses).
- **Python**: `CompositeRule(ABC, Generic[TContext])` with
  `@abstractmethod` on `_decide`/`_vacuous_result`.
  `ShortCircuitCompositeRule` same shape. `AndRule`/`OrRule` **stay
  real classes** (not factory functions, even though that's this
  package's own idiom for leaf predicates elsewhere) — they're already
  shipped, public, importable types; changing shape here would be a
  second, unforced break on top of `Data`→`sub_results`.
- **JS/TS**: `abstract class CompositeRule<TContext>`, private fields
  via `#`, matching `FunctionRule`'s existing `#predicate`/`#rules`
  convention. `decide` returns `boolean | undefined` (not `| null`) —
  matches `Rule.group`'s existing `string | undefined` idiom in this
  codebase. Honest caveat: `abstract` is compile-time-only in
  TypeScript, same as `Rule` itself being fully structural and erased
  at runtime — not a new risk this introduces.
- **Dart**: `abstract class CompositeRule<TContext>`, Dart 3
  super-parameter forwarding (`super.name, super.rules, {super.group}`)
  for the subclass constructors.

## 6 · Breaking change, deliberately

Pre-1.0 (`csharp-v0.4.0` and siblings are the only versions this ever
shipped in) — per this repo's own stated policy, a breaking change
pre-1.0 takes `MINOR`, not a special exception process. Accepted
explicitly by the user in this conversation ("if it breaks, that's
okay, that's the whole v0.* arg we hold"). What breaks concretely: any
consumer reading `andRuleResult.Data as List<RuleResult>` today stops
working — `Data` is `null`/`None`/`undefined` for every composite
result going forward, full stop.

## 7 · What this unlocks (secondary simplifications, not re-litigated here)

- **Mutation-testing survivors can't recur the same way.** The C# run
  found the *same* missing-cancellation-test gap four times (`AndRuleT`,
  `OrRuleT`, `RulesEngineT`×2) because the same logic was copy-pasted
  four times. One implementation means one place to get it right, once.
- **The structural-invariant chaos tests just added to
  `graduation_verdict` (this same branch, prior commit) partially
  become redundant, in a good way** — each language's new invariant
  checker exists specifically because there was no common base to
  trust, and dispatches by concrete rule type accordingly. Once
  `CompositeRule`'s own guarantees are proven once in the core
  library's own suite, a generic version needs no per-type dispatch at
  all. **Explicit open thread, not assumed free**: revisit those tests
  after `CompositeRule` ships rather than carrying duplicate coverage
  indefinitely.
- **Composite-authoring gets the same pit-of-success leaf-authoring
  already had** — implement `Decide` + `VacuousResult`, two narrow
  methods, instead of independently getting cancellation/short-circuit/
  `SubResults`/vacuous-truth all right by hand.
- **`docs/extending/new-rule-shape/`'s worked example gets
  dramatically shorter and more honest** — "extend `CompositeRule`,
  implement two methods" instead of hand-rolling a loop.
- **Open question, not decided here**: `AtLeastNRule` drops from ~25
  hand-rolled lines to ~8 lines extending a tested base. Worth asking,
  separately, whether it's general enough to promote from
  example-only into the shipped library. Not this PR's call to make
  unprompted.
- **Fewer independent places for the four ports to drift** — one
  reference algorithm (`CompositeRule`'s loop) to port correctly per
  language, not two independently hand-written ones per language each
  needing to agree with three siblings.

## 8 · Before writing a line of code: the "helpers" discussion

Explicitly not resolved yet — raised and deliberately deferred in
conversation, not forgotten:

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
  **Not yet implemented** — this plan is the record of the design
  discussion; §0 and this section get updated as code actually lands.
