<!-- Title: Package README Authoring Template -->
# Package README authoring template

> The structure every package's own `README.md` follows. This builds on
> the general standard in [`README.md`](README.md) — read that first.
> Unlike a sample or extending scenario, these files don't consolidate
> under `docs/`: each lives inside its own language's package
> directory, because that language's own packaging tool expects to
> find it there and renders it as that registry's own package
> description. Same reasoning as
> [`adding-a-fixture.md`](../adding-a-fixture.md)'s "why code stays in
> each language's own tree."

## Why one template across files that never sit next to each other

A person comparing two packages of the same polyglot project reads them
minutes apart, on two different registry pages, and forms one
impression of the project either way. A reader who has to relearn the
shape — where the install command is, where the guarantees are stated,
whether there's a deeper-docs table at all — on every package is
reading N unrelated projects that happen to share a name, not one
project in N languages. The fix isn't a shared file (packaging tooling
forbids that — see the blockquote above); it's a shared shape, enforced
by review rather than by any single file two branches could conflict
over.

## The shared skeleton, in order

1. **Title** — `# Verdict — <Language>` (`# Verdict — Python`,
   `# Verdict — JS/TS`), never the bare distribution name as the H1. The
   registry's own page chrome already shows the package name in the
   title bar and the URL; repeating it as the H1 wastes the one line
   every reader sees first on a fact they already know, instead of using
   it to say what this actually is. A language whose own name ends in a
   character markdownlint's `MD020` reads as a stray closed-heading
   marker (C#'s `#`) needs one more word after it — `# Verdict — C# SDK`
   — rather than dropping the language name to dodge the lint failure.
2. **Blockquote** — one line naming what this package is, nothing else.
   Neither a pointer to the top-level `README.md`'s narrative "why" nor
   an explanation of why the registry renders this file belongs here —
   both read as the document talking about itself on a page a consumer
   landed on to install something, the same failure "No narration about
   the document itself" already names for every other doc in this repo.
   `## Where to go next`'s own `docs/architecture/` row already covers
   the "why," in more depth than a narrative pointer would — a second
   row saying it again would be dead weight next to it, not a
   different fact. Every link in the file still has to be an
   absolute GitHub URL rather than a relative path — none of this
   file's sibling files travel with an install, so a relative link that
   resolves on GitHub 404s the moment a reader is looking at it from
   PyPI, npm, or wherever else — but that's a fact for whoever authors
   this file, not something the file needs to explain to its own reader.
3. **`## Install`** — the install command, the import/require statement,
   and — only where the registry actually has one — a note on a naming
   split between the distribution name and the import name (Python's
   `pip install verdict-rules` → `import verdict`, because the plain
   name was already taken). Skip the note entirely where there's no
   split to explain; don't manufacture one.
4. **One first-example section** — a single runnable example with real
   output shown, not a fragment, that demonstrates the whole path: build
   a rule or two, wrap them in a `RulesEngine`, and run one by name —
   never stop at a bare composite's own `.evaluate()`. `RulesEngine` is
   one of the five named primitives this package is built around; an
   example that skips straight from `AndRule` to `.evaluate()` shows
   four of the five and never introduces the one that holds a whole rule
   set. `docs/quickstart.md`'s own "one complete example" follows the
   identical shape one level deeper — the same primitives, the same
   run-by-name pattern, a second worked scenario rather than a
   restatement of this one; the two should never demonstrate a different
   subset of the API from each other. Heading text is that language's
   own idiom for "here's the whole thing" (Python's "A first rule," for
   instance) rather than a generic "Usage" — see
   [`README.md`](README.md)'s rule against narrating the document
   instead of showing the thing.
5. **Zero or more package/language-specific highlight sections** — see
   "What's genuinely package-manager and language specific" below. Not
   required, not capped at one; add exactly as many as that language's
   own idiom has something real to show that isn't true everywhere.
6. **`## What it guarantees`** — the execution-model contracts, stated
   in substance identically everywhere (sequential evaluation, vacuous
   truth's polarity, emptiness-is-not-absence, opaque `data`, zero
   runtime dependencies), each bullet using that language's own real
   type and method names. This is the actual value proposition — the
   one section a skimming reader most needs, so it never gets cut for
   space the way a "nice to have" section would.
7. **`## Where to go next`** — a table linking the deeper docs
   (quickstart, architecture, extending, maintenance, testing, samples,
   examples), every link an absolute GitHub URL pinned to that
   package's own release tag — see
   [`versioned-links.md`](../versioned-links.md) and
   `scripts/check_shipped_links.py`, which enforces this mechanically.
   No row for the top-level `README.md` itself: `docs/architecture/`
   already covers "why it's shaped this way," so a second row pointing
   at the same fact in narrative form is redundant next to it, not a
   different one.
License is deliberately not a mandated section: every registry this
project ships to already surfaces it from that package's own manifest
metadata (PyPI's `license` field, npm's `license` field, and so on), so
a `## Licence` heading repeats what the registry's own page chrome
already shows. Add one only if a package's own registry doesn't surface
license metadata on its page.

**`## Development` is deliberately not a mandated section either** —
dropped, not just unmandated, after it turned out to fail this
template's own three-leaks test below: `CONTRIBUTING.md`'s own
per-language section already carries the same commands, plus the
actual testing/PR bar around them, and a subset of that repeated here
is contributor-facing content on a page a consumer landed on to
install the package, not to change it. `## Where to go next` already
links `docs/maintenance/` for "changing this package itself," so
nothing is lost by dropping it — the skeleton is seven sections, not
eight.

## What's genuinely package-manager and language specific

The skeleton above is fixed; these are the legitimate places a single
package's README earns a section none of the others need, because the
fact itself is genuinely true of that one registry or that one
language — not because a writer felt like adding color:

- **A naming-split note** (Install, step 3) — only where the registry
  actually forced one.
- **A browser/CDN distribution section** — only where the registry
  actually serves a browser-loadable artifact (npm, via unpkg/jsDelivr/
  esm.sh). A registry with no browser story has nothing to write here.
- **A structural-vs-nominal-typing highlight** — worth its own section
  where the language's own type system makes the "no base class, no
  registration" property notable (TypeScript's `interface`, Python's
  `Protocol`); skip it where the language doesn't have an equivalent
  worth demonstrating.
- **A custom-type callout** — only where the language's own standard
  library has no built-in equivalent worth reusing (JS/TS's
  `UnknownLookupError`, filling the gap where Python has `KeyError`,
  C# has `KeyNotFoundException`, Dart has `ArgumentError` — see the next
  section for why that comparison itself doesn't belong on this page).

If a future package's own registry or language has a comparable, real
fact none of the others share, it earns its own highlight section the
same way — this list is illustrative, not closed.

## Three leaks to check for before calling a package README done

Each reads as legitimately helpful in isolation, which is exactly why
none of them are caught by skimming — the test is never "is this
sentence true," it's "does this sentence help someone using *this*
package":

- **No comparison to a sibling language's SDK.** A sentence like "the
  Dart and C# SDKs have nominal typing and require an explicit
  `implements`," or naming what Python/C#/Dart each raise where this
  package throws its own error type, is dead weight on this page: a
  reader who just installed this package is not choosing between it and
  a sibling they haven't installed, and the claim itself can go stale
  silently the moment that sibling's own design changes, since nothing
  about editing *that* sibling's code re-checks *this* package's
  registry page. State the fact about this language on its own terms.
  The comparison, if it's worth making at all, belongs in
  [`../../architecture/`](../../architecture/README.md) or
  [`../../extending/`](../../extending/README.md) — a repo doc a reader
  chose to open because they wanted the comparison, not one handed to
  every installer whether they asked or not.
- **No self-narration about this package's own maturity or roadmap.** A
  sentence like "this is `0.0.1` — correct, but minimal, the rest
  arrives before `0.1.0`" is a promise with an expiry date wired into a
  page that is effectively permanent: registries don't retroactively
  edit an old version's rendered page, mirrors cache it, and a local
  install's own copy on disk never updates itself. The day the promised
  version ships, every one of those copies is quietly lying. State what
  the package does; let the version number the registry already
  displays carry "how far along this is."
- **No internal maintainer-workflow leaking through.** A sentence
  describing how this repository generates or CI-enforces something
  about its own docs (a script that regenerates a hash, a test that
  fails if a tag drifts) describes *this repository's own* tooling to a
  reader who doesn't have it checked out and has no reason to run its
  scripts. It's dead information in the one place attention is
  scarcest, and if the maintainer workflow is ever renamed or removed,
  the shipped page is permanently wrong with no mechanism ever catching
  it — unlike a repo doc, which a link checker or a reader filing an
  issue can catch. State what a consumer does; the "how we keep it
  correct" belongs in
  [`../../../CONTRIBUTING.md`](../../../CONTRIBUTING.md) or that
  package's own dev-tooling docs, not its landing page.

## The same discipline extends to a language's first CHANGELOG entry

A package's `CHANGELOG.md` isn't the registry's own rendered page, but it
is the same kind of artifact for this purpose: pinned to a release tag,
permanently public, read by someone with no way to know it was ever
edited. A language's very first entry is where the self-narration leak
above is most tempting, because a first release genuinely is minimal in
substance — but the entry should still say only what shipped, in the
same past-tense, no-promise voice as every later entry. State what's in
this version; never a sentence about why this particular version is
minimal, what it's a placeholder for, or when the rest is expected to
arrive. An expiring promise is exactly as wrong here as on the README,
for the identical reason: the day the promised version actually ships,
every already-tagged copy of the entry that named it is quietly wrong.

## Adding a new package's README

Follow the skeleton above exactly for the shared sections; add only the
package/language-specific highlight sections that language's own idiom
genuinely earns, per the list above. Check the result against the three
leaks before calling it done.

## Related

- [`README.md`](README.md) — the general standard this template builds
  on.
- [`versioned-links.md`](../versioned-links.md) — why every link in a
  shipped package README is pinned to a release tag, never `main`.
- [`../../architecture/`](../../architecture/README.md) — where a
  genuine cross-language design comparison belongs instead of a
  package's own landing page.
