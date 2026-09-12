## Release

<!-- Which language, and from what version to what. -->

## Before merging

Merging a version bump **is** the release — there is no separate tag step, and
publishing is irreversible on every registry this repo targets. `npm` burns a
version number permanently; NuGet and pub.dev never delete. So the checks that
matter are the ones that happen before this merges.

- [ ] Version bumped in that language's own manifest.
- [ ] That package's own `CHANGELOG.md` — beside its manifest — has a
      section for the new version, written in this PR. **CI enforces this**:
      a missing section fails `Release readiness` rather than publishing and
      then failing the release.
- [ ] The version is genuinely new (no existing tag) — the release workflow
      checks this too, and does nothing if it finds one.
- [ ] Semver: a breaking change takes `MINOR` pre-1.0. The one carve-out for
      `PATCH` is in `docs/maintenance.md`, and its bar is "there is no way to
      rely on it correctly", not "we think nobody does".
- [ ] If `skills/verdict/**` changed, `.claude-plugin/plugin.json` is bumped
      too — it versions independently and will release on its own tag.

## After merging — post-flight

The release workflow verifies what it deterministically can: notes rendered
with code spans intact, every expected asset attached, and `scripts/get.sh`
still resolving. Registry propagation takes longer than a job should wait, so
these two are checked by hand:

- [ ] The version is live and **installable** from the registry.
- [ ] Provenance is present where the registry offers it — PyPI at
      `pypi.org/integrity/<pkg>/<version>/<file>/provenance`, npm at
      `dist.attestations`. Its *absence* is silent, so it is worth looking:
      a release published with a stored token instead of OIDC succeeds and
      carries none.

See `docs/maintenance.md` for both procedures in full.
