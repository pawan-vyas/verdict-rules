<!-- Title: Research The Ecosystem Before Deciding Its Idiom -->
# Research an ecosystem's own conventions before deciding what its idiom is

> [`verdict-is-a-protocol-spec`](verdict-is-a-protocol-spec.md) says each SDK
> first-classes its own language's idioms. **This is the practice that makes
> that principle real**: an idiom is a fact about an ecosystem, discoverable
> from its own documentation — not something to infer from the language you
> happen to know best.

## The failure this prevents

Reaching for one answer across every ecosystem because it is familiar, and
calling the result consistency. It is not consistency; it is one ecosystem's
convention imposed on four, and it reads as foreign in three of them.

## The worked example

Choosing a changelog format, the obvious answer was "Keep a Changelog
everywhere." Reading each ecosystem's own documentation instead:

| | What is actually true |
| :-- | :-- |
| **pub.dev** | *Parses* the file. Documents headings all H1 or all H2, containing the version, optionally `v`-prefixed. Brackets are not documented as supported |
| **PyPI** | Parses nothing. Keep a Changelog is the community norm, linked via `project.urls` |
| **npm** | Parses nothing, no changelog tab. Ships only if listed in `files` |
| **NuGet** | **No changelog file at all.** Release notes are a `PackageReleaseNotes` metadata string in the csproj |

One of the four does not use the mechanism being designed for. That is not
discoverable by analogy, only by reading.

It also changed the design rather than just the wording: because formats
differ legitimately, the shared extractor matches a heading *containing* the
version instead of one equal to a fixed shape — which is both more tolerant and
simpler than the version it replaced.

## How to apply it

Before deciding any per-language convention — naming, versioning, packaging
layout, error types, documentation format — read **that ecosystem's own docs**,
and record what they actually say in the language's plan or in
`docs/adding-a-language.md`. Cite it, so the next person can check whether it
has changed rather than re-deriving it.

Where a convention genuinely has no ecosystem answer, say so and pick a
sensible default. The verdict skill's own changelog is an example: nothing
hosts it, so there is no convention to research, and Keep a Changelog is
simply the reasonable choice.

Confirmed with the maintainer, 2026-09-12.
