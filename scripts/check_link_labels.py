#!/usr/bin/env python3
"""Catch a link whose label names a path that does not exist.

Three separate checks already run over this repo's links, and a link like
``[`python/README.md`](python/packages/verdict-rules/README.md)`` passes all
three. A link-existence checker resolves the target and is satisfied. An anchor
checker resolves the fragment and is satisfied. A bare-mention sweep skips it
entirely, because it *is* a link. Meanwhile the label names a file that has
never existed, and a reader who trusts the label goes looking for it.

The root README.md carried four of these at once -- one per language -- plus a
label naming a ``maintenance.md`` that was deleted when the maintenance docs were
split, and a quickstart writing ``../../`` labels over four-level targets, stale
from before that package moved under ``packages/``.

This repo's labels are deliberately SHORTENED, which is why a naive version of
this check reports mostly noise: a label names the meaningful suffix of its
target and drops the ``../`` climb and the ``README.md`` leaf, so
``[`testing/`](../../testing/README.md)`` is correct and intended. A label is
only wrong when it is a *complete path claim* that resolves nowhere -- either a
path that never existed, or a relative path with the wrong number of levels.

So a label passes if it is any of:

* not path-like at all (a bare symbol: ``AndRule``, ``RuleResult``)
* a suffix of the target path, modulo a trailing ``/`` or a ``README.md`` leaf
* resolvable as a real path, from the linking file's directory or the repo root

Anything else is reported. Markdown code spans are skipped, because a
double-backtick span showing a reader the *syntax* of a link is not a link.
"""

from __future__ import annotations

import re
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).parent.parent

# A link whose label begins with a backtick-quoted path, with optional trailing
# label text before the closing bracket -- the extended-label form this repo
# uses, e.g. [`AGENTS.md`'s "What this repo is" section](AGENTS.md#what-this-repo-is).
LINK = re.compile(r"\[`([^`]+)`([^\]]*)\]\(([^)]+)\)")

# A DOUBLE-backtick span, which is how this repo shows a reader the *syntax* of
# a link as prose rather than creating one. Only double -- a single-backtick span
# is the label itself on every link this checks, so blanking those out would
# erase every link here and make this script a silent no-op.
CODE_SPAN = re.compile(r"``.+?``")

# A label is a path claim only if it looks like one.
PATH_LIKE = re.compile(r"(/|\.(md|py|ts|js|mjs|dart|cs|csproj|json|ya?ml|toml|sh|txt)$)")

SKIP_PREFIXES = (
    # Vendored skills: not this repo's own content.
    ".agents/skills/",
    ".claude/skills/",
    # Generated.
    "js/packages/verdict-rules/etc/",
)


def tracked_markdown() -> list[str]:
    """Every tracked .md file, minus vendored and generated trees."""
    listing = subprocess.run(
        ["git", "ls-files", "*.md"],
        cwd=ROOT,
        capture_output=True,
        text=True,
        check=True,
    ).stdout.split()
    return [p for p in listing if not p.startswith(SKIP_PREFIXES)]


def _strip_code_spans(line: str) -> str:
    """Blank out code spans so link syntax shown as prose is not read as a link."""
    return CODE_SPAN.sub(lambda match: " " * len(match.group(0)), line)


def _is_suffix_of(label: str, target: str) -> bool:
    """Is `label` the shortened-suffix form of `target`?

    Compares path segments from the right, ignoring the `../` climb the label
    drops by convention and an optional `README.md` leaf the target may add.
    """
    label_parts = [p for p in label.strip("/").split("/") if p not in ("", ".", "..")]
    target_parts = [p for p in target.strip("/").split("/") if p not in ("", ".", "..")]
    if not label_parts:
        return False

    # A label naming a directory matches a target pointing at that directory's
    # own README.md, which GitHub renders when linking the directory itself.
    if target_parts and target_parts[-1] == "README.md" and label_parts[-1] != "README.md":
        target_parts = target_parts[:-1]

    return len(label_parts) <= len(target_parts) and target_parts[-len(label_parts):] == label_parts


def check(rel: str) -> list[str]:
    """Return human-readable failures for one markdown file."""
    failures: list[str] = []
    path = ROOT / rel

    for lineno, raw in enumerate(path.read_text().splitlines(), 1):
        for label, _tail, target in LINK.findall(_strip_code_spans(raw)):
            if not PATH_LIKE.search(label):
                continue
            if target.startswith(("http://", "https://", "#", "mailto:")):
                continue

            # A label naming a PATTERN rather than one file -- a placeholder
            # segment like <language> or <scenario> -- is one of the documented
            # exceptions and can never resolve by definition.
            if "<" in label and ">" in label:
                continue

            label_path = label.split("#")[0].rstrip("/")
            target_path = target.split("#")[0]
            if not label_path or not target_path:
                continue

            if _is_suffix_of(label_path, target_path):
                continue
            if (path.parent / label_path).exists() or (ROOT / label_path).exists():
                continue

            failures.append(
                f"{rel}:{lineno}: label `{label}` names no such path, and is not a "
                f"suffix of the target it points at ({target})"
            )

    return failures


def main() -> int:
    files = tracked_markdown()
    if not files:
        print("No tracked markdown files found.", file=sys.stderr)
        return 1

    failures = [failure for rel in files for failure in check(rel)]

    for failure in failures:
        print(f"::error::{failure}")

    if failures:
        print(
            f"\n{len(failures)} link label(s) name a path that does not exist. "
            "Either correct the label or shorten it to a real suffix of its target.",
            file=sys.stderr,
        )
        return 1

    print(f"Every link label across {len(files)} markdown file(s) names a real path.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
