<!-- Title: Features Land In Every Language -->
# A capability goes into every language, or into none

> Once more than one SDK ships, **no language runs ahead**. A feature is
> added to all of them or to none. This is what keeps "the same engine,
> in another language" a true statement rather than a family
> resemblance.

## Why it is worth the constraint

A capability present in one SDK and missing from three forces every
reader to ask which language a given piece of documentation is about
before trusting it. The shared design is the entire pitch; a feature gap
quietly retires it.

The constraint also does useful filtering. A new capability becomes a
piece of work in every language at once, so **if it is not worth doing
N times, that is evidence about whether it is worth doing at all**.
[`future_plan.md`](../../docs/future_plan.md) already sets a high bar
for additions; this raises it deliberately.

## What it does *not* mean

- **It does not reach language-idiomatic surface.** A `CancellationToken` in
  C#, an `AbortSignal` in JS, a `Result<T, E>` in Rust — these express the same
  engine the way each language expresses that kind of work. They are not
  capabilities one SDK has and others lack. See
  [`verdict-is-a-protocol-spec`](verdict-is-a-protocol-spec.md) for the
  three-layer split, and the test: *would this change what the shared fixture
  asserts?* If not, it is surface, and the language decides.

- **Fixing a defect in the reference implementation is not running
  ahead.** Corrections are expected to propagate. Languages that have
  not shipped yet inherit them for free, because the shared fixture is
  what they are built against.
- **Versions do not have to match.** Parity is proven by the fixture,
  not encoded in version numbers. Each language's version describes its
  own history — see
  [`releases-are-language-scoped`](releases-are-language-scoped.md).

## How it is enforced

Not by memory. A behaviour change shows up as a change to
`fixtures/graduation_verdict/`, and every language's suite fails until
it matches. A language cannot drift silently, which is the property that
makes the rule hold without anyone policing it.

Confirmed with the maintainer, 2026-09-12.
