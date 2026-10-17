# `.agents/incidents/` — failures worth not repeating

> A record of what went wrong, written so the next person or agent does not have to rediscover it.
> Not a blame record and not a changelog: a failure earns a file here only when knowing about it is
> the only thing that prevents a repeat.

## What belongs here

One of these has to be true:

- **It failed silently.** The build was green, the job succeeded, the output looked right, and it was
  still wrong.
- **It was structural.** The setup made the mistake easy, so being careful next time will not prevent
  it.
- **It cost real rework.** A force-push, a republish, a retraction, a restore.

If the fix is obvious from the diff and it could not plausibly happen again, it belongs in the commit
message instead. That bar is what keeps this directory worth reading.

## What does not belong here

- Routine bugs that the tests caught doing exactly their job.
- Anything naming or blaming a person. These are about systems and setups.
- Anything that belongs on a public surface. This directory is in-repo; write each entry the way an
  engineer would want to find it, not as an apology.

## Format

One file per entry, `NNN-short-slug.md`, covering: **What happened** · **How it surfaced** ·
**Impact** · **Root cause** (the structural reason, never "was careless") · **The fix** · **What
prevents a repeat** — a check, a rule, or an honest "nothing; just know about it."

That last section is the reason the file exists. Without it an entry is only a story.

## When to read this

Before doing anything irreversible — publishing, force-pushing, rewriting history, editing a release,
deleting data. Scan the index below; the prevention column is written to be enough on its own.

## Frontmatter

Worked example of a full entry file: the skill's `references/durable-index.md`. Each entry carries
`pin: always` by default — an incident exists precisely because not knowing it is expensive, which
is the definition of the tier. Drop one to `default` only when the setup that allowed it is
genuinely gone.

## Index

Every entry is also indexed in `AGENTS.md`. This table is where entries live once they spill out of
that index, and it is always complete — the `AGENTS.md` index is a subset of it, never the reverse.

| # | What went wrong | Prevention |
| :-- | :-- | :-- |
| — | _(none yet)_ | — |
