<!-- Title: Incidents -->
# `.agents/incidents/` — mistakes worth not repeating

> A record of things that went wrong, written so the **next** person or agent
> does not rediscover them. Not a blame log and not a changelog: an incident
> earns a file here only when the failure was **silent, surprising, or
> structural** — the kind you cannot avoid by being careful, only by knowing.

## What belongs here

Write one when a mistake has at least one of these properties:

- **It failed silently.** The build was green, the release succeeded, the
  output looked right, and it was still wrong.
- **It was structural.** The setup made the mistake easy, so care alone will
  not prevent a repeat.
- **It cost real rework.** A force-push, a republish, a retraction.

If the fix is obvious from the diff and the mistake could not plausibly recur,
it belongs in a commit message, not here.

## What does not belong here

- Routine bugs found by tests doing their job.
- Anything naming or blaming a person. These are about systems.
- Anything that belongs in a public surface — see
  [`../memory/public-surfaces-stay-professional.md`](../memory/public-surfaces-stay-professional.md).
  This directory is in-repo and readable, so write it as an engineer would want
  to find it, not as an apology.

## Format

One file per incident, named `NNN-short-slug.md`, each covering:

**What happened** · **How it surfaced** · **Impact** · **Root cause** —
the structural reason, not "was careless" · **The fix** · **What prevents a
repeat** — a check, a rule, or an honest "nothing; know about it."

The last section is the point of the file. An incident with no prevention is a
note; an incident with one is an improvement.

## Index

| # | Incident | Prevention |
| :-- | :-- | :-- |
| [001](001-release-notes-mangled-by-shell.md) | Release notes published with every code span empty | `--notes-file` |
| [002](002-build-output-committed-across-branches.md) | 436 build artefacts committed across two branches | Root-level `.gitignore` |
| [003](003-pushed-mid-rebase.md) | Pushed while a rebase was unresolved | Verify state before pushing |
| [004](004-research-published-to-public-surfaces.md) | Competitive research written into public issues and PRs | Standing rule + memory |
