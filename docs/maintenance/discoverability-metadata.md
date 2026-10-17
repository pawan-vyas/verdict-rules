<!-- Title: Discoverability Metadata, Kept in Sync Across Registries -->
# Discoverability metadata, kept in sync across registries

> Why every package's keywords describe the same project in the same
> words, what earns a place in that list, and pub.dev's one real
> structural exception.

Each registry has its own field for search and browse: PyPI's
`keywords` and npm's `keywords` (both lists), NuGet's `tags` (via
`<PackageTags>`), and pub.dev's `topics` (a list). They describe the
*same project*, so drift between them — one page naming `async` and
another not — undersells whichever package is missing a term somebody
searched for, for no reason connected to that package.

## A term has to describe what this package does

Which is **evaluate**. It takes conditions and produces a result: a
judgment on whether something is true. It never acts on that judgment —
there is no step here that enforces a consequence. That is a deliberate
scope boundary, and the keyword list says so.

So `policy` and `policy-evaluation` are **absent by decision, not
oversight**. A policy names a rule *together with* what happens when
it is enforced, so advertising it invites someone looking for something
that executes a consequence. `decision` and `decision-engine` stay: a
decision is this package's own output, not an act of enforcement.

## The canonical list

`rules-engine`, `rule-evaluation`, `eligibility`, `decision`,
`decision-engine`, `async` — six terms, carried in full by every
registry without a structural cap.

`eligibility` is the one plain-English term rather than an
engine-specific one, and stays deliberately: it is a recurring real use
case, it appears throughout this repo's own evals and fixtures, and it
matches how a person searches rather than how the library describes
itself.

| Registry | Field | Carries |
| :-- | :-- | :-- |
| PyPI | `keywords` (list) | all six |
| npm | `keywords` (list) | all six |
| NuGet | `tags` via `<PackageTags>` | all six, semicolon-delimited — an MSBuild authoring convention; `dotnet pack` converts it to the space-separated form the `.nuspec` `tags` element uses |
| pub.dev | `topics` (list) | a curated **five** — see below |

## pub.dev's `topics` is the one structural exception

Capped at 5 entries, each 2–32 lowercase alphanumeric-or-hyphen
characters, no double hyphens, and pub.dev maintains a canonical-topic
list it merges close spellings into (documented at
[`dart.dev/tools/pub/pubspec`](https://dart.dev/tools/pub/pubspec),
browsable at [`pub.dev/topics`](https://pub.dev/topics)). Of this
project's terms only `async` is canonical there, and it has real browse
traffic behind it. PyPI's, npm's and NuGet's fields have no comparable
cap or vocabulary — confirmed by reading each registry's own reference.

So Dart carries `rules-engine`, `rule-evaluation`, `eligibility`,
`decision-engine`, `async`. Bare `decision` is the one dropped:
`decision-engine` is strictly more specific, mirrors `rules-engine`'s
own naming pattern, and does not cost the fifth slot on a near-duplicate
of a term already carried.

## Adding a term, or a registry

A new term goes into every uncapped registry at once, and you re-decide
what the capped one curates down to. Check it against the evaluate-only
scope above before adding it anywhere.

For a new registry, research its own field the way pub.dev's is
researched here — its exact name, whether it caps count or enforces a
vocabulary, whether it has a browsable canonical list worth preferring
terms from — rather than assuming it behaves like whichever registry was
handled most recently. See
[`../../.agents/memory/research-the-ecosystem-before-deciding-its-idiom.md`](../../.agents/memory/research-the-ecosystem-before-deciding-its-idiom.md).

## Related

- [`releases/README.md`](releases/README.md) — a metadata change ships
  like any other package update: a version bump, a changelog entry, a
  fresh publish.
