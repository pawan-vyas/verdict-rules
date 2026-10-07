<!-- Title: Composite Sub-Rules Convention -->
# A composite's parts become readable, by convention rather than by type

> **Decided, not yet built.** A composite rule currently keeps the rules it
> was built from private in all four SDKs, so a rule *tree* cannot be walked
> before it is evaluated — only the *result* tree can, afterwards. This plan
> adds a sibling contract a composite satisfies, so one walk reaches every
> composite including ones this package never saw. Scheduled as its own PR
> after the branch currently in flight lands, per
> [`HANDOFF.md`](../../HANDOFF.md) §3.

## The gap, as verified in the source

Every composite stores its parts privately, and nothing public reaches them:

| Language | Today | Reachable another way? |
| :-- | :-- | :-- |
| Python | `self._rules` on `AndRule`/`OrRule`, `self._rule` on `NotRule` | By convention only, and no accessor exists |
| JS/TS | `#rules` / `#rule` | **No.** A true private field, so there is no back door at all |
| Dart | `_rules` / `_rule`, already `List.unmodifiable` | Library-private; unreachable from a consumer |
| C# | `internal SubRules` on `AndRule`/`OrRule`, `internal Rule` on `NotRule` | Internal, for the `DebuggerTypeProxy` only |

Two facts worth carrying forward rather than rediscovering:

- **C# already needs this capability internally** and already exposes it for
  inspection — just to the debugger. `NotRule<TContext>`'s own doc comment
  says the member exists "without widening this type's public surface", so
  the current privacy is a deliberate decision this plan reopens, not an
  oversight.
- **The internal naming is already non-uniform**: the two list-shaped
  composites say `SubRules`, the negation says `Rule`. That asymmetry is
  exactly what forces a walker into a type ladder, which is the thing this
  plan exists to avoid.

Nothing in the docs covers walking a rule tree today. The result tree is
covered thoroughly — see
[`architecture/`](../../docs/architecture/README.md#inspecting-a-composites-own-decision).
The asymmetry is that `RulesEngine` tells a caller what it holds
(`rule_names`/`group_names`), while a composite tells them nothing.

## Why it clears the bar, stated honestly

Against the two questions in
[`future_plan.md`](../../docs/future_plan.md#the-actual-test-not-does-it-sound-useful):

- **Real correctness subtlety — yes.** A consumer that resolves named rules
  through its own port, rejects duplicate names at build time, and records
  which rules a decision actually used, must enumerate the named rules inside
  a composite. Where it cannot, it does not fail: it reports *fewer* rules
  than were used, and its duplicate-name check misses the duplicates. Silent
  under-reporting on a passing build is the shape this question is for.
- **Repeated demand across two or more consumers — one so far.** A single
  consumer, with a concrete and specific requirement. **The maintainer judged
  the subtlety sufficient on its own**, which is a deliberate override of the
  second question and is recorded as such rather than dressed up as two.
  There is precedent: engine introspection shipped on one organically
  recurring need, and the lesson already recorded from `NotRule` is that this
  test's first question was once applied too narrowly.

## The shape

A **new sibling contract**, in each language's own idiom. `Rule` itself is
untouched, so nothing a consumer already implements breaks:

| Language | The contract | Member |
| :-- | :-- | :-- |
| C# | `interface ICompositeRule<TContext> : IRule<TContext>` | `IReadOnlyList<IRule<TContext>> SubRules { get; }` |
| Python | a second `@runtime_checkable Protocol`, `CompositeRule` | `sub_rules` property |
| JS/TS | an interface plus a type guard | `subRules` getter |
| Dart | `abstract interface class CompositeRule<TContext> implements Rule<TContext>` | `subRules` getter |

**The member is uniform across all three composites**, which is the whole
point: a negation reports a one-element list rather than a differently-named
single rule, so a walk needs no knowledge of which composite it is looking at.

C# as the reference, since it is the SDK where the member already exists:

```csharp
// ICompositeRule.cs -- new file. IRule<TContext> is unchanged.
namespace VerdictRules;

/// <summary>
/// A rule built from other rules, whose parts can be read without evaluating
/// it. Satisfied by AndRule, OrRule and NotRule -- and by a consumer's own
/// composite, which is what lets one walk reach a combinator this package
/// never saw.
/// </summary>
public interface ICompositeRule<TContext> : IRule<TContext>
{
    /// <summary>
    /// The rules this composite is built from, in evaluation order. Exactly
    /// one for a negation; empty for a vacuous And/Or, which is a valid
    /// composite with no parts.
    /// </summary>
    IReadOnlyList<IRule<TContext>> SubRules { get; }
}
```

`AndRule<TContext>`/`OrRule<TContext>` already hold the list, so the change is
the interface plus dropping `internal`. The negation stores its one-element
list once rather than allocating per call:

```csharp
public sealed class NotRule<TContext>(string name, IRule<TContext> rule, string? group = null)
    : ICompositeRule<TContext>
{
    private readonly IRule<TContext>[] _subRules = [rule];

    /// <inheritdoc />
    public IReadOnlyList<IRule<TContext>> SubRules => _subRules;
}
```

A consumer then walks on **one capability test, not a type ladder**, and the
same walk reaches their own composites:

```csharp
static void Walk<T>(IRule<T> rule, Action<IRule<T>> visit)
{
    visit(rule);
    if (rule is ICompositeRule<T> composite)
        foreach (var part in composite.SubRules)
            Walk(part, visit);
}
```

## Non-goals, and what this deliberately does not do

- **No visitor.** It would have to put `Accept` on `Rule` itself, breaking the
  standing promise that a custom rule needs only a name, a group and an
  evaluate. Its case set is also closed, so a consumer's own composite has no
  case to land in — the Open/Closed violation the dispatch rule exists to
  prevent, expressed as an interface. Once the parts are readable a consumer
  can write a visitor over them in a few lines, so this is the smaller and
  more enabling primitive.
- **No change to `Rule`.** Adding a required member to the contract every
  consumer implements is a breaking change, and an optional one is what the
  sibling contract already expresses more honestly.
- **The parts are rules, not results.** This says nothing about evaluation and
  adds no guarantee about what ran; that remains the result surface's job.
- **A leaf stays a leaf.** `FunctionRule` does not satisfy the new contract,
  so "is this structure or is this a terminal check" becomes answerable
  without inspecting concrete types.

## Decisions already made

- Option chosen: a published convention, over exposing the three built-ins
  alone. Exposing only the built-ins leaves a consumer's own combinator
  opaque, which is the half of the reported problem that matters most — the
  reporter had already built their own walkable combinators and the remaining
  gap was *their* users' composites.
- Name: `SubRules` and its per-language spellings, for symmetry with the
  already-public `SubResults` on a result. Rules to sub-rules, results to
  sub-results.
- Timing: its own PR, after the branch in flight lands.

## Decisions, resolved during implementation

- **C#: property or method?** A result spells its derived collection views as
  methods (`GetDecidedBy()`) because a get-only collection property gets
  serialized by `System.Text.Json`. Rules are never serialized, so a property
  is defensible here — but the asymmetry needs to be a recorded decision in
  [`api-surface-allowlist.yaml`](../../docs/maintenance/api-surface-allowlist.yaml)
  or the concept map, not something a later reader "fixes".
- **Non-generic C# arity element type.** `IRule` derives *from*
  `IRule<IReadOnlyDictionary<string, object?>>`, so the non-generic `AndRule`
  cannot promise `IReadOnlyList<IRule>` — its inner composite genuinely holds
  the base element type, and a caller may have passed one that is not an
  `IRule`. **Resolved: it exposes the base element type**, so a walk written
  against `ICompositeRule<TContext>` reaches both arities. The practical
  consequence, found by running the samples rather than reasoning about it: a
  consumer's composite declared only `ICompositeRule<Dict>` will not compile
  into the *non-generic* `AndRule`, whose constructor takes
  `IReadOnlyList<IRule>` -- because `IRule` is the more derived type. Build
  such a tree with the generic arity, or declare the composite
  `: IRule, ICompositeRule<Dict>`. Documented in the scenario's C# page.
- Whether a vacuous composite's empty list needs calling out in each
  language's own notes, or only in the shared contract. **Resolved: the
  shared contract plus each changelog**, not the per-language notes, which
  stay one screen.

## A non-goal, settled on the record

**Rebuilding a composite over new parts** (a `WithSubRules(parts)` member) is
not planned, and not merely deferred. Reading parts is *total* -- every
composite can say what it is built from. Rebuilding is *partial*, and the
contract cannot express that: a composite may hold state that is not in its
parts, and this repo's own documented threshold rule in
[`new-rule-shape/`](../../docs/extending/new-rule-shape/README.md) is exactly
that shape. Asking an implementor to promise "the same composite over new
parts" is a promise nothing verifies and that is silently broken by returning
one that dropped its threshold -- the feature's own failure mode would be a
wrong answer with no error, which is the shape this surface exists to prevent.

It would also change the contract's nature: `SubRules` is a read a walker
needs, while a rebuild is a construction capability most composites have no
reason to offer, and putting it on the same interface would force every
implementor to answer for it. If it ever happens it is a separate, opt-in
contract on its own evidence.

The safer pattern, for a consumer that needs substitution: rebuild the
composites whose construction you know, and make anything else a **build
problem**. That fails loudly at build time, where a silent rebuild would fail
quietly at decide time.

## Landing checklist

A public API addition in four languages, so the sweep in
[`a-behaviour-change-is-a-documentation-change`](../memory/a-behaviour-change-is-a-documentation-change.md)
applies in full:

- [ ] The contract plus the three composites, in all four SDKs
      ([`features-land-in-every-language`](../memory/features-land-in-every-language.md))
- [ ] Each SDK's API snapshot refreshed, and C#'s `PublicAPI.Unshipped.txt`
      entries added — see [`api-snapshots.md`](../../docs/maintenance/api-snapshots.md)
- [ ] A row in the cross-language concept map, since this is shared surface
      rather than one language's own
- [ ] A test per language that a walk over a *consumer-defined* composite
      reaches its parts — the capability the built-ins alone cannot prove
- [ ] An extending scenario, since this enables one that did not exist:
      walking a rule tree before evaluating it
      ([`extending/`](../../docs/extending/README.md))
- [ ] Four `agent-notes.md`, `SKILL.md`'s guarantee list, and a
      `plugin.json` bump
- [ ] An eval: "a walker that silently skips a composite it does not
      recognise" is a new way to be subtly wrong, which earns one
      ([`evals/`](../../skills/verdict-workspace/evals/README.md))
- [ ] Four `CHANGELOG.md` entries, each beside its own manifest
- [ ] Consider whether the fixtures should pin a walk, given that a fixture
      catches divergence but never four SDKs agreeing on a wrong answer

## Related

- [`future_plan.md`](../../docs/future_plan.md) — the test this was weighed
  against, and where this entry moves once it ships.
- [`verdict-is-a-protocol-spec`](../memory/verdict-is-a-protocol-spec.md) —
  why each language spells the contract its own way rather than copying C#'s.
- [`new-rule-shape/`](../../docs/extending/new-rule-shape/README.md) — the
  existing scenario a consumer's own composite is built from, and the one the
  new scenario sits beside.
