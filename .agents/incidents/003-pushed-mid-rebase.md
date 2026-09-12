<!-- Title: Incident 003 — Pushed Mid-Rebase -->
# 003 · Pushed while a rebase was still unresolved

> **2026-09-12 · no damage, recorded for the process failure**

## What happened

A `git rebase` onto `main` hit a conflict. In the same command block, without
checking the rebase had finished, the next steps ran: file edits (one of which
silently failed its assertion), a commit, and a
`git push --force-with-lease`.

## How it surfaced

The output showed `Could not apply 0831cc7…` alongside a Python
`AssertionError` and a successful-looking push — three results from one block,
with nothing making the ordering obvious.

## Impact

**None, by luck rather than design.** The branch ref had not moved during the
rebase, so the push was a no-op, and `git rebase --abort` restored the branch
exactly. The same sequence with the rebase one commit further along would have
force-pushed a partial history over a shared branch.

## Root cause

Chaining state-changing git operations in one block, where each later step
assumes the earlier one succeeded, and `--force-with-lease` protects against a
*stale remote* but not against *pushing the wrong local state*. It compares
what the remote is against what you last saw, and a mid-rebase HEAD passes that
check happily.

## The fix

Nothing to fix in the repo. The branch was restored and rebased again with the
conflict resolved deliberately.

## What prevents a repeat

- **Never chain a rebase with a push.** Rebase, then inspect
  `git status` and `git log`, then push as a separate step.
- **Before any force-push, check three things**: no rebase or merge in
  progress, the working tree is clean, and the commit list is what you expect.
- Treat a failed assertion in a scripted edit as a stop, not a warning. It
  means the file was not in the state assumed, so everything after it is
  operating on an unknown tree.
- `--force-with-lease` is not a safety net for this. It guards the remote, not
  your local correctness.
