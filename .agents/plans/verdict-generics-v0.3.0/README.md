# v0.3.0: Context Generics + Test-Suite Parity — Master Plan

> The full resolved design lives here because the conversation that produced it
> won't be available to whoever picks this up. This is not a summary to skim —
> it's the thing to implement from. Nothing in `python.md`/`typescript.md`/
> `csharp.md`/`dart.md` re-derives a decision already settled here; each just
> applies it.

## Status

**Core migration implemented, on its own PR, awaiting sign-off.** Every PR
described below requires explicit sign-off before merging — no
`gh pr merge --admin` autonomy on this effort, regardless of how green CI is.
That's a deliberate departure from this repo's usual merge pattern, because
this is large enough, and foundational enough, to warrant it.

Real progress, confirmed against the actual repo rather than assumed:

- Test-suite parity (this plan's phase 1) — C# done and merged
  ([PR #88](https://github.com/pawan-vyas/verdict-rules/pull/88)); JS
  ([PR #89](https://github.com/pawan-vyas/verdict-rules/pull/89)) and Dart
  ([PR #90](https://github.com/pawan-vyas/verdict-rules/pull/90)) open and
  green, awaiting sign-off.
- `graduation_verdict` fixture port (this plan's phase 2) — JS
  ([PR #91](https://github.com/pawan-vyas/verdict-rules/pull/91)), C#
  ([PR #92](https://github.com/pawan-vyas/verdict-rules/pull/92)), and Dart
  ([PR #93](https://github.com/pawan-vyas/verdict-rules/pull/93)) all open
  and green, awaiting sign-off.
- **The core `Rule<TContext>` migration itself is implemented, tested, and
  documented across all four languages, on a single PR**
  ([PR #94](https://github.com/pawan-vyas/verdict-rules/pull/94)) — cut from
  `main` before the six PRs above had merged, so it will need a rebase once
  they land. Comprehensive per-language tests (17 Python, 8 JS, 13 C#, 9
  Dart, on top of each language's own idiom coverage), each language's own
  architecture-doc section, the shared `docs/architecture/README.md`
  addition, the new `docs/extending/reusing-a-rule-across-contexts/`
  scenario (one page per language, every code sample compiled/run-verified),
  each language's own changelog, and the skill's version bump are all in
  that PR already — see its own description for the full breakdown.
- **The release shape below has changed from what was originally planned** —
  see "Release shape," which now reflects the sequencing actually confirmed,
  not the four-language-PRs-plus-shared-docs shape this plan started with.
- The new cross-language fixture (step 4 below) has not started — by design,
  it waits for PR #94 to merge and prove the design out first.

## What this release actually is

Two things, deliberately bundled because #76 gives the generics work the
clarity it needs across all four languages before it starts, not because
they're the same kind of change:

1. **Test-suite parity (issue #76).** Bring JS/C#/Dart up to the same
   contract-coverage bar Python already documents in
   [`docs/testing/`](../../../docs/testing/README.md), and port the
   `graduation_verdict` fixture (today Python-only) to all three.
2. **Generic context for `Rule`** (issue #33). `Rule<TContext>` — the
   long-parked design question — resolved and implemented across all four
   languages, on top of the now-equal-depth test suites from (1).

All four languages land at **v0.3.0** — a fact for *this plan* to track, not
something any public-facing text ever states. Each language's own
`CHANGELOG.md` describes only that language's own change, as a standalone
fact, exactly as if it shipped independently — no mention of the other three
languages, no "coordinated release," no "joint jump" language, no
cross-language narration anywhere public (changelogs, docs, code comments,
commit messages included). This repo's own `AGENTS.md` already states the
principle this follows: *"Public surfaces are not working surfaces... state a
decision as a fact."* The shared version number across four independent,
factual changelog entries is a coincidence a careful reader could notice —
it is never something the text itself points out.

## The resolved design (canonical reference)

### Scope: context only, not outcome

`Rule<TContext>` — the context a rule reads from becomes generic. `RuleResult`
and `RunResult` **stay exactly as they are today**, `Data`/`data` included,
un-genericized. This was a real, separate question (`RuleResult<TData>`),
deliberately rejected:

- It's the other axis entirely — context is input (read at every predicate
  site, today unsafely); `Data` is output (written once, already documented
  as opaque, a caller already expects to runtime-check it).
- It has its own hard sub-problem, proven against real shipped code, not
  hypothetically: `docs/extending/domain-adapter-module/python.md`'s own
  worked example has `result.data` be a `list[RuleResult]` (a composite's
  sub-results) while each `r.data` inside it is a domain object
  (`RateLimitStatus`) — two incompatible shapes under one field, today,
  working precisely *because* it isn't typed. Genericizing `Data` would force
  every rule that might ever compose under one `AndRule` to share one
  `TData`, which most real rule sets don't naturally have.

### `Rule<TContext>` — per-language mechanics (none of these are interchangeable; each is the correct answer for that language specifically)

**C#** — arity coexistence, closed-generic specialization for the interface only:

```csharp
// New:
public interface IRule<TContext>
{
    string Name { get; }
    string? Group { get; }
    Task<RuleResult> EvaluateAsync(TContext context, CancellationToken ct = default);
}

// Existing IRule: unchanged behavior, now ALSO a real specialization —
// closing TContext to a concrete type, NOT the open IRule<TContext>
// inheriting IRule (that direction is mechanically broken: it would force
// every typed rule to also implement an unimplementable dict-shaped method).
public interface IRule : IRule<IReadOnlyDictionary<string, object?>> { }
```

`RulePredicate<TContext>`, `FunctionRule<TContext>`, `AndRule<TContext>`,
`OrRule<TContext>`, `RulesEngine<TContext>` are each a **new, independent**
generic sibling — same name, different arity, matching `IComparer`/
`IComparer<T>` (verified: `IComparer<in T>` has no base interface at all —
[source](https://github.com/dotnet/dotnet/blob/main/src/runtime/src/libraries/System.Private.CoreLib/src/System/Collections/Generic/IComparer.cs)),
**not** `IEnumerable`/`IEnumerable<T>` (which only works because that
interface's `T` is in output/covariant position; `Rule<TContext>`'s is input).
The existing non-generic classes' internals stay completely untouched — each
generic sibling is a fresh, independent implementation, not a wrapper around
the old one. Fully additive; zero breaking changes.

**TypeScript** — explicit always, **no default type parameter**:

```typescript
export interface Rule<TContext> {   // no `= Context` default
  readonly name: string;
  readonly group?: string | undefined;
  evaluate(context: TContext): Promise<RuleResult>;
}
```

A default was considered and rejected: it would make "deliberately chose
dict-context" and "forgot to type this" look identical in source, which is a
real inconsistency in a codebase this strict elsewhere (`strict: true`,
`noUncheckedIndexedAccess: true`). Dict-context becomes `Rule<Context>`,
written out every time. Real migration cost, but narrow — this file's own
doc comment already discourages `implements Rule` in favor of a bare object
literal (*"any object of the right shape is a Rule. No implements clause, no
base class, no registration"*), so the cost lands on explicit type
annotations, not on every rule.

**Python** — non-breaking at runtime, by construction:

```python
TContext = TypeVar("TContext")

@runtime_checkable
class Rule(Protocol[TContext]):
    name: str
    group: str | None
    async def evaluate(self, context: TContext) -> RuleResult: ...
```

`FunctionRule`, `AndRule`, `OrRule`, `RulesEngine` become `Generic[TContext]`.
Zero runtime change — Python erases generics; `isinstance(x, Rule)` only ever
checked attribute/method presence, never a type parameter, so every existing
structural rule keeps satisfying `Rule` unconditionally. `FunctionRule("x", predicate)`
has its `TContext` *inferred* from `predicate`'s own annotation; an untyped
predicate infers `Any`, matching today's looseness exactly. Document
`Rule[dict[str, Any]]` as the explicit "same strictness as before" spelling
for anyone who wants it. Python cannot enforce "always explicit" at the
language level the way TS now does — recommend pyright's
`reportMissingTypeArgument` / mypy's `disallow_any_generics` in docs as the
equivalent discipline for consumers who want it, since verdict-rules itself
can't force a caller's own lint config.

**Dart** — the one real, unavoidable breaking migration, and it's narrower
than it first looks:

```dart
abstract interface class Rule<TContext> {
  String get name;
  String? get group;
  Future<RuleResult> evaluate(TContext context);
}
```

Dart has neither TS's default parameters nor C#'s arity coexistence — every
`implements Rule` becomes `implements Rule<Map<String, Object?>>` explicitly,
a real source change. But `FunctionRule('x', predicate)`/`AndRule('x', [...])`
call sites keep working via the same constructor-argument type inference
Python and TS get — the break lands specifically on `implements Rule`
declarations, which Dart's own existing `agent-notes.md` already documents as
the rarer, discouraged pattern (*"most rules should use `FunctionRule`...
it's the escape hatch back to shape-based rules"*, precisely because Dart
lacks structural typing for multi-member interfaces).

### Composition: sub-rules must share the exact `TContext` — enforced, not a convention

`AndRule<TContext>`/`OrRule<TContext>` require every sub-rule to be
`Rule<TContext>` — the *same* concrete type, not merely compatible ones. No
mixing. This is what the type system buys for free that dict-context never
could: today, two rules secretly expecting different shapes of dict can be
combined under one `AndRule` and only fail at runtime on a missing key; once
generic, that's a compile error.

**Cross-context reuse goes through an explicit adapter, never a special case
in `AndRule` or the engine:**

```csharp
public sealed class ProjectingRule<TOuter, TInner> : IRule<TOuter>
{
    private readonly IRule<TInner> _inner;
    private readonly Func<TOuter, TInner> _project;
    public async Task<RuleResult> EvaluateAsync(TOuter outer, CancellationToken ct = default)
        => await _inner.EvaluateAsync(_project(outer), ct);
}
```

One small, new, additive class per language. This is the way out when a rule
written against one context needs to run inside a composite built on another
— proxy it, don't bend `TContext` uniformity to accommodate it.

### Engines: `RulesEngine` and `RulesEngine<TContext>` coexist — neither replaces the other

- **`RulesEngine` (dict, unchanged)** — the answer for a genuinely
  heterogeneous set of rules: many different decision points, different
  natural shapes, addressed by name. This was already dict-context's
  first-class use case (see "Dict-context stays first-class," below) and
  nothing about adding generics takes it away.
- **`RulesEngine<TContext>` (new)** — for a cohesive family of rules that
  genuinely share one context (e.g., every eligibility check that makes up
  "is this order valid," all reading `OrderContext`).

**The fully-heterogeneous, richly-typed, runtime-string-keyed catalog — many
independently-typed decision points in one named, data-bindable registry
(the actual shape `edict`'s own Decision Point/Binding/Pin model wants) — is
explicitly out of scope for `verdict` itself.** No generics design in any
language makes `engine.RunNamedAsync(nameFromAConfigRow, ctx)` fully static;
the type information doesn't exist until the row is read, in any language,
ever — the same reason every DI container uses type-erased storage plus a
checked cast on retrieval:

```csharp
public sealed class DecisionCatalog
{
    private readonly Dictionary<string, object> _byName;
    public void Register<TContext>(string name, IRule<TContext> rule) => _byName[name] = rule;
    public async Task<RuleResult> RunNamedAsync<TContext>(string name, TContext context, CancellationToken ct = default)
        => await ((IRule<TContext>)_byName[name]).EvaluateAsync(context, ct);
}
```

That's `edict`'s layer to build (informed by this pattern), not `verdict`'s —
recorded here so it isn't re-litigated when `edict` is actually scaffolded.

### Dict-context stays first-class, permanently — never demoted to "escape hatch"

Real, legitimate use case, not a fallback for the untyped: a rule meant to be
reused across genuinely different aggregate shapes (an "is_verified_user"
check wanted inside both a checkout flow and an onboarding flow, where the
fact lives at a different nesting path in each) is naturally served by
dict-context, where a strictly-typed rule would need a `ProjectingRule`
adapter at every reuse site. Two tools for two situations — the same way
`FunctionRule` (wraps any predicate) and `AndRule`/`OrRule` (a specific
composition strategy) already coexist as equals, not as primary-and-legacy.

### Why this converges even though it needed four different mechanisms per language, and why the registry boundary was never going to be static

Recorded in full because it's the answer to a real "should we even do this"
gut-check that came up mid-design, not because it changes anything above:
needing different *mechanics* per language for the same semantic outcome
isn't a red flag — it's `docket`'s own already-stated principle
("polyglot semantics precede language-specific implementation") working as
intended. The registry/catalog boundary staying dynamic is the well-known
*gradual-typing boundary* every language with an "add static types to a
dynamic-and-name-addressed system" story eventually hits (Python's own
`typing`/`Any` boundary, TypeScript's whole reason for existing on top of
JS). `verdict`'s specific tension is real — its actual value in the
`data-driven-rule-construction` pattern is precisely that shapes aren't known
ahead of time — but the resolution (type the leaf fully, keep the
name-keyed registry boundary dynamic and checked) is the standard shape this
class of problem always resolves to, not a compromise unique to this
package.

## Fixtures

### `graduation_verdict` (existing) — untouched, the litmus test

Stays exactly as it is in Python, unchanged, unaudited-for-generics on
purpose. Porting it to JS/C#/Dart (parity work, below) uses the *existing*,
dict-based design — none of the three new ports reference `TContext`
anywhere. Passing, unchanged, after the generics migration lands in that
language is the concrete proof that dict-context really is just
`Rule<dict>`, not a design that quietly broke.

### A new cross-language fixture, proving the generic path — design deferred, direction recorded

**Not yet designed. Confirmed to come *after* the core migration (Release
shape, step 3, below) lands and its own unit tests prove the design out —
not before it, as originally planned here.** It should still happen once,
producing one language-agnostic spec, not four independent inventions.

Direction, as discussed, to ground that design session rather than starting
from nothing:

- Production-grade, not a toy — richer than a straight typed port of the
  graduation domain.
- A generalized business-flow domain in the spirit of a marketplace/
  onboarding flow — structurally similar to real systems, but **never
  naming any specific real consuming project**, per this repo's own
  standalone-portability rule (`AGENTS.md`: *"no naming a specific
  consumer's package, module, or internal vocabulary"*).
- Must exercise, concretely, in its own test assertions: a typed
  `Rule<TContext>`, a dict-context `Rule` coexisting in the same domain
  (the hybrid case), a `ProjectingRule` adapter reusing one rule across two
  differently-shaped contexts, and both `RulesEngine` forms.
- Lives at `fixtures/<name>/README.md`, mirroring
  `fixtures/graduation_verdict/README.md`'s own structure (policies,
  test subjects, edge cases, expected outcomes) — exact name TBD in that
  same design session.
- Written once, in Python first, producing the fixed spec every other
  language then implements against rather than inventing its own variant —
  this stays true even though it now happens as its own step after the
  single core-migration PR (Release shape, step 4), not inside a
  per-language PR.

## Release shape: revised — small parity/fixture PRs, then one single PR for the migration itself

**This section originally proposed one PR per language, each bundling all
five steps below internally, plus a trailing shared-docs PR.** That shape is
superseded — confirmed explicitly, not re-derived from first principles —
by the sequencing in this section. The reasoning for *why* four separate
per-language PRs made sense for test-suite parity specifically still holds
(smaller, independently reviewable, no language gated behind another's
red CI); what changed is that the core migration itself cannot honestly be
split the same way, because it is one coherent source change across four
languages plus the shared docs describing it, and a partial merge of that
particular change would leave `main` in a state no single language's docs
accurately describe.

**Confirmed order:**

1. **Test-suite parity — four independent PRs, one per language**, each a
   small, focused, drop-and-recreate rebuild against Python's own
   `test_rule.py`/`test_engine.py` (see "The real test-suite-parity gap,"
   below, for the full approach). No particular order between JS/C#/Dart;
   Python's own suite is already the done reference.
2. **`graduation_verdict` fixture port — three independent PRs, one each for
   JS, C#, Dart.** Confirmed to stay separate rather than folding into the
   single migration PR below: this is additive fixture work using the
   *existing*, dict-based design (see "Fixtures," below) — none of it
   touches `TContext` anywhere, so none of it carries the "must land
   coherently across all four languages" constraint that forces step 4 into
   one PR. No particular order between the three.
3. **The core `Rule<TContext>` migration — a single PR spanning all four
   languages.** Contains, for all four languages together: the generics
   implementation itself; comprehensive unit tests including edge cases and
   each language's own idiom gotchas; each language's own
   docs/samples/quickstart/changelog/skill-agent-notes sweep; **and** the
   shared cross-language docs sweep that was originally planned as a
   trailing fifth PR (`shared-docs.md` — root `README.md`,
   `docs/architecture/`, `docs/testing/README.md`, `docs/extending/` shared
   READMEs, `docs/maintenance/`, the skill's shared content plus a
   `plugin.json` version bump). All of it lands and merges together. Once
   this step starts, work through implementation, tests, and docs without
   stopping for interim check-ins — only the final merge needs sign-off.
4. **The new cross-language fixture (see "Fixtures," below) — resolved only
   after step 3 has landed and its own unit tests have proven the design
   out.** This reverses the original plan's internal ordering, which had the
   new fixture implemented *before* the core migration in each language's
   own PR. It is the one open point left in this whole program on purpose.

"Features land everywhere or nowhere" (this repo's own stated rule) is still
honored at the level that actually matters — nothing tags or publishes to
any registry until the full set for that step has merged — it now applies
per-step rather than describing one single release train end to end.

## The real test-suite-parity gap — confirmed scope: Python's *whole* suite, not just the subtle-contract subset

**Parity means JS/C#/Dart's unit test suites match Python's, in full** — every
baseline case Python tests, not only the nine "easy to get subtly wrong"
contracts `docs/testing/README.md` names. That checklist is explicitly scoped
to subtle-correctness contracts, not a complete enumeration of Python's test
suite — confirmed by the real counts, checked directly rather than inferred
from the summary tables:

**The formula, confirmed: `4 × N + Σ(languageᵢ × Mᵢ)`** — one universal
baseline `N`, ported 1:1 into every language, plus however many `M`
language-idiom-specific tests each language legitimately needs (a
`CancellationToken` propagation test in C#, a structural-typing demonstration
in JS, whatever each language's own idiom calls for — none of which are
gaps, and none of which get ported anywhere else).

`N = 40`, not 41 — corrected directly, not assumed. The first pass at this
audit found that Python's own `test_results_are_always_truthy` (in
`test_engine.py`) proves a Python-only idiom (the `x or default` fallback
pattern, meaningless in a language without implicit truthiness) with no
universal counterpart — it was never actually part of the reference other
languages port against, just miscounted as if it were. Already fixed:
extracted into Python's own `tests/test_python_idioms.py`, its own `Mᵢ` of
1, alongside `test_rule.py` (16) + `test_engine.py` (24) = 40. Every other
language's own idiom file follows the same pattern.

| | Baseline suite ported 1:1 (target: `N` = 40) |
|---|---|
| Python | 40 (`test_rule.py`: 16, `test_engine.py`: 24) — reference, done |
| JS | 32 today, not yet re-audited against the corrected `N` |
| C# | 29 today, not yet re-audited against the corrected `N` |
| Dart | 27 today, not yet re-audited against the corrected `N` |

**Approach, confirmed: rebuild each non-Python language's test suite from
scratch as a strict, file-for-file, test-for-test port of `test_rule.py`/
`test_engine.py`, rather than auditing the existing suite for gaps and
patching them.** Considered and rejected: diffing Python's full test list
against each language's existing file by name, then patching in whatever's
missing (an early pass at this did surface real gaps this way — a missing
context-delivery test, a missing engine-level empty-vacuous-pass test — both
now moot). Rebuilding from the reference is simpler and more reliable than
auditing an independently-evolved suite for drift: if it doesn't exist in
the new file, there's nothing to have drifted. Concretely, per language:

1. Delete the existing test file(s) entirely.
2. Recreate a strict 1:1 mirror of `test_rule.py`/`test_engine.py` — same
   file split, same test classes, same tests, same assertions, translated
   into that language's own idiom. This is `Rulesᵢ` = 40, by construction,
   not by audit.
3. Recreate a **separate**, clearly-labeled idiom file holding exactly the
   tests that have no Python counterpart on purpose (a `CancellationToken`
   test in C#, whatever JS's and Dart's own idioms call for) — never
   ported anywhere else, never counted against `N`.
4. This structure is what makes the whole thing auditable forever: the 1:1
   files are the ones to check against Python; the idiom file is
   explicitly not part of that check.

**The pass criteria is still coverage, not a literal count.** `N = 40` is
the reference every language mirrors 1:1, but a language's own idiom can
still express one of those 40 contracts differently (a C# `[Theory]`
covering several of Python's separate `test_` functions in one
parameterized case is full parity, not a shortfall) — this repo's own
principle already says so
("each language may use idiomatic mechanisms... must implement the same
semantic contracts"). The actual done-criteria for this phase is: every
contract Python's suite proves is proven *somewhere* in the other language's
suite, confirmed by reading test bodies against Python's, not by comparing
counts.

**Each language's own `docs/testing/<lang>.md` gets updated in the same
step that adds the test it describes, not deferred to a later docs pass.**
As each contract gap closes during the parity audit, that language's own
"which test proves which contract" table gains the new row immediately —
letting doc updates pile up until a later "docs sweep" is exactly how they'd
turn into an untracked backlog instead of landing alongside the work that
made them true.

**No CI pipeline runs any of these suites today, for any language** — a
pre-existing, shared gap tracked in `docs/future_plan.md`. Out of scope for
this release unless explicitly pulled in; not assumed included.

## Merge gate

Every PR in this plan — every test-parity PR, every fixture-port PR, and the
single core-migration PR — requires explicit approval before merging. No
autonomous merging on this effort under any circumstance, including green
CI, until the author says so for that specific PR.

## Per-language and shared plans

- [`python.md`](python.md)
- [`typescript.md`](typescript.md)
- [`csharp.md`](csharp.md)
- [`dart.md`](dart.md)
- [`shared-docs.md`](shared-docs.md) — no longer a trailing fifth PR; its
  content lands as part of the single core-migration PR (Release shape,
  step 3, above)
