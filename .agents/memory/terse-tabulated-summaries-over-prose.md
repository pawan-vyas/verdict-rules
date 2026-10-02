---
name: terse-tabulated-summaries-over-prose
description: User preference for PR descriptions/comments and doc summaries — dense tables over prose essays, zero information loss, no matter how minute
metadata:
  type: feedback
---

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

**Standing follow-up, not yet done**: formalize this into an actual
authoring template in `docs/maintenance/doc-authoring/` once the
queued doc-verbosity audit (see `.agents/plans/composite-rule-and-leaves-redesign/README.md`
§0) actually begins — that audit is expected to surface further
verbosity-optimization patterns beyond tabulation alone, so the
template should get written after seeing what the full sweep turns up,
not locked in ahead of it.
