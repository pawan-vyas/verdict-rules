<!-- Title: Doc Hygiene Audit Protocol -->
# Doc hygiene audit protocol

> Full, reusable spec for a documentation hygiene sweep across this
> repo's durable docs, given verbatim by the user, plus the
> bug-handling rule layered on top. Re-apply this whole protocol
> verbatim the next time a "doc audit" is requested — don't re-derive
> scope or method from scratch.

## Scope

**In**: architecture/design docs, dev docs, agent-facing docs — every
durable, committed doc. Concretely in this repo: `docs/`, each
language's `AGENTS.md`/`README.md`/`CHANGELOG.md`/`docs/quickstart.md`,
[`.agents/memory/`](memory/), `skills/verdict/` +
`skills/verdict-workspace/`, root `AGENTS.md`/`CLAUDE.md`/
`CONTRIBUTING.md`/`README.md`, `.github/` templates, `fixtures/*/README.md`.

**Out, fully**: transient planning/ticket docs and anything not this
repo's own content:

| Excluded | Why |
| :-- | :-- |
| Vendored skills (`mermaid-diagrams`, `context-fence`, under both `.agents/skills/` and `.claude/skills/`) | Not this repo's own content |
| `.agents/plans/` | Transient planning docs by definition — [`../plans/README.md`](../plans/README.md): a plan is deleted or folded into memory/docs once executed |

**Out of the violation-fixing sweep, but check for staleness anyway**:
`HANDOFF.md` — rewritten every session per the `context-fence` skill's
own instructions, so it's not swept for the violation categories below,
but do confirm its sections aren't pointing at stale state.

**Partially in**: `.agents/incidents/` — historical-record-by-design
(its own purpose is narrating what happened), so the "historical/
before-after narration" and "incident narration" categories below do
**not** apply there. Everything else does: verbosity/tabulation
trimming, internal-tracking-reference checks, internal
process-vocabulary checks, and terminology-drift checks all still
apply to an incident file.

## Method

1. Build a flat-file checklist, one checkbox per in-scope file, committed
   in the repo (survives interruption, crosses sessions).
2. Read every file **fully**, one by one, in directory order — not just
   files suspected to have problems. Fix violations found in the same
   pass, check it off, move to the next. No skipping ahead on the
   assumption a file is clean.
3. Commit in small batches (per directory/logical group, never one
   giant end-of-sweep commit), push as you go, update the checklist in
   the *same* commit as the fixes.
4. Short status report after each batch: what was read, what was
   found, what was fixed.
5. Stop and ask — don't guess and keep going — on anything needing a
   judgment call only the repo owner can make.
6. Defer the full test suite until the entire sweep is done, run it
   once at the end as a final regression check.

## Violations to fix

| Category | Strip/fix | Keep instead |
| :-- | :-- | :-- |
| Historical/before-after narration | "before this fix," "previously," "no longer," "used to," "originally," "the old X," "now resolved/now comes/now runs instead of," "X replaces Y," "closing a gap" | State current behavior directly, present tense. History belongs in git log / commit messages / a changelog |
| Incident narration | "an incident where X happened," "two attempts at Y each caused a regression," "found live," "confirmed live" inside an architecture/mechanism doc | Point to a real troubleshooting/gotchas doc ("see X for the gotcha"); if none exists, state the current mitigating behavior as a fact |
| Internal tracking references | Ticket/task IDs, sprint names, internal migration hashes used as narrative evidence, internal planning-doc filenames | A pointer to a real, versioned source file (migration file, class, docstring) is fine |
| Internal process vocabulary | "Phase N," "this session/initiative," "explicit direction," anything only legible to someone who was in the room | State what's true now |
| Terminology drift | A system called by an outdated/incorrect name relative to its current role (e.g. a live core data store called "legacy" just because it predates something newer) | The established, correct term, used consistently |
| Over-justified asides | A long historical justification where the current-state fact alone would do | Trim to the fact; keep a short "why" only when load-bearing (a real constraint or tradeoff) |

## Tabulation / verbosity pass (same pass, not separate)

Convert to a table: genuinely repeated parallel structure across items
("X does A, Y does B, Z does C" spread across sentences/bullets).
Trim: prose that restates a preceding code block/diagram/quote almost
verbatim.

**Don't force it**: not a 2-item list; not a genuinely sequential or
hierarchical explanation; not if the table would need its own "how to
read this" explanation (prose was better); diagram explanations are
exempt if the repo has a diagramming house style with its own required
explanation format (still trim if bloated). Preserve every fact and
technical detail — the goal is cutting repetition and narrative fat,
never information. See
[`terse-tabulated-summaries-over-prose`](terse-tabulated-summaries-over-prose.md)
for the sibling rule about PR/doc *summary* style specifically (a
narrower case of the same density-without-loss principle).

## Before calling a file done

- Confirm the whole file was read, not an excerpt.
- Confirm every fix preserves technical meaning — change phrasing,
  never the underlying fact.
- Check every cross-reference/link touched still resolves.
- Check the file off in the tracking checklist.

## Bugs found along the way (layered on top, given in the same session)

| Where found | Action |
| :-- | :-- |
| Core library code, any of the 4 languages | **Halt.** Report and explain the context, then raise an interactive placeholder question so the user is notified, and resolve together before continuing the sweep |
| Fixtures, samples, `docs/extending/` source code | Fix it and test it directly, yourself — unless genuinely critical, in which case treat it the same as a core-library bug (halt and ask) |
