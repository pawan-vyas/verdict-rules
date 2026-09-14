<!-- Title: Skill Release Procedure -->
# Skill release procedure

> Why `.claude-plugin/plugin.json`'s version is a separate number from
> any language's, and the concrete steps for releasing a change to
> `skills/verdict/`. The pipeline shape these steps plug into is shared
> across every release target; see
> [`README.md`](README.md).

`.claude-plugin/plugin.json`'s `version` is **not** part of any
language's own release procedure and is not meant to match that
language's own manifest version. It measures the *skill* under
`skills/verdict/`, and it exists for one consumer: Claude Code's
marketplace, which compares it against a user's vendored copy to decide
whether that copy is stale. No other harness reads it —
`scripts/install.sh` and `scripts/get.sh` vendor the skill by copying
files, with no version check anywhere.

So the two numbers drift apart in normal operation, and that drift is
correct. A library release with no skill change shouldn't touch
`plugin.json`; a wording tweak to
[`SKILL.md`](../../../skills/verdict/SKILL.md) with no library change
should bump `plugin.json` and nothing else.

**Bump `plugin.json`'s `version` in the same commit as any change to
`skills/verdict/`.** It's the only signal a marketplace user has that
an update exists, so a skill change shipped without a bump is invisible
to them. `.github/workflows/check-skill-version.yml` enforces this: it
fails a PR or a push to `main` that changes shipped skill content
without changing that version. (`skills/verdict-workspace/` is eval
working material, never ships, and is excluded.)

Release procedure for a skill-only change:

1. Bump `.claude-plugin/plugin.json`'s `version` in the same commit as
   the skill change itself — the check above requires this anyway.
2. Add a `## [X.Y.Z] - YYYY-MM-DD` entry to
   [`skills/verdict/CHANGELOG.md`](../../../skills/verdict/CHANGELOG.md).
3. Commit, then tag `skill-vX.Y.Z` and push the tag.
   `release-skill.yml` verifies the tag matches `plugin.json`, runs
   `scripts/build.sh`, and attaches the artifacts to a GitHub Release.

That last step matters for one specific audience.
[`../../../scripts/get.sh`](../../../scripts/get.sh) — the `curl … | sh` install path
— pulls `verdict-tools.zip` from whatever GitHub reports as the
*latest* release, so a skill change that never gets a release of its
own would stay invisible to those users until the next language
release happened to carry it. Marketplace and clone installs read the
repo directly and are already current the moment a change lands on
`main`. Both release workflows attach the **full** set of skill
artifacts for the same reason: whichever kind of release came last,
`latest` always has a usable `verdict-tools.zip`.
