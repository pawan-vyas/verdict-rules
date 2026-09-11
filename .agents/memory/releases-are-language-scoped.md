<!-- Title: Releases Are Language-Scoped -->
# Releases are language-scoped, deliberately — never one repo-wide version

> This repo is polyglot in layout, and each language releases
> independently on its own cadence to its own registry. There is **no**
> repo-wide `vX.Y.Z` tag and there should never be one.

Tags are `python-vX.Y.Z` today; `js-vX.Y.Z`/`csharp-vX.Y.Z` once those
SDKs exist. `CHANGELOG.md` groups entries by that language-scoped tag
for the same reason.

## Why, so it doesn't get "simplified" later

A single repo-wide tag would do one of two bad things: force lockstep
releases across languages that have no reason to release together, or
become ambiguous about which language a given tag actually describes.
Neither is recoverable once published — a tag that shipped is a tag
consumers pin to.

The same reasoning drives the CI layout: `.github/workflows/` files are
named and path-filtered per language (`test-python.yml`, filtered to
`python/**`) specifically so adding a language is a **new workflow
file**, never an edit to an existing one. Its own header comment says
so. Carry that default forward rather than reaching for a matrix runner
that installs every language's toolchain in one job.

Within a language, that language's own manifest version is the single
source of truth — `python/pyproject.toml` for Python. Semver, `0.x`
while that language's public API is still settling, so a breaking
change bumps `MINOR` pre-1.0 and `MAJOR` after, independently per
language.

The **skill's** own version is not part of this — see
[`skill-version-is-independent-of-sdk-versions`](skill-version-is-independent-of-sdk-versions.md).
`.claude-plugin/plugin.json` measures the skill's content, not any
language's API, and is expected to drift from every language manifest.
