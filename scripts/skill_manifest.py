#!/usr/bin/env python3
"""Read skills/verdict/MANIFEST.toml.

One parser rather than two independently hand-rolled ones. Before this,
build.sh had its own bash loop splitting the manifest's rows, and
check_skill_bundle.py had its own Python-level splitter — two places
that each had to agree with the manifest's row format, and with each
other. Both now import this module (or, for build.sh's shell context,
invoke it as a CLI) instead.

The fetch tier has two shapes. Most entries are one-off files with no
per-language sibling (`[[fetch]]`, unchanged). The repeated shape --
a topic's README plus that same topic's own file per language, same
directory, same filename stem as the language -- is `[[fetch_group]]`
instead: one `pattern` with a `{lang}` placeholder, expanded against
the manifest's own top-level `languages` list. A language with no file
for a given topic yet is skipped rather than erroring, so a topic
doesn't have to land in every language at once. This is what keeps
shipping a new language from touching every existing topic's rows —
see MANIFEST.toml's own header comment and
`.agents/memory/adding-a-variant-is-a-new-file.md` for why that matters
here specifically: every language ships from its own branch, so a
shared file each one has to write N new rows into is a rebase conflict
waiting to happen, not just clutter.

Also runnable directly, for build.sh's own bash loop:

    python3 scripts/skill_manifest.py bundled   # source<TAB>destination, one per line
    python3 scripts/skill_manifest.py fetch     # same, for the fetch tier (groups expanded)
"""

from __future__ import annotations

import sys
import tomllib
from pathlib import Path

DEFAULT_PATH = Path("skills/verdict/MANIFEST.toml")


def load(manifest_path: Path = DEFAULT_PATH) -> dict:
    with open(manifest_path, "rb") as f:
        return tomllib.load(f)


def _expand_fetch_groups(
    manifest: dict, repo_root: Path
) -> list[tuple[str, str]]:
    """One (readme, readme) row plus one row per language that has this topic's file.

    `repo_root` is where `{lang}`-expanded paths are checked for existence --
    the manifest's own source paths are already repo-relative, so this is the
    directory the manifest itself resolves from (its parent's parent, since it
    lives at skills/verdict/MANIFEST.toml).
    """
    languages: list[str] = manifest.get("languages", [])
    out: list[tuple[str, str]] = []
    for group in manifest.get("fetch_group", []):
        readme = group["readme"]
        out.append((readme, readme))
        pattern = group["pattern"]
        for lang in languages:
            candidate = pattern.replace("{lang}", lang)
            if (repo_root / candidate).exists():
                out.append((candidate, candidate))
    return out


def rows(tier: str, manifest_path: Path = DEFAULT_PATH) -> list[tuple[str, str]]:
    """(source, destination) pairs declared for one tier ("bundled" or "fetch").

    For "fetch", this is `[[fetch]]`'s literal entries plus every
    `[[fetch_group]]` expanded against `languages` (see
    `_expand_fetch_groups`) — callers never see the two shapes as
    different; both tiers still come back as one flat list of pairs.
    """
    manifest = load(manifest_path)
    entries = manifest.get(tier, [])
    out = [(e["source"], e["destination"]) for e in entries]
    if tier == "fetch":
        # manifest_path is skills/verdict/MANIFEST.toml; its own source
        # paths (docs/..., python/..., js/...) are relative to the repo
        # root two levels up.
        repo_root = manifest_path.resolve().parent.parent.parent
        out.extend(_expand_fetch_groups(manifest, repo_root))
    return out


def bundled_sources(manifest_path: Path = DEFAULT_PATH) -> list[str]:
    """Every repo-relative source path the bundled tier declares.

    Used by check-skill-version.yml: these paths ship in every skill
    install regardless of which repo directory they physically live in
    (docs/architecture/ sits outside skills/verdict/ entirely), so a
    workflow watching only skills/verdict/ for "did shipped content
    change" would miss an edit to it.
    """
    return [src for src, _ in rows("bundled", manifest_path)]


def main() -> int:
    if len(sys.argv) != 2 or sys.argv[1] not in ("bundled", "fetch"):
        print("usage: skill_manifest.py <bundled|fetch>", file=sys.stderr)
        return 2
    for source, destination in rows(sys.argv[1]):
        print(f"{source}\t{destination}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
