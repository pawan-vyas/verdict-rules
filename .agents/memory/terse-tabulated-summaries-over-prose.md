<!-- Title: Terse Tabulated Summaries Over Prose -->
# Tabulate a summary; don't write an essay

> A confirmed preference for PR descriptions, PR comments, and doc
> summaries: dense tables over prose, with zero information loss — every
> fact a prose version would carry, however minute, still present.

For a PR description, a PR comment, or a doc summary, default to tables
over prose paragraphs — terse, to the point, no essays. The test is not
"shorter" by itself; it's density without loss: every fact that a prose
version would carry, including minute ones, still has to be present,
just compressed into rows/columns instead of sentences.

**Why:** requested twice in direct succession for PR #103 — first a
standalone summary comment, then "PR also use the same style as the
tabulated comment." The user's own framing: convey everything "no
matter how minute" while not "looking like reading essays."

**How to apply:** Shape the tables to the content being described —
the PR #103 example (Summary / changed-why-impact / found-during-work
bugs / release note / test plan, one table each) is *one* worked
instance, explicitly flagged by the user as "just one sample, can't be
blindly applied" — don't copy that exact table schema into a
different kind of doc without checking it fits. The principle to
carry forward is the density-without-loss test itself, not a fixed
template.

**Formalized**: the authoring template is
[`docs/maintenance/doc-authoring/summaries.md`](../../docs/maintenance/doc-authoring/summaries.md),
written after the documentation sweep rather than ahead of it, so it is
built from what the sweep actually found. Two sections exist only because
of that ordering and would not have been guessed:

- **Never write a number a reader will trust later.** The sweep's most
  frequent single defect, and specific to summaries because a summary is
  where counts go. Real instances are named, including four testing
  docs whose stated totals were *each already stale* before the change that
  prompted re-reading them. Hence: state the durable fact and how to get
  the number, and if a count is worth stating, measure it rather than
  incrementing it.
- **What a summary must not quietly inherit** -- the residue of an edit
  rather than a decision: an orphaned bullet tail, a sentence that retracts
  itself mid-clause, an index describing contents it does not have, a link
  label naming a path that does not exist.

This file stays as the record of *why* the preference exists and that it
was confirmed twice in direct succession; the template is where the
how-to lives.
