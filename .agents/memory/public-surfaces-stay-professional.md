<!-- Title: Public Surfaces Stay Professional -->
# Nothing public carries competitive analysis or working narration

> Issues, pull request titles and descriptions, commit messages, and any
> tracked doc are **public, permanent, and read as the project speaking
> in its own voice**. They carry the task and the decision. They do not
> carry assessments of other people's packages, and they do not carry
> the narration of how the work was done.

## The rule

**Never** put in a public surface:

- **Judgments about other projects** — that something is abandoned,
  dead, unmaintained, derivative, worse than ours, squatting a name, or
  a competitor. Even when accurate and carefully sourced, it reads as
  disparagement of someone else's work and reflects on this project, not
  theirs.
- **Comparison tables against named third-party packages**, download
  counts, star counts, or repo-health commentary.
- **Working narration** — what was investigated, what turned out to be
  wrong, which framing was corrected, how many attempts something took.

**Do** put there: what is being built, what was decided, and what
remains. A naming decision is stated as *"`verdict` is unavailable on
npm; the chosen name is `@verdict/core`"* — a fact and a decision, with
no characterisation of whoever holds the name.

## Where the research goes instead

`scratch/` (gitignored). If it is worth not losing, commit it to a
**local-only branch that is never pushed**. It is genuinely useful
material — the point is that it is *ours*, not published.

## Why

**Feedback from the maintainer, 2026-09-11**, after competitive triage
of the npm, NuGet and pub.dev name holders was written into three public
issues and three PR descriptions: *"it looks poorly on us."* Correct.
The analysis was sound and the research was worth doing; publishing it
was the error. The authors of those packages can read these issues.

This cost a full cleanup: rewriting three issues, three PR descriptions,
and **force-pushing three branches**, because a follow-up commit leaves
the original text in the pushed history where it is still readable from
the PR. Prevention is much cheaper than that.

## How to apply

Before posting or committing anything public, reread it as its subject
would: *would I be comfortable if the author of the package I just
described read this?* If not, cut it — the useful version is almost
always shorter. When a decision rests on research, state the decision
and keep the research local.

Related: [`releases-are-language-scoped`](releases-are-language-scoped.md),
[`skill-version-is-independent-of-sdk-versions`](skill-version-is-independent-of-sdk-versions.md).
