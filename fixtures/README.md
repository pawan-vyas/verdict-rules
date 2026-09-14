<!-- Title: Cross-Language Parity Fixtures -->
# Cross-language parity fixtures

> The shared inputs and expected outcomes every language's own port
> must reproduce exactly — the evidence that a port is the same engine
> in another language, not something that merely resembles it. One
> directory per fixture, data only: no implementation code lives here.
> See [`../docs/maintenance/adding-a-fixture.md`](../docs/maintenance/adding-a-fixture.md)
> for the template a new one follows.

| Fixture | Proves |
| --- | --- |
| [`graduation_verdict/`](graduation_verdict/README.md) | Short-circuiting, vacuous-truth polarity, and emptiness-vs-absence, across a curriculum of subjects, students, and edge-case policies. |

## Why this directory exists separately from `docs/samples/` and each language's own tree

A fixture's **data** (here), a sample's **design spec**
(`docs/samples/<slug>/README.md`), and a language's own **code**
(`<language>/examples/<name>/`) are three different things that change
for three different reasons, so each lives where the thing that reads
it expects to find it — a language's own test runner and CI trigger
expect code inside that language's own top-level directory, not
centralized here. `adding-a-fixture.md` has the full reasoning; this
directory only ever holds the first of the three.

## Adding a fixture

A fixture is a heavier commitment than a sample or an extension
scenario — every language's port, present and future, must reproduce
it exactly, forever. It's the right tool when a behavior needs to be
proven across a wide, structured space of inputs (short-circuiting,
polarity, absence) rather than illustrated with one worked example.
See [`../docs/maintenance/adding-a-fixture.md`](../docs/maintenance/adding-a-fixture.md)
for the shape a new one follows, if the need arises.

## Related docs

- [`../docs/maintenance/adding-a-fixture.md`](../docs/maintenance/adding-a-fixture.md) —
  the template for a new fixture.
- [`../docs/maintenance/adding-a-language.md`](../docs/maintenance/adding-a-language.md) —
  the ritual a new language SDK follows, which includes passing every
  existing fixture here.
- [`../docs/testing/README.md`](../docs/testing/README.md) — where a
  fixture fits among the other testing layers.
- [`../docs/samples/README.md`](../docs/samples/README.md) — the
  language-agnostic design specs, including the ones built on top of a
  fixture here.
