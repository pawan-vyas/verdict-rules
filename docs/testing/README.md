<!-- Title: Verdict Testing Guide -->
# Verdict — Testing Guide

> The canonical reference for how this package is tested, and what a
> change must add to its own test suite before it's done. If you're
> looking for *what to change where* for a given kind of change, see
> [`../maintenance/`](../maintenance/README.md) — this doc is
> specifically about proving that change correct. The contracts and
> checklist below hold identically in every language this package ever
> ships for.

**A second, complementary layer exists in every language that has it**:
a `graduation_verdict` example project, checked against the shared
cross-language parity fixture at
[`../../fixtures/graduation_verdict/README.md`](../../fixtures/graduation_verdict/README.md).
The core suite below proves narrow, unit-level contracts in isolation;
that example project proves the primitives compose correctly *together*,
the way a real consumer actually uses them, across a far wider space of
inputs than anyone would hand-curate — a regression net for this
package itself, not just a worked sample.

## What actually needs proving, not just executed

Line coverage is necessary but not sufficient here — `verdict`'s whole
value proposition is a handful of contracts that are easy to satisfy by
accident while getting subtly wrong, and a test that only checks the
final `passed` boolean can pass while missing exactly the thing that
matters. Each language's own file in this directory names the concrete
tests that prove these:

- **Short-circuiting is a behavioral contract, not an optimization.**
  `AndRule`/`OrRule` must not just *return* the right answer, they must
  *never evaluate* the rule that comes after the deciding one. Prove
  this with a call-counter or a mutable list a later rule's predicate
  would append to: assert the tracking list is still empty, not just
  that `passed` came out right. A bug that evaluates every sub-rule but
  still computes the correct `passed` value would pass a weaker,
  boolean-only test and silently defeat the entire reason these two
  classes exist (see
  [`../architecture/README.md`](../architecture/README.md#execution-model-sequential-not-concurrent)).
- **Vacuous truth has a polarity, and it's easy to get backwards.**
  An empty `AndRule` passes, an empty `OrRule` fails. Both need a
  dedicated test precisely because "empty means pass" and "empty means
  fail" are both defensible in isolation and only one is correct per
  class — a new composite or run mode needs the same explicit test, not
  an assumption.
- **Absence is not emptiness, and gets the opposite treatment.** An
  unknown rule name or group label is a *lookup that matched nothing*,
  not an empty set: a group exists only because some rule declared it,
  so nothing matching can only be a typo or a stale name. The strict
  lookup form raises, while the try-prefixed form returns an explicit
  absence value for the same lookup. **Both halves need testing**: an
  unknown lookup returning a *pass* is the failure this distinction
  prevents, and a try-prefixed form that raised would defeat its only
  purpose. So does the distinction between absence and `passed=False`
  — collapsing them makes a typo indistinguishable from a legitimate
  rejection.
- **A fallback must be proven not to fire when it shouldn't.** Testing
  the try-prefixed lookup only against an *absent* group looks complete
  and isn't: the dangerous direction is a *present* group wrongly
  returning absence, because a caller writing `or True` would then
  approve something that actually failed. Test the whole matrix —

  | group state | `or True` | `or False` | strict |
  | :-- | :-- | :-- | :-- |
  | present, passing | `True` | `True` | `passed=True` |
  | **present, failing** | **`False`** | **`False`** | `passed=False` |
  | absent | `True` | `False` | raises |

  — because the middle row is the one that matters and the one a
  single-case test omits. The general lesson generalises past this API:
  an assertion written against only the branch you're thinking about
  often reduces to a tautology, and a green test proves nothing until
  you've watched it fail for the right reason.
- **The engine's run-everything and run-by-group modes never
  short-circuit — prove the opposite of the point above.** Assert every
  rule's name shows up in the results, even after an earlier one
  already failed. This is the direct mirror-image regression guard: a
  future refactor that accidentally makes these run methods stop early
  would be just as wrong as a composite failing to stop early is.
- **A run's results never flatten a composite's own sub-results.** A
  short-circuited composite still contributes exactly one entry to the
  outer run's results, holding exactly the sub-results that actually
  ran — not padded to the full list, not flattened into the caller's
  own results — the invariant
  [`../architecture/README.md`](../architecture/README.md#type-structure)
  documents as "one entry per *top-level* rule, regardless of internal
  composition depth."
- **Registering two rules under the same name silently keeps the last
  one, the same way a map with a repeated key would.** Worth a named
  test specifically so a future change to that behavior (raising on a
  duplicate name, say) is a deliberate, visible decision, not an
  accidental regression nobody notices.
- **A predicate's own exception is never caught, anywhere.** Easy to
  break with good intentions — a well-meaning catch-and-continue added
  to make the engine "more robust" would silently turn a real bug into
  a wrong, quiet result instead of a stack trace. Every entry point
  needs its own dedicated test: a composite's own `evaluate`, and each
  of the engine's own run modes.

## Checklist for a new contribution

| You added... | Your test must also prove |
| --- | --- |
| A new concrete `Rule` shape (composite or otherwise) | Plain delegation to whatever it wraps, **plus**, if it's a composite: short-circuit behavior in both directions it can short-circuit on (if any), and its vacuous-input behavior (empty list, or whatever "nothing configured" means for this shape) |
| A new `RulesEngine` run mode | That it evaluates the right subset, its own vacuous case (nothing matches the selector), and whether it short-circuits or not — state which, explicitly |
| A change to `RuleResult`/`RunResult`'s shape | Every existing test still passes unmodified (a required-field addition breaks every construction site — see [`../maintenance/before-merging-checklists.md`](../maintenance/before-merging-checklists.md#consumer-impact-checklist-for-a-shape-change)) plus a new assertion covering whatever the new field is for |
| A change to `Rule`'s required attributes/signature | Re-run every real consumer's own test suite, not just this package's — see the consumer-impact checklist linked above |

New tests live in whichever existing test file matches where the new
code lives (per
[`../maintenance/README.md`](../maintenance/README.md#where-to-make-a-change)),
or a new file named the same way if the new code lives in a new module.

## Related docs

- [`../maintenance/`](../maintenance/README.md) — where a given kind of
  change actually lives, and the consumer-impact checklist a shape
  change needs before merging.
- [`../architecture/README.md`](../architecture/README.md) — the
  execution-model reasoning the short-circuit tests above are proving.
- [`../extending/`](../extending/README.md) — testing guidance for code
  you write *using* verdict lives with your own project's conventions,
  not here; this doc is specifically about testing verdict itself.
- [`../../fixtures/graduation_verdict/README.md`](../../fixtures/graduation_verdict/README.md) —
  the shared, cross-language parity fixture behind the second testing
  layer described above.
