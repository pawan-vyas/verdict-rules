<!-- Title: Incident 009 — An Annotated Tag's SHA Never Equals a Commit SHA -->
# 009 · The tag-idempotency check rejected a tag pointing at the exact right commit

> **2026-09-16 · `dart-v0.0.2`, recovery run after fixing incident 008**

## What happened

After fixing [008](008-dart-tag-push-never-triggered-phase-2.md) and recovering
`dart-v0.0.2` via `workflow_dispatch`, phase 2 ran: `Publish to pub.dev`
succeeded (confirmed live via pub.dev's own API), but the following `release`
job — which calls the shared `release-github.yml` to create the GitHub
Release — failed at its very first step:

```
Tag dart-v0.0.2 already exists, pointing at a different commit
(3ef2b95b22716ab114ecd46d372b8e25f85ec493). A release is being attempted
for a version that has already shipped.
```

The tag was not, in fact, pointing at a different commit.
`git rev-parse refs/tags/dart-v0.0.2^{commit}` resolved to `7c8725b0...`,
exactly the commit `push-tag` had tagged. The error was comparing the wrong
thing.

## How it surfaced

Watching the recovery run to completion (`gh run watch`) rather than
stopping once pub.dev's own job went green — the failure was in a later,
independent job in the same run.

## Impact

`dart-v0.0.2` is genuinely live on pub.dev, but has no GitHub Release page
(no release notes, no attached skill-distribution assets) as of this
writing. No incorrect data shipped; a downstream artifact of the release
simply didn't get created. Recovery approach: [decided with the user —
see below / fill in once resolved].

## Root cause

`push-tag` creates an **annotated** tag (`git tag -a ... -m ...`, so the
release carries a message). `release-github.yml`'s own idempotency check
did:

```bash
existing="$(git rev-parse -q --verify "refs/tags/${{ inputs.tag }}")"
if [ "$existing" = "$(git rev-parse HEAD)" ]; then
```

For an **annotated** tag, `git rev-parse refs/tags/<tag>` returns the tag
*object's own SHA* — a distinct object from the commit it points at, with
its own hash. It is never equal to any commit's SHA, including the exact
commit the tag was created at. The check would reject every annotated tag
unconditionally, regardless of which commit it actually pointed to. It
happened to go unnoticed until now because no caller had ever pre-pushed an
annotated tag ahead of calling this workflow before Dart's two-phase design
— every other language's release lets this same workflow create the
(lightweight, by omission of `-a`... actually also annotated, but never
pre-existing) tag itself, so the "already exists" branch was never
exercised for a caller-pushed tag until this run.

This is the same shared file [008](008-dart-tag-push-never-triggered-phase-2.md)'s
fix depends on, and the same run that finally exercised 008's fix for the
first time was also the first run to ever exercise this branch of
`release-github.yml` — both gaps were invisible until Dart's first real,
complete two-phase run, not because either was subtle, but because nothing
before this had the shape to trigger either path.

## The fix

Dereference with `^{commit}`:
`git rev-parse -q --verify "refs/tags/${{ inputs.tag }}^{commit}"`.
`^{commit}` resolves an annotated tag to the commit it points at; for a
lightweight tag (already a commit reference) it is a no-op. This makes the
comparison correct for both tag kinds rather than only working by accident
for one.

## What prevents a repeat

- **Any script comparing `git rev-parse` output against a commit SHA must
  dereference with `^{commit}` (or `^{}`) whenever the ref in question could
  be an annotated tag** — an annotated tag's raw SHA is a different object
  class entirely, not "the same commit, different value," so no amount of
  retrying or re-running changes the outcome.
- A shared workflow's untaken branches are exactly as unverified as new
  code — `release-github.yml`'s tag-already-exists branch had existed since
  early in the project but was only exercised by the very first two-phase
  release. Grep a shared file for every conditional branch when adding the
  first caller that can actually reach one that's gone untaken.
