# Contributing to Verdict

> How to set up a change, what a contribution needs to prove before it's
> mergeable, and where a given kind of change actually belongs. Verdict
> is a polyglot design — only Python ships today — so this doc covers
> what's true across every language, with a per-language section below
> for the concrete commands.

## Before you start

Two constraints are load-bearing for every language this project ever
ships, not a style preference — see each language's own `AGENTS.md` and
`docs/maintenance.md` for the full reasoning, but the short version:

1. **Zero external dependencies**, in that language's own idiom. A PR
   that adds one — even a small, well-regarded one — needs a real
   discussion in an issue first, not a surprise in the diff.
2. **No knowledge of any specific domain.** Nothing under a language's
   own package source should ever reference rate limiting, access
   grants, discounts, or any other consumer's vocabulary. That logic
   belongs in a consumer's own adapter module — see `docs/extension.md`
   for what a well-formed adapter looks like, and open an issue first if
   you think something belongs in the core instead (see
   `docs/future_plan.md` for the test used to evaluate that).

If your change doesn't clear both, it's very likely better suited to
your own adapter code than to this repository — see `docs/extension.md`
before opening a PR that would fail either constraint.

## Python

```bash
cd python/
uv sync
uv run pytest
```

No external services or environment variables needed — the test suite
is as standalone as the package itself. Before opening a PR:

- **Every new concrete `Rule` shape** needs, at minimum, a plain
  delegation test, and if it's a composite: short-circuit behavior in
  both directions it can short-circuit on, plus its vacuous-input case
  (empty list, or whatever "nothing configured" means for that shape).
  See `docs/testing.md`'s "Checklist for a new contribution" for the
  full table by change kind.
- **A change to `RuleResult`/`RunResult`'s shape, or to `Rule`'s
  required attributes/signature**, is the one class of change that
  ripples outward to every consumer — see `docs/maintenance.md`'s
  consumer-impact checklist before touching either.
- Run `uv run pytest` from `python/` (covers the core suite plus
  `examples/graduation_verdict/`'s own 523 tests) — all of it needs to
  stay green, not just the file you touched.
- If your change touches anything documented, update that doc in the
  same PR — a stale doc is worse than no doc. `docs/maintenance.md`'s
  "Where to make a change" table names the right file for a given kind
  of change.

## Opening an issue

- **Bug report**: repro steps, expected vs. actual, and which language
  (today: always Python, but the template asks explicitly so it still
  makes sense once a second language ships).
- **Feature request**: before writing it up, run it through
  `docs/future_plan.md`'s own evaluation test — most "convenience"
  additions are a one-line `docs/extension.md` recipe in your own code,
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
  push. `AGENTS.md`'s "Cross-language coding & doc conventions" section
  covers the full list.

## A second language

If you're building the first JS/TS or C# SDK: read `AGENTS.md`'s "What
this repo is" section first — a new language lands as its own top-level
directory alongside `python/`, with its own `AGENTS.md` for that
language's conventions, and its own reference set under
`skills/verdict/references/<language>/`. Open an issue before starting
a large port — this is exactly the kind of change worth agreeing on
scope for before code exists.
