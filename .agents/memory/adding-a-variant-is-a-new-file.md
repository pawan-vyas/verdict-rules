<!-- Title: Adding A Variant Is A New File -->
# Adding a variant is a new file, never an edit to a shared one

> The repo-structure form of the dispatch rule `AGENTS.md` already states for
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
| `SKILL.md` listing every reference file | `references/<language>/`, routed to generically |
| Skill reference content restated per language | `MANIFEST` rows, copied from the repo's own docs |
| One repo-wide `CHANGELOG.md` | One beside each package's own manifest |

Six restructures, all the same shape. The pattern was operating long before it
was written down, which is how the changelog stayed monolithic until two
release tracks were already contending for it.

## The test, before adding anything

Ask: **what does the fifth one cost?** If the answer involves editing a file the
first four already share, the cost is not linear — it is a conflict every time
two of them move at once. Pay the restructure before the second variant exists,
not after the fourth.

## What this does not mean

Not every shared file is wrong. `AGENTS.md`, the shared docs, and the reusable
release workflow are shared **because their content is genuinely common**, and
duplicating them per language would create drift rather than remove conflict.

The distinction is whether adding a variant **forces** an edit. A shared file
that all variants *read* is fine. A shared file that each variant must *write
to* is the problem.

Related: [`features-land-in-every-language`](features-land-in-every-language.md),
[`verdict-is-a-protocol-spec`](verdict-is-a-protocol-spec.md).
