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

# Packages are discovered, not listed. A directory containing both a recognised
# manifest and a CHANGELOG.md is a package — so adding a language, or a second
# distribution within one, needs no edit here. A table would have been one more
# shared file every language branch had to touch.
#
# Each entry says how to read a version out of that ecosystem's manifest.
MANIFESTS = {
    "pyproject.toml": lambda t: _search(r'^version\s*=\s*"(.+?)"', t),
    "package.json": lambda t: json.loads(t).get("version"),
    "pubspec.yaml": lambda t: _search(r"^version:\s*(\S+)", t),
    ".csproj": lambda t: _search(r"<Version>(.+?)</Version>", t),
    "plugin.json": lambda t: json.loads(t).get("version"),
}

# Directories that look like packages but are not published from here.
SKIP = ("node_modules", "/dist/", "/bin/", "/obj/", ".agents/", "/build/")


def _search(pattern: str, text: str) -> str | None:
    match = re.search(pattern, text, re.M)
    return match.group(1) if match else None


# The one package whose manifest is not beside its changelog, because its
# location is fixed by an external convention rather than by us: Claude Code
# requires the plugin manifest at .claude-plugin/. Declared rather than
# discovered, and deliberately the only entry — languages are all discovered.
DECLARED = [
    ("skills/verdict", Path(".claude-plugin/plugin.json"), Path("skills/verdict/CHANGELOG.md")),
]


def discover() -> list[tuple[str, Path, Path, str]]:
    """Every (label, manifest, changelog, version) in the repository."""
    found = []
    for label, manifest, changelog in DECLARED:
        if manifest.exists() and changelog.exists():
            found.append((label, manifest, changelog, json.loads(manifest.read_text())["version"]))
    for changelog in sorted(Path(".").rglob("CHANGELOG.md")):
        text = str(changelog)
        if any(skip in f"/{text}" for skip in SKIP):
            continue
        if any(changelog == declared for _, _, declared in DECLARED):
            continue

        directory = changelog.parent
        for name, read in MANIFESTS.items():
            manifests = (
                sorted(directory.glob(f"*{name}")) if name.startswith(".")
                else ([directory / name] if (directory / name).exists() else [])
            )
            if not manifests:
                continue
            manifest = manifests[0]
            version = read(manifest.read_text())
            if version:
                found.append((str(directory), manifest, changelog, version))
            break
    return found


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

    packages = discover()
    if not packages:
        print("::error::No packages found — every package needs a CHANGELOG.md beside its manifest.")
        return 1

    for label, manifest, changelog, version in packages:
        # 1 — the declared version has a section.
        result = subprocess.run(
            [sys.executable, "scripts/changelog_section.py", str(changelog), version],
            capture_output=True,
            text=True,
        )
        if result.returncode != 0:
            failures.append(
                f"{label} is at {version} but {changelog} has no section for it. "
                f"Merging would publish and then fail the release."
            )
        else:
            print(f"OK {label} {version} -> {changelog} ({len(result.stdout)} bytes)")

        # 2 — versions descend.
        found = headings(changelog)
        for earlier, later in zip(found, found[1:]):
            if as_tuple(earlier) <= as_tuple(later):
                failures.append(
                    f"{changelog}: {later} appears below {earlier}, but a changelog "
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
