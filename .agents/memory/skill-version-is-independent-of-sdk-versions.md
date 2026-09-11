<!-- Title: Skill Version Is Independent Of SDK Versions -->
# `plugin.json`'s version is Claude Code's skill-update signal, nothing more

> `.claude-plugin/plugin.json` is a **Claude Code marketplace**
> artifact. Its `version` exists so Claude Code can tell whether a
> user's *vendored skill copy* is out of date. It says nothing about
> the library, no other harness reads it, and it is **not** meant to
> agree with any language's manifest version.

## What each version means

- **`python/pyproject.toml`'s `version`** — the Python library's public
  API. Published to PyPI, asserted against the `python-vX.Y.Z` tag by
  `release-python.yml`. Each future language gets its own, on its own
  cadence.
- **`.claude-plugin/plugin.json`'s `version`** — the **skill's**
  content, for Claude Code's marketplace only. Bumped when
  `skills/verdict/` changes: wording tweaks so the skill advertises the
  library better to an AI agent, new recipes, new usage guidance, or
  per-language reference content as an SDK ships. A library release
  with no skill change is not a reason to touch it.

The two drift apart in normal operation and that drift is **correct**.
`plugin.json` at `0.1.0` while Python is at `0.4.0` simply means the
skill hasn't needed a revision.

## Don't re-derive this as a bug

It looks like an unenforced invariant from inside the release workflow:
`release-python.yml` verifies tag ↔ `pyproject.toml` and never reads
`plugin.json`. That is not a missing check — there is no invariant.
A strict-equality assertion would force meaningless no-op bumps and
make the skill version lie about whether the skill changed.

## Vendoring does not depend on it

`scripts/install.sh` and `scripts/get.sh` vendor the skill by copying
files; neither reads or compares a version. Every non-Claude-Code
harness gets the skill that way, entirely version-blind. So bumping
`plugin.json` changes nothing for them — it is purely Claude Code's
update signal.

`.claude-plugin/marketplace.json` points at `source: "."`, so a Claude
Code marketplace user tracks the **repo**: bump `plugin.json` and push
to `main`, and the update is available. No GitHub Release is involved
in that path.

`scripts/get.sh` (the `curl … | sh` path) is the exception: it resolves
GitHub's *latest release* and pulls `verdict-tools.zip` from it, so it
needs an actual release to exist. That's what `release-skill.yml` is
for — a `skill-vX.Y.Z` tag, verified against `plugin.json`, cutting a
release of its own. Both release workflows attach the **full** set of
skill artifacts, so `latest` always carries a usable
`verdict-tools.zip` regardless of which kind of release came last.

## How a skill change ships

Bump `plugin.json` in the same commit as the skill edit, add a
`skill-vX.Y.Z` CHANGELOG entry, tag, push. Full procedure in
`docs/maintenance.md`. Marketplace and clone installs are current as
soon as it lands on `main`; the tag exists for `get.sh`.

## What enforces it

Because `plugin.json`'s version is the *only* marketplace update signal,
forgetting to bump it on a skill edit is silent — the change ships and
marketplace users never see it. `.github/workflows/check-skill-version.yml`
(added 2026-09-11) closes that: on a PR or push to `main` it re-derives
what actually changed in the base..HEAD range and fails if shipped skill
content moved without the version moving. It deliberately does **not**
compare `plugin.json` to any language manifest.

Confirmed with the maintainer, 2026-09-11.

Related: [`releases-are-language-scoped`](releases-are-language-scoped.md).
