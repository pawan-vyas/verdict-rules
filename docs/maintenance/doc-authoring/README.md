<!-- Title: Doc Authoring Standard -->
# Doc authoring standard

> The rules every doc in this repo follows, and where a doc category
> with its own extra structure (sample docs, this `maintenance/`
> directory itself) documents that structure on top of this file rather
> than repeating it. [`AGENTS.md`](../../../AGENTS.md)'s "Cross-language
> coding & doc conventions" section already covers blockquote framing,
> mermaid diagrams, and the em-dash rule for data values — this doc adds
> what isn't written down there yet.

| Doc | Extends this standard for |
| --- | --- |
| [`samples.md`](samples.md) | Sample spec/implementation docs under `docs/samples/` and each language's own samples directory |
| [`extending.md`](extending.md) | Extension-scenario spec/implementation docs under `docs/extending/` |
| [`architecture.md`](architecture.md) | The shared design doc plus one concrete file per language under `docs/architecture/` |
| [`testing.md`](testing.md) | The shared testing guide plus one concrete file per language under `docs/testing/` |
| [`package-readmes.md`](package-readmes.md) | Each package's own `README.md` — the file its registry renders as the package description |
| [`maintenance.md`](maintenance.md) | This `maintenance/` directory's own file-naming and structure conventions |

## No narration about the document itself

Never a sentence describing what the doc is doing — "this section aims
to show...", "a reader should come away recognizing...". State the
content directly: the fact, the code, the consequence. A doc that talks
about itself is a doc that isn't saying the thing yet.

## No unqualified "the reader"

Almost every doc here has more than one possible referent for that
word — someone reading this markdown page, and someone who will later
read or edit the *code* or *system* it describes — and leaving it
unqualified forces a re-read to work out which. Name the actual person
instead: "the next engineer who edits this function," "a support
agent," "whoever maintains this after the original author moves on."

## Argue from concrete cost, not taste

When a doc explains why something is wrong or why a design choice was
made, the reasoning is a specific, present-tense cost — not "this looks
messy," "this could be hard to change someday," or an appeal to
convention alone. A regression risked by editing already-shipped code,
a decision one person made that the next person inherits with no record
of why, a real resource cost paid today: these are arguments. "It's
cleaner" is not. Prefer a cost that is already true today over one that
depends on a hypothetical future edit.

## Every mention of another doc is a link, never a bare filename

A doc named in backticks without a link (`` `testing/` ``) is
invisible to every tool that checks whether a reference still resolves
— renaming, moving, or deleting the target leaves the mention silently
wrong forever, exactly the failure mode
[a behaviour change is a documentation change](../../../.agents/memory/a-behaviour-change-is-a-documentation-change.md)
already names for stale prose. If another doc in this repo is being
named — anywhere, for any reason, no matter how in-passing the
mention — it's a real inline link:
`` [`testing/`](../../testing/README.md) ``, not `` `testing/` ``.

**Link the specific section, not just the file, when that's what's
actually being referenced.** `` [`../extending/new-rule-shape/`'s type-structure note](../../architecture/README.md#type-structure) ``
catches a renamed or removed *section* immediately; a bare file-level
link to that doc would stay "resolving" even after the section being
pointed at is gone, silently pointing at the wrong part of a
still-existing file.

**Two narrow exceptions, both because linking would create a worse
problem than the bare mention:**

- **A filename used to name a pattern or convention, not one specific
  file** — "every directory gets its own `README.md`," "each package's
  own `CHANGELOG.md`." There is no single file a reader would jump to,
  so there is nothing to link.
- **A file that doesn't exist yet** — a future language's
  `releases/csharp.md` before C# ships, a sample directory's `<slug>/`
  before it's created. A link to a target that doesn't exist yet is a
  broken link today, which is the exact failure this rule exists to
  prevent, not a case it should manufacture.

Numbered citation/bibliography-style references (`[1]`, collected at
the end of the doc) are deliberately not used here — inline links keep
the target visible at the point of reading, without forcing a reader to
scroll to the bottom and back for every reference.

## A doc category that will grow per-variant is a directory, not a flat file family

If a doc is ever going to need more than one instance of the same shape
— one per language, one per registry, one per sample scenario — it
starts as `<category>/README.md` plus one file per instance, not a
single file that grows a new section per variant. See
[`maintenance.md`](maintenance.md) for the concrete pattern as applied
to this directory itself, and [`../releases/`](../releases/README.md)
for a worked instance. The test: if a second variant showing up would
mean editing a file the first variant already owns, it's a directory.

## A shared, language-agnostic doc never names one language's own file

The directory split above solves the file-per-variant problem; this
rule is what actually keeps the *shared* file inside it — the spec, the
architecture overview, any doc read by every language rather than
written by one — from quietly becoming a forced edit anyway. A spec's
own prose is exactly as durable as its structure only if it never says
which concrete `<language>.md` exists, and never hedges with "today":

- **No pointer to a specific implementation file in a spec's own
  blockquote or body.** `` "Each language's own file in this
  directory — [`python.md`](python.md) today — is the actual code" ``
  reads as a stable structural fact but is not one: it is true only
  until a second language's file exists beside it, at which point the
  sentence is either wrong (implying `python.md` is the only one) or
  needs an edit nobody's process actually triggers. GitHub already
  renders a directory's file listing when linking to the directory
  itself, and a sample's own fetch pairing already puts the language
  file right beside the spec — nothing in the spec's own prose needs to
  name it. Drop the pointer; don't replace it with a generic version.
- **No hardcoded path to another doc's language-specific file either.**
  Pointing a shared doc at `python/packages/verdict-rules/docs/quickstart.md`
  or `architecture/python.md` by name has the identical problem one
  level removed — link the shared parent
  (`architecture/README.md`, `docs/samples/README.md`) instead, or say
  "that language's own quickstart" with no link at all.
- **A concrete filename is fine as an illustrative example in a
  maintainer-facing template** (this file, `samples.md`, `extending.md`
  saying "`python.md`, for instance") — the reader is being taught the
  pattern, not handed a spec that must stay accurate to every language
  forever. Drop "today" there too, since the illustration doesn't
  become false when a second language's file exists — only the word
  "today" would.
- **Not even a "Related" section entry, if the target is a same-directory
  sibling.** Adding a bullet for a new language's file is additive, not
  an edit — but it is still a touchpoint nothing requires, since the
  sibling is already one click away in the same directory's own GitHub
  listing. A "Related" entry earns its place linking to a genuinely
  different doc a reader might not think to look for (a sibling
  scenario, a sample, `testing/`) — never a same-directory
  implementation file the directory listing already shows for free.

This was found the hard way: seven sample specs, seven extending
scenarios, both authoring templates, and `architecture/README.md`'s own
opening blockquote all repeated some variant of the pointer this rule
now forbids — see
[`../../../.agents/memory/shared-docs-never-name-one-languages-file.md`](../../../.agents/memory/shared-docs-never-name-one-languages-file.md)
for the full account of how it spread before anyone noticed the
pattern, not just the rule that came out of it.
