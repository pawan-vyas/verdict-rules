#!/usr/bin/env python3
"""Validate an assembled skill bundle before it is packaged.

Two things can go wrong when the skill is assembled from a manifest, and
neither is visible by reading either file alone:

1. ``SKILL.md`` routes to a document the manifest does not bundle, so an agent
   follows a pointer to a file that is not there.
2. The manifest places two bundled documents such that a relative link between
   them no longer resolves.

Links pointing at documents that are *not* bundled are expected and not an
error: those are the fetch tier, and ``SKILL.md`` explains that an unresolved
relative link resolves against the source repository at the pinned version.
Checking them here would flag the design as a defect.

Usage: check_skill_bundle.py <assembled-skill-dir> <manifest>
"""

from __future__ import annotations

import re
import sys
from pathlib import Path

LINK = re.compile(r"\]\(([^)#\s]+)")
SKILL_REF = re.compile(r"references/[A-Za-z0-9_./-]+\.md")


def bundled_destinations(manifest: Path) -> set[str]:
    """Destination paths, relative to references/, that the bundle contains."""
    destinations: set[str] = set()
    for line in manifest.read_text().splitlines():
        if not line.strip() or line.lstrip().startswith("#"):
            continue
        parts = line.split("\t")
        if len(parts) != 3 or parts[0].strip() != "bundled":
            continue
        destinations.add(parts[2].strip())
    return destinations


def check(skill_dir: Path, manifest: Path) -> list[str]:
    failures: list[str] = []
    references = skill_dir / "references"
    bundled = {(references / d).resolve() for d in bundled_destinations(manifest)}

    # 1 — every path SKILL.md routes to is present.
    skill_md = (skill_dir / "SKILL.md").read_text()
    for ref in sorted(set(SKILL_REF.findall(skill_md))):
        if "<language>" in ref:
            continue  # a template, resolved per language at read time
        if not (skill_dir / ref).exists():
            failures.append(f"SKILL.md routes to '{ref}', which the bundle does not contain")

    # 2 — links between bundled documents resolve where the manifest put them.
    for doc in sorted(references.rglob("*.md")):
        for target in LINK.findall(doc.read_text()):
            if target.startswith(("http", "mailto")):
                continue
            resolved = (doc.parent / target).resolve()
            if resolved in bundled and not resolved.exists():
                failures.append(
                    f"{doc.relative_to(references)}: link to '{target}' is a bundled "
                    f"document but does not resolve — check its destination in the manifest"
                )

    return failures


def main() -> int:
    if len(sys.argv) != 3:
        print(__doc__, file=sys.stderr)
        return 2

    skill_dir, manifest = Path(sys.argv[1]), Path(sys.argv[2])
    failures = check(skill_dir, manifest)

    for failure in failures:
        print(f"::error::{failure}")

    if failures:
        print(f"\n{len(failures)} problem(s) in the assembled skill bundle.", file=sys.stderr)
        return 1

    print("Skill bundle is internally consistent.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
