<!-- Title: Bringing csharp/dart/js-sdk to 0.0.1 Release Readiness -->
# Bringing csharp/dart/js-sdk to 0.0.1 release readiness

> The fallback checklist for this effort — rebase three language SDK
> branches onto the post-restructure `main`, then bring all three to
> the same release-machinery readiness Python already proves out, so a
> later push to `0.1.0` (or whatever terminal pre-1.0 version) never has
> to invent CI/release plumbing from scratch. Update this file in place
> as work lands or the plan changes; it is the thing to re-read after a
> context reset mid-flight, not a one-time note.

## Trigger and starting state

- PR #46 merged to `main` at `30839d0` on 2026-09-14, cutting
  `python-v0.2.2` (PyPI) and `skill-v0.5.0` (GitHub release) in the same
  merge. Both confirmed live.
- Three branches exist, each already past Stage 1 ("Minimal correct")
  per [`../../docs/maintenance/adding-a-language.md`](../../docs/maintenance/adding-a-language.md):
  `plan/csharp-sdk`, `plan/dart-sdk`, `plan/js-sdk`. None has claimed
  its registry name yet (checked npm, NuGet, pub.dev directly — all
  404/not-found for the settled names).
- All three were forked before this session's docs restructuring, so
  each still edits since-renamed paths (`docs/adding-a-language.md`,
  `docs/extension.md`, the hand-rolled `skills/verdict/MANIFEST`) —
  expect real rebase conflicts, not just a clean replay.

## Scope, settled with the user

- **The bar for all three**: Stage 2 groundwork (clerical decisions
  already on record, package metadata correct) plus full Stage 3
  ("Harden") machinery — `test-<lang>.yml`, `release-<lang>.yml`, the
  per-language `verify-published` job, `CHANGELOG.md` beside that
  package's own manifest, an entry in `scripts/check_shipped_links.py`.
  Not Stage 4 (graduation fixture, oracle suite, skill references,
  evals, full doc set) for csharp/dart — that stays deferred until the
  user decides to push a given language further.
- **js-sdk gets Stage 4 on top**, per the earlier standing instruction:
  real evals and authored docs, because it is the one being reviewed
  and possibly released first. It already has a head start from before
  this session's doc conventions existed
  (`skills/verdict/references/js/agent-notes.md`,
  `skills/verdict-workspace/evals/js/*.json`) — treat that as a
  starting point to refresh against the current conventions, not as
  already-done.
- **The actual first manual publish (Stage 2's `npm publish` /
  `dotnet nuget push` / `dart pub publish`) is not something this
  session performs.** It structurally requires the user's own registry
  account/session in every case (trusted publishing itself can only be
  configured after a human claims the name), and the ritual doc calls
  it out as a deliberate human step regardless. This work ends at
  "everything code- and workflow-side is ready; here is the exact
  manual checklist" for each language, not at a live publish.
  `release-<lang>.yml` and its `verify-published` job get written to
  the same shape as Python's, but stay unexercised for real until the
  user does that manual step — flag this plainly in the final recap
  rather than implying more confidence than a never-run workflow has
  earned.

## Per-branch checklist

Same shape for csharp and dart; js gets the extra Stage-4 rows.

### `plan/csharp-sdk` — [PR #5](https://github.com/pawan-vyas/verdict-rules/pull/5), Stage 2/3 done, CI green

- [x] Rebase onto `main` — clean, zero conflicts (this branch never
      touched a path the restructuring moved)
- [x] Confirm code still builds/tests clean post-rebase — 26/26
- [x] Repoint stale doc paths — `csharp/AGENTS.md` and
      `.agents/plans/csharp-sdk/PLAN.md` both had pre-restructure links,
      never caught before since this branch predates `lint-docs.yml`
- [x] `test-csharp.yml` — `changes`/`test`/`gate` shape, no `paths:`
      filter on the trigger
- [x] `release-csharp.yml` — version-change-on-main trigger, tags
      `csharp-v*`, `verify-published` against NuGet's version-specific
      flat-container endpoint, checks for `.signature.p7s`
- [x] `CHANGELOG.md` already existed beside `VerdictRules.csproj`,
      already correct
- [x] Added to `scripts/check_shipped_links.py`'s pinned-package
      table — surfaced and fixed a real pre-existing regex bug there
      (a link followed immediately by an XML closing tag)
- [x] Pushed; PR #5 (pre-existing draft) updated, all CI green

### `plan/dart-sdk` — [PR #7](https://github.com/pawan-vyas/verdict-rules/pull/7), Stage 2/3 done, CI green

- [x] Rebase onto `main` — clean, zero conflicts
- [x] Confirm code still builds/tests clean post-rebase — 28/28,
      `dart analyze` clean
- [x] Repoint stale doc paths — `dart/AGENTS.md` and its `PLAN.md`;
      also corrected a factually wrong CHANGELOG header claiming a
      repo-root `CHANGELOG.md` exists (it doesn't, hasn't for a while)
- [x] `test-dart.yml`, same gate shape
- [x] `release-dart.yml` — **not** structurally parallel to Python/C#,
      deliberately: pub.dev requires the publishing run to be
      triggered *by* the tag push itself (OIDC-verified), so this
      workflow runs in two phases from one file — a main-push tests
      and pushes only the tag, and that tag push re-triggers the same
      workflow into the phase that actually publishes. Required a
      small, non-breaking idempotency fix to `release-github.yml`'s
      tag-creation step to support a pre-existing tag at HEAD.
      `verify-published` checks pub.dev's version endpoint and
      `archive_sha256` (pub.dev has no attestation equivalent)
- [x] `CHANGELOG.md` beside `pubspec.yaml`, confirmed against pub.dev's
      real bare `## X.Y.Z` convention (not Keep a Changelog's bracketed
      form)
- [x] Added to `scripts/check_shipped_links.py`
- [x] Pushed; PR #7 (pre-existing draft) updated, all CI green

### `plan/js-sdk` — [PR #3](https://github.com/pawan-vyas/verdict-rules/pull/3), Stage 2/3 done and CI green; Stage 4 not started

- [x] Rebase onto `main` — one real conflict in `.claude-plugin/plugin.json`
      (this branch's own skill-content version bump collided with an
      unrelated, already-shipped version from this session); resolved
      by bumping past what's already tagged and moving this branch's
      changelog entry into its own newest section
- [x] Confirm code still builds/tests clean post-rebase — 27/27,
      typecheck and build clean on Node 18/20/22/24
- [x] Repoint stale doc paths — `js/AGENTS.md` and its `PLAN.md`
- [x] `test-js.yml`, same gate shape, Node matrix from the package's
      stated floor (18) through current (24)
- [x] `release-js.yml` — single-run shape like Python/C# (npm's
      Trusted Publishing doesn't require a tag-triggered run the way
      pub.dev does), `verify-published` against npm's version-specific
      endpoint, asserting `dist.attestations.provenance`
- [x] `CHANGELOG.md` beside `package.json`, already correct
- [x] Added to `scripts/check_shipped_links.py`
- [x] Refreshed `skills/verdict/references/js/agent-notes.md` — this
      was shipped, bundled content with a real factual error, not just
      a stale link: it claimed `docs/extension.md` was "bundled
      already," which stopped being true when `docs/extending/` moved
      to fetch-tier this session. Fixed to state current reality
- [ ] Refresh `skills/verdict-workspace/evals/js/*.json` against
      current conventions — **not started**
- [ ] Author `docs/architecture/js.md`, `docs/testing/js.md`, and a
      `js.md` alongside each `docs/samples/<scenario>/` and
      `docs/extending/<scenario>/` this SDK is ready to demonstrate —
      **not started**
- [ ] Root `README.md` gets a `### JavaScript/TypeScript` subsection —
      **not started**
- [ ] Run real evals against a fresh subagent — **not started**
- [x] Pushed; PR #3 (pre-existing draft) updated, all CI green

## Sequencing

csharp and dart first (lighter, parity-only scope, can happen in
either order or in parallel), js-sdk last since it carries the extra
Stage-4 work and is the one actually headed for review. Report a single
combined recap for all three at the end, per the user's ask — not a
check-in after each branch — but surface anything genuinely blocking
(an ambiguous conflict, a registry fact that contradicts what a
branch's own `PLAN.md` assumed) as it's found rather than guessing
silently through it.

## Related

- [`../../docs/maintenance/adding-a-language.md`](../../docs/maintenance/adding-a-language.md) —
  the ritual this checklist is a working instance of.
- [`../../docs/maintenance/releases/README.md`](../../docs/maintenance/releases/README.md) —
  the shared release pipeline shape `release-<lang>.yml` must match.
- [`polyglot-sdk-resume.md`](polyglot-sdk-resume.md) — the cross-language
  design claim every port is judged against.
