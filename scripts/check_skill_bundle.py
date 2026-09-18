#!/usr/bin/env python3
"""Validate an assembled skill bundle before it is packaged.

Everything under references/ ships in every install — there is no fetch
tier and no manifest declaring what is bundled; the assembled directory
itself is the source of truth. Two things can still go wrong, and neither
is visible by reading one file alone:

1. SKILL.md or an agent-notes.md routes to a references/ path that is not
   actually present in the assembled bundle.
2. A relative link between two bundled documents does not resolve from
   where they actually ended up.

A link to something outside references/ (a real repository path named in
REPOSITORY-MAP.md, for a reader to go fetch themselves) is expected and
not an error — those are never bundled, by design.

Usage: check_skill_bundle.py <assembled-skill-dir>
"""

from __future__ import annotations

import re
import sys
from pathlib import Path

LINK = re.compile(r"\]\(([^)#\s]+)")
SKILL_REF = re.compile(r"references/[A-Za-z0-9_./-]+\.md")


def check(skill_dir: Path) -> list[str]:
    failures: list[str] = []
    references = skill_dir / "references"

    # 1 — every references/ path SKILL.md or an agent-notes.md routes to is present.
    for doc in [skill_dir / "SKILL.md", *sorted(references.rglob("agent-notes.md"))]:
        text = doc.read_text()
        for ref in sorted(set(SKILL_REF.findall(text))):
            if "<language>" in ref:
                continue  # a template, resolved per language at read time
            if not (skill_dir / ref).exists():
                failures.append(f"{doc.relative_to(skill_dir)}: routes to '{ref}', which the bundle does not contain")

    # 2 — links between bundled documents resolve where they actually landed.
    for doc in sorted(references.rglob("*.md")):
        for target in LINK.findall(doc.read_text()):
            if target.startswith(("http", "mailto")):
                continue
            resolved = (doc.parent / target).resolve()
            if references in resolved.parents or resolved == references:
                if not resolved.exists():
                    failures.append(f"{doc.relative_to(references)}: link to '{target}' does not resolve")

    return failures


def main() -> int:
    if len(sys.argv) != 2:
        print(__doc__, file=sys.stderr)
        return 2

    skill_dir = Path(sys.argv[1])
    failures = check(skill_dir)

    for failure in failures:
        print(f"::error::{failure}")

    if failures:
        print(f"\n{len(failures)} problem(s) in the assembled skill bundle.", file=sys.stderr)
        return 1

    print("Skill bundle is internally consistent.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
