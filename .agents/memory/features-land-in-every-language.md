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
[`fixtures/`](../../fixtures/README.md), and every language's suite fails
until it matches. A language cannot drift silently, which is the property
that makes the rule hold without anyone policing it.

## What the fixture cannot catch

It catches **divergence**, never **agreement on the wrong answer**. A
defect introduced in the reference implementation and faithfully ported
three times leaves every suite green, because the fixture asserts what
the four agree on and they agree.

This is not hypothetical. `RunResult`'s failing-leaves view filtered the
flattened leaves instead of forwarding to each result's own view, in all
four SDKs, and three suites contained a test asserting the defect as
intentional. Nothing failed. The fixture had no failing-leaves
expectations at all, so the one mechanism that would have caught it was
silent on the question — and mutation testing cannot help either, since a
mutant is only killed by an assertion that already exists.

**So: a new view or guarantee is not covered until the fixture asserts
it.** When adding one, add the expectation to the fixture data in the
same change, and pick the value by hand from the scenario rather than
from what the implementation returns. A fixture populated from the code's
own output asserts only that the code is self-consistent.

Confirmed with the maintainer, 2026-09-12; the fixture-blindness section
added after the failing-leaves defect.
