---
kind: session-handoff
handoff_schema: 1
updated_utc: 2026-10-07T04:57:12Z
updated_local: 2026-10-07T10:27:12+05:30
branch: diagnostics/debugger-display-all-languages
state_at_commit: 706c181378391b94f51222c1fb07f9f0cecb3c9e
state_at_commit_short: 706c181
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

**One PR is open and is the whole of the current work**: #103 on branch
`diagnostics/debugger-display-all-languages`, covering issues #98/#100/#101. It carries unreleased
`0.4.0` for all four SDKs and unreleased `0.8.0` for the skill. **Release tags are deliberately on
hold** — see §2.

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

The working tree is clean as of this handoff, with no untracked files.

## 1 · What this project is (one paragraph)

`verdict` is a small, zero-dependency, async-native rule-evaluation engine —
`Rule`/`FunctionRule`/`AndRule`/`OrRule`/`NotRule`/`RulesEngine`/`PredicateOutcome`/`RuleResult`/
`RunResult` — designed to exist in more than one language with identical execution-model guarantees:
sequential, never concurrent, evaluation so short-circuiting is a real contract rather than an
optimization, and vacuous-truth polarity decided explicitly per composite shape. **All four language
SDKs are live on their registries at `0.3.1`**: Python (PyPI `verdict-rules`), JS/TS (npm
`verdict-rules`), C# (NuGet `VerdictRules`), Dart (pub.dev `verdict_rules`) — each with its own
top-level directory, its own `AGENTS.md`, and its own `docs/maintenance/releases/<language>.md`.
`Rule` is generic over its context in every language; dict-context stays permanently first-class
alongside a typed one — see
[`docs/architecture/README.md`](docs/architecture/README.md#generic-context). Cross-language docs
are in `docs/`, the AI-agent skill in `skills/verdict/` (vendored into other projects by
`scripts/install.sh`), the published site in `site/`, and agent working material in `.agents/`.

## 1b · External references (NOT part of this repo)

None. The repo is fully self-contained: both vendored skills (`mermaid-diagrams`, `context-fence`)
are in-tree copies, so no network, plugin install, or sibling checkout is needed to follow either.

## 2 · Where we are (this session's work)

Branch `diagnostics/debugger-display-all-languages`, PR #103 (open, not draft), covering
issues #98, #100 and #101. Everything is committed; nothing is released.

**Release tags are on hold by explicit decision.** Every adopter is running a locally cloned, pinned
working tree so they can read the source and suggest changes, so there is no pressure to publish —
and `skills/verdict/SKILL.md` now documents that case as the first branch of its version-resolution
instruction. Merging this branch *is* the release for all four SDKs and the skill, so do not merge
until the release decision is actually made.

Version state: all four SDKs at **`0.4.0`, unreleased** (no `<language>-v0.4.0` tag exists). The
skill is at **`0.8.0`, unreleased**; the last released skill is `skill-v0.7.0`, so that is a single
minor bump with one changelog entry and no intermediate versions.

### What the branch did, in order

1. **Diagnostics/inspection surface** (the original scope): rules, composites and engines print as
   more than an opaque object, in each language's own idiom.
2. **Two real defects in that new, unreleased surface**, found while testing it and fixed in all
   four languages after being put to the user as decisions:
   - **Exponential serialization.** A result stored its derived views, making the object graph a DAG
     that a tree-shaped encoder expands once per path. Measured at six depths; emptying the deciding
     view took a depth-16 result from 13.6 MB to 1,790 characters. Fixed by storing *positions*
     (`decided_by_indices` and its per-language spellings) and deriving every view on access.
   - **`RunResult`'s failing-leaves view filtered instead of forwarding.** A result's verdict is not
     a function of its leaves' verdicts, so filtering was wrong in both directions. Present
     identically in all four SDKs, and **three suites contained a test asserting the defect as
     intentional**, which is why nothing caught it.
3. **`NotRule` shipped** in all four languages, along with the public `SequentialEvaluator` /
   `ShortCircuitEvaluator`, Dart's `toJson()`, and a const `RuleResult.leaf`.
4. **The shared fixtures gained failing-leaves expectations**, which they had none of — the gap that
   let (2) go undetected. Values picked by hand from each scenario, not from implementation output.
5. **`docs/extending/` rebuilt** — all eight scenarios across four languages, every predicate moved
   to `PredicateOutcome`, every `result.data` read moved to sub-results.
6. **`docs/maintenance/` swept** — all 31 files read whole, 19 defects fixed.
7. **A full documentation audit of everything else**, 80+ files read whole, ~110 defects fixed. Run
   as a seven-group sequential workflow after the earlier passes were done by hand. Highlights:
   `docs/future_plan.md` argued at length against `NotRule`, which ships; the root `AGENTS.md` said
   only Python ships; the root `README.md` advertised five worked examples by names no fixture has
   ever had, and claimed a custom rule imports nothing from the package (false for Dart and C#).
8. **A false guarantee removed from the shipped skill.** All four `agent-notes.md` told agents a
   failed `AndRule` has exactly one failing leaf. It has as many as the sub-rule that stopped it,
   and the claim sat directly above the `[0]`/`.first` one-liner it licensed. The library was always
   correct. Each language's snippet now reads the whole list and was executed to prove it.
9. **Two new CI gates**, both for defects that leave every other signal green:
   `scripts/check_link_labels.py` (a link label naming a path that does not exist, while its target
   resolves) and `scripts/check_ascii_values.py` (non-ASCII punctuation in a value a tool parses,
   including markdown frontmatter). Both verified clean *and* firing on a deliberately reintroduced
   defect — the first draft of the link gate was a silent no-op.
10. **One new eval**: `cross-language/06-local-pin-resolution`, covering the version-resolution
    branch every adopter currently exercises.
11. **A composite's parts became readable**, under a contract separate from `Rule` --
    `ICompositeRule<TContext>` in C#, a runtime-checkable `CompositeRule` Protocol in Python, an
    `abstract interface class` in Dart, an interface plus an `isCompositeRule` type guard in JS/TS.
    A rule *tree* can now be walked before it is evaluated; previously only the *result* tree could
    be, afterwards. Reported by an adopter, and taken on verdict's own terms: it is a contract rather
    than three accessors because accessors alone leave a walk switching on concrete types, which
    falls through on a fourth kind of composite and reports the rules inside it as absent. Rebuilding
    a composite over new parts is an explicit non-goal. Scenario at
    `docs/extending/walking-a-rule-tree/`, eval at `cross-language/07-walking-a-rule-tree`.
12. **The tabulated-summary authoring template** landed at
    `docs/maintenance/doc-authoring/summaries.md`, deliberately written after the sweep so it is
    built from real defects. Its "never write a number a reader will trust later" section is the
    sweep's most frequent single finding; the default PR template now routes to it.
13. **The eight open decisions were resolved** in `f4b3850` -- see §4 for the list, and that
    commit's message for what each turned out to be. The marketplace example moving onto a shared
    `thresholds.json` was the substantial one: verified by changing the file and watching all four
    suites fail.
14. **A threshold composite was declined, and the recorded reason was corrected.** An adopter asked
    whether `AtLeastNRule` should be a built-in. It stays a documented example, declined on the
    demand bar -- `future_plan.md` had claimed it lacked subtlety, which is false: its stopping rule
    must decide both when the threshold is reached and when it becomes unreachable. The trigger for
    reopening it (a second, unrelated consumer) is recorded, and the reasoning also sits on
    `extending/new-rule-shape/`, which is the page a reader looking for the feature opens.
15. **Two durable facts sweep into `.agents/memory/`**: an adopter's needs are input rather than
    requirements, with what running a file-based channel taught; and when you decline a candidate,
    record *which* of the two questions it failed, because the reason rots faster than the verdict
    and has now been wrong twice.

### Current green state

Python 1435 · JS 159 core + 1765 graduation + 33 marketplace · Dart 153 core + 1271 + 32 ·
C# 183 core + 1744 + 32. Every gate in §5 passes, markdownlint is clean across 180 files, the skill
bundle is internally consistent, 34 evals assemble, and 288 exported symbols across four languages
are accounted for in the concept map.

## 3 · What to do next (prioritized)

1. **Decide on the release.** Nothing else is open -- §4 is clear. Merging releases all four SDKs
   *and* the skill, since each `release-<lang>.yml` and `release-skill.yml` triggers on a version
   bump landing on `main` and creates its own tag; there is no hand-tagging step. Until then the
   eight evals that test `0.4`-only surface are unrunnable (not merely unrun), because an eval
   fixture must pin a version that is actually published. (`origin` was current at this handoff;
   confirm with `git log --oneline origin/HEAD..HEAD` before assuming it still is.)
2. **Re-run mutation testing** if the library source changes again. It was run for all four
   languages earlier on this branch, *before* the composite-parts contract landed, so the
   surviving-mutant records in `docs/maintenance/mutation-survivors-<language>.md` do not cover
   `SubRules`/`sub_rules`/`subRules` or the new contract's narrowing. One language at a time, per §5.

## 4 · Known issues / open decisions

**The eight decisions this section carried are all resolved**, in `f4b3850` --
source doc comments reworded, `NotRule` added to the quickstarts and the
changelog display bullets, the marketplace example moved onto a shared
`thresholds.json`, changelog mentions tag-pinned with the rule written down,
`skill-v0.5.2`'s missing tag explained rather than created, the Kiro and
OpenHands drop-ins made always-on, examples indexes added for JS/Dart/C#, and
incident Title comments converted to ASCII.

Nothing is open. The adopter request that was pending (`G-004`, whether `AtLeastNRule` should be a
built-in) is answered and recorded: it stays a documented example, declined on the demand bar rather
than on subtlety, with the trigger for reopening it -- a second, unrelated consumer -- written into
[`future_plan.md`](docs/future_plan.md#declined-so-far-xor-and-at-least-n-of-m-on-demand-alone) and
the reasoning onto
[`extending/new-rule-shape/`](docs/extending/new-rule-shape/README.md), which is the page someone
lands on when they want it.

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
python3 scripts/check_link_labels.py       # a label naming a path that does not exist
npx --yes markdownlint-cli2@0.18.1         # cli2, not cli -- matches lint-docs.yml
```

Every repo consistency gate, all of which must pass before a release:

```bash
for s in check_api_snapshots check_changelogs check_package_readmes \
         check_package_metadata check_fixture_coverage check_doc_comment_links \
         check_shipped_links check_workflow_shell check_link_labels \
         check_ascii_values; do
  python3 "scripts/$s.py" || echo "FAILED: $s"
done

# Dart's API snapshot needs dart-apitool on PATH, installed separately:
#   dart pub global activate dart_apitool
PATH="$PATH:$HOME/.pub-cache/bin" python3 scripts/check_api_snapshot_dart.py

python3 scripts/build_evals.py            # evals.json is generated, never committed
bash scripts/build.sh                     # all three skill artifacts still assemble

# check_skill_bundle.py takes the *assembled* bundle, not skills/verdict/ --
# REPOSITORY-MAP.md is generated at build time and never checked in.
unzip -q dist/verdict.skill -d /tmp/sk && python3 scripts/check_skill_bundle.py /tmp/sk/verdict
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

- **Python** ≥3.10 (floor in `python/packages/verdict-rules/pyproject.toml`, which is also where the
  version lives — `python/pyproject.toml` is the workspace root and declares neither), managed with
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
- **A batch of costly subagents runs one at a time**, never in parallel — see
  [`.agents/memory/run-costly-subagent-batches-sequentially.md`](.agents/memory/run-costly-subagent-batches-sequentially.md).

## 6 · Key docs

- [`AGENTS.md`](AGENTS.md) — standing rules for every agent (`CLAUDE.md` is just `@AGENTS.md`);
  each language's own `AGENTS.md` adds that language's own layer.
- [`docs/architecture/README.md`](docs/architecture/README.md#generic-context) — design source of
  truth, including the "Generic context" subsection and the result-inspection surface.
- [`docs/testing/`](docs/testing/README.md) — the testing checklist, shared across every language.
- [`docs/extending/`](docs/extending/README.md) — the eight extension scenarios.
- [`fixtures/README.md`](fixtures/README.md) — the two cross-language parity fixtures every port
  must reproduce exactly.
- [`docs/maintenance/releases/`](docs/maintenance/releases/README.md) — the shared release pipeline
  plus each registry's own real mechanics.
- [`docs/future_plan.md`](docs/future_plan.md) — exploratory candidates, explicitly not a roadmap,
  including the one verdict it records as overturned. Read
  [`when-you-decline-record-which-question-failed.md`](.agents/memory/when-you-decline-record-which-question-failed.md)
  before adding or changing an entry there: its recorded *reasons* have been wrong twice, in
  opposite directions, while the verdicts stood.
- [`.agents/README.md`](.agents/README.md) — the agent working-material layout, and
  [`.agents/memory/`](.agents/memory/) — durable facts worth not re-deriving. Start with
  [`doc-hygiene-audit-protocol.md`](.agents/memory/doc-hygiene-audit-protocol.md) before any
  documentation sweep, and
  [`an-adopters-needs-are-input-not-requirements.md`](.agents/memory/an-adopters-needs-are-input-not-requirements.md)
  before answering a consumer's report or feature request.
