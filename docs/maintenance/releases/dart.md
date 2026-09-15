<!-- Title: Dart Release Procedure -->
# Dart release procedure

> The concrete steps for releasing `verdict_rules` to pub.dev. The
> pipeline shape these steps plug into — job ordering, when each check
> runs — is shared across every language; see
> [`README.md`](README.md) for that.

This package is published to pub.dev as `verdict_rules`, with a real
version pin standing between a change here and any consumer's
production code — the normal "cut a release, consumers upgrade when
ready" safety net applies, unlike an internal package vendored via a
path dependency.

Release procedure, once a change is ready to ship:

1. Bump `dart/packages/verdict_rules/pubspec.yaml`'s `version` (semver;
   `0.x` while the public API is still settling — a breaking change
   bumps `MINOR` pre-1.0, `MAJOR` after; see
   [`python.md`](python.md)'s own release procedure for the exact
   pre-1.0 `PATCH` carve-out and how high its bar is set).
2. Add a `## X.Y.Z` entry to
   [`../../../dart/packages/verdict_rules/CHANGELOG.md`](../../../dart/packages/verdict_rules/CHANGELOG.md),
   in the same commit as the version bump — never backfilled later. Bare
   heading, no brackets or date: pub.dev parses this file directly and
   documents whatever heading matches `## X.Y.Z`, unlike PyPI and npm,
   which follow Keep a Changelog but parse nothing.
3. **Merge to `main`. That is the whole release** — for every version
   after the first. See "Two phases, not one" below for why the very
   first version is a genuine exception to "merge is the whole release."

## Two phases, not one

`release-dart.yml` is not structurally parallel to
[`python.md`](python.md)'s or [`csharp.md`](csharp.md)'s own workflow,
for one mechanical reason: **pub.dev requires the workflow run that
publishes to be triggered *by* the tag push itself**, verified through
that run's own OIDC token claims against the tag pattern registered on
pub.dev's admin page. A run triggered by a push to `main` cannot
publish, even when a matching tag already exists.

So a version bump landing on `main` triggers *phase one*: the full test
matrix, then pushing (not publishing) the release tag — nothing reaches
pub.dev in this run. That tag push re-triggers the same workflow into
*phase two*, which calls pub.dev's own `dart-lang/setup-dart` reusable
publish workflow, then the shared GitHub-release tail for the release
notes and skill artifacts.

## The first publish is genuinely manual, unlike NuGet

pub.dev is explicit that only an *already-existing* package can be
published automatically — there is no equivalent to PyPI's "pending
publisher" or NuGet's ability to scope a Trusted Publishing policy to
allow a brand-new package ID. The very first version has to go out as a
real `dart pub publish` from a developer machine, by hand, before
automated publishing can even be configured on pub.dev's admin page.
Plan for this rather than discovering it mid-release — see
[`js.md`](js.md) for the equivalent, differently-shaped trap on npm.

## Provenance

pub.dev offers no attestation or signature equivalent to PyPI's PEP 740,
npm's `dist.attestations`, or NuGet's repository signature — stated
plainly here rather than leaving the gap looking accidental. What it
does publish is an `archive_sha256` per version, fetchable from that
version's own API endpoint
(`pub.dev/api/packages/verdict_rules/versions/<version>`).
`release-dart.yml`'s `verify-published` job asserts that field is
present after publishing, which is the closest this registry offers to
confirming what actually shipped. See
[`../supply-chain-and-ownership.md`](../supply-chain-and-ownership.md)
for how this compares across every registry this project targets.
