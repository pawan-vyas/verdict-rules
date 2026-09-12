# Pull request templates

One template per *kind* of change, rather than one template trying to cover
every kind. Adding a kind is a new file here — the same shape the rest of this
repo uses for languages, registries and run modes: what works for many should
work by adding a row, not by editing a central branch.

| Template | For | URL |
| :-- | :-- | :-- |
| [`../PULL_REQUEST_TEMPLATE.md`](../PULL_REQUEST_TEMPLATE.md) | **Default.** Behaviour changes: features, bug fixes, anything altering what the library does | *(automatic)* |
| [`clerical.md`](clerical.md) | Docs wording, repo config, CI, tooling, chores | `?template=clerical.md` |
| [`release.md`](release.md) | A version bump — which *is* the release | `?template=release.md` |

GitHub uses the default automatically and only offers the others through the
`?template=` query parameter, so **the default is deliberately the strictest**.
A clerical change answering "n/a" to most of it loses nothing; a behaviour
change that never saw the list is how `docs/testing.md` spent two versions
describing behaviour that had been removed.
