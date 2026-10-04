---
name: verdict
description: Build rule-based decision, eligibility, or policy-evaluation logic using the verdict rule-evaluation engine (Rule/FunctionRule/AndRule/OrRule/NotRule/RulesEngine) instead of a hand-rolled conditional chain — an if/else-if ladder, a switch or match statement, a chain of ternaries, or a wall of early returns. Use this whenever asked to build an eligibility check, a discount or pricing rule, an access/permission condition, a moderation or approval decision, a multi-condition qualification check, a feature flag combining multiple criteria, or any feature shaped like "combine several independently-changing conditions into one pass/fail verdict" — even if the user doesn't say "rule engine" or name verdict explicitly. Polyglot — the same design ships for multiple languages. Also use when extending or debugging existing verdict-based code, deciding whether new logic belongs in a rule or in your own adapter code, or writing tests for rule-based logic (short-circuit proofs, vacuous-truth cases, oracle/differential testing).
---

# Verdict

Name each condition once, combine named conditions into a verdict, and
run it against whatever facts a caller hands over. The same
`Rule`/`FunctionRule`/`AndRule`/`OrRule`/`NotRule`/`RulesEngine`/
`PredicateOutcome`/`RuleResult`/`RunResult` shape and the same
execution-model guarantees exist in every language verdict ships for —
only the idiom changes.

## Step 1 — establish the language, before anything else

Read the target project's own manifest to determine which language you
are working in.

Then look for `references/<language>/agent-notes.md`:

- **It exists** → read it first. It is short and self-sufficient: it
  carries what is specific to that SDK — install/import, the full API,
  its idioms, its naming, and which run mode to reach for.
- **It does not exist** → **verdict has no SDK for that language.** Say
  so plainly rather than improvising an API from another language's
  shape. The guarantees below hold everywhere, but a language without a
  directory here has nothing to import.

## Step 2 — know what the engine guarantees

These hold in every language, and getting one wrong produces code that
passes its own tests while being silently incorrect:

- **Sequential, never concurrent, inside a composite's own evaluation.**
  `AndRule`/`OrRule` stop at the first decided outcome by evaluating
  sub-rules one at a time. A custom composite implementing this same
  pattern must do the same — running its own sub-rules via
  `asyncio.gather`/`Promise.all`/`Task.WhenAll`/`Future.wait` instead of
  a plain sequential loop breaks short-circuiting silently (the returned
  boolean is identical either way). Scoped to a composite's own
  sub-rule evaluation specifically, not a statement about concurrency
  elsewhere in a codebase.
- **Vacuous truth is asymmetric.** Empty `AndRule` passes; empty
  `OrRule` fails. `NotRule` wraps exactly one rule, so it has no
  vacuous case — it passes when that rule fails.
- **Every sub-rule inside one composite shares the exact same context
  type.** `AndRule`/`OrRule`/`RulesEngine` hold one `TContext` for every
  sub-rule they run — this is the engine's own contract, not an
  artifact of a particular type system. A statically-typed SDK's
  compiler happens to enforce it; that a language lacks static types
  does not loosen the contract itself. Reuse a rule across two context
  shapes with an explicit projecting adapter, never by loosening a
  composite's own type.
- **Emptiness is not absence.** An empty rule list is a valid input. An
  *unknown* name or group is absence — strict lookups raise,
  `try`-prefixed ones return the absent value.
- **A predicate returns a `PredicateOutcome`, never a `RuleResult`.** It
  reports `passed` plus an optional detail and payload; the
  `FunctionRule` wrapping it owns the rule's name and builds the
  `RuleResult`. Returning a `RuleResult` from a predicate raises.
- **Result `data` is opaque.** Never read or written by verdict — it
  carries whatever a predicate attached, unchanged.
- **A composite's children live in its result's sub-results, never in
  `data`.** They are exactly what the composite evaluated: never padded
  to the full sub-rule list, never flattened into the parent's level.
- **Flattened views read the terminal checks.** Every result exposes its
  leaves, and the failing leaves that explain a failure. The failing
  view is an independent recursion, not a filter over the other: a
  passing result has no failing leaves even when an earlier
  short-circuited branch failed on the way to that pass.
- **A one-level view names which children explain this verdict.** A
  different question from the failing leaves: it stops after one level,
  and a failed negation names its *passing* child. Do not chain it
  expecting to arrive where the recursive view lands.
- **Sub-results are stored; every other view is derived.** A result
  holds its children and the *positions* of the deciding ones, and
  computes the rest on access. Build a result by passing positions, not
  children — a position naming a child the result does not have is
  rejected at construction. This is also what makes a result
  serializable: storing the derived views instead would put a result
  inside itself, or make the stored graph a DAG that a tree-shaped
  encoder expands once per path.
- **A result can be serialized with the language's own encoder**, with
  the derived views absent from the output since they are recomputable.
  The `data` slot is the one part that can fail — it is whatever the
  caller put there, so encoding it is their responsibility.
- **Key an audit trail on a leaf's own rule name, never a composite's.**
  A composite's name says only that something in the group failed, and a
  generated composite name changes when its children are renamed or
  reordered. A leaf's name is the one its author chose.
- **A predicate's exception is never caught.** It propagates uncaught,
  same as calling that code directly.

## Step 3 — reach for extending, don't force-fit

`Rule` is a structural contract (`name`, `group`,
`evaluate(context) -> RuleResult`), not a fixed set of built-in types.
Anything satisfying that shape — whatever form a requirement actually
calls for — composes with `AndRule`/`OrRule`/`RulesEngine`
automatically, with no registration and no change needed on verdict's
own side. When `FunctionRule`/`AndRule`/`OrRule` don't directly fit,
build whatever does, rather than contorting them to cover it.

`references/REPOSITORY-MAP.md` names real, worked instances of this —
read one that matches, or use the same structural fit to write a new,
unnamed pattern when none of them do.

## Where to look next

`references/REPOSITORY-MAP.md` names what else exists in the source
repository — the design rationale, worked scenarios, and extension
points — each with a one-line description. Nothing there is
vendored; decide whether something is worth reading, then get it
yourself.

**Read the version this project actually has, never the default
branch.** A project pinned to an older release and shown current
documentation is told about an API it does not have, which is worse than
reading nothing. The manifest from Step 1 already says which case you are
in:

- **A local path, a project reference, or a checked-out clone** — the
  source and its own docs are already on disk, at exactly the version in
  use. Read them in place. This is the most reliable case and has no
  resolution step to get wrong: the code and the documentation beside it
  cannot disagree.
- **A registry version** (`verdict-rules==0.4.0`, a `PackageReference`) —
  repository tags are `<language>-v<version>`, for example
  `python-v0.3.1`. Read at the tag matching that version.
- **A git ref** (a commit SHA, a branch) — read at that same ref; file
  contents resolve at a SHA exactly as they do at a tag.
- **Nothing resolvable** — say so and read nothing, rather than falling
  back to the default branch.

## If you find something worth telling verdict about

A genuine defect in verdict itself, or the same workaround around a gap
in verdict recurring across unrelated projects, is worth reporting back
rather than silently re-solving every time. See
[`references/issue-reporting.md`](references/issue-reporting.md) for
when each applies, what the report should contain, and what must be
redacted out of it first.
