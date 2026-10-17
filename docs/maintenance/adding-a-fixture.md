<!-- Title: Adding a Fixture -->
# Adding a fixture

> The template for a new cross-language parity fixture — a full,
> tested example project (like `graduation_verdict`) whose data and
> expected outcomes every language's own port must reproduce. Not about
> maintaining an existing fixture — that lives with the fixture itself,
> under its own `fixtures/<name>/README.md`. This is the shape a
> genuinely new one follows, if the need arises.

## The shape

```text
fixtures/<name>/
  README.md              the scenario's single home: the problem, the
                          design, and the data contract — what each file
                          and expectation field proves, and why it's shared
  <data files>            whatever data format the scenario needs —
                          JSON today

<language>/examples/<name>/
  README.md                how to run it and test it, in that
                            language's own idiom
  docs/testing.md           language-specific testing detail worth
                            more than a paragraph (a chaos/oracle
                            suite's mechanics, for instance) — optional,
                            only if the implementation genuinely needs it
  <implementation files>
```

Two homes, matching the same split used everywhere else in this repo:
**data and design** live together with the fixture, which is the
scenario's single home, and **code** lives in that language's own tree
where its own build and test tooling expects to find it.

## Why code stays in each language's own tree

A language's own test runner (`pytest`, a JS test runner, `dotnet
test`) has its own opinionated discovery and rootdir conventions, and a
CI workflow's own diff-based trigger typically watches only that
language's own top-level directory. Relocating a language's
implementation out of its own tree to sit under `fixtures/<name>/`
would fight both — the same reason
[`packages-and-changelogs.md`](packages-and-changelogs.md) has each
language's own package stay inside that language's own directory rather
than centralizing. Docs carry no such constraint, which is why they
consolidate under `fixtures/<name>/` while the code does not.

## Steps

1. **Write the data contract first** — `fixtures/<name>/README.md` plus
   the data files themselves. This is the thing every language's port
   is proven against, so it exists before any implementation does.
2. **Write the design into that same README** — the problem, the naive
   approach and its cost, and the design a correct implementation
   demonstrates. The fixture's README is the scenario's single home, so
   the design sits beside the contract rather than in a separate spec.
3. **Build the first language's implementation** in that language's own
   `examples/<name>/` (or wherever that language's own conventions put a
   full tested project), reading the fixture's data files rather than
   restating any of them, and add a row to
   [`../../fixtures/README.md`](../../fixtures/README.md)'s index.
4. **A second language's port** adds its own `examples/<name>/`
   directory and nothing else — additive on every axis: a new
   fixture-consuming directory in that language's own tree, nothing
   existing edited.

## What must stay identical across every language's port

Whatever the fixture's own `README.md` pins — for
[`graduation_verdict`](../../fixtures/graduation_verdict/README.md),
the verdict itself, how many rules actually ran (proving
short-circuiting survived the port), which rule is blamed, the flattened
leaf views on both a composite's result *and* a run's, the one-level
explanation of a composite's own verdict, and both absence-shaped lookup
behaviours — every language's implementation must reproduce exactly.

**Pin every view a reader could reach for, not only the one the first
implementation happened to use.** A run's failing-leaf view went
unpinned while a composite's was asserted eight ways, and the defect
that hid there was found downstream rather than here. See
[`adding-a-language.md`](adding-a-language.md)'s Stage 4 for where this
fits in a new language's own release ritual.

## Related

- [`adding-a-language.md`](adding-a-language.md) — the ritual a new
  language SDK follows, which includes passing every existing fixture.
- [`../../fixtures/`](../../fixtures/README.md) — every fixture that
  exists, as a worked example of this shape.
