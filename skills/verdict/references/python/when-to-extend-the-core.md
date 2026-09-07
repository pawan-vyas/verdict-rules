# When to Extend the Core (vs. Your Own Adapter Code)

Verdict earns its own package treatment by staying small and
domain-agnostic. Most ideas that feel like "verdict should have this"
don't actually need to change verdict itself — Extension Recipe 2
(a genuinely new rule shape) already lets you build almost anything
verdict doesn't ship, with zero risk to the package everyone else
depends on. Apply this test before proposing an actual change to
verdict's own source:

## The test: two questions, not one

1. **Does this have real correctness subtlety worth centralizing?**
   `AndRule`/`OrRule` earned their place because short-circuiting,
   vacuous-truth polarity, and the "never flatten sub-results" rule are
   all genuinely easy to get subtly wrong, and worth having tested once
   rather than re-implemented slightly differently by every consumer.
   Most "convenience" combinators (a `NotRule`, an `XorRule`, an
   "at-least-N" rule) have **no** such subtlety — they're a handful of
   obvious lines, no different in kind from `AtLeastNRule` in
   `extension-recipes.md`'s Recipe 2. Not clearing this bar isn't a
   rejection of the idea — it's already free via Recipe 2, at zero cost
   and zero risk to the shared package.
2. **Is there repeated, real demand across multiple independent
   consumers**, not just one call site that would be marginally more
   convenient? A structural observation from reading code ("this could
   theoretically help") is not the same as a demonstrated need. Watch
   for the same pattern showing up a second or third time in genuinely
   different consumers before promoting it.

A "yes" to both is what actually justifies a change to verdict's own
source. A "yes" to the first but "not yet" to the second is worth
noting for later, not building now — that's speculating ahead of real
usage, the same mistake as baking one consumer's domain knowledge into
the package.

## What this means in practice

- **New boolean combinators** (`NotRule`, `XorRule`, "at least N of M")
  almost never clear the bar — see Extension Recipe 2, write it in your
  own code.
- **A generic cross-cutting concern** (a timeout wrapper around a
  rule's predicate, a retry policy) usually belongs as a documented
  pattern/recipe your own team follows, not a core feature — it has
  some real design nuance (fail-open vs. fail-closed, cancellation
  semantics) but that nuance is about *your* policy choice, not
  something verdict should decide on your behalf.
- **A genuine gap in what the engine exposes** — read-only introspection
  on `RulesEngine` (what rules/groups does it hold), or a shared,
  tested helper for walking a `RunResult` tree into a plain structure —
  can clear the bar if it's come up more than once, since the
  underlying data already exists and the correctness question (how do
  you tell a composite's own nested sub-results apart from an opaque
  domain payload) is real and worth solving once.
- **A performance opportunity with a real design tradeoff** — e.g.
  whether `run_all`/`run_group` could evaluate concurrently since they
  never short-circuit anyway — is worth flagging explicitly (it would
  need to be opt-in, since it changes the ordering guarantees any
  future side-effecting rule set could rely on), but shouldn't be built
  speculatively before an actual consumer's rule set is large and
  I/O-bound enough for it to matter.

When a change does clear both questions, it's a change to verdict's own
source (not a consumer's adapter code) — treat it with the weight that
implies: it affects every consumer at once, usually with no version pin
protecting anyone from a mistake (verdict is commonly consumed via an
editable local path dependency, not a published, versioned package).
Grep every consumer you know about for `from verdict import` and
re-run each one's own test suite before considering the change done.
