# Contributing to Verdict

> How to set up a change, what a contribution needs to prove before it's
> mergeable, and where a given kind of change actually belongs. Verdict
> is a polyglot design, and this doc stays language-invariant on
> purpose — the concrete commands live beside each language's own code,
> never duplicated here.

## Before you start

Two constraints are load-bearing for every language this project ever
ships, not a style preference — full reasoning in each language's own
`AGENTS.md` and
[`docs/maintenance/constraints.md`](docs/maintenance/constraints.md):
zero external dependencies, and no knowledge of any specific consumer's
domain. A PR that would cross either needs a real discussion in an
issue first, not a surprise in the diff — see [`docs/extending/`](docs/extending/README.md) for
where that logic belongs instead, and [`docs/future_plan.md`](docs/future_plan.md) for the
test used to decide whether something belongs in the core at all.

## Setting up a change

Each language keeps its own concrete setup commands in its own
`AGENTS.md`, under "Before calling a change done" — `python/AGENTS.md`,
`js/AGENTS.md`, `dart/AGENTS.md`, and so on for any later one. A new
language adds its own file there; nothing here changes for it to do so.
None of them need external services or environment variables — every
language's test suite is as standalone as its own package.

Before opening a PR, regardless of language:

- **Every new concrete `Rule` shape** needs, at minimum, a plain
  delegation test, and if it's a composite: short-circuit behavior in
  both directions it can short-circuit on, plus its vacuous-input case
  (empty list, or whatever "nothing configured" means for that shape).
  See [`docs/testing/`'s "Checklist for a new contribution"](docs/testing/README.md#checklist-for-a-new-contribution) for the
  full table by change kind.
- **A change to `RuleResult`/`RunResult`'s shape, or to `Rule`'s
  required attributes/signature**, is the one class of change that
  ripples outward to every consumer — see
  [`docs/maintenance/before-merging-checklists.md`](docs/maintenance/before-merging-checklists.md)'s
  consumer-impact checklist before touching either.
- Run that language's full suite via its own `AGENTS.md` command — all
  of it needs to stay green, not just the file you touched.
- If your change touches anything documented, update that doc in the
  same PR — a stale doc is worse than no doc.
  [`docs/maintenance/README.md`](docs/maintenance/README.md)'s
  table names the right file for a given kind of change.

## Opening an issue

- **Bug report**: repro steps, expected vs. actual, and which language.
- **Feature request**: before writing it up, run it through
  [`docs/future_plan.md`'s own evaluation test](docs/future_plan.md#the-actual-test-not-does-it-sound-useful) — most "convenience"
  additions are a one-line [`docs/extending/`](docs/extending/README.md) scenario in your own code,
  not a core feature. The issue template asks the same two questions
  that test does.

## Pull requests

- Keep a PR scoped to one change — a new `Rule` shape and an unrelated
  doc fix are two PRs, not one.
- Tests first-class: a PR that changes behavior without a test proving
  it is going to get asked for one, not merged as-is.
- Match the existing doc conventions if you touch any `.md` file — a
  blockquote framing right under the title, and (for anything with a
  Mermaid diagram) validated with that diagram's own tooling before you
  push. [`AGENTS.md`'s "Cross-language coding & doc conventions" section](AGENTS.md#cross-language-coding--doc-conventions)
  covers the full list.

## A new language

Read [`AGENTS.md`'s "What this repo is" section](AGENTS.md#what-this-repo-is)
first — a new language lands as its own top-level directory alongside
`python/`, `js/`, and `dart/`, with its own `AGENTS.md` for that
language's conventions, and its own reference set under
`skills/verdict/references/<language>/`. Open an issue before starting
a large port — this is exactly the kind of change worth agreeing on
scope for before code exists.
