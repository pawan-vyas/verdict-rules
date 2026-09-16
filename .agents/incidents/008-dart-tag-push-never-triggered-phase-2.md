<!-- Title: Incident 008 — Dart's Tag Push Never Triggered Phase 2 -->
# 008 · `release-dart.yml`'s own tag push never started the publish run

> **2026-09-16 · `dart-v0.0.2`, the first real run of `release-dart.yml`**

## What happened

`release-dart.yml` is deliberately two-phase: phase 1 (triggered by a push to
`main`) tests and pushes a tag; that tag push is supposed to re-trigger the
same workflow into phase 2, which actually publishes to pub.dev. This is
different from `release-python.yml` and the JS release workflow, both of
which publish in a single run and never need a second trigger.

Phase 1 ran clean: tests passed, `dart-v0.0.2` was pushed and genuinely
existed (`git ls-remote --tags origin` confirmed it). Nothing was flagged as
failed anywhere in the Actions UI. But no second run ever started, and
`verdict_rules` `0.0.2` was never live on pub.dev (`curl` against the
version-specific API endpoint returned `404`).

## How it surfaced

Watching for the expected second workflow run after phase 1's tag-push step
finished — `gh run list` showed nothing tag-triggered, and pub.dev's API
confirmed the version was never published. Phase 1 gave no error of its own;
the absence only showed up by checking a downstream system pub.dev never
touched.

## Impact

No incorrect data shipped anywhere — the release simply never happened.
Recovered by hand via `gh workflow run release-dart.yml --ref dart-v0.0.2`
(the `workflow_dispatch` escape hatch already built into the workflow),
which is not subject to the same suppression and correctly routed into
phase 2 since `github.ref_type` reads `tag` for a dispatch against a tag ref.
`release-github.yml`'s own tag-idempotency (tolerates a pre-existing tag at
the same commit) meant the recovery run hit no conflict.

## Root cause

`push-tag`'s `actions/checkout@v7` step used no explicit `token:`, so its
later `git push origin <tag>` authenticated with the default, auto-issued
`GITHUB_TOKEN`. GitHub has a documented, intentional rule: any push made
using a workflow run's own default `GITHUB_TOKEN` never triggers another
workflow run — including the same workflow's own trigger conditions — to
stop a workflow from re-triggering itself in an infinite loop. This is not
a permissions gap; `permissions: contents: write` grants the *ability* to
push, not trigger-eligibility. The design in the file's own top-of-file
comment was correct about *what* needed to happen (a tag push re-triggering
the workflow) but the implementation never accounted for *which credential*
that push needed to use to actually fire the event — and since this was the
very first real run of a two-phase design unique to Dart among this repo's
release workflows, nothing had ever exercised the gap before.

## The fix

A fine-grained PAT (`VERDICT_RULES_DART_RELEASE_TAG_TOKEN`, scoped to this
repo only, `Contents: Read and write`) stored as a repository secret, wired
into `push-tag`'s checkout step via `token: ${{ secrets.VERDICT_RULES_DART_RELEASE_TAG_TOKEN }}`.
A PAT-authenticated push is not subject to the anti-recursion suppression, so
the tag-push event fires normally and phase 2 starts without manual
intervention.

## What prevents a repeat

- Any workflow step whose entire purpose is *to cause another workflow run*
  (a tag push meant to re-trigger, a commit meant to fire a downstream CI)
  needs a non-default credential on its checkout — check this explicitly
  whenever a workflow's design depends on GitHub re-triggering itself, not
  just when something visibly fails.
- A two-phase (or any N-phase, self-triggering) release design is unproven
  until its *first* real run has been watched end-to-end, including
  confirming the downstream trigger actually fired — a green single-phase
  run proves nothing about a later phase that depends on an event the first
  phase merely hopes will occur.
- `workflow_dispatch` as a stated "escape hatch" in a workflow's `on:` block
  is worth keeping even after the root cause is fixed — it is the correct,
  non-destructive recovery path for exactly this failure mode, and does not
  require deleting or recreating anything as long as the downstream job's
  tag-handling is already idempotent (verify that before relying on it).
