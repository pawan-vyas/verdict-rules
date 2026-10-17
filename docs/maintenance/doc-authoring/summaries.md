<!-- Title: Summary And Density Authoring Template -->
# Writing a summary: density without loss

> How to write a pull-request description, a PR comment, or any doc section
> whose job is to *summarize* — on top of [`README.md`](README.md)'s
> repo-wide standard. The rule is dense tables over prose paragraphs, and
> the bar is not "shorter": it is **every fact a prose version would carry,
> however minute, still present**, compressed into rows instead of
> sentences. A summary that dropped a fact to fit a table has failed, not
> succeeded.

## The test, before the shape

Ask of any candidate table: *would a reader get the same facts out of it?*
If a fact had to go to make the row fit, the table is the wrong shape for
that content — not the content's problem.

Then, and only then, pick the shape. There is no fixed schema here. One
worked instance exists (a PR described with a table each for summary,
changed-why-impact, bugs found during the work, release note, and test
plan) and it is **one sample, not a form to fill in** — a different change
has different parallel facts, so it wants different columns. What carries
forward is the density-without-loss test, not the columns.

## Tabulate parallel facts; never tabulate reasoning

| Shape | Belongs in | Why |
| :-- | :-- | :-- |
| "For item X, what is the value of Y?" | a table | That is the question a table answers in one glance |
| "Because A, B follows, which is why C" | prose | Splitting a causal chain into cells deletes the link that *was* the point |
| Three short, genuinely different statements | prose | A one-row table built to satisfy a reflex is worse than the sentence |

A table whose headers need their own explanatory paragraph has relocated
the cognitive load, not removed it. If you find yourself writing "how to
read this table", the prose was better.

**Don't force it** on: a two-item list; a genuinely sequential or
hierarchical explanation; or a diagram's own explanation block, which is
narrative by design under this repo's mermaid convention — still trim that
if it is bloated, but it stays prose.

## Cut process narration, keep design rationale

They are the same content in different tense, which is why this is the
easiest cut to get wrong in both directions.

- **Keep**: why the obvious alternative is wrong. That is load-bearing, and
  a reader who doesn't get it re-proposes the alternative.
- **Cut**: the story of how you arrived there. "First draft," "was found,"
  "a self-audit revealed," "fixed in the same pass," "this session."

The test: delete the process phrase. If the sentence is still true and
still fully explains the rejected alternative, that is the version to keep.
If it genuinely needs the discovery story, the content belongs in
[`../../../.agents/incidents/`](../../../.agents/incidents/) instead —
which is the one place narration is the point.

Also cut prose that restates a code block, diagram or quote immediately
above it. Say what the reader cannot see, not what they just read.

## Never write a number a reader will trust later

This is the failure mode a documentation sweep of this repo surfaced more
than any other, and it is specific to summaries because a summary is where
counts naturally go.

| Written | What happened |
| :-- | :-- |
| "the same seven types in four languages" | The list grew; the sentence stayed |
| "asserted in 20 tracked files" | 18 by the time anyone checked |
| "roughly 2000 em-dashes across 137 markdown files" | File count moved with every doc added |
| "Twelve restructures, all the same shape" | A thirteenth landed; the table and the total disagreed |
| Four testing docs' stated test totals | **All four were already stale** before the change that prompted re-reading them |

Two rules follow. **State the durable fact and say how to get the number**
— "asserted across a couple of dozen tracked files; search for it, since
the number moves whenever a doc is added" survives, where a figure does
not. And **if a count is genuinely worth stating, measure it, never
increment it**: the four totals above were each wrong by a different amount
precisely because someone reasonable had added their delta to a figure that
was already drifting.

Same discipline for arithmetic a reader can check. A sweep found an opening
line claiming eighteen files over a breakdown summing to seventeen, and a
section introducing "two more" above a list of six bullets. Both read
fluently; both are wrong on inspection, and inspection is exactly what a
summary invites.

## What a summary must not quietly inherit

Found in this repo's own summaries and index sections, each the residue of
an edit rather than a decision:

- **The orphaned tail of a deleted bullet** — a fragment left hanging under
  an unrelated link, still grammatical enough to skim past.
- **A sentence that retracts itself mid-clause** — a parenthetical
  correcting its own claim, leaving the reader unsure which half holds.
- **An index describing contents it does not have** — five worked examples
  named, by names no directory has ever had. An index is where a stale
  claim survives longest, because nothing it describes has to be wrong for
  the index to be.
- **A link label naming a path that does not exist** while the target
  resolves. No link checker sees it; `scripts/check_link_labels.py` now
  does.
- **A reference to a planning doc that was deleted.** Those are also
  banned outright in code comments and commit messages — see
  [`../../../AGENTS.md`](../../../AGENTS.md#cross-language-coding--doc-conventions).

## Before calling a summary done

- Every fact from the prose version is still present somewhere.
- No table contains a causal chain, and no table needs instructions.
- Every number was measured just now, or replaced with how to measure it.
- Any arithmetic in the prose adds up against its own breakdown.
- No process narration survives that a reader does not need.

## Related

- [`README.md`](README.md) — the repo-wide standard this builds on,
  including the no-narration and concrete-cost rules this applies to
  summaries specifically.
- [`../../../.agents/memory/terse-tabulated-summaries-over-prose.md`](../../../.agents/memory/terse-tabulated-summaries-over-prose.md)
  — the confirmed preference this template formalizes.
- [`../../../.agents/memory/doc-hygiene-audit-protocol.md`](../../../.agents/memory/doc-hygiene-audit-protocol.md)
  — the sweep procedure whose verbosity pass this is the authoring half of.
