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

## Working notes stay in the repo

> Every **durable** note — memory, standing rules, plans, specs,
> decisions, tasks, discussion conclusions, session handoffs — lives
> **inside this repo**, committed. **Never** write durable notes to
> `~/.claude/`, a harness's own global memory store, the home
> directory, or `/tmp`. Only genuinely transient logs and throwaway
> scripts may live outside a tracked file, under
> [`.agents/scratch/`](.agents/scratch/) (gitignored).

Where things go:

| Kind | Home |
| :-- | :-- |
| Standing rules (this file) | `AGENTS.md`, plus each language's own (`python/AGENTS.md`) |
| Session state — resume from here | `HANDOFF.md` |
| Plans, specs, playbooks, resume notes | [`.agents/plans/`](.agents/plans/) |
| Durable facts / confirmed preferences | [`.agents/memory/`](.agents/memory/) |
| Vendored operations manuals an agent follows | [`.agents/skills/`](.agents/skills/), mirrored to `.claude/skills/` |
| Published documentation, for human readers | `docs/`, and each language's own `docs/` |
| Throwaway docs, logs, scripts, research notes | [`.agents/scratch/`](.agents/scratch/) (gitignored) |
| Mistakes worth not repeating | [`.agents/incidents/`](.agents/incidents/) |

`.agents/` is the harness-neutral home for everything an agent
produces that outlives a session — `memory/` for what is *already
true*, `plans/` for what is *going to happen*. This is a deliberate
repo-local override of the context-fence default (`docs/agent-memory/`):
`docs/` here is published package documentation, and agent working
material stays out of it. A genuinely new *kind* of durable agent
material gets a new sibling under `.agents/` with its own `README.md`
— not a subdirectory of whichever existing one roughly fits. See
[`.agents/README.md`](.agents/README.md) for the full layout.

**Start of session:** resume from [`HANDOFF.md`](HANDOFF.md) and honor
its **§0.1 freshness & alignment protocol** before trusting §2–§3 — if
the repo has moved since the handoff was sealed, reconcile first. The
full operations spec (RESUME / SETUP / ADOPT / SWEEP / HANDOFF) is
vendored locally at
[`.agents/skills/context-fence/SKILL.md`](.agents/skills/context-fence/SKILL.md),
mirrored to `.claude/skills/context-fence/` for Claude Code's native
discovery. No installation or network needed — read it and follow the
operation by name rather than improvising.

**Harness plan files count as durable context.** When a harness has a
plan mode, a scratch mode, or any equivalent that writes a working
document somewhere of its own choosing, point it at
[`.agents/plans/`](.agents/plans/) inside this repo. If its plan path
isn't overridable, copy the file into `.agents/plans/` as soon as it
has content and make every later edit to the in-repo copy — never
leave the authoritative version in `~/.claude/plans/` or any other
harness-private store.

**Harness memory files count as durable context too.** The same rule,
and it matters more, because a memory feature is designed to be
invisible: it accumulates quietly and is read back automatically, so
nothing ever *feels* wrong while every durable fact you learn is
landing outside the repo. When a harness offers one, write the memory
to [`.agents/memory/`](.agents/memory/) instead — one file per fact,
committed. If the harness insists on maintaining its own store and the
path isn't overridable, keep **only a pointer** there — a line saying
the real memory lives in this repo, naming `AGENTS.md` and
`.agents/memory/` — and never a copy of the fact itself. Two stores
holding the same fact is worse than one holding a pointer: they drift,
and the stale one is the one that gets read in the session where it
matters. Anything you'd be tempted to "remember" about this repo
belongs in `.agents/memory/`, where the next agent — in any harness —
can actually find it.

**Public surfaces are not working surfaces.** Issues, pull request
descriptions, commit messages, and every tracked doc are public and
permanent, and they speak in the project's voice. They carry the task
and the decision — never an assessment of anyone else's package
(abandoned, unmaintained, a competitor, squatting a name), never
comparison tables or download counts against a named third party, and
never the narration of how the work went. State a decision as a fact:
*"`verdict` is unavailable on npm; the chosen name is `@verdict/core`"*
— no characterisation of whoever holds it. Research that supports a
decision belongs in `.agents/scratch/` (gitignored), or on a local
branch that is never pushed. See
[`.agents/memory/public-surfaces-stay-professional.md`](.agents/memory/public-surfaces-stay-professional.md).

**Throwaway material goes in `.agents/scratch/`, not a harness temp
dir.** Every harness offers somewhere of its own to put working files —
a session scratchpad, `/tmp`, a hidden cache. Use
[`.agents/scratch/`](.agents/scratch/) instead, for the same reason
plans and memory are overridden to `.agents/`: one known location, next
to the repo it concerns, readable by the user in their own editor and
by the next agent regardless of harness. It is **gitignored**, so
nothing there is committed — that is the point. Research notes, logs,
generated reports, scratch scripts, anything that stops mattering once
it has been read. If something there turns out to be durable, promote
it to `.agents/memory/` or `.agents/plans/` rather than leaving it.

**Write an incident when something fails silently or structurally.**
[`.agents/incidents/`](.agents/incidents/) records mistakes so the next person
or agent does not rediscover them — not a blame log and not a changelog. A
mistake earns a file when it failed while everything looked green, when the
setup made it easy so care alone will not prevent a repeat, or when it cost a
force-push or a republish. Read the index there before doing anything
irreversible: publishing, force-pushing, or editing a release. Each entry ends
with what prevents a repeat, which is the part that makes it worth writing.

**Don't create parallel planning docs.** In-flight work and next steps
go in `HANDOFF.md` §3; anything longer-lived goes in `.agents/plans/`
as a named plan. A second "notes" or "TODO" file at the repo root is
how context gets lost, not how it gets organized.

## Asking the user

When you have a question, a doubt, or a decision that is genuinely the
user's to make, **ask through the harness's interactive question tool**
— not as prose buried at the end of a long reply, and not by guessing
and proceeding. If the harness has no such tool, ask in the
conversation instead; the rule is that the question is put to the user
explicitly, not the specific mechanism. Explain the context *upfront*:
what you found, why it forces a choice, and what each option costs.
Give a recommendation rather than an even-handed survey.

Reserve **blocking** questions — stopping with nothing delivered until
the user answers — for cases where proceeding under any assumption
would be unsafe or would waste the work if wrong. Otherwise do
everything that doesn't depend on the answer first, then ask. A
routine judgment call with an obvious default isn't a question; make
it, say you made it, and move on.

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

## Adding a variant is a new file, never an edit to a shared one

The repo-structure form of the dispatch rule below. **If adding the Nth thing
means editing a file the other N−1 share, the structure is wrong** — restructure
so the Nth is a new file, directory or row, and nothing existing moves. This
matters more here than in most repos, because several languages are developed
on separate branches at once: a shared file is a conflict multiplied by the
number of languages in flight.

Already applied to test workflows, release workflows, PR templates, skill
routing, skill reference content, and per-package changelogs. The test before
adding anything: *what does the fifth one cost?* Full reasoning in
[`.agents/memory/adding-a-variant-is-a-new-file.md`](.agents/memory/adding-a-variant-is-a-new-file.md).

A shared file every variant *reads* is fine. A shared file each variant must
*write to* is the problem.

## Research an ecosystem before deciding its idiom

Each SDK first-classes its own language's conventions, and a convention is a
**fact about that ecosystem**, discoverable from its own documentation — never
inferred from whichever language you know best. Read the registry's or
language's own docs, and record what they say with a citation.

The cost of not doing this is imposing one ecosystem's convention on four and
calling it consistency. See
[`.agents/memory/research-the-ecosystem-before-deciding-its-idiom.md`](.agents/memory/research-the-ecosystem-before-deciding-its-idiom.md)
for the worked example, where one of four registries turned out not to use the
mechanism being designed at all.

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
  cases (an empty rule list) and absence cases (an unknown rule name or
  group, which raise rather than pass vacuously) each get their own
  explicit test, never an assumption. See each language's own testing doc (today:
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

## Before treating a behaviour change as done

Changing what the library does is the small half. The larger half is
that other files now describe something untrue, and **nothing fails when
they do** — the tests pass and CI stays green while the wrong answer
sits there. Sweep outward from the code every time: source docstrings,
`docs/architecture.md` **and its diagrams**, `docs/extension.md` (does
this enable a recipe, or invalidate one?), `docs/testing.md` (it names
specific tests by name), `docs/maintenance.md`, each language's
quickstart and samples, the shared fixture if the change is behavioural,
`skills/verdict/references/` with a `plugin.json` bump, that package's own
`CHANGELOG.md`, and the `README.md`.

Extend diagrams rather than only correcting their prose — a diagram
describing the old shape is more misleading than stale text, because it
reads as authoritative. Full reasoning and the worked example of this
going wrong:
[`.agents/memory/a-behaviour-change-is-a-documentation-change.md`](.agents/memory/a-behaviour-change-is-a-documentation-change.md).

## Before treating a doc change as done

- Every relative link and anchor fragment you touched actually
  resolves — don't assume a path or a heading slug, check it.
- Every mermaid diagram you touched or added passes the validator
  referenced above.
- Every code sample you write in a doc actually runs — **execute it**
  against the real installed package in a scratch file, don't eyeball it
  for plausibility. A sample that looks right and is subtly wrong is
  worse than no sample, because a reader trusts it and then debugs their
  own code for the mismatch.
- The bar scales with position: a document's **first or headline example
  must run verbatim**, imports and all, since that is the one people
  paste. Later examples may assume the setup established above them —
  repeating boilerplate at every snippet makes an advanced doc
  unreadable — but everything after that implied preamble must still be
  correct as written.
