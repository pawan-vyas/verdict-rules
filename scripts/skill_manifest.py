#!/usr/bin/env python3
"""Read skills/verdict/MANIFEST.toml.

One parser rather than two independently hand-rolled ones. Before this,
build.sh had its own bash loop splitting the manifest's rows, and
check_skill_bundle.py had its own Python-level splitter — two places
that each had to agree with the manifest's row format, and with each
other. Both now import this module (or, for build.sh's shell context,
invoke it as a CLI) instead.

Also runnable directly, for build.sh's own bash loop:

    python3 scripts/skill_manifest.py bundled   # source<TAB>destination, one per line
    python3 scripts/skill_manifest.py fetch     # same, for the fetch tier
"""

from __future__ import annotations

import sys
import tomllib
from pathlib import Path

DEFAULT_PATH = Path("skills/verdict/MANIFEST.toml")


def load(manifest_path: Path = DEFAULT_PATH) -> dict:
    with open(manifest_path, "rb") as f:
        return tomllib.load(f)


def rows(tier: str, manifest_path: Path = DEFAULT_PATH) -> list[tuple[str, str]]:
    """(source, destination) pairs declared for one tier ("bundled" or "fetch")."""
    entries = load(manifest_path).get(tier, [])
    return [(e["source"], e["destination"]) for e in entries]


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
