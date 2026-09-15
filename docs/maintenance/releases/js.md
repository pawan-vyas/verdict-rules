<!-- Title: JS/TS Release Procedure -->
# JS/TS release procedure

> The concrete steps for releasing `verdict-rules` to npm. The pipeline
> shape these steps plug into — job ordering, when each check runs — is
> shared across every language; see
> [`README.md`](README.md) for that.

This package is published to npm as `verdict-rules`, with a real
version pin standing between a change here and any consumer's
production code — the normal "cut a release, consumers upgrade when
ready" safety net applies, unlike an internal package vendored via a
`file:` dependency.

Release procedure, once a change is ready to ship:

1. Bump `js/packages/verdict-rules/package.json`'s `version` (semver;
   `0.x` while the public API is still settling — a breaking change
   bumps `MINOR` pre-1.0, `MAJOR` after; see
   [`python.md`](python.md)'s own release procedure for the exact
   pre-1.0 `PATCH` carve-out and how high its bar is set).
2. Add a `## [X.Y.Z] - YYYY-MM-DD` entry to
   [`../../../js/packages/verdict-rules/CHANGELOG.md`](../../../js/packages/verdict-rules/CHANGELOG.md),
   in the same commit as the version bump — never backfilled later. The
   release body is built from it, and a missing section fails the
   release rather than publishing empty notes.
3. **Merge to `main`. That is the whole release.**

`release-js.yml` notices the version has no matching tag, runs the full
test matrix across every supported Node version, builds, publishes to
npm via Trusted Publishing, then tags and cuts the GitHub release — in
that order, each step gated on the one before it, per the shared
pipeline shape.

`package.json`'s `version` field is the single source of truth for what
shipped — CI asserts it matches the tag being pushed and fails loudly
on drift, rather than silently publishing a mismatch.

## The first publish is a genuine, unavoidable trap

Unlike NuGet (see [`csharp.md`](csharp.md)), npm cannot attach a
Trusted Publishing policy to a package that has never been published —
there is no equivalent to PyPI's "pending publisher." The very first
version has to go out as a real `npm publish` from a developer machine,
by hand, before the trusted publisher can be configured at all. This
package's own first publish was a throwaway `0.0.0` placeholder,
published specifically to create the package record — never the real
first version — with the actual `0.0.1` left for CI to publish once the
Trusted Publisher was configured against `release-js.yml`.

Configuring the Trusted Publisher on npmjs.com needs two settings that
are easy to get wrong silently:

- **Environment name must match `release-js.yml`'s own
  `environment: npm` declaration on its `publish-npm` job exactly.** A
  mismatch here doesn't error — the OIDC claim simply never matches, and
  the publish step fails as if no trust existed at all.
- **"Allow `npm publish`" must be explicitly checked.** npm's staged
  publish (`npm stage publish`, a separate manual-approval flow) is
  always allowed by default; direct `npm publish` — what
  `release-js.yml` actually runs — needs that box checked, or the
  publish step fails even with a correctly configured trust
  relationship.

Once both of npm's own account requirements are met — 2FA enabled on
the account that did the manual placeholder publish, since npm now
refuses any publish, even the very first one, without either that or a
bypass-2FA token — this workflow owns every version after the
placeholder with no further manual `npm publish` needed.

## Provenance

npm publishes provenance attestations (`dist.attestations`) automatically
for a public repository publishing a public package under Trusted
Publishing — nothing extra to configure beyond publishing over OIDC
rather than a stored token, the same shape as PyPI's PEP 740.
`release-js.yml`'s `verify-published` job asserts
`dist.attestations.provenance.predicateType` is present on the
version-specific registry endpoint after publishing, rather than
trusting that the push step exiting zero means the right thing is live.
See [`../supply-chain-and-ownership.md`](../supply-chain-and-ownership.md)
for how this compares across every registry this project targets.
