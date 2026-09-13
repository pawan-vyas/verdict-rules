<!-- Title: Links in Shipped Content Are Pinned to a Version -->
# Links in shipped content are pinned to a version, never to `main`

> Why every GitHub link inside anything that ships (a package README,
> package metadata) points at that release's own tag, and what that costs.

A README bundled into a package is rendered on **every version's** registry
page, permanently. A link in it pointing at `main` therefore shows someone
reading an old version the *current* documentation — describing APIs that
version does not have, with no way for them to tell. It is the same failure the
skill's fetch tier avoids by pinning, and worse here because there is no
warning.

So every GitHub link in content that ships — a package README, package metadata
— points at that release's own tag:

```text
https://github.com/pawan-vyas/verdict-rules/blob/python-v0.2.1/docs/architecture.md
```

Tags are immutable, so such a link resolves forever and always describes what
the installed version actually has. The cost is that these URLs move with each
release; `scripts/check_shipped_links.py` enforces it, and `--fix` does the
rewriting mechanically, so a release is: bump the version, run the fixer,
write the changelog entry.

Two consequences worth knowing:

- **The links are briefly dead during a release.** They reference a tag the
  workflow creates *after* publishing, so between merge and tag there is a
  short window where they 404. That is the correct trade: the alternative is
  tagging before publishing, which would mean a tag that does not correspond to
  anything released.
- **A moved file breaks any `main`-pinned link permanently** if it was already
  published, because registry metadata is immutable. That is how `0.1.0`,
  `0.1.1` and `0.2.0` ended up with a dead Changelog link when the repo-root
  changelog was removed — those cannot be corrected, only superseded.
