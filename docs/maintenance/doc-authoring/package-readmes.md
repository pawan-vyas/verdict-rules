<!-- Title: Package README Authoring Template -->
# Package README authoring template

> The structure every package's own `README.md` follows. Builds on the
> general standard in [`README.md`](README.md) — read that first. These
> files don't consolidate under `docs/`: each lives in its own language's
> package directory because that language's packaging tool expects it
> there and renders it as the registry's package description. Same
> reasoning as [`adding-a-fixture.md`](../adding-a-fixture.md)'s "why code
> stays in each language's own tree."

A reader comparing two packages of one polyglot project reads them
minutes apart on two registry pages and forms a single impression. Having
to relearn where the install command is, or whether there's a deeper-docs
table at all, makes them N unrelated projects sharing a name. Packaging
tooling forbids a shared file, so the shape is shared instead — enforced
by review, not by a file two language branches would conflict over.

## The skeleton — seven sections, in order

| # | Section | Contents, and the constraint that is easy to get wrong |
| :-- | :-- | :-- |
| 1 | **Title** | `# Verdict — <Language>`. Never the bare distribution name: the registry's own chrome already shows it in the title bar and the URL, so repeating it spends the first line a reader sees on a fact they have. A language name ending in a character `MD020` reads as a stray closed-heading marker (C#'s `#`) takes one more word — `# Verdict — C# SDK` — rather than dropping the language name to dodge the lint. |
| 2 | **Blockquote** | One line naming what this package is. Not a pointer to the top-level `README.md`'s narrative "why" (section 7's `docs/architecture/` row covers it in more depth), and not an explanation of why the registry renders this file — both are the document talking about itself to someone who landed here to install something. |
| 3 | **`## Install`** | Install command, then the real import statement. A note on a distribution-name/import-name split *only* where the registry forced one (`pip install verdict-rules` → `import verdict`); never manufacture one. |
| 4 | **One first-example section** | A single runnable example with real output — build a rule or two, wrap them in a `RulesEngine`, run one **by name**. Stopping at a bare composite's `.evaluate()` never introduces the one primitive that holds a whole rule set, which is what the reader came for. Heading in that language's own idiom ("A first rule"), never a generic "Usage". |
| 5 | **Highlight sections** | Zero or more, per the table below. Not required, not capped at one. |
| 6 | **`## What it guarantees`** | The execution-model contracts, identical in substance everywhere, each bullet in that language's real type and method names: sequential evaluation, vacuous truth's polarity, emptiness-is-not-absence, opaque `data`, zero runtime dependencies. This is the value proposition and the section a skimmer most needs — it never gets cut for space. |
| 7 | **`## Where to go next`** | A table linking the deeper docs (quickstart, architecture, extending, maintenance, testing, the worked fixtures). No row for the top-level `README.md` — `docs/architecture/` already carries "why it's shaped this way." |

**Every link is an absolute GitHub URL pinned to that package's own
release tag.** No sibling file travels with an install, so a relative link
that resolves on GitHub 404s from PyPI, npm, or anywhere else, and an
unpinned one shows a reader docs for an API their version lacks. See
[`versioned-links.md`](../versioned-links.md);
`scripts/check_shipped_links.py` enforces it.

`scripts/check_package_readmes.py` discovers packages rather than listing
them, and enforces the mechanically-checkable subset: the order of
sections 3, 4, 6 and 7, an install snippet naming the real registry
command for the package's actual name as read from its own manifest, and
section 4's example executed against the real built artifact — a wheel, a
packed tarball, a `.nupkg` — installed into a fresh scratch directory.
That last one is the only check that catches a README describing an API
the shipped package doesn't have, so a fragment that cannot run on its own
fails the gate rather than shipping. Sections 2 and 5 are free-form by
design and no script checks them. That example and the language's own
`quickstart.md` (`doc/quickstart.md` in Dart, `docs/` elsewhere) follow
the identical shape one level apart — same primitives, same run-by-name
pattern, the quickstart a *second* worked scenario rather than a
restatement. The two must never demonstrate a different subset of the API.

### Two sections deliberately not mandated

- **License** — every registry this project ships to surfaces it from the
  package manifest (PyPI's and npm's `license` fields, and so on), so the
  heading repeats the page's own chrome. Add one only for a registry that
  doesn't.
- **`## Development`** — dropped, not merely unmandated: it failed the
  three-leaks test below. Setup commands live in that language's own
  `AGENTS.md`, which
  [`../../../CONTRIBUTING.md`](../../../CONTRIBUTING.md) routes to along
  with the testing and PR bar around them; a subset of that here is
  contributor-facing content on a page someone opened to install the
  package. Section 7 already links `docs/maintenance/` for changing the
  package itself.

## What's genuinely registry- or language-specific

A package earns a highlight section none of the others need only when the
fact is true of that one registry or language — not because a writer
wanted color. Illustrative, not closed: a future language with a
comparable real fact earns one the same way.

| Highlight | Earned only when |
| :-- | :-- |
| Naming-split note (in Install) | the registry actually forced one |
| Browser/CDN distribution | the registry serves a browser-loadable artifact (npm, via unpkg/jsDelivr/esm.sh) |
| Structural-vs-nominal typing | the language's type system makes "no base class, no registration" notable (TypeScript's `interface`, Python's `Protocol`) |
| Custom-type callout | the standard library has no equivalent worth reusing (JS/TS's `UnknownLookupError`) |
| Debugger/tooling integration | the ecosystem has a real standard mechanism (C#'s `[DebuggerDisplay]`/`[DebuggerTypeProxy]` plus SourceLink) |

## Three leaks to check before calling one done

Each reads as helpful in isolation, which is why skimming misses them.
The test is never "is this sentence true" but "does it help someone using
*this* package":

- **No comparison to a sibling language's SDK.** "The Dart and C# SDKs
  have nominal typing and require an explicit `implements`," or naming
  what the other three raise where this one throws its own error type, is
  dead weight: a reader who just installed this is not choosing between it
  and a sibling they haven't installed, and the claim goes stale silently
  — editing *that* sibling re-checks nothing on *this* registry page.
  State the fact about this language on its own terms. A genuine
  comparison belongs in
  [`../../architecture/`](../../architecture/README.md) or
  [`../../extending/`](../../extending/README.md), which a reader opened
  because they wanted it.
- **No self-narration about maturity or roadmap.** "This is `0.0.1` —
  correct, but minimal, the rest arrives before `0.1.0`" is a promise with
  an expiry date on an effectively permanent page: registries don't
  retroactively edit a published version's rendered page, mirrors cache
  it, and an install's own copy on disk never updates. The day the
  promised version ships, every copy is quietly lying. State what the
  package does; the version number the registry already displays carries
  how far along it is.
- **No internal maintainer workflow.** Describing a script that
  regenerates a hash or a test that fails when a tag drifts addresses a
  reader who doesn't have this repository checked out, in the one place
  attention is scarcest — and if that workflow is renamed or removed, the
  shipped page is permanently wrong with nothing to catch it, unlike a
  repo doc a link checker or an issue-filer can. State what a consumer
  does.

### The same three apply to two other permanent surfaces

- **A language's first `CHANGELOG.md` entry.** Pinned to a tag, public,
  read by someone with no way to know it was edited. A first release
  genuinely *is* minimal, which is what makes the self-narration leak
  tempting — the entry still says only what shipped, in the same
  past-tense, no-promise voice as every later one. Never why this version
  is minimal, what it's a placeholder for, or when the rest arrives.
- **Doc comments that compile into the package.** C#'s XML docs,
  TypeScript's JSDoc surfaced through `.d.ts`, anything a language server
  reads out of the installed package: shipped with every version, read
  without a browser, exactly as unfixable after the fact as a registry
  page — and easier to forget to check. The sibling-comparison leak is
  most tempting here, since explaining *why* a type is shaped a certain
  way invites reaching for another language's equivalent. Reach for the
  fact about this language instead.

## Adding a new package's README

Follow the skeleton exactly for the shared sections; add only the
highlights that language genuinely earns. Check the result against the
three leaks before calling it done — including any doc comment shipping
compiled into the package, not just the README's prose.

## Related

- [`README.md`](README.md) — the general standard this builds on.
- [`versioned-links.md`](../versioned-links.md) — why every link in a
  shipped README is pinned to a release tag, never `main`.
- [`../../architecture/`](../../architecture/README.md) — where a genuine
  cross-language design comparison belongs instead.
