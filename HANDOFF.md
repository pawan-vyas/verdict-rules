---
kind: session-handoff
handoff_schema: 1
updated_utc: 2026-09-11T14:52:16Z
updated_local: 2026-09-11T20:22:16+05:30
branch: main
state_at_commit: bf5e4e197803b06aa0a5f883bff4191b3f8aa89e
state_at_commit_short: bf5e4e1
# Freshness: run `git log --oneline "$(git log -1 --format=%H -- HANDOFF.md)"..HEAD`. Empty (+ clean
# tree) = current. Non-empty = stale — reconcile per §0.1 before trusting §2–§3. (Comparing against
# state_at_commit directly always shows the handoff commit itself as "drift" — see §0.1.)
---

# verdict — session handoff / resume brief

> The **migratable state container** for this project: session-to-session, not cross-session. It may
> be freely rewritten when a new session/tool takes over (see §0.1). Standing rules live in
> `AGENTS.md` (imported by `CLAUDE.md`), plus `python/AGENTS.md` for Python-specific ones. Verify
> against the code; the source of truth for what's built is `docs/architecture.md` and the code
> under `python/src/`.

## 0 · How to use this file

You (the next agent) are continuing work on **verdict**. Read §1 for what it is, §2 for where we
are, §3 for what to do next, §4 for known issues, §5 for how to verify. Everything is committed on
`main`.

**Keep every durable note in the repo** — standing rules in `AGENTS.md`, session state here,
everything else an agent produces under `.agents/` (`memory/` for durable recall, `plans/` for
multi-session specs and playbooks, `skills/` for the vendored manuals); committed, never in
`~/.claude/`, home, or `/tmp`. Transient logs/scripts → `scratch/` (gitignored). Note the
repo-local override: agent material lives under **`.agents/`**, not the context-fence default
`docs/agent-memory/` — `docs/` here is published package documentation for human readers. See
[`.agents/README.md`](.agents/README.md). Update this file at session end so the next one resumes
from the repo alone.

## 0.1 · Freshness & alignment protocol (read before trusting §2–§3)

The frontmatter is a staleness marker. `state_at_commit` is the HEAD this body describes; the commit
that wrote this file is its child, so its own hash isn't self-recorded. **Don't diff against
`state_at_commit` directly** — the range `state_at_commit..HEAD` always contains at least the handoff
commit itself (the one that added this file), so it would read "stale by 1" the instant this file is
written, even with zero drift. Diff against the commit that last touched `HANDOFF.md` instead — that
range is empty exactly when nothing has happened since:

```
git log --oneline "$(git log -1 --format=%H -- HANDOFF.md)"..HEAD
git status --short
```

- **Empty log + clean tree** → current; proceed with §2–§3.
- **Non-empty, or dirty tree** → the repo moved. Do NOT trust §2–§3 blindly. Reconcile — same
  procedure whether you're mid-session (RESUME found drift) or migrating to a fresh session: (1) read
  `git log -p "$(git log -1 --format=%H -- HANDOFF.md)"..HEAD` + `git diff` to see what actually
  happened; (2) sweep any external or uncommitted context back into the repo; (3) rewrite §2–§3 to
  reality; (4) update the frontmatter — new `updated_*` and `state_at_commit` = current HEAD *before*
  committing; (5) commit (its parent = the recorded `state_at_commit`, keeping the marker N-1 again).
  Reconciling **always** bumps the frontmatter and commits — don't leave a reconciled body sitting
  next to a frontmatter that now understates what §2 describes; that's a worse state than the
  staleness it was fixing. Standing rules never go here.

## 1 · What this project is (one paragraph)

`verdict` is a small, zero-dependency, async-native rule-evaluation engine —
`Rule`/`FunctionRule`/`AndRule`/`OrRule`/`RulesEngine`/`RuleResult`/`RunResult` — designed to exist
in more than one language with identical execution-model guarantees (sequential, never concurrent,
evaluation so short-circuiting is a real contract; vacuous-truth polarity decided explicitly per
composite shape). The repo is polyglot in layout but **only Python ships today**: `python/` holds the
package (`python/src/verdict/`), its tests, docs, and a fully tested example project; a second
language would land as a new sibling top-level directory with its own `AGENTS.md`. Cross-language
docs live in `docs/`, the AI-agent skill for building with verdict in `skills/verdict/` (vendored
into consuming projects by `scripts/install.sh`), and the published site in `site/`. Source of truth
for the design is [`docs/architecture.md`](docs/architecture.md); for what's released,
[`CHANGELOG.md`](CHANGELOG.md).

## 1b · External references (NOT part of this repo)

**None.** As of this session the repo is fully self-contained: everything durable a session needs is
committed inside the boundary. The two vendored skills (`mermaid-diagrams`, `context-fence`) are
in-tree copies — no network, no plugin install, and no sibling checkout is required to follow either
of them.

## 2 · Where we are (this session's work)

Released and stable at `python-v0.1.0` (tagged 2026-09-07). The last commits before this session
were release polish: PyPI/Python-versions/CI/license badges on the README, the `pip install
verdict-rules` step in `skills/verdict/SKILL.md`, a favicon for the site, and the finalized
changelog entry.

**This session raised the context fence** (SETUP), which is the only change on top of `bf5e4e1`:

- `AGENTS.md` — new "Working notes stay in the repo" section: the invariant, a "where things go"
  table, the `.agents/` override, the start-of-session resume pointer, and two standing rules
  carried over from a sibling personal repo's own `AGENTS.md` — **harness plan/scratch-mode files
  count as durable context** (point the harness at `.agents/plans/`, or copy its file in and make
  the in-repo copy authoritative) and **don't create parallel planning docs** (in-flight work lives
  in this file's §3, longer-lived work in `.agents/plans/`). Nothing else in that file changed.
- `.agents/skills/context-fence/` + `.claude/skills/context-fence/` — the context-fence operations
  manual vendored to both discovery paths (`SKILL.md`, `references/handoff-template.md`,
  `scripts/detect_state.sh`, `scripts/handoff_meta.sh`), matching how `mermaid-diagrams` is already
  vendored. Both copies are tool-owned: safe to overwrite on any future SETUP run, don't hand-edit.
- `.github/workflows/release-skill.yml` — **new release path.** A `skill-vX.Y.Z` tag verifies the
  tag against `.claude-plugin/plugin.json`, runs `scripts/build.sh`, and cuts a GitHub Release. This
  closes the last gap: `scripts/get.sh` resolves GitHub's *latest release* for `verdict-tools.zip`,
  so before this a skill-only change had no way to reach curl-pipe users until some language
  release happened to carry it. Both release workflows now attach the full `dist/*` set — also
  de-hardcoded in `release-python.yml`, which previously named the three artifacts inline and could
  drift from `build.sh`. Marketplace and clone installs read the repo and were never affected.
- `.github/workflows/check-skill-version.yml` — **new gate.** Fails a PR or push to `main` that
  changes shipped skill content without bumping `.claude-plugin/plugin.json`'s version, which is
  Claude Code's only signal to a marketplace user that their vendored skill copy is stale. It
  re-derives the changed paths from the base..HEAD range rather than trusting the path filter, and
  excludes `skills/verdict-workspace/` (eval material, never ships). Logic verified locally against
  five scenarios, including the two that must fail and the no-base-commit edge. `docs/maintenance.md`
  documents the rule; the workflow header states plainly that it is *not* a check against any
  language manifest.
- `.agents/README.md`, `.agents/memory/README.md`, `.agents/plans/README.md` — `.agents/` is now the
  single home for durable agent working material, with `memory/` and `plans/` as siblings of the
  existing `skills/`. Each README says what belongs in it and what doesn't.
- `.agents/plans/polyglot-sdk-resume.md` — the former root-level `RESUME.md`, folded in and
  rewritten to stand on this repo's own terms, with every path it cites now resolving. `RESUME.md`
  is deleted and its `/RESUME.md` line removed from `.gitignore`.
- `.agents/memory/releases-are-language-scoped.md` and
  `.agents/memory/skill-version-is-independent-of-sdk-versions.md` — the first two memory entries.
- **Portability sweep across the published docs.** Several docs made factual claims about where
  this package is deployed and where the code originated. Neither is something this repo should
  claim — it stands alone, and it documents patterns rather than deployments. Rewritten in `docs/extension.md`, `docs/architecture.md`, `docs/maintenance.md`,
  `docs/future_plan.md`, `python/docs/quickstart.md`, `python/docs/samples/1_README.md`,
  `python/docs/samples/6_data-driven-rule-sets.md`, and `CHANGELOG.md`. The rate-limiting and
  access-control *illustrations* stay — they're generic patterns, which `AGENTS.md` explicitly
  allows — only the claims that they exist somewhere are gone.
- **Stale-path fix**: `python/examples/graduation_verdict/README.md` (2×) and
  `python/examples/graduation_verdict/docs/testing.md` (1×) told the reader to run their commands
  from a directory that does not exist in this repo. Now `# From python/`, and both commands
  verified to actually run from there.
- `.gitignore` — added `scratch/`, `.claude/settings.local.json`, `*.local.json`.
- `HANDOFF.md` — this file.

No package source, tests, or published docs were touched.

## 3 · What to do next (prioritized)

1. **Commit the fence** (it is the only uncommitted work):
   ```
   git add AGENTS.md .gitignore HANDOFF.md .agents .claude/skills/context-fence
   git commit -m "Adopt the context-fence discipline for durable agent notes"
   git push
   ```
2. **Verify nothing else regressed** before or after that commit — see §5.
3. **No feature work is in flight.** `docs/future_plan.md` is explicitly an exploratory thinking
   exercise, not a backlog — don't treat any candidate there as committed. If a second language is
   being started, read `.agents/plans/polyglot-sdk-resume.md` first (especially its "explicitly not
   decided yet" list), then `docs/architecture.md` and `docs/extension.md` fresh rather than that
   file's compressed summary of them.

## 4 · Known issues / blockers

- **No defects open.** One was raised this session and withdrawn: that `plugin.json`'s version
  isn't asserted against `python/pyproject.toml`. It shouldn't be — the two measure different
  things and are meant to drift. Working that through surfaced the real (inverse) risk instead,
  now closed by `.github/workflows/check-skill-version.yml`. Background in
  [`.agents/memory/skill-version-is-independent-of-sdk-versions.md`](.agents/memory/skill-version-is-independent-of-sdk-versions.md).
  The `scripts/get.sh` staleness window noted alongside it is also closed — see
  `.github/workflows/release-skill.yml` in §2.
- The other standing issue at the start of this session — a gitignored root-level `RESUME.md`,
  outside version control and so absent from any clone — is resolved: rewritten to stand on this
  repo's own terms and folded into `.agents/plans/polyglot-sdk-resume.md`.
- **`.pytest_cache/` and `.venv/` are present in the working tree** but correctly gitignored — no
  action needed, just don't be surprised by them.
- CI is green on the release commit.

## 5 · Verify (gate / test commands)

```
cd python && uv sync && uv run pytest --cov=verdict --cov-report=term-missing
```

That's exactly what `.github/workflows/test-python.yml` runs, across a 3.10–3.14 matrix (the
`requires-python` floor through the newest stable). There is no separate lint/type gate configured.
**547 tests pass** as of this session (`python/tests/` plus
`python/examples/graduation_verdict/`, the latter 523 of them).

For doc changes, additionally validate every mermaid diagram you touched:

```
node .claude/skills/mermaid-diagrams/scripts/validate_diagrams.js <file...>
```

## 5b · Tooling / skills

- **Python** ≥3.10 (floor set by `python/pyproject.toml`), managed with **`uv`**; `python/uv.lock` is
  committed. Test deps: `pytest>=8.0`, `pytest-asyncio>=0.24`. The package itself has **zero runtime
  dependencies** — adding one requires an explicit discussion first (see `AGENTS.md`).
- **Node** is needed only for the mermaid diagram validator, not for the package.
- **Vendored skills** (both mirrored to `.agents/skills/` and `.claude/skills/`):
  `mermaid-diagrams` (diagram standards + validator — mandatory for any diagram in this repo) and
  `context-fence` (this discipline's own operations manual).
- `skills/verdict/` is the *shipped* skill for consumers, and `skills/verdict-workspace/` is its
  eval-loop working material — only `evals/evals.json` there is tracked.

## 6 · Key docs

- [`AGENTS.md`](AGENTS.md) — standing rules for every agent; `CLAUDE.md` is just `@AGENTS.md`.
  `python/AGENTS.md` adds the Python-specific layer.
- [`docs/architecture.md`](docs/architecture.md) — the design source of truth (why sequential
  evaluation, why `Rule` is structural, why `RuleResult.data` stays opaque).
- [`docs/extension.md`](docs/extension.md) — the five extension recipes; Recipe 3's
  one-adapter-module boundary is the pattern consumers should be steered toward.
- [`docs/maintenance.md`](docs/maintenance.md) — the two never-slip constraints and the release
  procedure. [`docs/testing.md`](docs/testing.md) — the testing checklist.
- [`docs/future_plan.md`](docs/future_plan.md) — exploratory candidates, explicitly not a roadmap.
- [`python/examples/graduation_verdict/`](python/examples/graduation_verdict/) — the worked example,
  including the oracle/differential chaos suite worth re-deriving in any future language.
- [`.agents/README.md`](.agents/README.md) — the agent working-material layout (`memory/`, `plans/`,
  `skills/`), and [`.agents/plans/polyglot-sdk-resume.md`](.agents/plans/polyglot-sdk-resume.md) —
  where a second language would start.
- [`.agents/skills/context-fence/SKILL.md`](.agents/skills/context-fence/SKILL.md) — the full
  RESUME/SETUP/ADOPT/SWEEP/HANDOFF spec this file is the exit seal of.
