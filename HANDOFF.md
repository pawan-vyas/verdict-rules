---
kind: session-handoff
handoff_schema: 1
updated_utc: 2026-09-17T17:12:58Z
updated_local: 2026-09-17T22:42:58+05:30
branch: main
state_at_commit: def163d294859c2a4fdedd384f3d0a73ef45cc6a
state_at_commit_short: def163d
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

**`main` is at `def163d`, CI green, all four language SDKs merged.** A multi-PR generics effort
(GitHub issue #33, `Rule<TContext>`) is in progress across a dedicated plan directory — see §2 and
§3. Two test-suite-parity PRs are open right now, independently mergeable, each awaiting explicit
merge approval.

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
polarity decided explicitly per composite shape. **All four language SDKs are merged to `main`**:
Python (PyPI `verdict-rules`, under `python/`), JS/TS (npm `verdict-rules`, under `js/`), C# (NuGet
`VerdictRules`, under `csharp/`), and Dart (pub.dev `verdict_rules`, under `dart/`) — each with its
own top-level directory, its own `AGENTS.md`, and its own `docs/maintenance/releases/<language>.md`.
Cross-language docs are in `docs/`, the AI-agent skill in `skills/verdict/` (vendored into other
projects by `scripts/install.sh`), the published site in `site/`, and agent working material in
`.agents/`. Design source of truth: [`docs/architecture/`](docs/architecture/README.md).

## 1b · External references (NOT part of this repo)

**None.** The repo is fully self-contained: both vendored skills (`mermaid-diagrams`,
`context-fence`) are in-tree copies, so no network, plugin install, or sibling checkout is needed to
follow either. Nothing a session needs lives outside the boundary.

## 2 · Where we are (this session's work)

The active effort is resolving [GitHub issue #33](https://github.com/pawan-vyas/verdict-rules/issues/33)
— adding `Rule<TContext>` generics to every language while keeping the plain-dict context first-class
permanently (not an "escape hatch"). This turned into a large, deliberately sequenced, multi-PR
program. The full plan lives at
[`.agents/plans/verdict-generics-v0.3.0/README.md`](.agents/plans/verdict-generics-v0.3.0/README.md)
(master plan) with one file per language (`csharp.md`, `typescript.md`, `dart.md`) plus
`shared-docs.md` for the cross-language sweep — **read the master plan before touching anything in
this effort**, it has the full design rationale (why generics are scoped to context only, never
`RuleResult`; C#'s arity-based `IRule`/`IRule<TContext>` coexistence; TypeScript's deliberate lack of
a default type parameter; Dart's `implements Rule<Map<String, Object?>>` migration).

**Confirmed sequencing for the whole effort** (each step gated on explicit user sign-off before its
PR merges — never auto-merge anything in this program):

1. **Test-suite parity, one PR per language** — drop each non-Python language's existing test file
   entirely and recreate it as a strict, file-for-file, test-for-test 1:1 port of Python's own
   `test_rule.py`/`test_engine.py`, plus a separate, clearly-labeled idiom file for that language's
   own non-portable coverage. Baseline is 4 × 40 (Python's `test_rule.py` + `test_engine.py` function
   count) plus each language's own idiom-test count. Status:
   - **C# — done, merged** ([PR #88](https://github.com/pawan-vyas/verdict-rules/pull/88)).
     `csharp/tests/VerdictRules.Tests/RuleTests.cs` + `EngineTests.cs` (the 1:1 mirror) +
     `CSharpIdiomTests.cs` (6 idiom tests: `CancellationToken` propagation, structural typing for
     delegates, an explicit `IRule` implementation). 48 tests total.
   - **JS/TS — open, CI green, awaiting merge approval**
     ([PR #89](https://github.com/pawan-vyas/verdict-rules/pull/89), branch
     `js/test-parity-1to1-python-port`). `test/rule.test.js` + `test/engine.test.js` (the mirror) +
     `test/js-idioms.test.js` (2 idiom tests: structural typing on a bare object literal,
     `UnknownLookupError`'s own field shape). 46 tests total (44 engine + 2 pre-existing CDN-doc
     tests, untouched).
   - **Dart — open, CI green, awaiting merge approval**
     ([PR #90](https://github.com/pawan-vyas/verdict-rules/pull/90), branch
     `dart/test-parity-1to1-python-port`). `test/rule_test.dart` + `test/engine_test.dart` (the
     mirror) + `test/dart_idioms_test.dart` (2 idiom tests: structural typing for function types via
     a tear-off, an explicit `Rule` implementation on a plain class). 44 tests total. Picked up two
     gaps the old suite never covered as a byproduct of the port: duplicate-rule-name resolution,
     predicate-exception propagation through all four call sites.
   - Each language's own `docs/testing/<language>.md` was rewritten to match (test counts, a new
     mermaid diagram showing the three-file layout with the idiom file styled purple, the contract
     table pointing at new file/test names) and validated with the mermaid skill's validator.
2. **`graduation_verdict` fixture port for JS/C#/Dart** (currently missing for all three — only
   Python has it, under `python/examples/graduation_verdict/`) — **three separate per-language PRs**,
   confirmed explicitly this session rather than bundled: it's additive fixture work, not the
   cross-language source change that forces step 4 into one PR. Not started yet.
3. **The new cross-language "production-grade" generics-showcase fixture** — design deliberately
   deferred. Direction hints only: marketplace/onboarding-flow-inspired but fully generalized (never
   naming a real consuming project or product), showcasing the typed+dict hybrid, `ProjectingRule`,
   and both engine forms. Written first in Python, `fixtures/<name>/README.md`. **Resolved only after
   step 4 lands and its own unit testing proves the design out** — this is the one open point in the
   whole program, confirmed explicitly this session.
4. **The core `Rule<TContext>` migration — a single PR spanning all four languages.** Confirmed
   explicitly this session (previously each language's plan implied its own PR for this step too):
   core generics implementation, comprehensive unit tests (including edge cases and per-language
   idiom gotchas) for all four languages, each language's own docs/samples/quickstart/changelog/
   skill-agent-notes sweep, **and** the shared cross-language docs sweep (root `README.md`,
   `docs/architecture/`, `docs/testing/README.md`, `docs/extending/` shared READMEs,
   `docs/maintenance/`, the skill's shared content plus a `plugin.json` version bump) all land and
   merge together in one PR — confirmed to fold in rather than stay a separate trailing PR, so
   nothing on `main` describes a stale non-generic reality even briefly. Work through this step
   without stopping for interim check-ins once it starts; only the final merge itself needs sign-off.
   Sample/doc content should showcase `Rule<TContext>` where a scenario is genuinely single-context,
   and keep the plain-dict form where a scenario is genuinely heterogeneous (a registry-style,
   runtime-string-keyed catalog) — without turning every doc into an exhaustive both-forms-always
   demo; match doc weight to which form that example actually needs. **Not started yet** — steps 1–2
   come first.

Each of steps 1–2 is its own small, focused, independently-approved PR. Step 4 is deliberately the
one place this program abandons the "one PR per language" pattern, because it changes source code
that must land coherently across all four languages and their shared docs at once — a partial merge
would leave `main` in a state no single language's docs accurately describe.

## 3 · What to do next (prioritized)

1. **Get explicit sign-off on PR #89 (JS) and PR #90 (Dart), then merge both.** Both are green,
   reviewed against this same session's own work. Confirm no release is cut on merge (already proven
   for C# by watching `release-csharp.yml` skip every downstream job when the version tag already
   exists — the same `detect`-gate pattern is identical across `release-js.yml`/`release-dart.yml`,
   but JS's npm Trusted Publishing has its own nuances worth a real look before assuming).
2. **Start step 2 — `graduation_verdict` fixture ports**, one PR each for JS, C#, and Dart, no
   particular order between them. Use `python/examples/graduation_verdict/` as the reference to port
   test-for-test, the same drop-and-recreate discipline as step 1 rather than inventing a new
   structure per language.
3. **Do not start step 4 (the single generics PR) until steps 1–2 are fully merged.** When it starts,
   read `.agents/plans/verdict-generics-v0.3.0/README.md` in full first — it has the per-language
   generics mechanics already researched and confirmed (C#'s arity-based interface coexistence
   verified against real `IComparer`/`IComparer<T>` .NET source; TypeScript's no-default-type-param
   decision; Dart's breaking-migration scope). Work through implementation, tests, and docs for all
   four languages plus the shared sweep before asking for anything beyond the final merge approval.
4. **Step 3 (the new cross-language fixture) stays unstarted until step 4 has landed and its own
   tests have proven the design** — don't jump ahead to it opportunistically even if step 4 leaves
   idle time in some language.

## 4 · Known issues / blockers

- No open defect in any shipped package itself.
- The `graduation_verdict` fixture exists only for Python — JS/C#/Dart's own testing docs each note
  this gap explicitly (see `docs/testing/<language>.md`'s "Current state" section); step 2 above
  closes it.
- No CI pipeline gate exists yet for any language's test suite running on every PR touching that
  language's path — see `docs/future_plan.md` if this is ever picked up. (This is distinct from the
  *release* workflows, which do gate on the version-vs-tag check and are already verified working.)

## 5 · Verify (gate / test commands)

```bash
# Python
cd python && uv sync && uv run pytest

# JS/TS -- tests import from a built dist/, so build first
cd js/packages/verdict-rules && npm install && npm run build && npm test

# C# -- no solution file, name the project explicitly
cd csharp
dotnet build src/VerdictRules/VerdictRules.csproj -warnaserror
dotnet test tests/VerdictRules.Tests/VerdictRules.Tests.csproj

# Dart
cd dart/packages/verdict_rules && dart pub get && dart analyze && dart test
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
  `Microsoft.NET.Test.Sdk` + `xunit.runner.visualstudio` only — no FluentAssertions/Moq, confirmed
  and kept minimal/official on purpose.
- **Dart SDK** ≥3.0.0 for Dart. **`pana`** (pub.dev's own package analyzer —
  `dart pub global activate pana`) reproduces pub.dev's real scoring locally, no publish required.
- **`jq`** only by CI workflows (preinstalled on `ubuntu-latest`).
- **Vendored skills**, mirrored to both `.agents/skills/` and `.claude/skills/`:
  `mermaid-diagrams` (diagram standards + validator) and `context-fence` (the operations manual
  this file is the exit seal of). Both are tool-owned: refresh wholesale, never hand-edit.
- `skills/verdict/` is the *shipped* skill; `skills/verdict-workspace/` is its eval working
  material — the per-target eval JSON files under `evals/<target>/` are tracked, the assembled
  `evals/evals.json` is gitignored. Evals run via the **skill-creator** plugin, not `claude plugin
  eval`.

## 6 · Key docs

- [`AGENTS.md`](AGENTS.md) — standing rules for every agent (`CLAUDE.md` is just `@AGENTS.md`);
  each language's own `AGENTS.md` adds that language's own layer.
- [`.agents/plans/verdict-generics-v0.3.0/README.md`](.agents/plans/verdict-generics-v0.3.0/README.md) —
  the master plan for the active generics effort (§2–§3 above); read this before touching that
  program at all.
- [`docs/architecture/`](docs/architecture/README.md) — design source of truth: why evaluation is
  sequential, why `Rule` is structural, why `RuleResult.data` stays opaque.
- [`docs/testing/`](docs/testing/README.md) — the testing checklist, shared across every language;
  each language's own `docs/testing/<language>.md` names which test proves which contract.
- [`docs/maintenance/doc-authoring/`](docs/maintenance/doc-authoring/README.md) — every doc-category
  template this repo enforces by review.
- [`docs/maintenance/releases/`](docs/maintenance/releases/README.md) — the shared release pipeline
  plus each registry's own real mechanics.
- [`docs/extending/`](docs/extending/README.md) — the seven extension scenarios;
  `domain-adapter-module/`'s one-adapter-module boundary is the pattern to steer consumers toward.
- [`docs/future_plan.md`](docs/future_plan.md) — exploratory candidates, explicitly not a roadmap.
- [`python/examples/graduation_verdict/`](python/examples/graduation_verdict/) — the worked example,
  including the oracle/differential chaos suite worth re-deriving in any future language; the
  reference for step 2 above.
- [`.agents/README.md`](.agents/README.md) — the agent working-material layout, and
  [`.agents/memory/`](.agents/memory/) — durable facts worth not re-deriving.
