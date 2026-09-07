# AGENTS.md

Standing instructions for any AI agent working in this repo, written to
be harness-agnostic — the same rules apply whether you're reading this
as Claude Code, another coding agent that honors `AGENTS.md`, or a
model given this file as raw context.

## What this repo is

`verdict` is a small, zero-dependency, async-native rule-evaluation
engine for Python — see the root [`README.md`](README.md) for what it
is and its "Where to go next" table for the full doc suite
(`docs/quickstart.md`, `docs/architecture.md`, `docs/extension.md`,
`docs/maintenance.md`, `docs/testing.md`, `docs/samples/`).
[`examples/`](examples/README.md) holds full, tested mini-projects
behind the more comprehensive samples.

## Keep this repo standalone-portable

This is load-bearing, not a style preference: **never introduce a
reference to any specific consuming project** into this repo — no file
paths, class names, or business vocabulary from wherever this package
happens to be used. This package is designed to work standalone, moved
into its own repo, or vendored into any other project with zero
rewriting. Concretely:

- No relative links reaching outside this repo's own tree (e.g.
  `../../some_other_project/...`) in any doc under `docs/`,
  `examples/`, or `skill/`.
- No naming a specific consumer's package, module, or internal
  vocabulary in prose, code comments, or examples — describe a pattern
  generically ("a rate-limiting adapter," "an access-control adapter")
  rather than naming the real one, even when the real one is exactly
  what motivated the doc.
- Before adding a link or a proper noun to any doc in this repo, ask:
  "would this still make sense if this repo were the only thing that
  traveled with a reader?" If not, generalize it or drop it.

## The two constraints that must never quietly slip

Both are covered in full in [`docs/maintenance.md`](docs/maintenance.md),
but they're standing rules, not just documentation to consult on
request:

1. **Zero external dependencies.** `pyproject.toml`'s `dependencies`
   list is empty on purpose. Don't add one without a real, explicit
   discussion first.
2. **No knowledge of any specific domain.** Nothing under `src/verdict/`
   should ever import or reference rate limiting, access grants,
   discounts, or any other consumer's vocabulary — that logic belongs
   in a consumer's own adapter module, never here.

## Coding & doc conventions — keep new code indistinguishable from existing code

This repo won't always sit inside a larger project with its own global
agent instructions, so the load-bearing subset of those conventions
lives here instead of being assumed:

- **Docstrings**: Google-style (`Args:`/`Returns:`/`Raises:`) on every
  public class, function, and method — see any function in
  `src/verdict/rule.py` for the exact shape to match.
- **`from __future__ import annotations`** at the top of every module;
  modern type hints throughout (`str | None`, `list[Rule]`, never
  `Optional[...]`/`List[...]` from `typing`).
- **Frozen dataclasses for immutable value types** (`@dataclass(frozen=True)`)
  — the shape `RuleResult`/`RunResult` already use; match it for any
  new value type.
- **No hardcoded values that could plausibly change** — pull from a
  parameter, config, or (in `examples/`) a data file instead of a
  literal. This is the entire point of every example project here, not
  just a style preference.
- **Dispatch is a table or structural typing, not an if/elif ladder**
  keyed on one discriminant. `Rule` being a Protocol *is* this principle
  already applied — nothing centrally dispatches on "what kind of rule
  is this," because a caller already knows which concrete type it holds.
  If you're tempted to write `if kind == "a": ... elif kind == "b": ...`
  against a closed, small set of variants, reach for a lookup structure
  first; reserve an actual ladder for genuinely ordered/sequential logic
  (a parser, a rule-priority evaluator), not variant selection.
- **No internal task/ticket references anywhere** — not in code
  comments, not in commit messages, not in docs. A comment explains the
  code as it stands today; it never points at a ticket number, a
  project-tracker link, or "see task NNNN," which means nothing outside
  whatever system generated it and rots the moment that system does.
- **Every doc opens with a blockquote framing** right under its title —
  a one-paragraph "what this covers, why it exists" statement in `>`
  form, for the same immediate visual emphasis, before the first `##`
  section. Every doc in `docs/` and `examples/*/docs/` already does
  this; match it.
- **Testing**: short-circuit behavior is proven with a call-counter or
  mutable-list side effect, never just the final boolean; vacuous-truth
  cases (an empty rule list, an unknown group) get their own explicit
  test, never an assumption. See
  [`docs/testing.md`](docs/testing.md) for the full checklist, and
  `examples/graduation_verdict/docs/testing.md` for the
  oracle/differential-testing pattern when validating a rule-based
  decision across a wide input space.

## Authoring a diagram in this repo's docs

Every mermaid diagram in this repo (in `docs/`, `examples/*/docs/`, or
anywhere else) follows the standards vendored at
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
