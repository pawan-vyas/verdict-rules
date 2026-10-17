# `.agents/plans/` — plans, specs, and playbooks

> The in-repo home for work spanning more than one session: multi-step plans, design specs, migration
> playbooks, resume notes for something not built yet. Sibling of `../memory/` — that one holds what is
> settled, this one holds what is intended.

## What belongs here

One file or directory per plan, named for the work rather than the date. A plan states what is being
built, the decisions already made, the decisions deliberately still open, and enough context to resume
without re-deriving it.

**This is also where a harness's own plan-mode output belongs.** Point the harness here if its path is
configurable; if it isn't, copy the document in as soon as it has real content and treat the in-repo
copy as authoritative from then on.

A plan is finished material, not a scratchpad. Once it has been executed, either delete it or fold its
durable conclusions into `../memory/` or the published docs — a plans directory that only ever grows
is one nobody trusts.

## What does not belong here

- **Exploratory thinking that isn't a commitment** — a wishlist is not a plan; keep it with the
  project's own roadmap or forward-looking docs.
- **Transient working notes** → `../scratch/` (gitignored).

## Frontmatter

Worked example of a full entry file: the skill's `references/durable-index.md`. Each plan carries
`pin: always | default | archive` for the `AGENTS.md` index. An executed plan is usually re-pinned
to `archive` on its way out, if it isn't deleted outright.

## Index

Every plan is also indexed in `AGENTS.md`. This table is where plans live once they spill out of that
index, and it is always complete — the `AGENTS.md` index is a subset of it, never the reverse.

| Plans | What it says |
| :-- | :-- |
| — | _(none yet)_ |
