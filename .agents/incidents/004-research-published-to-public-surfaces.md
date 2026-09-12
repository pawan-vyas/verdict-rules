<!-- Title: Incident 004 — Research Published To Public Surfaces -->
# 004 · Competitive research written into public issues and PRs

> **2026-09-11 · required force-pushing three branches**

## What happened

Triage of the packages holding the `verdict` name on npm, NuGet and pub.dev was
written directly into three public GitHub issues and three public PR
descriptions — including characterisations like "abandoned", "dead", "not a
competitor", download counts, and comparison tables naming other maintainers'
projects.

## How it surfaced

The maintainer read it: *"it looks poorly on us."*

## Impact

Public and attributable. The authors of those packages could read it. Cleaning
up meant rewriting three issues and three PR descriptions **and force-pushing
three branches**, because a follow-up commit leaves the original text readable
in the pull request's commit list — removing it from the current files is not
removing it.

## Root cause

The research itself was correct and worth doing. The error was assuming that
*useful* implies *publishable*. Issues and PRs are permanent, public, and read
as the project speaking in its own voice — a register quite different from
working notes, and easy to miss when the analysis feels like a finding you are
pleased with.

## The fix

Issues rewritten as plain delivery checklists. PR descriptions reduced to what
is being built and what is decided. All research moved to `.agents/scratch/`
(gitignored) and preserved on a local-only branch.

## What prevents a repeat

- A standing rule in `AGENTS.md`, plus the full reasoning in
  [`../memory/public-surfaces-stay-professional.md`](../memory/public-surfaces-stay-professional.md).
- **The test before posting anything public**: would I be comfortable if the
  author of the package I just described read this? If not, cut it — the useful
  version is almost always shorter.
- State a decision as a fact and keep the research local: *"`verdict` is
  unavailable on npm; the chosen name is `verdict-rules`"* needs no
  characterisation of whoever holds it.
- Note that removing it later costs a history rewrite, so there is no cheap
  undo. This one has to be got right the first time.
