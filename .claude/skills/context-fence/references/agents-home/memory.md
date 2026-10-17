# `.agents/memory/` — durable recall

> The in-repo home for what an agent would otherwise write to a harness's own memory store and lose
> the moment the session, the machine, or the tool changes. One file per fact.

## What belongs here

A fact or convention that was learned once and will be needed again: a preference confirmed with the
maintainer, a non-obvious constraint, a conclusion that took real work to reach. Name each file for
the fact it holds, in kebab-case, and state the fact in the file — including *why* it is true, since a
fact with no reasoning attached is one nobody dares act on later.

## What does not belong here

- **Standing rules** → `AGENTS.md`. Memory is what an agent knows; rules are what it must do.
- **Plans and specs** → `../plans/`. That sibling holds work not yet done; this one holds what is
  settled.
- **Session state** → `HANDOFF.md`, which is rewritten every session.
- **Anything the repo already records** — the code, the git history, the changelog, the published
  docs. Memory is for what those don't say.
- **Anything transient** → `../scratch/` (gitignored).

## Frontmatter

Worked example of a full entry file: the skill's `references/durable-index.md`. Each entry carries
`pin: always | default | archive`, which decides whether its index line stays in `AGENTS.md` when
the index is over budget. Choose it by what it costs *not* to know the fact, not by how recent it
is — see the skill's `references/durable-index.md`.

## Index

Every entry is also indexed in `AGENTS.md`. This table is where entries live once they spill out of
that index, and it is always complete — the `AGENTS.md` index is a subset of it, never the reverse.

| Entries | What it says |
| :-- | :-- |
| — | _(none yet)_ |
