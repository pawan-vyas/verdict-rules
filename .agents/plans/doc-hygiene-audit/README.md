<!-- Title: Doc Hygiene Audit Plan -->
# Doc hygiene audit

> Queued in [`composite-rule-and-leaves-redesign`](../composite-rule-and-leaves-redesign/README.md)
> §0 as the step after mutation testing landed, then formalized into a
> full hygiene sweep (not just verbosity) partway through — see
> [`.agents/memory/doc-hygiene-audit-protocol.md`](../../memory/doc-hygiene-audit-protocol.md)
> for the complete, reusable spec (scope definition, method, violation
> categories, tabulation guidance, bug-handling rule). This file is only
> the running record: progress and findings specific to this pass.

## Scope: 222 files

Every tracked `*.md` file in the repo (`git ls-files '*.md'`, 310 total)
minus vendored skill content (86 files, not this repo's own) and
`.agents/plans/` (3 files, transient by definition) — full exclusions.
`HANDOFF.md` is out of the violation-fixing sweep (rewritten every
session) but still gets a staleness check. `.agents/incidents/` (10
files) is partially in: verbosity/tracking/process-vocab/terminology
checks apply, the narration bans don't — see the protocol file's scope
table for the full reasoning. Full ordered checklist:
[`scope-checklist.md`](scope-checklist.md).

## Progress

| Status | Count |
| :-- | --: |
| Swept, no change needed | 0 |
| Swept, fixed | 0 |
| Not yet swept | 222 |

Work already done earlier in this session (`.agents/incidents/`,
`.agents/memory/`, `.github/`, root instruction files, `csharp/`,
`dart/`, the `docs/architecture/` + `docs/extending/new-rule-shape/`
#100-staleness fixes) is being re-verified against the formal violation
categories as the sweep reaches it again, not re-done from zero — most
of it already holds up; see the Findings log below for what the earlier
pass already caught.

## Open questions (blocking specific files, not the whole sweep)

- **JS/TS `buildRuleResult` export gap** — `docs/extending/new-rule-shape/js.md`'s
  `ThresholdRule` example needs to build a `RuleResult` with correct
  `leaves`/`failingLeaves`, but the only function that computes them
  (`buildRuleResult`) is marked internal and not exported from
  `index.ts`. Paused per explicit instruction — do not resolve or edit
  this file until answered. Every other file continues to be swept.

## Findings log

Fixes land directly in their files and in git history — this section is
for cross-file patterns worth generalizing, not a per-file diff log.

- **`.agents/incidents/`**: 9 files already dense and structured. One
  stale placeholder fixed in incident 009 (dangling "fill in once
  resolved" bracket left over from before the actual fix shipped).
- **`.agents/memory/`**: all 19 pre-existing files already dense and
  purposeful — the memory system's own discipline already produces the
  density this audit wants elsewhere.
- **A finished, merged-PR plan left in `.agents/plans/` past its own
  expiry** — `post-generics-docs-and-skill-rebalance/README.md` (574
  lines, PR #95, merged 2026-09-18) violated `plans/README.md`'s own
  "delete or fold into memory/docs once executed" rule. One reusable
  heuristic extracted to
  [`../../memory/context-shape-is-decided-by-when-its-known.md`](../../memory/context-shape-is-decided-by-when-its-known.md)
  before deleting the plan.
- **A stale test-suite count, copy-pasted across all four languages.**
  Every `examples/graduation_verdict/docs/testing.md`'s opening
  blockquote said "its two test files serve two different purposes"
  directly above a table showing four or five suites — stale from
  before the structural-invariant/fuzz/shrink suites existed, never
  updated in lockstep with the table below it. Fixed in all four
  languages, plus each language's own `README.md` Files table, which
  had the identical staleness one level up and was missing the newer
  files entirely.
- **`docs/architecture/` (README + all 4 language files) never caught
  up to the #100 `DecidedBy`/`NotRule`/`Leaves`/`FailingLeaves`
  redesign** — no mention of any of it anywhere, including a sequence
  diagram still showing the pre-rename `data=[...]` field. This is debt
  from the still-open PR #103 itself, not old debt. Fix in progress:
  shared README's "Inspecting a composite's own decision" section
  written; per-language files still pending.
- **The `new-rule-shape` extending scenario's own `ThresholdRule`
  example had a real bug, not just staleness**, in 3 of 4 languages:
  Python and Dart built their custom composite's `RuleResult` with
  `data=sub_results`/`data: subResults` — the exact pre-#100 pattern
  this package's own breaking-change note says is no longer where
  sub-results belong. Fixed in Python and Dart. C#'s own example was
  already correct. JS's fix is blocked on the open question above.
