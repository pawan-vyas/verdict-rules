<!-- Title: C# Release Procedure -->
# C# release procedure

> The concrete steps for releasing `VerdictRules` to NuGet. The
> pipeline shape these steps plug into — job ordering, when each check
> runs — is shared across every language; see
> [`README.md`](README.md) for that.

This package is published to NuGet as `VerdictRules`, with a real
version pin standing between a change here and any consumer's
production code — the normal "cut a release, consumers upgrade when
ready" safety net applies, unlike an internal package vendored via a
project reference.

Release procedure, once a change is ready to ship:

1. Bump `csharp/src/VerdictRules/VerdictRules.csproj`'s `<Version>`
   (semver; `0.x` while the public API is still settling — a breaking
   change bumps `MINOR` pre-1.0, `MAJOR` after; see
   [`python.md`](python.md)'s own release procedure for the exact
   pre-1.0 `PATCH` carve-out and how high its bar is set).
2. Add a `## [X.Y.Z] - YYYY-MM-DD` entry to
   [`../../../csharp/src/VerdictRules/CHANGELOG.md`](../../../csharp/src/VerdictRules/CHANGELOG.md),
   in the same commit as the version bump — never backfilled later. The
   release body is built from it, and a missing section fails the
   release rather than publishing empty notes.
3. **Merge to `main`. That is the whole release.**

`release-csharp.yml` notices the version has no matching tag, runs the
full test matrix, packs, publishes to NuGet via Trusted Publishing,
then tags and cuts the GitHub release — in that order, each step gated
on the one before it, per the shared pipeline shape.

`VerdictRules.csproj`'s `<Version>` is the single source of truth for
what shipped — CI asserts it matches the tag being pushed and fails
loudly on drift, rather than silently publishing a mismatch.

## Publishing is a token exchange, not a direct OIDC publish

Unlike PyPI, NuGet's Trusted Publishing doesn't let the workflow publish
directly against a live OIDC token. `release-csharp.yml`'s `publish-nuget`
job trades a short-lived GitHub OIDC token for a temporary NuGet API
key — one that lives **one hour** — via `NuGet/login@v1`, then passes
that key to `dotnet nuget push`. The key is requested immediately before
the push, deliberately: nothing between login and push should be slow
enough to risk it expiring.

The one genuine advantage this buys over npm and pub.dev (see
[`js.md`](js.md) and [`dart.md`](dart.md)): with a correctly scoped
policy, NuGet's Trusted Publishing can perform the *very first* publish
of a package that has never existed before. There is no manual
placeholder-publish trap here — the one-time human step is registering
the policy on nuget.org (organization/user, repository, the workflow
filename `release-csharp.yml`, optional environment), not publishing by
hand first.

## Provenance

NuGet counter-signs every accepted package automatically — `.signature.p7s`
sits at the package root of the downloaded `.nupkg`, applied by nuget.org's
own publish pipeline regardless of how the package was pushed. Unlike
PyPI's PEP 740 attestations or npm's `dist.attestations`, this isn't
something Trusted Publishing specifically enables; it's baseline registry
policy. `release-csharp.yml`'s own `verify-published` job downloads the
published `.nupkg` from NuGet's version-specific flat-container endpoint
and asserts the signature file is present, rather than trusting that the
push step exiting zero means the right thing is live. See
[`../supply-chain-and-ownership.md`](../supply-chain-and-ownership.md)
for how this compares across every registry this project targets.
