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

**Standing follow-up, now unblocked**: formalize this into an authoring
template under `docs/maintenance/doc-authoring/`. It was deliberately
held until the documentation audit
([`doc-hygiene-audit-protocol`](doc-hygiene-audit-protocol.md)) had run,
so the template could be written from what the sweep actually turned up
rather than locked in ahead of it. That sweep has since covered
`docs/extending/`, `docs/maintenance/`, the root files, and this
directory, and the patterns it surfaced beyond tabulation are already
recorded in the protocol's own verbosity pass. The template is the
remaining step.
