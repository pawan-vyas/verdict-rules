<!-- Title: Maintenance Directory Conventions -->
# This directory's own conventions

> How `docs/maintenance/` itself is organized — on top of
> [`README.md`](README.md)'s repo-wide standard — so a new doc lands in
> the right shape without re-deciding it each time.

## Files are named for what they cover, not numbered

There's no real order among these docs — nothing here is read
top-to-bottom the way a tutorial is — so a number would only ever be
arbitrary, and arbitrary numbers fight every future split or merge (a
doc splits in two, and now every later number is wrong, or a doc merges
into another and leaves a gap). A descriptive filename never needs
renumbering for either reason. [`../README.md`](../README.md) is the
one exception, and it's a real exception: every directory gets exactly
one `README.md` as its index, and GitHub renders it automatically when
linking to the directory itself — that's a convention worth keeping
distinct from the "what does this cover" naming used for everything
else.

## A doc category that will grow per-variant is a directory, not a flat file family

Applied here as [`../releases/`](../releases/README.md): a shared
[`README.md`](../releases/README.md) for whatever is identical across
every release target, plus one file per target
([`verdict-agent-skill.md`](../releases/verdict-agent-skill.md),
[`python.md`](../releases/python.md), a future `<language>.md`) living
beside it. Adding a target is a new file in that directory — nothing
existing is touched to make room for it, and the directory's own
`README.md` never needs another paragraph appended per variant.

The test for whether a doc needs this treatment: would a second
variant showing up mean editing a file the first variant already owns?
If yes, it's a directory-with-README from the start, even before a
second variant actually exists — `releases/` was built this way before
a second language had shipped, precisely so the first one to ship
didn't have to restructure anything.

## Content migrates out of a shared doc as a variant earns its own file

[`supply-chain-and-ownership.md`](../supply-chain-and-ownership.md)
isn't itself a directory, because its per-registry detail is temporary
by design: each registry's paragraph lives there only until that
language ships and gets its own
`releases/<language>.md`, at which point the paragraph moves there.
PyPI's already has. A doc doesn't need converting to a directory just
because it currently *mentions* several variants — only when it's
accumulating real per-variant content that would otherwise mean editing
a file another variant already owns.
