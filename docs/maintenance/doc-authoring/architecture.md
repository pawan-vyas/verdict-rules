<!-- Title: Architecture Doc Authoring Template -->
# Architecture doc authoring template

> The structure `docs/architecture/` follows: a shared,
> language-agnostic design doc plus one concrete file per language. This
> builds on the general standard in [`README.md`](README.md) — read
> that first, especially the durable-shared-doc rule this template
> exists to keep `docs/architecture/README.md` honest against. Flatter
> than [`samples.md`](samples.md) or [`extending.md`](extending.md):
> one directory, not one per scenario, since there is exactly one
> architecture to document, not a growing set of worked examples or
> extension points.

## One directory, one shared doc, one file per language

`docs/architecture/`:

- **The shared doc** — `docs/architecture/README.md`. Every concept
  that holds identically in every language: the type structure (as a
  relationship diagram, not a class diagram in any one language's
  syntax), the execution model, the three run modes, extensibility, and
  the emptiness-vs-absence distinction. Written once, read by every
  language. Renders automatically on GitHub when linking to the
  directory itself.
- **The implementation** — `docs/architecture/<language>.md`, one per
  language that has documented its own concrete realization
  (`python.md`, for instance). Real type names, real method names, and
  a class diagram in that language's own type-system syntax — the
  concrete answer to whatever the shared doc leaves as "each language's
  own file names this."

A language that hasn't written its own file yet simply has no file
here; nothing reserves the slot.

## Shared doc (`README.md`) structure

Not a strict numbered order the way a sample or scenario spec has one —
this doc is closer to a reference than a narrative — but every section
follows the same rule: state the concept once, generically, and hand
off to "each language's own file in this directory" for the concrete
realization rather than naming one.

- **Title + blockquote framing** — what the doc covers and why it's
  separate from the narrative root `README.md` and each language's own
  quickstart. Never a pointer to a specific `<language>.md` by name —
  see [`README.md`](README.md)'s durable-shared-doc rule.
- **Design philosophy** — the responsibilities kept separate on purpose,
  and why structural typing over nominal typing is what makes the
  plain-predicate rule shape free.
- **Type structure**, **execution model**, **run modes** — one section
  each, with a diagram where the relationship is easier shown than
  told. Every diagram here is relationship-shaped (a Composite pattern,
  a decision tree), never a concrete class diagram in one language's
  own type syntax — that belongs in `<language>.md`.
- **Extensibility** and **testing** — what adding a new rule shape or
  run mode costs, and the emptiness-vs-absence distinction every
  language's SDK handles identically. Cross-links to
  [`../../extending/`](../../extending/README.md) and
  [`../../testing.md`](../../testing.md) rather than restating them.
- **Related docs** — the narrative root, maintenance, extending,
  testing, samples. Not each language's own concrete file — it's
  already one click away in this same directory's own GitHub listing,
  so a Related bullet for it is a touchpoint nothing requires.

## Implementation file (`<language>.md`) structure

- **Title + blockquote** pointing back to `README.md` ("read that
  first," with a relative link).
- A concrete realization of each shared-doc section that has one: the
  actual type/`Protocol`/interface names, the actual method names in a
  table, a class diagram in that language's own syntax, the actual
  exception type a lookup raises, the actual concurrency primitive that
  looks tempting and why it breaks the sequential guarantee.
- **Related** — the shared doc, and any sample or extending scenario
  this file's own method-name table gets cited from.

## Adding a new language's file

Add `docs/architecture/<language>.md` to the existing directory —
nothing in `README.md` changes. If `README.md` names a concept the new
language realizes differently enough to be worth a callout (a language
without structural typing, say), the new file is where that callout
goes — not an edit to the shared doc for one language's own exception.
