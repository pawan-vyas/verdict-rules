---
kind: session-handoff
handoff_schema: 1
updated_utc: 2026-09-18T04:33:41Z
updated_local: 2026-09-18T10:03:41+05:30
branch: generics/v0.3.0-rule-context
state_at_commit: 6f38ceb47cf7ce434e9c699feac50b108df5afcf
state_at_commit_short: 6f38ceb
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

**`main` is merged through PR #93.** The generics effort (GitHub issue #33, `Rule<TContext>`) is
down to its **last open PR**: [PR #94](https://github.com/pawan-vyas/verdict-rules/pull/94), branch
`generics/v0.3.0-rule-context` (this branch). It has been rebased onto post-#93 `main`, and now also
contains the cross-language generics-showcase fixture that the original plan deferred until after
PR #94 merged — that deferral was explicitly reversed by the user mid-program (see §2). Everything
described below is **committed and green on this branch**; the only remaining step is explicit user
sign-off to merge it.

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

**This branch is a PR branch, not `main`.** §2 and §3 below describe `generics/v0.3.0-rule-context`
(PR #94), which sits ahead of `main` by design until it merges.

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

The generics program's master plan doc (`.agents/plans/verdict-generics-v0.3.0/README.md`) lives
only on the still-open plan branch/PR
([#84](https://github.com/pawan-vyas/verdict-rules/pull/84)), not on this branch or on `main` — it
is referenced below by PR URL rather than by relative path for that reason. It has the full design
rationale for everything summarized in §2 (why generics are scoped to context only, never
`RuleResult`; C#'s arity-based `IRule`/`IRule<TContext>` coexistence; TypeScript's deliberate lack of
a default type parameter; Dart's `implements Rule<Context>` migration) and is worth reading in full
if any of §2's per-language mechanics need re-deriving.

## 2 · Where we are (this session's work)

The full generics program (GitHub issue #33) is functionally complete on this branch. Sequencing,
confirmed and executed exactly as planned (each step gated on explicit user sign-off before its PR
merged — never auto-merged):

1. **Test-suite parity** (one PR per language, dropping each non-Python suite and recreating it as a
   strict 1:1 port of Python's own tests plus a separate idiom file) — **all merged**: C# (#88),
   JS/TS (#89), Dart (#90).
2. **`graduation_verdict` fixture port for JS/C#/Dart** (Python already had it) — **all merged**: JS
   (#91), C# (#92), Dart (#93). Each is 57 curated + 500 chaos-generated cases against its own
   seeded PRNG, 557 tests total per language, matching Python's own port's expected outcomes
   exactly.
3. **The core `Rule<TContext>` migration, one PR spanning all four languages** — implemented, tested,
   documented. Per-language mechanics (each genuinely different, none interchangeable):
   - **Python** — `Rule` becomes `Protocol[TContext]`; the rest become `Generic[TContext]`.
     Non-breaking at runtime (generics erase).
   - **JS/TS** — `Rule<TContext>` etc. gain **no default type parameter**, deliberately. Dict-context
     is `Rule<Context>`, written out every time.
   - **C#** — arity coexistence: `IRule<TContext>` is new and independent; `IRule` becomes the closed
     specialization `IRule : IRule<IReadOnlyDictionary<string, object?>>` (matches
     `IComparer`/`IComparer<T>`'s real BCL relationship, not `IEnumerable<T>`'s inheritance
     direction). Every generic type is a fresh, independent implementation, fully additive.
   - **Dart** — the one real breaking migration: `implements Rule` becomes
     `implements Rule<Context>` (new `Context` typedef, exported) or an explicit typed context.
     Constructor call sites are unaffected (inferred).
   - Shared docs: `docs/architecture/README.md`'s new "Generic context" subsection, a new shared
     extending scenario `docs/extending/reusing-a-rule-across-contexts/` (one page per language), the
     skill's `plugin.json` version bump.
4. **The new cross-language generics-showcase fixture, `marketplace_eligibility`** — originally
   planned to be designed only *after* PR #94 merged, so its own tests could prove the design out
   independently. The user explicitly overrode that sequencing mid-program, directing it be built
   **inside PR #94 itself**, immediately. Built exactly as the deferred plan specified (Python
   first, spec-and-implementation, each other language porting against the already-fixed spec):
   a two-sided marketplace (sellers/buyers) plus an independent compliance-event catalog, proving
   two typed contexts sharing no fields, one rule (`is_verified_identity`) reused across both via a
   `ProjectingRule` adapter, and a dict-context catalog coexisting in the same codebase. Deliberately
   narrower in scope than `graduation_verdict` — doesn't pin short-circuit counts or run an
   oracle/chaos suite, since `graduation_verdict` already proves those guarantees; this fixture's job
   is proving the generic-context design specifically. Landed as four commits, one per language
   (Python bundled with the fixture's own data/spec commit), each independently tested and doc'd:
   `fixtures/marketplace_eligibility/`, `docs/samples/marketplace-eligibility/` (one page per
   language plus a language-agnostic spec with two validated mermaid diagrams), and each language's
   own `examples/marketplace_eligibility/` project.
5. **The rebase** (PR #94 was cut before #89–#93 merged) — done, clean. One real breakage surfaced
   only after rebasing: Dart's `graduation_verdict` example (`AtLeastNRule implements Rule`) needed
   the same breaking migration as everything else, since it's source code, not test code, and the
   rebase brought both changes into the same tree for the first time. Fixed in its own clearly-
   labeled commit, verified via `dart analyze` + the full 557-test suite before folding back in.

**Full commit range on this branch, oldest first** (`9437328` = the PR #93 merge commit, the last
point this branch shared with post-rebase `main`):

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
```

**Full verification, this session, all four languages, every suite** (see §5 for the exact commands):

| Language | Core | `graduation_verdict` | `marketplace_eligibility` | Total |
| --- | --- | --- | --- | --- |
| Python | (bundled below) | (bundled below) | 33 | 650 |
| JS | 54 | 557 | 33 | 644 |
| C# | 61 | 557 | 32 | 650 |
| Dart | 55 | 557 | 33 | 645 |

All green. `dart analyze`/`dotnet build -warnaserror`/`tsc` all clean. Every new doc code sample was
compiled/run-verified against the real built package, not eyeballed. Every new/edited mermaid diagram
passed the real-renderer validator.

## 3 · What to do next (prioritized)

1. **Get explicit user sign-off on PR #94, then merge it.** Nothing else is blocking — this is the
   single remaining step in the whole generics program. Never merge without that explicit go-ahead
   (standing rule for this program, not just this PR).
2. **After #94 merges**, the generics program (issue #33) is complete. Consider whether the plan PR
   ([#84](https://github.com/pawan-vyas/verdict-rules/pull/84)) should be closed or merged as a
   historical record — it was left open throughout so its plan doc stayed available for reference;
   check with the user rather than assuming either way.
3. **No other step is deferred.** Every item the original plan's step 3 punted past #94 (the new
   fixture) is now done *inside* #94, per the user's explicit override — there is no trailing PR to
   plan for.

## 4 · Known issues / blockers

- No open defect in any shipped package itself.
- `.agents/scratch/check_links.py` reports pre-existing broken links unrelated to this session's
  work (in `.agents/memory/*.md` and this file's own §1b-style references to the plan branch) — none
  of them touch anything this session added or edited; verified by re-running the checker after
  every doc change made here.
- No CI pipeline gate exists yet for any language's test suite running on every PR touching that
  language's path — see `docs/future_plan.md` if this is ever picked up. (This is distinct from the
  *release* workflows, which do gate on the version-vs-tag check and are already verified working.)

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
- [PR #84](https://github.com/pawan-vyas/verdict-rules/pull/84) — the master plan for the generics
  effort described in §2 (full design rationale); see §1b for why this is a PR link, not a path.
- [`docs/architecture/`](docs/architecture/README.md) — design source of truth: why evaluation is
  sequential, why `Rule` is structural, why `RuleResult.data` stays opaque, and the new "Generic
  context" subsection this session's work added.
- [`docs/testing/`](docs/testing/README.md) — the testing checklist, shared across every language;
  each language's own `docs/testing/<language>.md` names which test proves which contract.
- [`docs/extending/reusing-a-rule-across-contexts/`](docs/extending/reusing-a-rule-across-contexts/README.md) —
  the new shared extension scenario this session added, one page per language.
- [`docs/samples/marketplace-eligibility/`](docs/samples/marketplace-eligibility/README.md) — the
  new fixture's language-agnostic spec plus one implementation page per language.
- [`docs/maintenance/doc-authoring/`](docs/maintenance/doc-authoring/README.md) — every doc-category
  template this repo enforces by review.
- [`docs/maintenance/releases/`](docs/maintenance/releases/README.md) — the shared release pipeline
  plus each registry's own real mechanics.
- [`docs/future_plan.md`](docs/future_plan.md) — exploratory candidates, explicitly not a roadmap.
- [`python/examples/graduation_verdict/`](python/examples/graduation_verdict/) — the flagship
  dict-context worked example; every other language's own port matches its fixture data exactly.
- [`.agents/README.md`](.agents/README.md) — the agent working-material layout, and
  [`.agents/memory/`](.agents/memory/) — durable facts worth not re-deriving.
