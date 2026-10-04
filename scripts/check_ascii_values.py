#!/usr/bin/env python3
"""Keep non-ASCII typographic punctuation out of machine-consumed values.

Em-dashes are this repo's house prose style, in docs, docstrings and code
comments alike, and nothing here touches those. What this rejects is one of them
sitting in a value another tool parses or displays: a JSON field, a YAML value a
workflow reads, a manifest's ``description``. Python's ``json.dump`` with its
default ``ensure_ascii=True`` escapes an em-dash to ``\\u2014``, which every
conformant parser decodes identically -- so this is not a correctness bug. It is
a legibility one: the escape is opaque to someone reading the raw file, and a
plain ASCII ``--`` sidesteps both that and any question of whether four
languages' JSON libraries round-trip Unicode identically.

A manifest's ``description`` is the case that cannot be taken back. It renders
straight onto a registry page, and a registry does not retroactively edit the
page of an already-published version.

Comments are exempt, because a comment is prose. That exemption is also why a
``.json`` file is checked line-for-line with no exemption at all: JSON has no
comment syntax, so *every* character in one is part of a value by definition. A
sweep found all four published descriptions clean and the unpublished JS
workspace root carrying an em-dash in its own ``description``.

Shell ``run:`` blocks inside a workflow are exempt too: an ``echo`` there is text
written for a person reading a CI log, not a value anything dispatches on.
"""

from __future__ import annotations

import re
import subprocess
import sys
import unicodedata
from pathlib import Path

ROOT = Path(__file__).parent.parent
WORKFLOWS = ".github/workflows/"

# Dashes, curly quotes and the ellipsis character -- the typographic punctuation
# that shows up in prose and has an unambiguous ASCII spelling.
BANNED = re.compile(r"[‐-―‘-‟…′″]")

ASCII_SPELLING = {
    "—": "--",
    "–": "-",
    "…": "...",
    "‘": "'",
    "’": "'",
    "“": '"',
    "”": '"',
}

# `#` introduces a comment, including a trailing one after a value.
HASH_COMMENT = (".yaml", ".yml", ".toml")

# A `.json` file has no comment syntax, so every character in one is part of a
# value -- with one standard exception: a tsconfig/jsconfig is JSONC, and
# TypeScript's own parser accepts `//` there. Matched by name, not extension.
JSONC_STEMS = ("tsconfig", "jsconfig")

SKIP_PREFIXES = (
    ".agents/skills/",
    ".claude/skills/",
    # Generated: its content is whatever the extractor emitted.
    "python/packages/verdict-rules/api-snapshot.json",
    "js/packages/verdict-rules/etc/",
)

XML_COMMENT = re.compile(r"<!--.*?-->", re.DOTALL)


def _strip_hash_comment(line: str) -> str:
    """Drop a `#` comment, including a trailing one, respecting quoted values.

    A `#` inside a quoted string is data, not a comment marker. YAML also
    requires whitespace before an inline `#`, which keeps a value like
    `color: #fff` from being read as an empty value plus a comment.
    """
    in_single = in_double = False
    for index, char in enumerate(line):
        if char == "'" and not in_double:
            in_single = not in_single
        elif char == '"' and not in_single:
            in_double = not in_double
        elif char == "#" and not in_single and not in_double:
            if index == 0 or line[index - 1] in " \t":
                return line[:index]
    return line


def _strip_slash_comment(line: str) -> str:
    """Drop a `//` comment from a JSONC line, respecting quoted values."""
    in_string = False
    index = 0
    while index < len(line):
        char = line[index]
        if char == '"' and (index == 0 or line[index - 1] != "\\"):
            in_string = not in_string
        elif not in_string and line.startswith("//", index):
            return line[:index]
        index += 1
    return line


def tracked() -> list[str]:
    """Every tracked data/config file, minus vendored and generated ones."""
    patterns = [
        "*.json",
        "*.yaml",
        "*.yml",
        "*.toml",
        "*.csproj",
        "*.props",
        # A harness drop-in is prose with a parsed frontmatter block on top.
        # Only the frontmatter is checked -- see _frontmatter_only below.
        "*.md",
        "*.mdc",
    ]
    listing = subprocess.run(
        ["git", "ls-files", *patterns],
        cwd=ROOT,
        capture_output=True,
        text=True,
        check=True,
    ).stdout.split()
    return [p for p in listing if not p.startswith(SKIP_PREFIXES)]


def _describe(char: str) -> str:
    """Name the offending character the way a reader would recognise it."""
    name = unicodedata.name(char, "unknown character")
    suggestion = ASCII_SPELLING.get(char)
    ordinal = f"U+{ord(char):04X}"
    if suggestion:
        return f"{name} ({ordinal}) -- write {suggestion!r} instead"
    return f"{name} ({ordinal})"


def _shell_block_lines(path: Path) -> set[int]:
    """1-indexed line numbers belonging to a workflow's shell `run:` blocks.

    An `echo` in a `run:` block is a message for a person reading a CI log, not a
    value any tool dispatches on, so it keeps the house prose style.
    """
    exempt: set[int] = set()
    lines = path.read_text().splitlines()
    inside = False
    indent = 0
    for index, raw in enumerate(lines, 1):
        stripped = raw.strip()
        if re.match(r"^-?\s*run:\s*[|>]", stripped) or stripped == "run: |":
            inside = True
            indent = len(raw) - len(raw.lstrip())
            continue
        if inside:
            if stripped and (len(raw) - len(raw.lstrip())) <= indent:
                inside = False
            else:
                exempt.add(index)
        if re.match(r"^-?\s*run:\s*\S", stripped):
            exempt.add(index)
    return exempt


def _frontmatter_only(text: str) -> tuple[str, int]:
    """The YAML frontmatter block of a markdown file, and the line it starts on.

    A markdown body is prose and keeps the em-dash. Its frontmatter is not: a
    harness reads `description:`, `globs:`, `inclusion:` and friends as config,
    and the drop-ins under scripts/harness-templates/ are both at once -- a
    parsed block on top of a prose pointer. Returns an empty block for a file
    with no frontmatter, which is most of them.
    """
    lines = text.splitlines()
    if not lines or lines[0].strip() != "---":
        return "", 0
    for index, line in enumerate(lines[1:], start=1):
        if line.strip() in ("---", "..."):
            return "\n".join(lines[1:index]), 2
    return "", 0


def check(rel: str) -> list[str]:
    """Return human-readable failures for one data/config file."""
    path = ROOT / rel
    suffix = path.suffix
    text = path.read_text()

    first_lineno = 1
    if suffix in (".md", ".mdc"):
        text, first_lineno = _frontmatter_only(text)
        if not text:
            return []
        # Frontmatter is YAML, so `#` comments apply to it.
        suffix = ".yaml"

    if suffix in (".csproj", ".props"):
        # Blank XML comments out, preserving line structure.
        text = XML_COMMENT.sub(lambda m: re.sub(r"[^\n]", " ", m.group(0)), text)

    exempt_lines: set[int] = set()
    if rel.startswith(WORKFLOWS):
        exempt_lines = _shell_block_lines(path)

    is_jsonc = suffix == ".json" and path.stem.split(".")[0] in JSONC_STEMS
    failures: list[str] = []

    for lineno, raw in enumerate(text.splitlines(), first_lineno):
        if lineno in exempt_lines:
            continue

        # A comment is prose and keeps the house style.
        if suffix in HASH_COMMENT:
            line = _strip_hash_comment(raw)
        elif is_jsonc:
            line = _strip_slash_comment(raw)
        else:
            line = raw

        for match in BANNED.finditer(line):
            failures.append(
                f"{rel}:{lineno}: {_describe(match.group(0))}, in a value "
                f"consumed by tooling rather than read as prose"
            )

    return failures


def main() -> int:
    files = tracked()
    if not files:
        print("No tracked data or config files found.", file=sys.stderr)
        return 1

    failures = [failure for rel in files for failure in check(rel)]

    for failure in failures:
        print(f"::error::{failure}")

    if failures:
        print(
            f"\n{len(failures)} non-ASCII character(s) in machine-consumed values. "
            "Prose keeps the em-dash; a parsed value does not.",
            file=sys.stderr,
        )
        return 1

    print(f"Every value across {len(files)} data/config file(s) is plain ASCII.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
