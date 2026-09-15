---
kind: session-handoff
handoff_schema: 1
updated_utc: 2026-09-15T08:33:35Z
updated_local: 2026-09-15T14:03:35+05:30
branch: main
state_at_commit: 7d068aad3d5af884c0a26a8f7a51529d698db7dc
state_at_commit_short: 7d068aa
# Freshness: run `git log --oneline "$(git log -1 --format=%H -- HANDOFF.md)"..HEAD`. Empty (+ clean
# tree) = current. Non-empty = stale — reconcile per §0.1 before trusting §2–§3. (Comparing against
# state_at_commit directly always shows the handoff commit itself as "drift" — see §0.1.)
---

# verdict — session handoff / resume brief

> The **migratable state container** for this project: session-to-session, not cross-session. It may
> be freely rewritten when a new session or tool takes over (see §0.1). Standing rules live in
> [`AGENTS.md`](AGENTS.md) (imported by `CLAUDE.md`), plus each language's own `AGENTS.md` — never
> here. Verify against the code; the source of truth for the design is `docs/architecture/`, and
> for what shipped, each package's own `CHANGELOG.md`.

## 0 · How to use this file

You (the next agent) are continuing work on **verdict**. Read §1 for what it is, §2 for where we
are, §3 for what to do next, §4 for known issues, §5 for how to verify.

**`main` is at `7d068aa`, CI green. Four PRs are open** — see §2 and §3. This is not a "nothing in
flight" state: three language SDKs and a doc-audit branch are mid-review, each independently
mergeable.

## 0.1 · Freshness & alignment protocol (read before trusting §2–§3)

The frontmatter is a staleness marker. `state_at_commit` is the HEAD this body describes; the commit
that wrote this file is its child, so its own hash isn't self-recorded. **Don't diff against
`state_at_commit` directly** — the range `state_at_commit..HEAD` always contains at least the handoff
commit itself, so it would read "stale by 1" the instant this file is written, even with zero drift.
Diff against the commit that last touched `HANDOFF.md` instead; that range is empty exactly when
nothing has happened since:

```bash
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

**Open PRs live on their own branches, not on `main`.** This file describes `main`'s own state; §2
and §3 name each open PR's branch explicitly, since `git log`/`git status` on `main` alone won't
surface them.

## 1 · What this project is (one paragraph)

`verdict` is a small, zero-dependency, async-native rule-evaluation engine —
`Rule`/`FunctionRule`/`AndRule`/`OrRule`/`RulesEngine`/`RuleResult`/`RunResult` — designed to exist
in more than one language with identical execution-model guarantees: sequential, never concurrent,
evaluation so short-circuiting is a real contract rather than an optimization, and vacuous-truth
polarity decided explicitly per composite shape. **Python is the only language merged to `main`**
(PyPI `verdict-rules` at `0.2.3`, under `python/` — a workspace root; the package itself is in
`python/packages/verdict-rules/`). **JS/TS, C#, and Dart each exist complete on their own open PR**
(§2) — not yet on `main` — each with its own top-level directory (`js/`, `csharp/`, `dart/`), its
own `AGENTS.md`, and its own `docs/maintenance/releases/<language>.md` documenting that registry's
real first-publish mechanics. Cross-language docs are in `docs/`, the AI-agent skill in
`skills/verdict/` (its own release cadence, currently `0.5.0` on `main`; vendored into other
projects by `scripts/install.sh`), the published site in `site/`, and agent working material in
`.agents/`. Design source of truth: [`docs/architecture/`](docs/architecture/README.md).

## 1b · External references (NOT part of this repo)

**None.** The repo is fully self-contained: both vendored skills (`mermaid-diagrams`,
`context-fence`) are in-tree copies, so no network, plugin install, or sibling checkout is needed to
follow either. Nothing a session needs lives outside the boundary.

## 2 · Where we are (this session's work)

Starting point was `fcfb637` (the previous handoff). Everything since is the polyglot SDK buildout
this handoff was written to capture — far more than fits as a commit list, so grouped by theme
instead:

- **Three language SDKs built out complete, each on its own open PR:**
  - **JS/TS** — [PR #3](https://github.com/pawan-vyas/verdict-rules/pull/3), branch `plan/js-sdk`.
    Full package under `js/packages/verdict-rules/` (source, tests, README, CHANGELOG, `docs/
    quickstart.md`), `js/AGENTS.md`. npm Trusted Publishing bootstrapped by hand: a throwaway
    `0.0.0` placeholder published manually (npm has no PyPI-style pending-publisher — see
    `docs/maintenance/releases/js.md`), account 2FA enabled, Trusted Publisher configured against
    `release-js.yml`. `0.0.1` (the real first version) will publish from CI once this PR merges and
    the tag lands. The minified CDN/global build was dropped before that happens — Socket.dev
    flagged it, and the transfer-size saving doesn't justify the ding for a library this size (see
    `js/AGENTS.md`'s "Distribution shape is effectively permanent").
  - **C#** — [PR #5](https://github.com/pawan-vyas/verdict-rules/pull/5), branch
    `plan/csharp-sdk`. Full package under `csharp/src/VerdictRules/`, `csharp/AGENTS.md`. One
    explicitly recorded open question not yet resolved: how far back `TargetFrameworks` should
    reach (`netstandard2.0` for .NET Framework reach) — decide before `0.0.1` actually publishes to
    NuGet, not before this PR merges.
  - **Dart** — [PR #7](https://github.com/pawan-vyas/verdict-rules/pull/7), branch
    `plan/dart-sdk`. Full package under `dart/packages/verdict_rules/`, `dart/AGENTS.md`. Verified
    against `pana` (pub.dev's own analyzer, run locally — no publish needed): `150/160` pub points,
    the one gap being a false negative of testing against this unmerged branch (`pana` clones the
    `repository:` field at `main`, where `dart/` doesn't exist yet — resolves on merge).
- **Python `0.2.3` cut and released** — a docs-only patch (package identical to `0.2.2`), confirmed
  live on PyPI.
- **Three new doc-authoring templates**, joining `package-readmes.md`:
  `docs/maintenance/doc-authoring/language-agents.md` (every language's own `AGENTS.md`),
  `skill-agent-notes.md` (`skills/verdict/references/<language>/agent-notes.md`),
  `release-procedures.md` (`docs/maintenance/releases/<language>.md`) — each grounded in real
  convergence across the languages that already existed, not invented from scratch.
- **Two full doc audits**, resolved via interactive review (recommended fix presented, user
  decided): bare-backtick doc references real-linked across all four `AGENTS.md` files; AGENTS.md
  title suffix and section order standardized to the template (`# AGENTS.md — <Language> SDK`
  everywhere, `csharp`/`dart`'s section order corrected to match); JS's initial CHANGELOG entry
  rewritten to drop self-narration ("claiming the name", "arrives before 0.1.0") matching Python's
  factual voice; `"only Python ships today"` dropped entirely (not updated) from `plugin.json`,
  `marketplace.json`, `CONTRIBUTING.md`, and the root `README.md` — the Quickstart section and the
  new Status table (below) are the actual source of truth now, so the sentence never needs editing
  again as a language ships.
- **Root `README.md` restructured**: the top-of-file badge line became a `## Status` section at the
  end — one row per language plus a standing last row for the AI-agent skill, live-queried badges
  throughout (PyPI/npm version, GitHub Actions tests, a GitHub-tag-filtered skill version, Socket.dev
  linked rather than embedded since its badge image sits behind a Cloudflare check that blocks
  GitHub's own image proxy). `## Where to go next` gained JS/TS's own quickstart rows, mirroring
  Python's.
- **This handoff refresh** — the previous one was 47 commits stale.

Currently open, independently mergeable: **PR #3** (JS/TS), **PR #5** (C#), **PR #7** (Dart), and
**[PR #50](https://github.com/pawan-vyas/verdict-rules/pull/50)** (`docs/audit-fixes-round2` — the
doc-audit fixes above that touch shared/root files, kept off the language branches on purpose to
avoid conflicts between them).

## 3 · What to do next (prioritized)

1. **Merge PR #50 first** — small, green, touches only shared/root files
   (`plugin.json`/`marketplace.json`/`CONTRIBUTING.md`/`README.md`/the doc-authoring templates).
   After merging, rebase PRs #3/#5/#7 onto the new `main` (each has rebased cleanly every time so
   far — no real conflicts across any of them).
2. **Review and merge PR #3 (JS/TS)** when ready, then cut `js-v0.0.1`: bump
   `js/packages/verdict-rules/package.json`'s version (already `0.0.1`), add the CHANGELOG entry
   (already written), merge, tag. `release-js.yml` publishes over the already-configured Trusted
   Publisher — no manual `npm publish` needed this time.
3. **Review and merge PR #5 (C#) and PR #7 (Dart) independently**, whenever each is ready — read
   that language's own `docs/maintenance/releases/<language>.md` before cutting its first release;
   each registry's first-publish mechanics are genuinely different (NuGet can bootstrap over
   Trusted Publishing directly; pub.dev cannot, same manual-first-publish trap npm had). C#'s target
   -framework question (§2) needs an explicit decision before that first publish, not before merge.
4. **Once a language actually ships its first real version**: add its keyword to `plugin.json`
   (deferred on purpose — edit only after rebasing post-release, to avoid conflicts with an
   in-flight branch) and confirm its `README.md` Status-table row's badges resolve for real (JS/TS's
   row already exists, currently showing the `0.0.0` placeholder state since it's a live-queried
   badge — it self-updates once `0.0.1` actually publishes, nothing to edit by hand).
5. **`skills/verdict-workspace/evals/js/*.json`** (4 cases) exist, validate cleanly through
   `scripts/build_evals.py`, and are meant to run via the **skill-creator** plugin (installed,
   user-scope) against a local build of `verdict-rules` — not the generic `claude plugin eval` CLI
   command, whose native `case.yaml`/`graders/*.md` format this repo's evals don't use. Check
   whether they were actually run in this same session before assuming they still need it.

## 4 · Known issues / blockers

- **Several links are transiently broken on `main` right now, all self-healing on their own PR's
  merge** — confirmed via `.agents/scratch/check_links.py`, not a bug to chase down: `docs/
  maintenance/releases/csharp.md` and `dart.md` each link to that SDK's own `CHANGELOG.md`, which
  doesn't exist on `main` until `plan/csharp-sdk`/`plan/dart-sdk` merge; `README.md`'s new `js/`
  rows under "Where to go next" likewise, until `plan/js-sdk` merges.
- **Socket.dev's badge image cannot be embedded in `README.md`** — confirmed directly:
  `badge.socket.dev` sits behind a Cloudflare bot check that returns the same 403 to GitHub's image
  proxy as to a plain `curl`, regardless of pinned-vs-`latest` version. Every Supply Chain cell in
  the Status table links to the live scorecard instead of an `<img>`.
- **C#'s target-framework question is explicitly unresolved** (§2) — do not resolve it unprompted;
  it's recorded with its full reasoning in `csharp/AGENTS.md`.
- No open defect in any shipped package itself.

## 5 · Verify (gate / test commands)

Each language's SDK lives only on its own open PR's branch until merged — `main` only has `python/`
today.

```bash
# Python (on main)
cd python && uv sync && uv run pytest

# JS/TS (checkout plan/js-sdk first)
cd js && npm run typecheck && npm run build && npm test

# C# (checkout plan/csharp-sdk first) -- no solution file, name the project explicitly
cd csharp
dotnet build src/VerdictRules/VerdictRules.csproj -warnaserror
dotnet test tests/VerdictRules.Tests/VerdictRules.Tests.csproj

# Dart (checkout plan/dart-sdk first)
cd dart/packages/verdict_rules && dart analyze && dart test
```

For a doc change, additionally validate every mermaid diagram touched:

```bash
node .claude/skills/mermaid-diagrams/scripts/validate_diagrams.js --markdown <file.md>
```

Real relative-link checking (not grep):

```bash
python3 .agents/scratch/check_links.py
python3 scripts/check_shipped_links.py
npx --yes markdownlint-cli@0.49.1 "**/*.md"
```

For a change to the distribution scripts, confirm all three skill artifacts still build:

```bash
bash scripts/build.sh && ls dist/   # verdict-plugin.zip  verdict-tools.zip  verdict.skill
```

## 5b · Tooling / skills

- **Python** ≥3.10 (floor in `python/packages/verdict-rules/pyproject.toml`), managed with
  **`uv`**. Zero runtime dependencies.
- **Node** 18+ for JS/TS (`npm`, workspace-rooted at `js/`); also needed for the mermaid diagram
  validator regardless of language.
- **.NET SDK** (targets `net8.0`/`netstandard2.1`) for C#.
- **Dart SDK** ≥3.0.0 for Dart. **`pana`** (pub.dev's own package analyzer —
  `dart pub global activate pana`) reproduces pub.dev's real scoring locally, no publish required;
  used this session to find and fix a real formatting deficit before it ever shipped.
- **`jq`** only by CI workflows (preinstalled on `ubuntu-latest`).
- **Vendored skills**, mirrored to both `.agents/skills/` and `.claude/skills/`:
  `mermaid-diagrams` (diagram standards + validator) and `context-fence` (the operations manual
  this file is the exit seal of). Both are tool-owned: refresh wholesale, never hand-edit.
- `skills/verdict/` is the *shipped* skill; `skills/verdict-workspace/` is its eval working
  material — the per-target eval JSON files under `evals/<target>/` are tracked, the assembled
  `evals/evals.json` is gitignored. Evals run via the **skill-creator** plugin, not `claude plugin
  eval` (see §3, item 5).

## 6 · Key docs

- [`AGENTS.md`](AGENTS.md) — standing rules for every agent (`CLAUDE.md` is just `@AGENTS.md`);
  each language's own `AGENTS.md` (`python/AGENTS.md` on `main`; `js/AGENTS.md`, `csharp/AGENTS.md`,
  `dart/AGENTS.md` on their own open PRs) adds that language's own layer.
- [`docs/architecture/`](docs/architecture/README.md) — design source of truth: why evaluation is
  sequential, why `Rule` is structural, why `RuleResult.data` stays opaque.
- [`docs/maintenance/doc-authoring/`](docs/maintenance/doc-authoring/README.md) — every doc-category
  template this repo enforces by review: `package-readmes.md`, `language-agents.md`,
  `skill-agent-notes.md`, `release-procedures.md`, and more.
- [`docs/maintenance/releases/`](docs/maintenance/releases/README.md) — the shared release pipeline
  plus each registry's own real mechanics (`python.md`, and — once merged — `js.md`, `csharp.md`,
  `dart.md`).
- [`docs/extending/`](docs/extending/README.md) — the seven extension scenarios;
  `domain-adapter-module/`'s one-adapter-module boundary is the pattern to steer consumers toward.
- [`docs/testing/`](docs/testing/README.md) — the testing checklist, shared across every language.
- [`docs/future_plan.md`](docs/future_plan.md) — exploratory candidates, explicitly not a roadmap.
- [`python/examples/graduation_verdict/`](python/examples/graduation_verdict/) — the worked example,
  including the oracle/differential chaos suite worth re-deriving in any future language.
- [`.agents/README.md`](.agents/README.md) — the agent working-material layout, and
  [`.agents/memory/`](.agents/memory/) — durable facts worth not re-deriving, including
  [`adding-a-variant-is-a-new-file.md`](.agents/memory/adding-a-variant-is-a-new-file.md) (why
  PR #50 stays off the language branches) and
  [`research-the-ecosystem-before-deciding-its-idiom.md`](.agents/memory/research-the-ecosystem-before-deciding-its-idiom.md).
