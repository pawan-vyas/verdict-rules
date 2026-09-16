<!-- Title: Discoverability Metadata, Kept in Sync Across Registries -->
# Discoverability metadata, kept in sync across registries

> Why every package's keywords/topics/tags describe the same project in
> the same words, what actually belongs in that list, and the one real
> structural exception — pub.dev's hard cap — rather than a difference
> to work around per package.

Every registry this project ships to has its own field for
search/browse discoverability: PyPI's `keywords` (a list, in
`pyproject.toml`), npm's `keywords` (a list, in `package.json`), NuGet's
`tags` (set via `<PackageTags>` in a `.csproj`), and pub.dev's `topics`
(a list, in `pubspec.yaml`). These describe the *same project*, so
drift between them — one registry's page naming `async` and another's
not — undersells whichever package is missing a term a reader searched
for, for no reason connected to that package itself.

## What actually belongs in the list: only what this package does

Before syncing terms across registries, each term has to earn its spot
on what this package *actually does* — which is evaluate. `verdict`
takes conditions and produces a `RuleResult`/`RunResult`: a judgment on
whether something is true. It never acts on that judgment — there is
no step here that enforces a consequence, applies an outcome, or does
anything once a verdict is reached. That is a real, deliberate scope
boundary, not an oversight, and the keyword list should say so rather
than imply otherwise.

That's why **`policy` and `policy-evaluation` are dropped from the
canonical list entirely**, not merged into `decision`/`decision-engine`
as redundant synonyms the way an earlier pass of this reasoning had it.
A "policy" names a rule *together with* what happens when it's
enforced — the evaluate-and-then-act shape, not the evaluate-alone
shape this package actually has. Advertising `policy` invites a reader
looking for something that executes a consequence to a package that
only ever answers a yes/no question. `decision` and `decision-engine`
stay: a decision is verdict's own *output* — the judgment itself, not
an act of enforcing anything — so both sit on the right side of that
line regardless of whether they're phrased as the bare or the
qualified form.

## The current union (source of truth)

`rules-engine`, `rule-evaluation`, `eligibility`, `decision`,
`decision-engine`, `async` — 6 terms, carried in full by every registry
with no structural cap on this field (PyPI, npm, NuGet). `eligibility`
stays despite being the one plain English term in the list rather than
an engine-specific one: it's a genuine, recurring target use case for
this library, appearing throughout this repo's own evals and samples,
and it's the one term that matches how an actual person searches
rather than how the library describes itself internally.

A future addition goes into every registry without a structural cap;
check whether it changes what Dart (or any future capped registry)
should curate down to 5, and check it against the evaluate-only scope
above before adding it at all.

## The one real structural exception: pub.dev's `topics`

**pub.dev's `topics` field**: capped at 5 entries, each 2-32 lowercase
alphanumeric-or-hyphen characters, no double hyphens, and pub.dev
maintains a canonical-topic list it merges close spellings into
(documented at
[`dart.dev/tools/pub/pubspec`](https://dart.dev/tools/pub/pubspec) and
browsable at [`pub.dev/topics`](https://pub.dev/topics)) — confirmed
that none of this project's own terms are canonical topics there
except `async`, which has real browse traffic behind it (96 packages
at the time this was checked). PyPI's, npm's, and NuGet's own fields
have no comparable cap or vocabulary — confirmed by reading each
registry's own reference, not assumed.

So Dart's `topics:` carries a **curated 5**, not the full 6:
`rules-engine`, `rule-evaluation`, `eligibility`, `decision-engine`,
`async`. Bare `decision` is the one dropped, for the same
bare-vs-qualified reasoning that already ruled out `policy` in favor of
nothing: `decision-engine` is strictly more specific, mirrors
`rules-engine`'s own naming pattern, and doesn't cost the fifth slot on
a near-duplicate of a term already carried.

## NuGet, once C# actually publishes

`VerdictRules.csproj`'s `<PackageTags>` is
`rules-engine;rule-evaluation;eligibility;decision;decision-engine;async`
— the same full 6-term union PyPI's and npm's own fields already carry,
with no structural cap the way pub.dev's `topics` has. `policy` was
dropped on `plan/csharp-sdk` itself before that branch's first release,
same reasoning as every other language: it names a rule together with
what happens when it's enforced, and this package only ever evaluates,
never acts on the result.

The one real syntactic difference from PyPI/npm: `PackageTags` is an
MSBuild property, so it's **semicolon-delimited**
(`rules-engine;rule-evaluation;...`), not a list the way
`pyproject.toml`'s and `package.json`'s `keywords` are. `dotnet pack`
converts that to the space-separated form the underlying `.nuspec`
`tags` element actually uses when it generates the package — the
semicolons are an MSBuild authoring convention, not what NuGet itself
stores.

## Before publishing a new language

Research that registry's own discoverability field the same way this
page describes pub.dev's — its exact name, whether it caps count or
enforces a vocabulary, and whether it has a browsable canonical list
worth preferring terms from — rather than assuming it behaves like
whichever registry was handled most recently. See
[`../../.agents/memory/research-the-ecosystem-before-deciding-its-idiom.md`](../../.agents/memory/research-the-ecosystem-before-deciding-its-idiom.md)
for why this matters generally, not just for this one field.

## Related

- [`releases/README.md`](releases/README.md) — the shared release
  pipeline this metadata change goes through like any other package
  update: a version bump, a changelog entry, a fresh publish.
