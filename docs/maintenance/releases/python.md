<!-- Title: Python Release Procedure -->
# Python release procedure

> The concrete steps for releasing `verdict-rules` to PyPI. The pipeline
> shape these steps plug into — job ordering, when each check runs — is
> shared across every language; see
> [`README.md`](README.md) for that.

This package is published to PyPI as `verdict-rules`, with a real
version pin standing between a change here and any consumer's
production code — the normal "cut a release, consumers upgrade when
ready" safety net applies, unlike an internal package vendored via an
editable local path.

Release procedure, once a change is ready to ship:

1. Bump `python/pyproject.toml`'s `version` (semver;
   `0.x` while the public API is still settling — a breaking change
   bumps `MINOR` pre-1.0, `MAJOR` after).

   **One carve-out, pre-1.0 only**: removing behaviour that was never
   intended and has no valid use may go in `PATCH`. The bar is
   deliberately high — not "we think nobody relies on it" but "there is
   no way to rely on it correctly." `0.1.1` is the precedent: an unknown
   group returned a vacuous pass, which could only ever fire on a typo
   or a stale name, because a group exists exactly when some rule
   declares it. Anything a caller could legitimately have depended on is
   a breaking change and takes `MINOR`, however unlikely that dependency
   seems.
2. Add a `## [X.Y.Z] - YYYY-MM-DD` entry to
   [`../../../python/packages/verdict-rules/CHANGELOG.md`](../../../python/packages/verdict-rules/CHANGELOG.md), in the same commit
   as the version bump — never backfilled later. The release body is built
   from it, and a missing section fails the release rather than publishing
   empty notes.
3. **Merge to `main`. That is the whole release.**

`release-python.yml` notices the version has no matching tag, runs the
full test matrix, builds, publishes to PyPI via Trusted Publishing, then
tags and cuts the GitHub release — in that order, each step gated on the
one before it, per the shared pipeline shape.

`pyproject.toml`'s `version` field is the single source of truth for
what shipped — CI asserts it matches the tag being pushed and fails
loudly on drift, rather than silently publishing a mismatch.

## Build mechanics specific to the workspace

Two things about the Python workspace are easy to get wrong when
touching the release workflow:

- `uv build` at the workspace root does *not* build the package. The
  root has no `[project]`, so a bare `uv build` tries to build the root
  itself and emits a garbage `packages-0.0.0` artifact rather than
  failing cleanly. The release workflow uses
  `uv build --package verdict-rules`.
- Build output lands in the **workspace root's** `dist/`, not the
  member's, which is why the workflow still uploads `python/dist/`.

See [`../packages-and-changelogs.md`](../packages-and-changelogs.md) for
why the workspace is laid out this way in the first place.

## Provenance

PyPI attestations (PEP 740) are automatic under Trusted Publishing —
nothing was configured for this beyond publishing over OIDC rather than
a token. Our `0.2.0` release carries an attestation naming the
publisher (`GitHub`), the repository, and the workflow file — fetchable
at `pypi.org/integrity/verdict-rules/<version>/<file>/provenance`. See
[`../supply-chain-and-ownership.md`](../supply-chain-and-ownership.md)
for how this compares across every registry this project targets.
