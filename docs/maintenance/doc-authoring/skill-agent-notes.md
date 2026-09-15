<!-- Title: Skill Agent-Notes Authoring Template -->
# Skill agent-notes authoring template

> The structure every `skills/verdict/references/<language>/agent-notes.md`
> follows. This builds on the general standard in
> [`README.md`](README.md) — read that first. Unlike a package README
> or a language's own `AGENTS.md`, this file is read by an agent
> already mid-task in a *consumer's* project, not a contributor to this
> repo — it is the one hand-written file [`SKILL.md`](../../../skills/verdict/SKILL.md)
> routes to per language, everything else being this repository's own
> documents copied in verbatim. See
> [`../../../skills/verdict/CHANGELOG.md`](../../../skills/verdict/CHANGELOG.md)'s
> `0.3.0` entry for why that split exists — restating facts here
> instead of linking to the real doc is what made the skill go stale
> twice before.

## Why short, and why one shape

Every fact this file could restate about *what verdict is* already
lives in `references/docs/`, copied verbatim from this repository's own
documentation — repeating any of it here is exactly the duplication
that goes stale the moment the real doc changes and this file doesn't.
What's left, once that's excluded, is genuinely small and genuinely the
same five questions for every language: how do I install and import
this, what's the whole API, what mistakes does this language's own
idiom make tempting, what should I test, and how do I fetch more.
Python's and JS/TS's own files converged on the same five sections
independently before this template existed — evidence the shape is
load-bearing, not imposed.

## The shared skeleton, in order

1. **Title** — `# <Language> — agent notes`.
2. **Opening paragraph** — states plainly that this file is short by
   design, that everything about what verdict *is* lives in
   `references/docs/` (this repository's own documents, not a summary),
   and that this file carries only what's specific to this language's
   own SDK and to writing this language against it.
3. **`## Install and import`** — the install command, then the actual
   import statement a real file would start with. If the distribution
   name and the import/require name differ, state the split and name
   one real-world precedent if one exists (Python's `verdict-rules` →
   `verdict`, the same split as `beautifulsoup4` → `bs4`).
4. **`## The API, in one screen`** — every type and method signature
   that exists, in that language's own real syntax, dense enough to fit
   on one screen with no prose between entries — comments on the same
   line for what a signature alone doesn't say (which run mode
   short-circuits, what a lookup throws or returns on absence). This is
   the fact an agent reaches for most often; it must never require
   scrolling.
5. **`## Mistakes that show up in generated <Language> specifically`**
   — one bullet per mistake, each naming the *language's own* tempting
   wrong tool by name (a concurrency primitive that silently destroys
   short-circuiting, that language's own falsy-coercion footgun, a
   predicate returning a bare boolean instead of a real result object) —
   not a generic restatement of the shared contract, which
   [`../../testing/`](../../testing/README.md) already covers once for
   every language. A comparison to another language's own version of
   the same footgun is fine here (Python's `or` vs. JS's `||`) — this
   file's reader is an agent already working in a polyglot-aware skill,
   not an installer with no reason to know another SDK exists.
6. **`## Testing what matters`** — a link to
   [`../../testing/`](../../testing/README.md) (fetch it) as the full
   checklist, then the handful of contracts easiest to skip, stated
   generically enough to match [`testing.md`](testing.md)'s own shared
   section rather than repeating a specific test's name.
7. **`## Fetching the deeper documents`** — the real, runnable fetch
   recipe: read the installed version, build the tag, fetch at that tag
   and never the default branch, and say plainly if `curl` fails because
   the tag doesn't exist yet rather than silently falling back. Name
   which documents are already bundled (no fetch needed) versus
   fetch-tier for this language specifically, since that split can
   differ once a language has its own quickstart or samples and another
   doesn't yet.

## Adding a new language's `agent-notes.md`

Add `skills/verdict/references/<language>/agent-notes.md` — the one
hand-written file that language needs. Everything else about what
verdict *is* comes from `scripts/build.sh` copying this repository's
own documents into the bundle per
[`../../../skills/verdict/MANIFEST.toml`](../../../skills/verdict/MANIFEST.toml);
writing more than this one file here is a sign something is being
restated that should be linked instead.

## Related

- [`README.md`](README.md) — the general standard this template builds
  on.
- [`language-agents.md`](language-agents.md) — the sibling template for
  a language's own top-level `AGENTS.md`, a contributor-facing
  equivalent to this consumer-facing file.
- [`testing.md`](testing.md) — the shared testing contracts this file's
  own "Testing what matters" section points a reader at rather than
  restating.
