#!/usr/bin/env python3
"""Extract one version's section from a changelog, whatever its heading format.

Ecosystems disagree about changelog headings, and each is right for itself:

  pub.dev  parses the file and documents `## 1.2.3`, optionally `v`-prefixed
  PyPI     parses nothing; Keep a Changelog (`## [1.2.3] - 2026-01-01`) is the
           community norm and is what `project.urls` links to
  npm      parses nothing; Keep a Changelog by convention
  NuGet    has no changelog file at all — release notes are package metadata

So this matches a heading that *contains* the version rather than one that
equals a fixed shape. That is deliberately tolerant: it accepts every format
above, and it means a language adopting its own idiom needs no change here.

Usage: changelog_section.py <changelog-path> <version>
Prints the section body, or exits 1 with an explanation.
"""

from __future__ import annotations

import re
import sys
from pathlib import Path


def extract(text: str, version: str) -> str | None:
    """The body under the first heading whose text contains ``version``."""
    # `v?` because pub.dev documents an optional v-prefix; the boundaries stop
    # 0.1.1 matching inside 10.1.11.
    wanted = re.compile(
        r"^(#{1,3})\s+.*(?<![\w.])v?" + re.escape(version) + r"(?![\w.])"
    )
    lines = text.splitlines()

    start = None
    level = None
    for i, line in enumerate(lines):
        match = wanted.match(line)
        if match:
            start, level = i + 1, len(match.group(1))
            break

    if start is None:
        return None

    # Stop at the next heading of the same or shallower depth, so subsections
    # under this version are kept.
    end = len(lines)
    boundary = re.compile(r"^#{1," + str(level) + r"}\s")
    for i in range(start, len(lines)):
        if boundary.match(lines[i]):
            end = i
            break

    return "\n".join(lines[start:end]).strip()


def main() -> int:
    if len(sys.argv) != 3:
        print(__doc__, file=sys.stderr)
        return 2

    path, version = Path(sys.argv[1]), sys.argv[2]
    if not path.exists():
        print(f"::error::{path} does not exist", file=sys.stderr)
        return 1

    section = extract(path.read_text(), version)
    if not section:
        print(
            f"::error file={path}::No section for version {version}. Add the "
            f"changelog entry in the same commit as the version bump — a "
            f"release cannot publish without it.",
            file=sys.stderr,
        )
        return 1

    print(section)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
