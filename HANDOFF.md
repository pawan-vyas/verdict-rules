---
kind: session-handoff
handoff_schema: 1
updated_utc: 2026-09-11T17:02:26Z
updated_local: 2026-09-11T22:32:26+05:30
branch: main
state_at_commit: fcfb6370f6ace9babdf10aa014a4cf876a868d79
state_at_commit_short: fcfb637
# Freshness: run `git log --oneline "$(git log -1 --format=%H -- HANDOFF.md)"..HEAD`. Empty (+ clean
# tree) = current. Non-empty = stale — reconcile per §0.1 before trusting §2–§3. (Comparing against
# state_at_commit directly always shows the handoff commit itself as "drift" — see §0.1.)
---

# verdict — session handoff / resume brief

> The **migratable state container** for this project: session-to-session, not cross-session. It may
> be freely rewritten when a new session or tool takes over (see §0.1). Standing rules live in
> `AGENTS.md` (imported by `CLAUDE.md`), plus `python/AGENTS.md` for Python-specific ones — never
> here. Verify against the code; the source of truth for the design is `docs/architecture.md`, and
> for what shipped, each package's own `CHANGELOG.md`.

## 0 · How to use this file

You (the next agent) are continuing work on **verdict**. Read §1 for what it is, §2 for where we
are, §3 for what to do next, §4 for known issues, §5 for how to verify.

**Everything is committed, merged, and pushed.** `main` is at `fcfb637`, CI green. Nothing is in
flight and no branch is outstanding.

## 0.1 · Freshness & alignment protocol (read before trusting §2–§3)

The frontmatter is a staleness marker. `state_at_commit` is the HEAD this body describes; the commit
that wrote this file is its child, so its own hash isn't self-recorded. **Don't diff against
`state_at_commit` directly** — the range `state_at_commit..HEAD` always contains at least the handoff
commit itself, so it would read "stale by 1" the instant this file is written, even with zero drift.
Diff against the commit that last touched `HANDOFF.md` instead; that range is empty exactly when
nothing has happened since:

```
git log --oneline "$(git log -1 --format=%H -- HANDOFF.md)"..HEAD
git status --short
```

- **Empty log + clean tree** → current; proceed with §2–§3.
- **Non-empty, or dirty tree** → the repo moved. Do NOT trust §2–§3 blindly. Reconcile: (1) read
  `git log -p "$(git log -1 --format=%H -- HANDOFF.md)"..HEAD` + `git diff` to see what actually
  happened; (2) sweep any external or uncommitted context back into the repo; (3) rewrite §2–§3 to
  reality; (4) update the frontmatter — new `updated_*` and `state_at_commit` = current HEAD *before*
  committing; (5) commit, so its parent is the recorded `state_at_commit` and the marker stays N-1.
  Reconciling **always** bumps the frontmatter and commits. Standing rules never go here.

`git pull` before trusting any of this if you have been away — the formula above compares against
your *local* HEAD, so an out-of-date clone reads "current" while `origin/main` has moved.

## 1 · What this project is (one paragraph)

`verdict` is a small, zero-dependency, async-native rule-evaluation engine —
`Rule`/`FunctionRule`/`AndRule`/`OrRule`/`RulesEngine`/`RuleResult`/`RunResult` — designed to exist
in more than one language with identical execution-model guarantees: sequential, never concurrent,
evaluation so short-circuiting is a real contract rather than an optimization, and vacuous-truth
polarity decided explicitly per composite shape. The repo is polyglot in layout but **only Python
ships today**, under `python/` (a workspace root; the package itself is in
`python/packages/verdict-rules/`, with its own `src/verdict/`, tests, and docs, plus a fully
tested example project). A second language lands as a new sibling top-level directory with its own
`AGENTS.md`. Cross-language docs are in `docs/`, the AI-agent skill in `skills/verdict/` (vendored
into other projects by `scripts/install.sh`), the published site in `site/`, and agent working
material in `.agents/`. Design source of truth: [`docs/architecture.md`](docs/architecture.md).

## 1b · External references (NOT part of this repo)

**None.** The repo is fully self-contained: both vendored skills (`mermaid-diagrams`,
`context-fence`) are in-tree copies, so no network, plugin install, or sibling checkout is needed to
follow either. Nothing a session needs lives outside the boundary.

## 2 · Where we are (this session's work)

Starting point was `bf5e4e1` (release polish after `python-v0.1.0`, tagged 2026-09-07). Range
**`bf5e4e1..fcfb637`** — four commits, fast-forwarded onto `main` via PR #1 and pushed. `main`'s
history stays linear (the repo allows only merge commits, so the PR was landed by fast-forward
rather than taking a merge bubble for a branch already sitting on `main`'s HEAD; GitHub marked #1
merged once the commits became reachable):

- **`243d6b5` — docs standalone.** Several docs asserted as fact that specific named consumers run
  this code in production, and `CHANGELOG.md` described where the source originated. Neither
  survives the package being read elsewhere. Rewritten across `docs/extension.md`,
  `docs/architecture.md`, `docs/maintenance.md`, `docs/future_plan.md`, `python/docs/quickstart.md`,
  and two sample docs, so each claim now stands on the design itself. The rate-limiting and
  access-control *illustrations* stay — they're generic patterns. Also fixed three code samples that
  told the reader to run commands from a directory that doesn't exist in this layout.
- **`204d4e0` — skill versioning + release.** `.claude-plugin/plugin.json`'s version measures the
  skill's content (it's Claude Code's marketplace signal that a vendored copy is stale);
  `python/pyproject.toml`'s measures the library. They are meant to drift, and nothing should assert
  they match. Two new workflows: `check-skill-version.yml` (fails a PR/push to `main` that changes
  shipped skill content without bumping `plugin.json`) and `release-skill.yml` (`skill-vX.Y.Z` tag →
  verify against `plugin.json` → build → release). Both release workflows now attach the full
  `dist/*`, which is load-bearing — see §4.
- **`86b933c` — context fence.** `AGENTS.md` gained a "Working notes stay in the repo" section
  (where things go; harness plan *and* memory files count as durable context; no parallel planning
  docs) and an "Asking the user" section. `.agents/` is now the home for agent working material:
  `memory/`, `plans/`, and the existing `skills/`, each with a README. The gitignored root-level
  `RESUME.md` was rewritten to stand on its own and folded into
  `.agents/plans/polyglot-sdk-resume.md`.
- **`fcfb637` — handoff.** This file, resealed against the three above; then reconciled again after
  the merge, per §0.1.

## 3 · What to do next (prioritized)

**Nothing is pending.** The session's work is merged and green; there is no half-finished thread to
pick up. If you are starting fresh, the useful entry points are:

1. **Feature work is not in flight, and `docs/future_plan.md` is not a backlog** — it is an
   explicitly exploratory thinking exercise. Don't treat any candidate there as decided; re-derive
   its reasoning first.
2. **If a second language is starting**, read
   [`.agents/plans/polyglot-sdk-resume.md`](.agents/plans/polyglot-sdk-resume.md) first — especially
   its "explicitly not decided yet" list (npm/NuGet name availability, `js/` vs `typescript/`,
   `csharp/` vs `dotnet/`, per-language CI layout) — then `docs/architecture.md` and
   `docs/extension.md` fresh, rather than that file's compressed summary of them.
3. **If you change anything under `skills/verdict/`**, bump `.claude-plugin/plugin.json`'s version in
   the same commit, or `check-skill-version.yml` will fail the push. That version is unrelated to
   `python/pyproject.toml`'s — see §4 and `docs/maintenance.md`.
4. **The first skill-only release will exercise `release-skill.yml` for the first time** (§4):
   ```
   # bump .claude-plugin/plugin.json, add a ## skill-vX.Y.Z CHANGELOG entry, commit, then:
   git tag skill-vX.Y.Z && git push origin skill-vX.Y.Z
   ```

## 4 · Known issues / blockers

- **`release-skill.yml` has never run.** Its shell logic was verified locally (tag parsing, and
  both the matching and mismatching `plugin.json` cases), but it can only execute on a real
  `skill-vX.Y.Z` tag push, so treat its first run as unproven. By contrast
  `check-skill-version.yml` **is** proven: it ran green on PR #1 and again on the push to `main`,
  and on the PR it correctly resolved the merge base and took the skip path
  (`No shipped skill or plugin-manifest files changed`) rather than passing by accident — the
  enforcement path itself is still only locally verified.
- **Load-bearing invariant, easy to break.** `scripts/get.sh` resolves whatever GitHub calls the
  *latest* release and pulls `verdict-tools.zip` from it. So **every** release workflow must attach
  the full skill artifact set — if one stops, a release of that kind becomes "latest" without a
  `verdict-tools.zip` and `get.sh` fails outright. Both workflows state this in their headers. Don't
  "tidy" either one down to just its own artifacts.
- **`.pytest_cache/` and `.venv/` are present in the working tree** but correctly gitignored, as are
  `skills/verdict-workspace/{benchmarks,iteration-1}/` (regenerable eval output). No action needed.
- No open defects in the package itself. One was raised this session and withdrawn — that
  `plugin.json`'s version isn't asserted against `pyproject.toml`. It shouldn't be; see
  [`.agents/memory/skill-version-is-independent-of-sdk-versions.md`](.agents/memory/skill-version-is-independent-of-sdk-versions.md),
  written specifically to stop that conclusion being re-derived.

## 5 · Verify (gate / test commands)

```
cd python && uv sync && uv run pytest --cov=verdict --cov-report=term-missing
```

Exactly what `.github/workflows/test-python.yml` runs, across a 3.10–3.14 matrix (the
`requires-python` floor through newest stable). **547 tests pass** as of `fcfb637`, confirmed on CI across all five matrix versions
(`python/tests/` plus `python/examples/graduation_verdict/`, the latter 523 of them). There is no
separate lint or type gate configured.

For a doc change, additionally validate every mermaid diagram touched:

```
node .claude/skills/mermaid-diagrams/scripts/validate_diagrams.js --markdown <file.md>
```

For a change to the distribution scripts, confirm all three artifacts still build:

```
bash scripts/build.sh && ls dist/   # verdict-plugin.zip  verdict-tools.zip  verdict.skill
```

## 5b · Tooling / skills

- **Python** ≥3.10 (floor in `python/pyproject.toml`), managed with **`uv`**; `python/uv.lock` is
  committed. Test deps: `pytest>=8.0`, `pytest-asyncio>=0.24`. The package has **zero runtime
  dependencies** — adding one requires an explicit discussion first, per `AGENTS.md`.
- **Node** is needed only for the mermaid diagram validator, and **`jq`** only by the two CI
  workflows (preinstalled on `ubuntu-latest`). Neither is needed to use or test the package.
- **Vendored skills**, mirrored to both `.agents/skills/` and `.claude/skills/`:
  `mermaid-diagrams` (diagram standards + validator — mandatory for any diagram in this repo) and
  `context-fence` (the operations manual this file is the exit seal of). Both are tool-owned:
  refresh them wholesale, never hand-edit.
- `skills/verdict/` is the *shipped* skill; `skills/verdict-workspace/` is its eval working
  material, where only `evals/evals.json` is tracked.

## 6 · Key docs

- [`AGENTS.md`](AGENTS.md) — standing rules for every agent (`CLAUDE.md` is just `@AGENTS.md`);
  `python/AGENTS.md` adds the Python layer.
- [`docs/architecture.md`](docs/architecture.md) — design source of truth: why evaluation is
  sequential, why `Rule` is structural, why `RuleResult.data` stays opaque.
- [`docs/extension.md`](docs/extension.md) — the five extension recipes; Recipe 3's
  one-adapter-module boundary is the pattern to steer consumers toward.
- [`docs/maintenance.md`](docs/maintenance.md) — the two never-slip constraints, the language
  release procedure, and the separate skill release procedure.
  [`docs/testing.md`](docs/testing.md) — the testing checklist.
- [`docs/future_plan.md`](docs/future_plan.md) — exploratory candidates, explicitly not a roadmap.
- [`python/examples/graduation_verdict/`](python/examples/graduation_verdict/) — the worked example,
  including the oracle/differential chaos suite worth re-deriving in any future language.
- [`.agents/README.md`](.agents/README.md) — the agent working-material layout, and
  [`.agents/memory/`](.agents/memory/) — durable facts worth not re-deriving.
