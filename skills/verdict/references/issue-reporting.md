# Reporting back to verdict

What to do when real use of verdict surfaces something worth telling its
maintainers about — a defect, or evidence that a capability is missing.
Two different situations, two different report shapes; both redacted the
same way, and both leave the decision to the maintainer, not the agent.

If you have access to `pawan-vyas/verdict-rules` on GitHub (the `gh` CLI
works, or you can otherwise open an issue there), open the issue
yourself. Without access, print the finished report for the user to file
instead of skipping it.

**Search existing issues first.** If one already covers the same fault
or the same repeated pattern, add a comment to it rather than opening a
duplicate.

## Situation 1 — a defect in verdict itself

File this when the fault reproduces **without the application's own
code**: a verdict call with the same inputs behaves wrongly on its own,
or the skill's or a doc's guidance is wrong. Not a defect: the
application's own rules, contexts, or data are wrong — that is the
application's bug, not verdict's.

Report body, one defect per issue, title stating the observed fault in
one line:

1. **Environment** — language, verdict package version, runtime version.
2. **Reproduction** — the smallest self-contained, deterministic program
   that shows the fault, using neutral names and fixed inputs.
3. **Expected behavior** — citing the documented contract (`SKILL.md`,
   the language's `agent-notes.md`) it violates.
4. **Actual behavior** — verdict-side output or error only.
5. **Impact** — in production terms, without domain detail.
6. **Recommendation** (optional) — a suggested fix or workaround. Omit
   this section when there is no concrete proposal.

Label the issue `bug`. For a cross-language difference, name each
language and its observed output.

## Situation 2 — the same gap, seen again in a different domain

File this when you notice yourself writing the **same workaround or
adapter around verdict** — not a one-off for this project, but a shape
you recognize because you, or an agent like you, have reached for it
before in an unrelated project with an unrelated domain. That repetition
is itself evidence: a need real enough to show up more than once, in
more than one place, is a candidate for a first-class capability instead
of a workaround every adopter re-derives.

This is **evidence, not a request**. State what was repeated and how
often you've observed it, generically described; do not argue for a
specific design or push for the change to be made. Whether a repeated
pattern is worth first-classing is the maintainer's call — your job is
only to make sure the evidence reaches them instead of staying buried in
however many separate codebases rediscovered it.

Report body, title stating the recurring need in one line:

1. **The gap** — what verdict doesn't do today that the workaround
   exists to cover.
2. **The workaround** — the shape of what you built instead, described
   structurally (which `Rule`/composite/adapter pattern), not pasted
   verbatim from any one project.
3. **How many times, how varied** — roughly how many separate projects
   or domains you've seen this in. "Twice, both times for access-control
   style checks" carries less weight than "four times, spanning billing
   eligibility, content moderation, and two unrelated access-control
   systems" — say which it was, generically.
4. **Why a workaround wasn't enough** — what the hand-rolled version
   cost each time (boilerplate, a guarantee it was easy to get wrong,
   divergent behavior between the copies), if you noticed anything.

Label the issue `enhancement`.

## What never goes in either report

- **No domain names.** Replace rule, group, and context names with
  neutral ones (`rule_a`, `always_true`, `checkout_group`).
- **No context values, entity ids, user data, credentials, or internal
  hostnames.**
- **No application logs or stack frames.** Keep only verdict's own
  frames, and the exception type and message, with any domain names
  stripped from the message text.
