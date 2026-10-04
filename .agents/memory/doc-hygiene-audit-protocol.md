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
language's `AGENTS.md`/`README.md`/`CHANGELOG.md`/quickstart and its
`examples/*/README.md` + `examples/*/docs/`, `.agents/memory/`,
`skills/verdict/` + `skills/verdict-workspace/`, root `AGENTS.md`/
`CLAUDE.md`/`CONTRIBUTING.md`/`README.md`, `.github/` templates, and
`fixtures/README.md` plus `fixtures/*/README.md`.

Every index `README.md` is in scope as a file in its own right, not just
as a route to the files it lists. An index is where a stale claim hides
longest, because nothing it describes has to be wrong for the index to
be — a sweep found `fixtures/README.md` carrying the orphaned tail of a
deleted bullet, and the root `README.md` describing five worked examples
by names no fixture has ever had.

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
3. **No grep for discovery or verification inside the sweep.** Read the
   whole file, then write the whole file. A search tells you a term is
   present, never whether the sentence around it is true, and a count of
   matches reads as evidence while being none — a file "touched" with the
   right symbol count can still be broken in a way only reading finds.
   Grep is fine for building the file list in step 1, and for one narrow
   job inside the sweep: confirming a concrete claim about the repo
   elsewhere (does this path exist, is this symbol really exported, does
   this anchor resolve). Never for deciding whether a file needs reading
   or has been fixed. Stated by the maintainer after a sweep reported
   eighteen broken files where twenty-six were broken, because two had
   been marked clean from a match count rather than a read.
4. Commit in small batches (per directory/logical group, never one
   giant end-of-sweep commit), push as you go, update the checklist in
   the *same* commit as the fixes.
5. Short status report after each batch: what was read, what was
   found, what was fixed.
6. Stop and ask — don't guess and keep going — on anything needing a
   judgment call only the repo owner can make.
7. Defer the full test suite until the entire sweep is done, run it
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

## Tabulation and narrative placement, in more detail

Supplied as reference guidance from a parallel audit in another repo.
Four rules worth applying verbatim:

1. **Tabulate parallel facts; never tabulate reasoning.** A table wins
   when a reader is asking "for item X, what's the value of column Y?"
   It loses on connected reasoning ("because A, B follows, which is why
   C") — splitting that into cells removes the causal link that was the
   point. A table whose headers need their own explanatory paragraph has
   relocated the cognitive load, not removed it; three short, genuinely
   different sentences beat a one-row table built to satisfy a reflex.
2. **Design rationale and process narration are the same content in
   different tense.** Explaining *why the obvious alternative is wrong*
   is load-bearing — keep it. Narrating *the process that produced that
   conclusion* is not. The test: if deleting "first draft," "was found,"
   "a self-audit revealed," or "fixed in the same pass" leaves the
   sentence still true and still fully explaining the rejected
   alternative, rewrite it that way. If the sentence genuinely needs the
   discovery story, the content belongs in an incident record instead.
3. **"Legacy" is a factual claim, not a tone word.** It asserts a thing
   is superseded and no longer load-bearing. Test before using it: would
   removing the word change whether anything currently depends on the
   system? If nothing changes — it's still relied upon — the word is
   wrong, independent of age.
4. **Incident narration has exactly two legitimate homes**: the
   designated incident directory ([`../incidents/`](../incidents/)), or
   a doc whose *own opening states* that its purpose is extracting
   generalizable discipline from real past incidents. The exemption
   applies because the doc said so up front, not because any doc could
   claim it retroactively. Neither home excuses a tracking reference, a
   "legacy" mischaracterization, a broken link, or an unverified
   concrete claim — the exemption covers the narrative *shape* only.

A diagram's own explanation block is narrative by design (this repo's
mermaid convention requires one) — exempt from the tabulation push,
though still trimmable if bloated.

## Before calling a file done

- Confirm the whole file was read, not an excerpt.
- Confirm every fix preserves technical meaning — change phrasing,
  never the underlying fact.
- Check every cross-reference/link touched still resolves, and that a
  link's own label names the path it actually points at — a label naming
  a file that doesn't exist passes every link checker, because the target
  is fine.
- **Execute every code sample, don't read it.** A sample that looks right
  and is subtly wrong is worse than none, and the mismatch is usually in
  the output comment rather than the code: a sweep of the root `README.md`
  found all four examples correct and the C# one's expected output
  written `false` where the runtime prints `False`. Nothing but running it
  catches that.
- Check the file off in the tracking checklist.

## Bugs found along the way (layered on top, given in the same session)

| Where found | Action |
| :-- | :-- |
| Core library code, any of the 4 languages | **Halt.** Report and explain the context, then raise an interactive placeholder question so the user is notified, and resolve together before continuing the sweep |
| Fixtures, samples, `docs/extending/` source code | Fix it and test it directly, yourself — unless genuinely critical, in which case treat it the same as a core-library bug (halt and ask) |
