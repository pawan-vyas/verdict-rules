# v0.3.0: Context Generics + Test-Suite Parity — Master Plan

> The full resolved design lives here because the conversation that produced it
> won't be available to whoever picks this up. This is not a summary to skim —
> it's the thing to implement from. Nothing in `python.md`/`typescript.md`/
> `csharp.md`/`dart.md` re-derives a decision already settled here; each just
> applies it.

## Status

**Planning only. No source, doc, or fixture file has been touched yet.** Every
PR described below requires explicit sign-off before merging — no
`gh pr merge --admin` autonomy on this effort, regardless of how green CI is.
That's a deliberate departure from this repo's usual merge pattern, because
this is large enough, and foundational enough, to warrant it.

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

**Not yet designed. This is a required step before any language's Phase 3
(below) starts, and it should happen once, producing one language-agnostic
spec, not four independent inventions.**

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
- **Python's PR is where this spec gets written** (Python goes first) —
  every subsequent language's PR implements against that already-fixed
  spec, never invents its own variant.

## Release shape: sequenced, gated PRs — not one PR

Considered and rejected: one PR spanning all four languages. The
counter-argument that won: a diff this size (source, tests, two fixtures ×4,
docs, changelogs, skill notes, per language) is easier to review carefully as
four-plus-one focused units than as one bundle where the lowest-risk
languages sit gated behind the highest-risk one, and where a single red CI
check in any language blocks review of all four. Resolution: **sequenced
PRs, each independently reviewed and approved, but nothing tagged or
published to any registry until the full set has merged** — "features land
everywhere or nowhere" (this repo's own stated rule) preserved at the level
that actually matters, consumer-visible releases, without forcing one
unreviewable mega-diff.

**Order:**

1. **Python** — sets the reference pattern, is the litmus-test language, and
   is where the new fixture's spec gets written.
2. **TypeScript** — closest in risk profile to Python; sanity-checks the
   pattern transfers before the two higher-risk languages.
3. **C#** — the new coexistence pattern, own risk shape (arity coexistence,
   `IRule : IRule<...>`).
4. **Dart** — highest risk, the one real breaking migration; goes last so it
   benefits from three completed, working precedents.
5. **Shared docs** — the final PR; see `shared-docs.md`.

Each of the four language PRs is one complete, self-contained unit covering
*everything* for that language, in this internal order:

1. Test-suite parity fixes (from the real, documented gap below).
2. Port `graduation_verdict` (JS/C#/Dart only — Python's already exists,
   untouched).
3. Implement the new cross-language fixture (against the spec Python wrote).
4. The core `Rule<TContext>` migration itself.
5. That language's own docs/samples/quickstart/changelog/skill-agent-notes
   sweep (its own `<lang>.md` files only — shared, non-language-suffixed
   docs are `shared-docs.md`'s job, last).

## The real test-suite-parity gap — confirmed scope: Python's *whole* suite, not just the subtle-contract subset

**Parity means JS/C#/Dart's unit test suites match Python's, in full** — every
baseline case Python tests, not only the nine "easy to get subtly wrong"
contracts `docs/testing/README.md` names. That checklist is explicitly scoped
to subtle-correctness contracts, not a complete enumeration of Python's test
suite — confirmed by the real counts, checked directly rather than inferred
from the summary tables:

| | Test count |
|---|---|
| Python | **41** (`test_rule.py`: 16, `test_engine.py`: 25) |
| JS | 32 |
| C# | 29 |
| Dart | 27 |

Against the nine subtle-correctness contracts specifically, the picture is as
already found: JS is at full parity there, C# and Dart both specifically
lack a duplicate-rule-name test and a predicate-exception-propagation test
(C#'s own `docs/testing/csharp.md` self-documents this in these words: *"Not
yet covered: a duplicate-rule-name registration... and a predicate's own
thrown exception propagating uncaught... both real contracts... worth
porting before this package leaves 0.0.x"* — Dart's contract table omits the
same two rows). But the raw count gap (41 vs. 32/29/27) is wider than those
two named items account for — a first pass already surfaced likely
additional gaps by name-matching against Python's full list, not yet
confirmed by reading test bodies:

- **`test_predicate_receives_the_context`** — an explicit test that the
  context object passed to `evaluate()` reaches the predicate unchanged.
  Not obviously present by name in JS's or C#'s test files.
- **Engine-level vacuous-truth on an empty `RulesEngine`**
  (`test_empty_engine_run_all_vacuously_passes`) — distinct from `AndRule`'s
  own empty-list test, which the other languages do have. JS/C# have
  `AnEmptyEngineReportsNothing` (introspection: no names/groups exist) but
  not obviously a dedicated test that `run_all` on an empty engine still
  returns `passed: true`.
- **`test_results_are_always_truthy`** — worth checking what this actually
  asserts before deciding whether it's a real cross-language contract or a
  Python-idiom-specific check.

**This name-matching pass is not the audit — it's evidence the audit is
needed.** Each language's own plan (`python.md` confirming the baseline;
`typescript.md`/`csharp.md`/`dart.md`) now requires a real, test-body-level
diff against Python's full 41-test suite as the *first* action item of that
language's parity phase, not an assumption that the summary tables already
captured everything.

**No CI pipeline runs any of these suites today, for any language** — a
pre-existing, shared gap tracked in `docs/future_plan.md`. Out of scope for
this release unless explicitly pulled in; not assumed included.

## Merge gate

Every PR in this plan — all four language PRs and the final shared-docs PR —
requires explicit approval before merging. No autonomous merging on this
effort under any circumstance, including green CI, until the author says so
for that specific PR.

## Per-language and shared plans

- [`python.md`](python.md)
- [`typescript.md`](typescript.md)
- [`csharp.md`](csharp.md)
- [`dart.md`](dart.md)
- [`shared-docs.md`](shared-docs.md) — the final PR
