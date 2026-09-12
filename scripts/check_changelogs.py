#!/usr/bin/env python3
"""Check every package's changelog against its manifest, and its own ordering.

Each package keeps its changelog beside its own manifest — that is what the
packaging tools bundle, and it means each track has exactly one writer, so two
releases can never contend for the same file. The table below is the only place
that mapping lives; adding a package is a row.

Two assertions per package:

1. The version its manifest declares has a matching changelog section. Without
   this a merge publishes and *then* fails its release, leaving a live version
   with no tag — and no registry here lets a version be taken back.
2. Versions descend. A changelog is read newest-first, and sections landing
   wherever an insertion happened to anchor is how the previous single file
   ended up with 0.3.0 above 0.2.1 above 0.2.0.
"""

from __future__ import annotations

import json
import re
import subprocess
import sys
from pathlib import Path

# label | manifest | changelog | how to read the version from the manifest
PACKAGES = [
    (
        "Python",
        "python/pyproject.toml",
        "python/CHANGELOG.md",
        lambda p: re.search(r'^version = "(.+?)"', p.read_text(), re.M).group(1),
    ),
    (
        "Skill",
        ".claude-plugin/plugin.json",
        "skills/verdict/CHANGELOG.md",
        lambda p: json.loads(p.read_text())["version"],
    ),
]

VERSION_HEADING = re.compile(r"^#{1,3}\s+.*?(?<![\w.])v?(\d+\.\d+\.\d+)(?![\w.])")


def headings(changelog: Path) -> list[str]:
    return [
        m.group(1)
        for m in (VERSION_HEADING.match(line) for line in changelog.read_text().splitlines())
        if m
    ]


def as_tuple(version: str) -> tuple[int, ...]:
    return tuple(int(part) for part in version.split("."))


def main() -> int:
    failures: list[str] = []

    for label, manifest_path, changelog_path, read_version in PACKAGES:
        manifest, changelog = Path(manifest_path), Path(changelog_path)

        if not changelog.exists():
            failures.append(f"{label}: {changelog_path} does not exist")
            continue

        version = read_version(manifest)

        # 1 — the declared version has a section.
        result = subprocess.run(
            [sys.executable, "scripts/changelog_section.py", changelog_path, version],
            capture_output=True,
            text=True,
        )
        if result.returncode != 0:
            failures.append(
                f"{label} is at {version} but {changelog_path} has no section for it. "
                f"Merging would publish and then fail the release."
            )
        else:
            print(f"OK {label} {version} -> {changelog_path} ({len(result.stdout)} bytes)")

        # 2 — versions descend.
        found = headings(changelog)
        for earlier, later in zip(found, found[1:]):
            if as_tuple(earlier) <= as_tuple(later):
                failures.append(
                    f"{changelog_path}: {later} appears below {earlier}, but a changelog "
                    f"reads newest-first"
                )
                break

    for failure in failures:
        print(f"::error::{failure}")

    if failures:
        print(f"\n{len(failures)} changelog problem(s).", file=sys.stderr)
        return 1

    print("Every package's changelog matches its manifest and is ordered newest-first.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
