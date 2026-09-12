<!-- Title: Incident 006 — Deleting a Base Branch Closes a Stacked PR -->
# 006 · Deleting a merged PR's branch permanently closed a PR stacked on it

> **2026-09-12 · recoverable, but the PR object could not be reopened**

## What happened

PR #31 was merged with `gh pr merge --rebase --delete-branch --admin`. PR
#32 was stacked on it — based on `refactor/per-package-changelogs` (#31's
branch), not `main` — as part of a deliberate PR chain: #31 → #32 → #3/#5/#7.
Deleting #31's branch as part of its own merge left #32 pointed at a base ref
that no longer existed. GitHub responded by auto-closing #32 outright, with no
comment explaining why.

## How it surfaced

Asked, out of curiosity, why GitHub showed PR #32 as closed. `gh pr view 32`
showed `closed: true`, `mergedAt: null` — closed, not merged — timestamped
three seconds after #31's own merge. The repo's branch list confirmed
`refactor/per-package-changelogs` no longer existed.

## Impact

**None to the actual work.** #32's branch (`refactor/distribution-roots`) and
every one of its commits were untouched by any of this — a PR's open/closed
state is GitHub's own bookkeeping, not a property of the branch or its
commits. The real cost was that the closed PR was **permanently unusable**:
`gh pr reopen` and `gh pr edit --base` both refused, since GitHub locks a
closed PR to the base ref it last pointed at, and that ref was gone. The only
way back was rebasing the branch onto the new `main` and opening a fresh PR
(#36) referencing the old one.

## Root cause

`--delete-branch` deletes eagerly the moment its own PR merges, with no
awareness of — or warning about — any other PR based on that branch. GitHub's
stacked-PR feature (the "Preview stack" UI) can retarget a *dependent* PR
automatically when a chain is created through it, but a plain
`gh pr merge --delete-branch` on the *underlying* branch is not that feature
and does not get that behavior. A PR chain built by hand (branch A, branch B
based on branch A, opened as ordinary PRs) has no automatic retargeting
anywhere in the flow — the dependency exists only in each PR's `baseRefName`,
which a branch deletion invalidates outright.

## The fix

Rebased `refactor/distribution-roots` onto the post-merge `main` (one commit
auto-dropped as already-upstream, confirming the rebase was correct) and
opened #36 as #32's replacement, same branch and commits, new PR object.

## What prevents a repeat

- **Before merging any PR with a stack under it, check what's based on its
  branch first** — `gh pr list --json baseRefName` naming the about-to-be-
  deleted branch. If anything is, either use GitHub's own "Preview stack" /
  stacked-PR feature (which is aware of the chain and retargets it), or omit
  `--delete-branch` and retarget the dependent PR's base to the new merge
  target manually before deleting anything.
- **A closed PR with a deleted base cannot be reopened or retargeted** — this
  is not a transient GitHub error to retry. The only recovery is a fresh PR
  against a rebased branch; don't spend time trying `gh pr reopen`/`edit`
  again once the base ref is confirmed gone.
- **The branch and its commits are never what's at risk here** — a closed PR
  is bookkeeping. Verify the branch and its commit history are intact
  (`git ls-remote`, `git log`) before worrying about anything else; the actual
  content is very unlikely to be the casualty of this specific mistake.
