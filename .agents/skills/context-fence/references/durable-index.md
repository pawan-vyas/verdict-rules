# The durable-context index in `AGENTS.md`

> How every durable entry stays visible without `AGENTS.md` growing without bound. Same shape as the
> [agentskills.io](https://agentskills.io/specification) progressive-disclosure model the vendored
> skills already use: a short always-loaded layer that *names* what exists, and the full content one
> hop away, loaded only when it turns out to matter.

## Why per-entry, not per-home

A single line saying "durable facts live in `.agents/memory/`" makes discovery a **choice**: an agent
reading `AGENTS.md` with a task already in hand has to decide to take an extra hop, and usually
doesn't. A one-line hook per entry is in context at the moment it becomes relevant — an agent about to
write a CI step sees the line about shell interpolation without having gone looking for it. That is
the whole point of indexing, and it is worth the maintenance.

What stays out of `AGENTS.md` is the **content**. An index line is a link plus a hook of roughly ten
words; if it is long enough to explain the fact, it belongs in the entry.

## Writing an entry and indexing it are one operation

Never two. A durable note that isn't indexed is one the next agent won't find, which is the same
outcome as not having written it. Add the line in the same edit as the file.

## Tiers — what survives a squeeze

Each entry carries a `pin` in its own frontmatter. The tier is chosen by **what it costs not to know
the entry**, not by how recent or how interesting it is:

| `pin` | Meaning | Use for |
| :-- | :-- | :-- |
| `always` | Never spills out of `AGENTS.md`, regardless of budget | Entries where not knowing causes a silent or irreversible failure — every incident's prevention defaults here, along with hard constraints that are expensive to violate |
| `default` | Indexed while the budget allows | Most facts and active plans |
| `archive` | Never indexed in `AGENTS.md`; lives in its home's `README.md` index only | Still true, no longer hot |

## The budget and the spill

The index block gets roughly **2,000 characters**. The binding constraint is a character cap, not an
entry count — several harnesses truncate a rules file at around 12k characters, and a truncated tail
fails silently, so the budget is measured in the same unit as the thing that will break.

When the block exceeds budget:

1. `always` entries never move.
2. Among `default` entries, spill **oldest-touched first** — recency is the tiebreaker that decides
   *order*, never the key that decides *importance*. Use the entry's last modification in git.
3. A spilled entry moves into its home's `README.md` index. It is never deleted and never silently
   dropped.
4. **The collapse is visible.** The home's line in `AGENTS.md` says how many entries are no longer
   listed and links the README: `**Facts** — .agents/memory/ · 6 shown, [+12 more](…/README.md)`.
   Silent truncation is the failure mode this whole design exists to avoid; a number that says
   "something is not shown here" preserves the property that `AGENTS.md` never lies about coverage.

## Re-promotion

SWEEP re-evaluates placement as part of its normal pass. An archived or spilled entry that gets
touched, referenced, or turns out to matter again comes back into the index on the next sweep — which
is why this needs no operation of its own.

## The shape of an entry

Every entry under a home is a markdown file with two frontmatter fields and a body. Nothing else is
required — the fence is plain files, and an entry a human can't read at a glance has already failed.

```markdown
---
pin: default          # always | default | archive — see the tier table above
hook: omitting locale from the cache key serves the wrong language
---

# Cache keys must include the locale

The response cache is keyed on path + query only. Two users on the same path with different
`Accept-Language` headers share an entry, so the second one is served the first one's language.

Found the third time it was reported as "random" wrong-language pages, 2026-08-14.
```

`hook` is the line that goes in the `AGENTS.md` index — written once, here, so the index and the entry
can never disagree about what the entry says. The title is for whoever opens the file; the hook is for
whoever doesn't.

Incidents add the section structure from `agents-home/incidents.md` on top of this; plans add the
decisions-made / decisions-open structure from `agents-home/plans.md`.

## Shape of the index block

```markdown
## Durable context

**Facts** — `.agents/memory/`
- [cache-keys-include-locale](.agents/memory/cache-keys-include-locale.md) — omitting it serves the wrong language
- [staging-mirrors-prod-schema](.agents/memory/staging-mirrors-prod-schema.md) — schema drift breaks the seed script

**Incidents** — `.agents/incidents/` · read this list before anything irreversible
- [001](.agents/incidents/001-silent-skip.md) — suite goes green when a required env var is unset

**Plans** — `.agents/plans/` · 2 shown, [+4 more](.agents/plans/README.md)
- [multi-region-rollout](.agents/plans/multi-region-rollout.md) — read replicas first, writes last
```
