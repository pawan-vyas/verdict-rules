## What this changes and why

<!-- One or two sentences. Link the issue this addresses, if any. -->

## Checklist

This is the default template, and it assumes the change **affects behaviour** —
new capability, bug fix, or anything altering what the library does. That is
the strictest case, so it is the safe default: a clerical change that answers
"n/a" to most of it loses nothing, whereas a behaviour change that never saw
this list is how documentation goes stale for two versions.

Other templates exist for changes this does not fit — append
`?template=clerical.md` or `?template=release.md` to the pull request URL. See
[`.github/PULL_REQUEST_TEMPLATE/README.md`](.github/PULL_REQUEST_TEMPLATE/README.md).

**Code**

- [ ] Scoped to one change — no unrelated fixes bundled in.
- [ ] Tests added or updated, passing locally (`uv run pytest` from `python/`).
- [ ] If this is a new `Rule` shape or run mode: short-circuit behaviour and
      the vacuous-input case each have their own test, not just an assertion
      on the final `passed` boolean.
- [ ] If behaviour changed: the shared fixture under
      `fixtures/graduation_verdict/` pins it, so no language port can miss it.

**Documentation** — a behaviour change is a documentation change. Nothing
fails when a doc is wrong, so this is the part that has to be deliberate.

- [ ] Source docstrings.
- [ ] `docs/architecture.md`, **including its diagrams** — a diagram showing
      the old shape is more misleading than stale prose.
- [ ] `docs/extension.md` — does this enable a recipe, or invalidate one?
- [ ] `docs/testing.md` — it names specific tests; renaming one breaks it
      silently.
- [ ] `docs/maintenance.md`, language quickstarts, samples, `README.md`.
- [ ] `skills/verdict/references/` updated, with `.claude-plugin/plugin.json`
      bumped in the same commit (CI enforces this).
- [ ] A changelog entry in *that package's own* `CHANGELOG.md`, written
      with the version bump rather than backfilled.

**Verified, not assumed**

- [ ] Every code sample added or changed was **executed** against the built
      package — the first example in a document runs verbatim.
- [ ] Every relative link and anchor resolves; every mermaid diagram validates.
