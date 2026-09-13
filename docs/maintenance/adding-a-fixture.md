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
  README.md              the data contract: what each file and each
                          expectation field proves, and why it's shared
  <data files>            whatever data format the scenario needs —
                          JSON today

docs/samples/<sample-slug>/
  README.md               the language-agnostic design spec: the
                           problem, the naive way, the design, what a
                           solution must demonstrate — same template as
                           every other sample, per
                           [`doc-authoring/samples.md`](doc-authoring/samples.md)
  <language>.md            one per language, pointing at that
                           language's own implementation below

<language>/examples/<name>/
  README.md                how to run it and test it, in that
                            language's own idiom
  docs/testing.md           language-specific testing detail worth
                            more than a paragraph (a chaos/oracle
                            suite's mechanics, for instance) — optional,
                            only if the implementation genuinely needs it
  <implementation files>
```

Three homes, three different things, matching the same split used
everywhere else in this repo: **data** lives with the fixture, **design**
lives with the sample spec, **code** lives in that language's own tree
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
consolidate under `docs/samples/<sample-slug>/` while the code does not.

## Steps

1. **Write the data contract first** — `fixtures/<name>/README.md` plus
   the data files themselves. This is the thing every language's port
   is proven against, so it exists before any implementation does.
2. **Write the sample spec** — `docs/samples/<sample-slug>/README.md`,
   following [`doc-authoring/samples.md`](doc-authoring/samples.md)
   exactly as any other sample would, referencing the fixture's data
   contract for the shared parity data rather than restating it.
3. **Build the first language's implementation** in that language's own
   `examples/<name>/` (or wherever that language's own conventions put
   a full tested project), reading the fixture's data files, and add
   `docs/samples/<sample-slug>/<language>.md` pointing at it.
4. **A second language's port** adds its own `examples/<name>/`
   directory and its own `docs/samples/<sample-slug>/<language>.md` —
   additive on every axis: a new fixture-consuming directory in that
   language's own tree, a new file in the sample-spec directory, nothing
   existing edited.

## What must stay identical across every language's port

Whatever the fixture's own `README.md` pins — for
[`graduation_verdict`](../../fixtures/graduation_verdict/README.md),
the verdict itself, how many rules actually ran (proving
short-circuiting survived the port), which rule is blamed, and both
absence-shaped lookup behaviors — every language's implementation must
reproduce exactly. See
[`adding-a-language.md`](adding-a-language.md)'s Stage 4 for where this
fits in a new language's own release ritual.

## Related

- [`doc-authoring/samples.md`](doc-authoring/samples.md) — the template
  a fixture's own sample spec and per-language docs follow.
- [`adding-a-language.md`](adding-a-language.md) — the ritual a new
  language SDK follows, which includes passing every existing fixture.
- [`../../fixtures/graduation_verdict/README.md`](../../fixtures/graduation_verdict/README.md) —
  the one fixture that exists today, as a worked example of this shape.
