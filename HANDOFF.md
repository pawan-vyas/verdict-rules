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

Branch `diagnostics/debugger-display-all-languages`, PR #103, covering issues #98/#100/#101.
Everything below is committed and pushed; nothing is released.

**The branch started as a diagnostics/inspection pass and turned up two real defects in its own
new, unreleased surface.** Both were fixed in all four languages after being put to the user as
decisions rather than assumed:

1. **A result's serialized size was exponential in nesting depth.** `decidedBy` stored the same
   `RuleResult` objects already held in `subResults`, making the stored graph a DAG; every
   tree-shaped walk expands a shared node once per path. Measured: 3,223 chars at depth 4, 13.6 MB
   at depth 16, and `OutOfMemoryException` in C# at depth 40. The fix stores *positions*
   (`decidedByIndices` / `decided_by_indices` / `DecidedByIndices`) and derives `decidedBy` from
   them. Depth 16 is now 1,942 chars. The indices form also admits a construction-time range check
   the objects form could not: an index naming a non-child raises.
2. **Nothing anywhere proved a result could leave the process.** Python, C# and Dart had zero
   serialization tests; JS had one `length > 0` assertion. Dart could not be encoded at all, since
   `jsonEncode` needs a `toJson()` — now added to both result types, that SDK's own convention.

**Current state, all measured rather than asserted:**

| Language | Package tests | Mutation score | Survivors |
| :-- | --: | --: | :-- |
| Python | 143 (100% line) | 241/242 (99.6%) | 1, equivalent |
| C# | 171 | 174/176 (98.9%) | 2 (1 equivalent, 1 tool mis-attribution) |
| JS | 144 (100% line/branch/func) | 238/243 (97.9%) | 5, all equivalent |
| Dart | 133 | 57/57 (100%) | 0 |

Example suites: Python 1,420 total from `python/`, JS 1,765 + 33, C# 1,744 + 32, Dart 1,271 + 32.

**Two mutation findings worth not relearning.** A previous C# survivor record filed nine
`ConfigureAwait(false)` mutants as an *equivalence class*; they were not equivalent, only
unobservable to every assertion on a result. `ContextCaptureTests.cs` kills them with a recording
`SynchronizationContext`, which moved C# from 92.0% to 98.9%. And Dart's 100% is the weakest of the
four, not the strongest: its tool generates 57 mutations where mutmut generates 242, has no ternary
rule, and so verifies none of that language's derived accessors — the oracle suite carries them.

**The documentation sweep is complete.** All four testing pages rebuilt against measured numbers
(three claimed no CI test workflow existed; all four exist and gate every PR). `docs/architecture/`
carries the stored-versus-derived fact and four revalidated class diagrams. All four survivor
records rewritten. Every landing quickstart and every per-language quickstart now shows a typed
context, each verified by execution rather than inspection. The skill, its four `agent-notes.md`,
four package changelogs and the skill changelog describe the final shape.

**`scripts/check_package_readmes.py` was fixed, not just worked around.** Its C# check cleared
`packageSources` but not the global packages folder, so it extracted into `~/.nuget/packages` and a
same-version entry left there satisfied a later restore before the freshly packed `.nupkg` was
opened. It was seeding the staleness that then broke it. The dangerous direction was never the
failure but the pass — the same shadowing would confirm a README against stale code just as
silently.

## 3 · What to do next (prioritized)

1. **Cut the four RC tags, then run the skill evals, then promote 0.4.0.** The evals currently pin
   `verdict-rules==0.3.1`, so they validate the previous surface and only become meaningful once an
   RC exists. Tags are `<language>-v<version>` per `SKILL.md`; **Dart's release workflow triggers on
   `dart-v*`**, so an RC tag there fires a real publish path — the prerelease guard in
   `release-dart.yml`'s phase-detection step is in place for exactly this. Confirm before pushing
   any tag; this is the one outward-facing step left.
2. **Run the six new evals sequentially** — never in parallel, per
   [`.agents/memory/`](.agents/memory/). Two cross-language (which result view to read; serializing
   a result) and one per language (build the "at least N of these" composite, which cannot be
   completed without that language's own spelling).
3. **Then the ground-up rewrite, as its own long-term PR.** Explicitly deferred: the rule-engine
   redesign, the CEL-profile decision, and the new-package/new-name question all belong there, not
   here. This branch is the scoped unblocking release.

## 4 · Known issues / blockers

- No open defect in any shipped package. Both defects found this session were in unreleased surface
  and are fixed.
- **Nothing is released from this branch yet.** Four packages sit at `0.4.0` in their manifests and
  changelogs with no corresponding tag; the skill sits at `0.8.0`, also untagged (`skill-v0.7.0` is
  the latest released).
- The untracked `skills/verdict-workspace/evals/csharp/.gitignore` — origin unknown, outside this
  session's changes, flagged rather than acted on.
- C#'s NuGet publish-verification step has hit its documented propagation-lag failure mode three
  times (`csharp-v0.0.1`, `js-v0.0.4`'s npm equivalent, `csharp-v0.3.0`). If it recurs a fourth
  time it earns an `.agents/incidents/` entry and a structural fix — a `workflow_dispatch`
  re-verify job rather than an ever-widening retry budget.

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

Real relative-link checking (not grep), and the linter CI actually runs:

```bash
python3 scripts/check_shipped_links.py
python3 scripts/check_doc_comment_links.py
npx --yes markdownlint-cli2@0.18.1        # cli2, not cli -- matches lint-docs.yml
```

Every repo consistency gate, all of which must pass before a release:

```bash
for s in check_api_snapshots check_changelogs check_package_readmes \
         check_package_metadata check_fixture_coverage check_doc_comment_links \
         check_shipped_links check_workflow_shell; do
  python3 "scripts/$s.py" || echo "FAILED: $s"
done

# Dart's API snapshot needs dart-apitool on PATH, installed separately:
#   dart pub global activate dart_apitool
PATH="$PATH:$HOME/.pub-cache/bin" python3 scripts/check_api_snapshot_dart.py

python3 scripts/build_evals.py            # evals.json is generated, never committed
bash scripts/build.sh                     # all three skill artifacts still assemble
```

Mutation testing, one language at a time — never in parallel, and clear each
tool's own cache first or a stale run reports against code that no longer
exists:

```bash
bash scripts/run_mutation_python.sh       # rm -rf python/packages/verdict-rules/mutants first
bash scripts/run_mutation_csharp.sh       # rm -rf csharp/tests/VerdictRules.Tests/StrykerOutput first
bash scripts/run_mutation_js.sh
bash scripts/run_mutation_dart.sh         # rm -rf dart/packages/verdict_rules/mutation-test-report first
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
- the post-generics rebalance plan (since removed) —
  the next piece of work, full scope and reasoning.
- [`docs/testing/`](docs/testing/README.md) — the testing checklist, shared across every language.
- [`docs/extending/`](docs/extending/README.md) — the eight extension scenarios.
- [`fixtures/README.md`](fixtures/README.md) — the ten worked samples §2's new plan doc audits.
- [`docs/maintenance/releases/`](docs/maintenance/releases/README.md) — the shared release pipeline
  plus each registry's own real mechanics, including the NuGet propagation-lag pattern from §2/§4.
- [`docs/future_plan.md`](docs/future_plan.md) — exploratory candidates, explicitly not a roadmap.
- [`.agents/README.md`](.agents/README.md) — the agent working-material layout, and
  [`.agents/memory/`](.agents/memory/) — durable facts worth not re-deriving.
