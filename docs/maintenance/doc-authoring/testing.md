<!-- Title: Testing Doc Authoring Template -->
# Testing doc authoring template

> The structure `docs/testing/` follows: a shared, language-agnostic
> testing guide plus one concrete file per language. This builds on the
> general standard in [`README.md`](README.md) — read that first,
> especially the durable-shared-doc rule this template exists to keep
> `docs/testing/README.md` honest against. Same shape as
> [`architecture.md`](architecture.md): one directory, not one per
> scenario, since there is exactly one testing guide to document.

## One directory, one shared doc, one file per language

`docs/testing/`:

- **The shared doc** — `docs/testing/README.md`. Every contract that
  holds identically in every language's SDK: short-circuiting,
  vacuous-truth polarity, absence-vs-emptiness, the fallback matrix,
  run-everything-never-short-circuits, no-flattening, duplicate-name
  handling, predicate-exception propagation — stated as reasoning, never
  as a specific test's name. Plus the contribution checklist. Written
  once, read by every language. Renders automatically on GitHub when
  linking to the directory itself.
- **The implementation** — `docs/testing/<language>.md`, one per
  language that has documented its own concrete realization
  (`python.md`, for instance). The measured totals, the concrete file
  layout, and a table naming which test proves which shared-doc
  contract.

A language that hasn't written its own file yet simply has no file
here; nothing reserves the slot.

## Shared doc (`README.md`) structure

Not a strict numbered order — closer to a reference than a narrative —
but every contract follows the same rule: state it once, generically,
and hand off to "each language's own file in this directory" for the
concrete test names, never listing one language's `test_*` names inline.

- **Title + blockquote framing** — what the doc covers, and its
  relationship to [`../README.md`](../README.md)
  ("where a change goes" vs. "proving it correct"). Never a pointer to a
  specific `<language>.md` by name — see [`README.md`](README.md)'s
  durable-shared-doc rule.
- **The second testing layer** — the cross-language parity fixture
  (`fixtures/<name>/`) every language's own example project is checked
  against, stated generically; the specific test counts and file paths
  are that language's own concrete detail.
- **What actually needs proving, not just executed** — one bullet per
  contract, each naming *why* line coverage alone would miss it (a test
  that only checks the final boolean can pass while missing the actual
  guarantee). No test-function names here — that mapping lives in
  `<language>.md`'s own table.
- **Checklist for a new contribution** — a table, change kind → what
  the test must also prove. Already generic; keep it that way.
- **Related docs** — architecture, maintenance, extending, the shared
  fixture. Not each language's own concrete file — it's already one
  click away in this same directory's own GitHub listing.

## Implementation file (`<language>.md`) structure

- **Title + blockquote** pointing back to `README.md` ("read that
  first," with a relative link).
- **`## The suite`** — the run command, then the measured totals: test
  count, coverage if that language has a tool wired in, and a **per-file
  table** (file, test count, what it proves). Not a pasted run
  transcript: a literal block of tool output is the part that rots
  silently, and every one of these files had drifted by a wide margin
  before they were rebuilt against measured numbers. State the figures
  as facts and re-measure them when the suite changes.
- **The mutation score, named as the stronger measure** — line coverage
  proves a line ran, not that anything would notice it being wrong, so
  this section links that language's own survivor list under
  [`../mutation-survivors-python.md`](../mutation-survivors-python.md)
  and its siblings. Where the tool's own reach is narrow enough that a
  high score overstates the guarantee, say so here rather than letting
  the number speak for itself.
- **Which files are the portable contract suite** — the subset another
  language's suite is audited against, and why the rest sit beside it
  rather than inside it.
- **The CI workflow** that runs it, and which job is the one to mark
  required in branch protection.
- **The second testing layer**, if that language has example projects
  checked against a shared fixture — their totals, and a link to their
  own testing doc.
- **Test layout**, if a diagram earns its place — real file names
  (`test_rule.py`, `test_engine.py`), unlike the shared doc's diagrams,
  which are conceptual, not literal.
- **Which test proves which contract** — a table, one row per
  shared-doc contract, naming the concrete test function(s) and file.
  **Verify every name against the source before committing**; a
  hand-written test name that no longer exists is invisible to every
  checker this repo has.
- **Confirmed to bite** — at least one worked instance of a contract
  test actually failing when the behaviour is broken by hand, so the
  table is evidence rather than an inventory.
- **Related** — the shared doc, that language's survivor list, and the
  second-layer example project's own testing doc if one exists.

## Adding a new language's file

Add `docs/testing/<language>.md` to the existing directory — nothing in
`README.md` changes. A language whose test suite proves a contract
differently enough to be worth a callout (a different concurrency
primitive to avoid, say) puts that callout in its own file, not in an
edit to the shared doc.
