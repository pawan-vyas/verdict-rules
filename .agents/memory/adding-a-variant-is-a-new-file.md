<!-- Title: Adding A Variant Is A New File -->
# Adding a variant is a new file, never an edit to a shared one

> The repo-structure form of the dispatch rule [`AGENTS.md`](../../AGENTS.md) already states for
> code. **If adding the Nth thing means editing a file the other N−1 share,
> the structure is wrong.** Restructure so the Nth thing is a new file, a new
> directory, or a new row — and nothing existing moves.

## Why it matters more here than in most repos

This repository is built to hold several languages, each on its own branch
until it ships. Every shared file is therefore a **merge conflict multiplied by
the number of languages in flight**, and worse, a place where one language's
change can silently break another's.

## Where it has already been applied

Each of these was a restructure done specifically to remove a shared edit:

| Shared thing | Became |
| :-- | :-- |
| One `test.yml` for all languages | `test-<lang>.yml`, path-filtered, one per language |
| One release workflow | `release-<lang>.yml` calling a shared tail |
| One PR template | `PULL_REQUEST_TEMPLATE/` with one file per kind of change |
| [`SKILL.md`](../../skills/verdict/SKILL.md) listing every reference file | `references/<language>/`, routed to generically |
| Skill reference content restated per language | `MANIFEST.toml` rows, copied from the repo's own docs |
| One repo-wide `CHANGELOG.md` | One beside each package's own manifest |
| One `docs/maintenance.md` monolith | [`docs/maintenance/`](../../docs/maintenance/README.md), one file per concern |
| One `docs/architecture.md` monolith | [`docs/architecture/`](../../docs/architecture/README.md) — shared `README.md` plus one concrete file per language |
| A release procedure written per-target inline | [`docs/maintenance/releases/`](../../docs/maintenance/releases/README.md) — shared pipeline plus one file per target |
| A sample's spec restated inside each language's own package tree | [`docs/samples/<scenario>/`](../../docs/samples/README.md) — spec plus one implementation file per language, in one directory |
| One `docs/extension.md` monolith, seven Python-only "Recipes" | [`docs/extending/<scenario>/`](../../docs/extending/README.md) — spec plus one implementation file per language, one directory per scenario, "recipes" renamed to "scenarios" |
| One `docs/testing.md` monolith, Python specifics bundled straight in | [`docs/testing/`](../../docs/testing/README.md) — shared contracts and checklist plus one concrete file per language |

Twelve restructures, all the same shape. The pattern was operating long before it
was written down, which is how the changelog stayed monolithic until two
release tracks were already contending for it — and the docs restructures were
the same lesson applied to prose instead of code: a doc is bound by nothing a
language's own tooling imposes, so it has even less excuse to stay monolithic
than the workflows did.

## The test, before adding anything

Ask: **what does the fifth one cost?** If the answer involves editing a file the
first four already share, the cost is not linear — it is a conflict every time
two of them move at once. Pay the restructure before the second variant exists,
not after the fourth.

## What this does not mean

Not every shared file is wrong. [`AGENTS.md`](../../AGENTS.md), the shared docs, and the reusable
release workflow are shared **because their content is genuinely common**, and
duplicating them per language would create drift rather than remove conflict.

The distinction is whether adding a variant **forces** an edit. A shared file
that all variants *read* is fine. A shared file that each variant must *write
to* is the problem.

The root [`README.md`](../../README.md) is the one deliberate exception that
still obeys the same rule at a finer grain: it repeats a worked example per
language on purpose, because it is the landing page and has to explain the
library "in one shot" without sending a first-time reader elsewhere. A landed
language adds its own collapsible `<details>` block under **Quickstart**
(collapsed by default — only Python, the canonical example, stays open) and a
new row each in **Status** and **Where to go next**'s tables — new blocks and
rows, appended, never an edit to an existing language's own. Same dispatch
rule, applied at the sub-file level instead of across files, precisely because
this one file is intentionally not split into a directory the way every other
multi-language doc in this repo is.

Setup commands used to be duplicated a third time in a `## Development`
section here too, one `### <Language>` subsection per language — removed once
it became clear each language's own `AGENTS.md` already carries the identical
commands under its "Before calling a change done" section, and
[`CONTRIBUTING.md`](../../CONTRIBUTING.md) already pointed there. Three places
that should say the same thing is exactly the drift this file warns about;
`CONTRIBUTING.md` now names the pattern generically ("each language keeps its
own setup commands in its own `AGENTS.md`") and needs no edit at all when a
language lands, which is a stronger form of this rule than an appended
row — zero edits, not just an additive one.

Related: [`features-land-in-every-language`](features-land-in-every-language.md),
[`verdict-is-a-protocol-spec`](verdict-is-a-protocol-spec.md).
