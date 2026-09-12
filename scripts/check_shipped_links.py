#!/usr/bin/env python3
"""Every GitHub link in shipped content must be pinned to that package's version.

A README bundled into a package is rendered on *every* version's registry page,
forever. A link in it pinned to `main` therefore shows whoever is reading an old
version the *current* documentation — describing APIs that version does not
have. That is the same failure the skill's fetch tier is designed to avoid, and
it is worse here because the reader has no way to know.

Tags are immutable, so a version-pinned link resolves forever and always
describes what the reader actually installed. The cost is that these URLs must
move with each release, which is what this check exists to enforce — and what
`--fix` does mechanically.

Usage:
  check_shipped_links.py          report anything not pinned to the right version
  check_shipped_links.py --fix    rewrite them, then report what changed
"""

from __future__ import annotations

import json
import re
import sys
from pathlib import Path

REPO = "pawan-vyas/verdict-rules"
LINK = re.compile(
    r"https://github\.com/" + re.escape(REPO) + r"/blob/(?P<ref>[^/]+)/(?P<path>[^)\"\s]+)"
)

# Content that is bundled into a published artifact, and so is read long after
# the commit that wrote it. label | files | tag prefix | how to read the version
SHIPPED = [
    (
        "Python",
        ["python/README.md", "python/pyproject.toml"],
        "python-v",
        lambda: re.search(
            r'^version = "(.+?)"', Path("python/pyproject.toml").read_text(), re.M
        ).group(1),
    ),
]


def main() -> int:
    fix = "--fix" in sys.argv[1:]
    problems: list[str] = []
    fixed = 0

    for label, files, prefix, read_version in SHIPPED:
        expected = f"{prefix}{read_version()}"

        for name in files:
            path = Path(name)
            if not path.exists():
                continue
            text = path.read_text()
            original = text

            for match in LINK.finditer(original):
                if match.group("ref") == expected:
                    continue
                if fix:
                    continue  # handled by the substitution below
                problems.append(
                    f"{name}: link pinned to '{match.group('ref')}', expected "
                    f"'{expected}' — {match.group('path')}"
                )

            if fix:
                text = LINK.sub(
                    lambda m: (
                        m.group(0)
                        if m.group("ref") == expected
                        else f"https://github.com/{REPO}/blob/{expected}/{m.group('path')}"
                    ),
                    text,
                )
                if text != original:
                    path.write_text(text)
                    changed = len(LINK.findall(original)) - sum(
                        1 for m in LINK.finditer(original) if m.group("ref") == expected
                    )
                    fixed += changed
                    print(f"fixed {changed} link(s) in {name} -> {expected}")

    if fix:
        print(f"\n{fixed} link(s) repinned." if fixed else "\nNothing to repin.")
        return 0

    for problem in problems:
        print(f"::error::{problem}")

    if problems:
        print(
            f"\n{len(problems)} shipped link(s) not pinned to the released version. "
            f"Run: python3 scripts/check_shipped_links.py --fix",
            file=sys.stderr,
        )
        return 1

    print("Every shipped link is pinned to its package's released version.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
