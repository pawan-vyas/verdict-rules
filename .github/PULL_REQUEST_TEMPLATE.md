<!-- markdownlint-disable MD041 (GitHub shows the PR title as the H1; a template starting with its own would duplicate it) -->
## What this changes and why

<!-- One or two sentences for a small change. Link the issue this addresses, if
any. For anything larger, prefer dense tables over prose paragraphs, keeping
every fact a prose version would carry -- see
../docs/maintenance/doc-authoring/summaries.md. -->

## Checklist

This is the default template, and it assumes the change **affects behaviour** —
new capability, bug fix, or anything altering what the library does. That is
the strictest case, so it is the safe default: a clerical change that answers
"n/a" to most of it loses nothing, whereas a behaviour change that never saw
this list is how documentation goes stale for two versions.

Other templates exist for changes this does not fit — append
`?template=clerical.md` or `?template=release.md` to the pull request URL. See
[`.github/PULL_REQUEST_TEMPLATE/README.md`](PULL_REQUEST_TEMPLATE/README.md).

### Code

- [ ] Scoped to one change — no unrelated fixes bundled in.
- [ ] Tests added or updated, passing locally in that language's own suite — each
      language's own `AGENTS.md` names the command.
- [ ] If this is a new `Rule` shape or run mode: short-circuit behaviour and
      the vacuous-input case each have their own test, not just an assertion
      on the final `passed` boolean.
- [ ] If behaviour changed: the shared fixture under
      `fixtures/graduation_verdict/` pins it, so no language port can miss it.

### Documentation

A behaviour change is a documentation change. Nothing fails when a doc
is wrong, so this is the part that has to be deliberate.

- [ ] Source docstrings.
- [ ] [`docs/architecture/`](../docs/architecture/README.md), **including its
      diagrams** — a diagram showing the old shape is more misleading than stale
      prose.
- [ ] [`docs/extending/`](../docs/extending/README.md) — does this enable a scenario, or invalidate one?
- [ ] [`docs/testing/`](../docs/testing/README.md) — it names specific
      tests; renaming one breaks it silently.
- [ ] Each language's own `AGENTS.md` — they restate the guarantees in that
      language's own terms, so one contract change leaves all of them wrong at once.
- [ ] [`docs/maintenance/`](../docs/maintenance/README.md), language quickstarts,
      samples, the [root `README.md`](../README.md).
- [ ] `skills/verdict/SKILL.md` and `skills/verdict/references/` updated, with
      `.claude-plugin/plugin.json` bumped in the same commit (CI enforces this).
- [ ] If the change introduced a new way to be *subtly* wrong — a lookup that fails
      differently, a polarity, an ordering guarantee — an expectation under
      [`skills/verdict-workspace/evals/`](../skills/verdict-workspace/evals/README.md),
      which is the only check that an agent reading the skill still lands on the
      right design.
- [ ] A changelog entry in *that package's own* `CHANGELOG.md`, written
      with the version bump rather than backfilled.

### Verified, not assumed

- [ ] Every code sample added or changed was **executed** against the built
      package — the first example in a document runs verbatim.
- [ ] Every relative link and anchor resolves; every mermaid diagram validates.
