# AGENTS.md

Standing instructions for any AI agent working in this repo, written to
be harness-agnostic — the same rules apply whether you're reading this
as Claude Code, another coding agent that honors `AGENTS.md`, or a
model given this file as raw context.

## What this repo is

`verdict` is a small, zero-dependency, async-native rule-evaluation
engine — the same `Rule`/`FunctionRule`/`AndRule`/`OrRule`/
`RulesEngine`/`RuleResult`/`RunResult` design and execution-model
guarantees, meant to exist in more than one language. **Only Python
ships today** — see [`python/README.md`](python/README.md) and its own
`python/AGENTS.md` for everything Python-specific. A second language
lands as a new top-level directory alongside `python/`, with its own
`AGENTS.md` for that language's own conventions — check which
directories actually exist before assuming a language has an SDK yet.
[`skills/verdict/`](skills/verdict/SKILL.md) is the AI-agent skill for
building with verdict, vendored back into consuming projects via
`scripts/install.sh`.

## Keep this repo standalone-portable

This is load-bearing, not a style preference: **never introduce a
reference to any specific consuming project** into this repo — no file
paths, class names, or business vocabulary from wherever this package
happens to be used. This package is designed to work standalone, moved
into its own repo, or vendored into any other project with zero
rewriting. Concretely:

- No relative links reaching outside this repo's own tree (e.g.
  `../../some_other_project/...`) in any doc under `python/docs/`,
  `python/examples/`, or `skills/`.
- No naming a specific consumer's package, module, or internal
  vocabulary in prose, code comments, or examples — describe a pattern
  generically ("a rate-limiting adapter," "an access-control adapter")
  rather than naming the real one, even when the real one is exactly
  what motivated the doc.
- Before adding a link or a proper noun to any doc in this repo, ask:
  "would this still make sense if this repo were the only thing that
  traveled with a reader?" If not, generalize it or drop it.

## The two constraints that must never quietly slip, per language

Both are covered in full in each language's own maintenance doc (today:
[`docs/maintenance.md`](docs/maintenance.md)), but they're
standing rules for every language this repo ever ships, not just
documentation to consult on request:

1. **Zero external dependencies**, in that language's own idiom.
   Don't add one without a real, explicit discussion first.
2. **No knowledge of any specific domain.** Nothing under a language's
   own package source should ever import or reference rate limiting,
   access grants, discounts, or any other consumer's vocabulary — that
   logic belongs in a consumer's own adapter module, never here.

## Cross-language coding & doc conventions

These apply regardless of which language directory you're working in;
each language's own `AGENTS.md` adds the syntax-specific detail on top:

- **No hardcoded values that could plausibly change** — pull from a
  parameter, config, or (in an `examples/` project) a data file instead
  of a literal. This is the entire point of every example project in
  this repo, not just a style preference.
- **Dispatch is a table or structural typing, not an if/elif ladder**
  keyed on one discriminant. `Rule` being structurally typed (a
  `Protocol` in Python; the closest equivalent in any future language)
  *is* this principle already applied — nothing centrally dispatches on
  "what kind of rule is this," because a caller already knows which
  concrete type it holds. If you're tempted to write
  `if kind == "a": ... elif kind == "b": ...` against a closed, small
  set of variants, reach for a lookup structure first; reserve an
  actual ladder for genuinely ordered/sequential logic (a parser, a
  rule-priority evaluator), not variant selection.
- **No internal task/ticket references anywhere** — not in code
  comments, not in commit messages, not in docs. A comment explains the
  code as it stands today; it never points at a ticket number, a
  project-tracker link, or "see task NNNN," which means nothing outside
  whatever system generated it and rots the moment that system does.
- **Every doc opens with a blockquote framing** right under its title —
  a one-paragraph "what this covers, why it exists" statement in `>`
  form, for the same immediate visual emphasis, before the first `##`
  section. Every doc in this repo already does this; match it.
- **Testing**: short-circuit behavior is proven with a call-counter or
  mutable-list side effect, never just the final boolean; vacuous-truth
  cases (an empty rule list, an unknown group) get their own explicit
  test, never an assumption. See each language's own testing doc (today:
  [`docs/testing.md`](docs/testing.md)) for the full
  checklist, and
  [`python/examples/graduation_verdict/docs/testing.md`](python/examples/graduation_verdict/docs/testing.md)
  for the oracle/differential-testing pattern when validating a
  rule-based decision across a wide input space.

## Authoring a diagram in this repo's docs

Every mermaid diagram in this repo (in any language's `docs/`, an
`examples/*/docs/`, or anywhere else) follows the standards vendored at
[`.claude/skills/mermaid-diagrams/SKILL.md`](.claude/skills/mermaid-diagrams/SKILL.md)
(also vendored at `.agents/skills/mermaid-diagrams/SKILL.md` for
harnesses that read that location instead) — color palette, node-shape
vocabulary, arrow styling, emoji rules, link indexing, and the
real-renderer validator script. Follow it directly rather than
improvising diagram conventions from scratch, and validate every new or
edited diagram with its bundled `scripts/validate_diagrams.js` before
calling a doc change done.

## Before treating a doc change as done

- Every relative link and anchor fragment you touched actually
  resolves — don't assume a path or a heading slug, check it.
- Every mermaid diagram you touched or added passes the validator
  referenced above.
- Every code sample you write in a doc actually runs — verify it
  against the real installed package, don't just eyeball it for
  plausibility.
