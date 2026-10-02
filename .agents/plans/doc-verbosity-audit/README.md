<!-- Title: Doc-Verbosity Audit Plan -->
# Doc-verbosity audit

> Queued in [`composite-rule-and-leaves-redesign`](../composite-rule-and-leaves-redesign/README.md)
> §0 as the step after mutation testing landed. A strict, one-file-at-a-time
> sweep of every doc this repo owns, re-evaluating the over-verbosity problem
> the user flagged — not a grep-driven search for "the verbose parts," and
> not limited to a single pre-decided fix (tabulation). This file is the
> running record: scope, methodology, the skipped list, and progress.

## Methodology (binding, confirmed before starting)

- One file at a time, read fully top to bottom — no grepping or sampling to
  pre-select which files or sections look verbose.
- Traversal order is the scope checklist below, top to bottom.
- A file earns an edit only when there's a concrete verbosity problem to fix
  (prose that could be a table, a repeated explanation, a paragraph that
  restates what code already shows) — not a stylistic pass for its own sake.
- [`terse-tabulated-summaries-over-prose`](../../memory/terse-tabulated-summaries-over-prose.md)
  is one confirmed pattern, not the only one expected — this audit is meant
  to surface others.
- Commit in small batches as real fixes land, not one giant end-of-audit
  commit — consistent with how the mutation-testing phase was run.

## Scope: 224 files, in

Every tracked `*.md` file in the repo (`git ls-files '*.md'`, 310 total)
minus the skipped list below (86 files). The full ordered checklist lives in
[`scope-checklist.md`](scope-checklist.md).

| Directory | Count |
| :-- | --: |
| `docs/` | 132 |
| `.agents/` (own content only — see skipped list) | 33 |
| `python/` | 9 |
| `skills/` (this repo's own `verdict`/`verdict-workspace` skills) | 8 |
| `js/` | 8 |
| `dart/` | 7 |
| `csharp/` | 7 |
| `scripts/` | 6 |
| `.github/` | 6 |
| `fixtures/` | 3 |
| Root (`README.md`, `AGENTS.md`, `CLAUDE.md`, `CONTRIBUTING.md`, `HANDOFF.md`) | 5 |

## Skipped: 86 files, vendored content

Not authored in this repo — reviewing them is reviewing someone else's
document twice (once under `.agents/skills/`, once under its `.claude/`
mirror). Confirmed with the user before starting: "vendored ones are not
our repo content, those can be skipped."

| Vendored skill | Mirrored at | Files (both copies) |
| :-- | :-- | --: |
| `context-fence` | `.agents/skills/` + `.claude/skills/` | 4 |
| `mermaid-diagrams` | `.agents/skills/` + `.claude/skills/` | 82 |

Full list: `git ls-files '*.md' | grep -E '^\.(agents\|claude)/skills/(context-fence\|mermaid-diagrams)/'`.
If a future audit decides vendored content should also be reviewed (e.g. to
report upstream), re-run that command for the current list rather than
trusting this count to stay accurate.

## Progress

See [`scope-checklist.md`](scope-checklist.md) for the per-file checklist.
Summary kept current here as the sweep proceeds:

| Status | Count |
| :-- | --: |
| Swept, no change needed | 0 |
| Swept, fixed | 0 |
| Not yet swept | 224 |

## Findings log

Fixes land directly in their files and in git history — this section is
only for a pattern worth generalizing (a new verbosity pattern beyond
tabulation, a systemic issue spanning many files). Empty until the sweep
surfaces one.
