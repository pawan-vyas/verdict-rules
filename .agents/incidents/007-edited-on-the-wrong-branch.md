<!-- Title: Incident 007 — Edited On The Wrong Branch -->
# 007 · Edited and nearly committed a repo-wide fix directly onto an SDK branch

> **2026-09-13 · no damage, recorded for the process failure and a recurring side-effect**

## What happened

A fix meant for `main` (removing em-dashes from data/config values across the
whole repo) was written, verified, and staged for commit — all directly on
`plan/dart-sdk`, left checked out from unrelated verification work earlier in
the same session. The intent throughout had been a fresh branch off `main`.

## How it surfaced

`check_changelogs.py`'s own output, run as a routine pre-commit check, printed
`OK dart/packages/verdict_rules 0.0.1 -> ...` — a package that has no reason
to exist on a branch meant to hold only repo-root and fixture changes. That
one unexpected line was the only signal; `git status` was not checked first
and would have shown the same thing sooner.

## Impact

**None, caught before the commit.** `git stash push -u` moved every
uncommitted change (including new, untracked files) off the branch cleanly;
`git checkout main && git checkout -b <new-branch>` then `git stash pop`
landed them where they belonged. Committed one branch later than intended,
nothing lost.

## Root cause

No `git status` or `git branch --show-current` check before starting a new,
unrelated piece of work — a habit that would have caught this on the first
command rather than several edits in. Compounded by the session's own
pattern that turn: rapid switching between SDK branches for verification
(rebase, test, push, repeat) leaves the working directory checked out
somewhere that has nothing to do with the next task, and nothing forces a
deliberate `main` checkout before starting one.

## The recurring side effect worth naming on its own

Checking out `plan/dart-sdk` for verification (`dart analyze`, `dart test`)
regenerates `dart/packages/verdict_rules/.dart_tool/` and similar build
output in the working tree. Switching to another branch afterward — `main`
included — does not delete untracked files, so they persist as an untracked
`dart/` directory on branches that have no `dart/` at all. This showed up
**twice** in the same session, on two different `main`-based fix branches,
and would have been silently `git add -A`'d into a commit meant for a totally
unrelated fix (the same shape of mistake incident 002 already exists for,
recurring here across branches for a second reason instead of a first).

## The fix

Nothing to fix in the repo — `dart/`'s own `.gitignore` entries are already
correct for the Dart branch itself; the leak is purely an artifact of
switching away from that branch without a clean check.

## What prevents a repeat

- **`git branch --show-current` (or read `git status`'s own first line)
  before the first edit of a new, unrelated task** — not after drafting the
  fix, not after staging it. The cheapest point to catch this is before
  anything has changed at all.
- **After checking out away from any language branch, run `git status
  --short` before staging anything new.** An unexpected untracked top-level
  directory — `dart/`, or any other language's own root — is that language's
  regenerated build output surviving the checkout, not part of the current
  task, and should be `rm -rf`'d before it can be swept into `git add -A`.
- Treat an unexpected line in a verification script's own output (a package
  that shouldn't be there, a version that doesn't match) as a "which branch
  am I actually on" signal, not just a check-script quirk to shrug off.
