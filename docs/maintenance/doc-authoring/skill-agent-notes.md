<!-- Title: Skill Agent-Notes Authoring Template -->
# Skill agent-notes authoring template

> The structure every `skills/verdict/references/<language>/agent-notes.md`
> follows, and the one rule that keeps it short. Builds on the general
> standard in [`README.md`](README.md) — read that first. This file is
> read by an agent already mid-task in a *consumer's* project, not by a
> contributor to this repo; it is the only hand-written content a language
> adds to the skill.

## The rule that decides what goes in

**Nothing that is true of verdict generally.** The execution-model
guarantees, the result-inspection surface, what a predicate contract is —
[`SKILL.md`](../../../skills/verdict/SKILL.md) states each once, for every
language. Restating any of it here is the duplication that goes stale the
moment the real statement changes and this file doesn't, which is what
happened before this rule existed.

What survives that cut is small and turns out to be the same four
questions in every language: how do I install and import this, what is the
whole API, how do I read a result, and which run mode do I want.

## The skeleton, in order

1. **Title** — `# <Language> — agent notes`, then one line saying this
   file carries only what is specific to this SDK and pointing at
   `SKILL.md` for the rest.
2. **`## Install and import`** — the install command, then the actual
   import a real file would start with. Where the distribution name and
   the import name differ, say so and name a real precedent if one exists
   (Python's `verdict-rules` → `verdict`, the same split as
   `beautifulsoup4` → `bs4`).
3. **`## The API, in one screen`** — every type and signature that
   exists, in that language's real syntax, dense enough to fit on one
   screen with no prose between entries. Comments sit on the same line
   for what a signature alone does not say: which run mode
   short-circuits, what a lookup throws or returns on absence, which
   constructor parameter takes positions rather than results. This is the
   fact an agent reaches for most often, so it must never require
   scrolling.
4. **`## Reading a result`** — the result surface in that language's own
   spelling, and anything about it that is true only here: C#'s three
   methods where the others have accessors, Dart's `toJson()`, JS's
   frozen instances. Then the audit-trail rule — key on a leaf's own rule
   name, never a composite's — shown as code.
5. **`## Which run mode`** — a four-row table, need → what to reach for.

A language-specific footgun worth naming (a concurrency primitive that
silently destroys short-circuiting, a falsy-coercion trap) belongs inline
next to the thing it applies to, not in a section of its own. Comparing it
to another language's version of the same trap is fine here — this file's
reader is an agent already working in a polyglot-aware skill.

## Adding a new language's file

Create `skills/verdict/references/<language>/agent-notes.md`. Nothing
registers it: `scripts/build.sh` copies `skills/verdict/` wholesale, and
`SKILL.md` routes to `references/<language>/` without naming any
language. Writing more than this one file is the signal that something is
being restated which should be linked instead.

## Related

- [`README.md`](README.md) — the general standard this builds on.
- [`language-agents.md`](language-agents.md) — the sibling template for a
  language's own `AGENTS.md`, the contributor-facing counterpart to this
  consumer-facing file.
