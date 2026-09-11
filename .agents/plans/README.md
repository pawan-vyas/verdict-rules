<!-- Title: Verdict Agent Plans -->
# `.agents/plans/` — plans, specs, and playbooks

> The in-repo home for work that spans more than one session: multi-step
> plans, design specs, migration playbooks, and resume notes for
> something that hasn't been built yet. Sibling of
> [`../memory/`](../memory/) under [`.agents/`](../README.md) — memory
> holds what is *already true*, plans hold what is *going to happen*.

## What belongs here

One directory or file per plan, named for the work rather than the
date. **This is also where a harness's own plan/scratch mode output
belongs** — point the harness here if its path is configurable, and if
it isn't, copy the file in as soon as it has content and treat the
in-repo copy as authoritative from then on (see
[`AGENTS.md`](../../AGENTS.md)'s "Harness plan files count as durable
context"). A plan states what's being built, the decisions already made,
the decisions explicitly still open, and enough context to resume
without re-deriving it. Open each with the same blockquote framing
every doc in this repo uses.

A plan is finished material, not a scratchpad — when it's executed,
either delete it or fold its durable conclusions into
[`../memory/`](../memory/) or the published
[`docs/`](../../docs/). A plans directory that only ever grows is a
plans directory nobody trusts.

## What does not belong here

- **Exploratory thinking that isn't a commitment** →
  [`docs/future_plan.md`](../../docs/future_plan.md) already exists for
  candidate features, and is explicitly not a roadmap.
- **Anything naming a specific consuming project.** This repo is
  designed to travel standalone (see
  [`AGENTS.md`](../../AGENTS.md)'s "Keep this repo
  standalone-portable") — a plan committed here is bound by that rule
  like every other file. Generalize a consumer's vocabulary out before
  a plan lands here.
- **Transient working notes** → `scratch/` (gitignored) or `/tmp`.
