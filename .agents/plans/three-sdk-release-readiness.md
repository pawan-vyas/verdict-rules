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

### `plan/csharp-sdk`

- [ ] Rebase onto `main`, resolve conflicts (expect: doc path renames,
      `skills/verdict/MANIFEST*`, `skills/verdict/CHANGELOG.md`)
- [ ] Confirm code still builds/tests clean post-rebase
- [ ] Repoint anything the branch added that named an old doc path
      (`docs/adding-a-language.md` → `docs/maintenance/adding-a-language.md`,
      `docs/extension.md` → `docs/extending/`) at the new location
- [ ] `test-csharp.yml` — `changes`/real-jobs/`gate` shape, no `paths:`
      filter on the trigger (see adding-a-language.md's own warning)
- [ ] `release-csharp.yml` — version-change-on-main trigger, tags
      `csharp-v*`, includes a `verify-published` job against NuGet's
      version-specific endpoint (never the cached summary)
- [ ] `CHANGELOG.md` already exists beside `VerdictRules.csproj` —
      confirm it follows this repo's Keep-a-Changelog convention
      already used elsewhere
- [ ] Add to `scripts/check_shipped_links.py`'s pinned-package table
- [ ] Push (force-with-lease, solo branch)

### `plan/dart-sdk`

- [ ] Rebase onto `main`, resolve conflicts
- [ ] Confirm code still builds/tests clean post-rebase
- [ ] Repoint old doc paths the same way as csharp
- [ ] `test-dart.yml`, same gate shape
- [ ] `release-dart.yml`, tags `dart-v*`, `verify-published` against
      pub.dev's version-specific endpoint — note in the workflow that
      pub.dev offers no attestation equivalent, per
      adding-a-language.md's own callout, rather than leaving the gap
      looking accidental
- [ ] `CHANGELOG.md` beside `pubspec.yaml` — confirm pub.dev's own
      convention (it parses this file and documents `## 1.2.3`)
- [ ] Add to `scripts/check_shipped_links.py`
- [ ] Push (force-with-lease)

### `plan/js-sdk`

- [ ] Rebase onto `main`, resolve conflicts (likely the heaviest of
      the three — 14 commits ahead, already touches skill references
      and evals)
- [ ] Confirm code still builds/tests clean post-rebase
- [ ] Repoint old doc paths
- [ ] `test-js.yml`, same gate shape
- [ ] `release-js.yml`, tags `js-v*`, `verify-published` against npm's
      version-specific endpoint, asserting `dist.attestations`
- [ ] `CHANGELOG.md` beside `package.json` — confirm npm's Keep-a-Changelog
      convention (already in place per the branch's own history)
- [ ] Add to `scripts/check_shipped_links.py`
- [ ] Refresh `skills/verdict/references/js/agent-notes.md` against
      current conventions (it predates the `MANIFEST.toml` migration
      and the docs restructuring)
- [ ] Refresh `skills/verdict-workspace/evals/js/*.json` the same way;
      confirm `scripts/build_evals.py` still discovers the directory
      cleanly post-rebase
- [ ] Author `docs/architecture/js.md`, `docs/testing/js.md`, and a
      `js.md` alongside each `docs/samples/<scenario>/` and
      `docs/extending/<scenario>/` this SDK is ready to demonstrate —
      same quality bar as the Python set, not a stub
- [ ] Root `README.md` gets a `### JavaScript/TypeScript` subsection
      under **Quickstart** and **Development**, appended, per
      adding-a-language.md's explicit exception for that one file
- [ ] Run real evals against a fresh subagent with the skill vendored
      into an isolated scratch project (not a documentation audit —
      see the standing correction on this from earlier in the session)
- [ ] Push (force-with-lease)

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
