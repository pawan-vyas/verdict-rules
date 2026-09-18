---
kind: session-handoff
handoff_schema: 1
updated_utc: 2026-09-18T09:23:35Z
updated_local: 2026-09-18T14:53:35+05:30
branch: main
state_at_commit: 53399f7ff1431ffb33cba006e1362c363f7c2250
state_at_commit_short: 53399f7
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

**The generics program (GitHub issue #33) is fully closed.** All four language SDKs and the skill
are on `main`, released, live on their registries. **One small PR is open** (#95, a planning doc for
the next piece of work) — see §2 and §3.

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

**One untracked, unexplained file sits in the working tree as of this handoff**:
`skills/verdict-workspace/evals/csharp/.gitignore` (contents: `bin/`/`obj/`). It predates this
session's own changes and wasn't touched by anything recorded in §2 — flagged rather than silently
committed or deleted, since its origin isn't known. Look at it before doing either.

## 1 · What this project is (one paragraph)

`verdict` is a small, zero-dependency, async-native rule-evaluation engine —
`Rule`/`FunctionRule`/`AndRule`/`OrRule`/`RulesEngine`/`RuleResult`/`RunResult` — designed to exist
in more than one language with identical execution-model guarantees: sequential, never concurrent,
evaluation so short-circuiting is a real contract rather than an optimization, and vacuous-truth
polarity decided explicitly per composite shape. **All four language SDKs are on `main`, all at
`v0.3.0`, all live**: Python (PyPI `verdict-rules`), JS/TS (npm `verdict-rules`), C# (NuGet
`VerdictRules`), Dart (pub.dev `verdict_rules`) — each with its own top-level directory, its own
`AGENTS.md`, and its own `docs/maintenance/releases/<language>.md`. `Rule` (or each language's own
idiom) is generic over its context (`Rule<TContext>`/`Rule[TContext]`) in every language as of this
release; dict-context stays permanently first-class alongside it — see
[`docs/architecture/README.md`](docs/architecture/README.md#generic-context). Cross-language docs
are in `docs/`, the AI-agent skill in `skills/verdict/` (vendored into other projects by
`scripts/install.sh`), the published site in `site/`, and agent working material in `.agents/`.

## 1b · External references (NOT part of this repo)

None. The repo is fully self-contained: both vendored skills (`mermaid-diagrams`, `context-fence`)
are in-tree copies, so no network, plugin install, or sibling checkout is needed to follow either.

## 2 · Where we are (this session's work)

**The generics program (issue #33) closed out completely this session**, in this order:

1. **PR #94 merged** — the full `Rule<TContext>` migration across all four languages, the new
   cross-language `marketplace_eligibility` fixture, and (caught mid-review by the user, not by any
   automated check) a real `AGENTS.md` dispatch-rule violation: `ruleForSubject`'s
   `subject_type`/`subjectType`/`SubjectType` dispatch used a sequential `if`-chain (Python, JS, C#)
   or a `switch` (Dart) — the exact predicate-chain shape the dispatch rule forbids, in the flagship
   `graduation_verdict` example, in **all four languages**. A full manual (not grep) read-through of
   every real source file in the repo confirmed this was isolated to that one dispatch site plus the
   same shape one level down in each language's chaos-data generator — 8 files, fixed as a
   builder-per-variant table in each language's own idiom, plus the 5 docs whose own extension
   instructions ("adding a subject type needs a new branch...") went stale the moment that became a
   lookup. The oracle files in each language were confirmed as the one correctly sanctioned exception
   (explicitly documented "deliberately the naive way," used as independent differential-testing
   ground truth) and left untouched. Full verification after every fix: all four languages' full test
   suites green (Python 650, JS 644, C# 650, Dart 645), zero warnings, clean `dart analyze`.
   `main` was **not mergeable via the normal path** — required-review branch protection
   (`required_approving_review_count: 1`, CODEOWNERS-backed) blocked it; merged with explicit user
   authorization via `gh pr merge --admin`, bypassing that protection deliberately, not by default.
2. **All five releases fired and confirmed live**: `python-v0.3.0`, `js-v0.3.0`, `csharp-v0.3.0`,
   `dart-v0.3.0`, `skill-v0.6.0` (skill version stayed independent, per its own changelog's stated
   cadence — only the four language packages realigned to `0.3.0`). C#'s release hit a **known,
   previously-documented failure mode**: the actual NuGet push succeeded (confirmed via the push job's
   own log — both `.nupkg`/`.snupkg` got `201 Created`), but the workflow's own post-publish
   `verify-published` step exhausted its 20×30s (~10min) retry budget against nuget.org's flatcontainer
   endpoint before nuget.org's own indexing caught up — the same shape of gap the workflow's code
   comments already record happening at `csharp-v0.0.1` and `js-v0.0.4`. Confirmed live via direct
   `curl` roughly 15 minutes after the push (not a re-run, not a fix — nuget.org's own indexing simply
   finished). **This is the third occurrence of this exact pattern** — worth a real fix (not another
   budget widening) if it recurs a fourth time; not recorded as a formal incident this session per the
   user's own choice (monitor-and-confirm, not incident-write-up).
3. **Program closed out on GitHub, not just in code**:
   - [Issue #33](https://github.com/pawan-vyas/verdict-rules/issues/33) (the generics tracking issue)
     — closed, linked to PR #94 and the five live releases.
   - [PR #84](https://github.com/pawan-vyas/verdict-rules/pull/84) (the generics plan doc, branch
     `plans/generics-v0.3.0`) — **closed without merging**, matching the
     `plan/python-namespacing` (#9) precedent: its real design rationale is already captured in
     `docs/architecture/README.md`'s own "Generic context" section via PR #94, so the raw planning doc
     was redundant rather than something that needed to land on `main`. Branch deleted, both remotely
     and locally, per explicit user instruction (PRs themselves can't be deleted on GitHub — only
     closed; confirmed this isn't a permissions gap, it's a platform limitation).
   - [Issue #76](https://github.com/pawan-vyas/verdict-rules/issues/76) ("bring JS/Dart/C# to Python's
     test parity, then add the graduation-fixture layer") — found open and stale during this closeout
     sweep; the work it describes was already fully done via PRs #88–93 (merged before this session's
     own generics work began). Closed with a pointer to those six PRs.
   - [Issue #39](https://github.com/pawan-vyas/verdict-rules/issues/39) — found during the same sweep,
     checked, **confirmed still genuinely open and valid**: the "naive way" sections in three specific
     samples (`shipping-fee-waiver`, `loyalty-tier-promotion`, `data-driven-rule-sets`) don't land the
     same "I recognize this" hook `dynamic-discounts` does. Nothing in the generics program touched it.
     Left open deliberately — noted as a related-but-orthogonal candidate in the new plan doc (next
     bullet), not resolved.
   - Branch cleanup: `main`, `generics/v0.3.0-rule-context`, and `plans/generics-v0.3.0` are the only
     branches this session touched directly; all fully merged or explicitly closed-and-deleted, both
     locally and on GitHub. (A larger, separate branch-cleanup pass earlier this same session removed
     19 more already-merged branches unrelated to the generics program itself — see the commit log
     around `git log --oneline b7e7fa9~30..b7e7fa9` if that context is ever needed again.)
4. **A new planning doc opened as [PR #95](https://github.com/pawan-vyas/verdict-rules/pull/95)**
   (branch `docs/post-generics-rebalance-plan`, CI green, doc-only), recording two follow-on problems
   the user identified once the generics program landed, explicitly scoped as one future PR:
   - **Sample/extending docs are dict-context-biased** — measured, not assumed: 6/10
     `docs/samples/*/python.md` and 7/8 `docs/extending/*/python.md` are confirmed dict-shaped; only
     this generics cycle's own additions (`marketplace-eligibility`,
     `reusing-a-rule-across-contexts`) show typed context. Not a factual error anywhere — a
     statistical bias in what an agent learns from reading the docs, since dict-context is documented
     as "exactly as first-class as typed, never a fallback" but the sample mix doesn't reflect that.
   - **Skill token economics** — `MANIFEST.toml`'s 18 near-identical `fetch_group` entries are
     mechanically compressible without losing the file's own "hand-authored, never silently enlarges
     the skill" guardrail; `agent-notes.md`'s shared boilerplate (confirmed via a direct diff between
     Python's and Dart's own files) repeats near-verbatim across all four languages, with a real
     bundled-vs-shared-doc tension to resolve, not a trivial cut. **Confirmed, not assumed, as
     already-correct**: the two docs added this generics cycle are already properly wired into
     `MANIFEST.toml` and resolve via `scripts/skill_manifest.py fetch` — checked directly before
     writing the plan, in response to the user asking specifically whether this was a gap.
   - Full detail, including which specific samples are plausible typed-context candidates and why
     (and, just as importantly, which ones should explicitly stay dict-context) in
     [`.agents/plans/post-generics-docs-and-skill-rebalance/README.md`](.agents/plans/post-generics-docs-and-skill-rebalance/README.md).

**Full commit range this session, `main`, oldest first** (from the last point `main` and the
generics branch diverged through the merge and everything after):

```text
c18277b python: add generic context to Rule (Rule[TContext])
7f812bb js: add generic context to Rule (Rule<TContext>)
79e957e csharp: add generic context to IRule (IRule<TContext>)
f12406b dart: add generic context to Rule (Rule<TContext>)
a9d138d docs: shared cross-language generic-context sweep + skill version bump
90ad8b5 fix: repin shipped links to each language's new version tag
2ee069b dart: fix graduation_verdict example for the Rule<TContext> migration
ce413cd New cross-language fixture: marketplace_eligibility (proves the generic path)
b4c929c js: implement the marketplace_eligibility fixture (proves the generic path)
44504d7 csharp: implement the marketplace_eligibility fixture (proves the generic path)
6f38ceb dart: add the marketplace_eligibility fixture port
9efc95a docs(handoff): refresh session state @ 6f38ceb
cae5619 vscode: fix false 'Import could not be resolved' on python/'s workspace venv
a29eeaa python: replace subject_type dispatch chains with a lookup table
d787555 python/examples: add the missing marketplace_eligibility index row
f2ca83c js: replace subjectType dispatch chains with a lookup table
035497b csharp: replace SubjectType dispatch chains with a lookup table
9000e3a dart: replace subjectType dispatch switch/chains with a lookup table
8a5fbd9 docs: fix extension instructions stale after the dispatch-table refactor
dbc0404 docs(handoff): refresh session state @ 8a5fbd9
b7e7fa9 chore: archive the completed pre-generics SDK plan docs
95c5e01 release: align all four language packages on v0.3.0
53399f7 Merge pull request #94 from pawan-vyas/generics/v0.3.0-rule-context
```

## 3 · What to do next (prioritized)

1. **Get PR #95 reviewed and merged** (or, if it's genuinely fine to fast-track, use the same explicit
   admin-bypass path as PR #94, but only on explicit instruction — it's a low-stakes doc-only PR, not
   a reason to skip asking). Nothing blocks it; CI is green.
2. **After #95 merges, start the plan it records** —
   [`.agents/plans/post-generics-docs-and-skill-rebalance/README.md`](.agents/plans/post-generics-docs-and-skill-rebalance/README.md)
   has the full scope: rebalancing sample/extending docs' dict-vs-typed mix (with specific candidate
   samples already triaged), and the skill's `MANIFEST.toml`/`agent-notes.md` token-economics pass.
   Read it before starting — it also records what the fix is **not** (don't invert the bias, don't cut
   the per-language "mistakes" sections, don't break the hand-authored `MANIFEST.toml` guardrail).
3. **Decide on [issue #39](https://github.com/pawan-vyas/verdict-rules/issues/39)** — fold into the
   same PR as §3.2 (it touches the same three sample files the docs-bias fix already identifies as
   typed-context candidates) or keep it separately triaged. Not decided; ask rather than assume either
   way, per the plan doc's own §3.
4. **If C#'s NuGet-indexing-lag pattern recurs a fourth time**, it's worth writing a real
   `.agents/incidents/` entry and considering a structural fix (a `workflow_dispatch`-triggered
   standalone re-verify job, rather than baking an ever-widening retry budget into the release
   workflow itself) instead of widening the budget a third time.

## 4 · Known issues / blockers

- No open defect in any shipped package itself.
- The untracked `skills/verdict-workspace/evals/csharp/.gitignore` noted in §0.1 — origin unknown,
  not part of this session's own changes, flagged rather than acted on.
- No CI pipeline gate exists yet for any language's test suite running on every PR touching that
  language's path — see `docs/future_plan.md` if this is ever picked up. (Distinct from the *release*
  workflows, which do gate on the version-vs-tag check and are working correctly.)
- C#'s NuGet publish-verification step has now hit its documented propagation-lag failure mode three
  times (`csharp-v0.0.1`, `js-v0.0.4`'s npm equivalent, and this session's `csharp-v0.3.0`) — see §3.4.

## 5 · Verify (gate / test commands)

```bash
# Python
cd python && uv sync && uv run pytest

# JS/TS -- tests import from a built dist/, so build first
cd js && npm install && npm run build --workspaces --if-present && npm test

# C# -- no solution file, name each project explicitly
cd csharp
dotnet build -warnaserror src/VerdictRules/VerdictRules.csproj
dotnet test tests/VerdictRules.Tests/VerdictRules.Tests.csproj
dotnet build -warnaserror examples/GraduationVerdict/GraduationVerdict.csproj
dotnet test examples/GraduationVerdict.Tests/GraduationVerdict.Tests.csproj
dotnet build -warnaserror examples/MarketplaceEligibility/MarketplaceEligibility.csproj
dotnet test examples/MarketplaceEligibility.Tests/MarketplaceEligibility.Tests.csproj

# Dart -- core package, then each standalone example package
cd dart/packages/verdict_rules && dart pub get && dart analyze . && dart test
cd ../../examples/graduation_verdict && dart pub get && dart analyze . && dart test
cd ../marketplace_eligibility && dart pub get && dart analyze . && dart test
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
- **.NET SDK** (targets `net10.0`/`netstandard2.1`) for C#. Test suite is **`xunit`** +
  `Microsoft.NET.Test.Sdk` + `xunit.runner.visualstudio` only.
- **Dart SDK** ≥3.0.0 for Dart. **`pana`** (pub.dev's own package analyzer) reproduces pub.dev's real
  scoring locally, no publish required.
- **`jq`** only by CI workflows (preinstalled on `ubuntu-latest`).
- **Vendored skills**, mirrored to both `.agents/skills/` and `.claude/skills/`: `mermaid-diagrams`
  (diagram standards + validator) and `context-fence` (the operations manual this file is the exit
  seal of). Both are tool-owned: refresh wholesale, never hand-edit.
- `skills/verdict/` is the *shipped* skill; `skills/verdict-workspace/` is its eval working
  material — the per-target eval JSON files under `evals/<target>/` are tracked, the assembled
  `evals/evals.json` is gitignored. Evals run via the **skill-creator** plugin, not `claude plugin
  eval`.

## 6 · Key docs

- [`AGENTS.md`](AGENTS.md) — standing rules for every agent (`CLAUDE.md` is just `@AGENTS.md`);
  each language's own `AGENTS.md` adds that language's own layer. Its dispatch-table rule is what
  this session's own review caught a real violation against — read it again if extending any example.
- [`docs/architecture/README.md`](docs/architecture/README.md#generic-context) — design source of
  truth, including the "Generic context" subsection this program added.
- [`.agents/plans/post-generics-docs-and-skill-rebalance/README.md`](.agents/plans/post-generics-docs-and-skill-rebalance/README.md) —
  the next piece of work, full scope and reasoning.
- [`docs/testing/`](docs/testing/README.md) — the testing checklist, shared across every language.
- [`docs/extending/`](docs/extending/README.md) — the eight extension scenarios.
- [`docs/samples/`](docs/samples/README.md) — the ten worked samples §2's new plan doc audits.
- [`docs/maintenance/releases/`](docs/maintenance/releases/README.md) — the shared release pipeline
  plus each registry's own real mechanics, including the NuGet propagation-lag pattern from §2/§4.
- [`docs/future_plan.md`](docs/future_plan.md) — exploratory candidates, explicitly not a roadmap.
- [`.agents/README.md`](.agents/README.md) — the agent working-material layout, and
  [`.agents/memory/`](.agents/memory/) — durable facts worth not re-deriving.
