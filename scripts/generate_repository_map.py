#!/usr/bin/env python3
"""Generate references/REPOSITORY-MAP.md from the repository's own docs.

Each entry's one-line description is pulled directly from the source
document (an extending scenario's own summary blockquote; a sample's own
"The question" line) rather than hand-copied, so the map cannot drift from
what the documents actually say.

Usage: generate_repository_map.py <output-path>
"""

from __future__ import annotations

import re
import sys
from pathlib import Path

ROOT = Path(__file__).parent.parent
REPO = "pawan-vyas/verdict-rules"

LINK = re.compile(r"\[([^\]]+)\]\([^)]+\)")


def _paragraph_after(lines: list[str], start: int, strip_prefix: str) -> str:
    """Join a wrapped paragraph starting at `start` into one line."""
    collected = []
    for line in lines[start:]:
        if not line.strip():
            break
        collected.append(line.removeprefix(strip_prefix).strip())
    return " ".join(collected).removeprefix(strip_prefix).strip()


def _one_liner(paragraph: str) -> str:
    """The paragraph's own first sentence, with any embedded markdown link
    reduced to its label -- a relative link copied out of its original
    document would not resolve from the generated map's own location."""
    paragraph = LINK.sub(r"\1", paragraph)
    period = paragraph.find(". ")
    return paragraph if period == -1 else paragraph[: period + 1]


def _extending_entry(readme: Path) -> str:
    lines = readme.read_text().splitlines()
    quote_start = next(i for i, l in enumerate(lines) if l.startswith("> "))
    return _one_liner(_paragraph_after(lines, quote_start, "> "))


def _sample_entry(readme: Path) -> str:
    lines = readme.read_text().splitlines()
    q_start = next(i for i, l in enumerate(lines) if l.startswith("**The question**"))
    return _one_liner(_paragraph_after(lines, q_start, "**The question**: "))


def main() -> int:
    if len(sys.argv) != 2:
        print(__doc__, file=sys.stderr)
        return 2
    out = Path(sys.argv[1])

    extending = sorted(d for d in (ROOT / "docs/extending").iterdir() if d.is_dir())
    samples = sorted(d for d in (ROOT / "docs/samples").iterdir() if d.is_dir())

    lines = [
        "# Where things live in the source repository",
        "",
        "Nothing below is vendored with this skill — it exists so an agent can",
        "decide whether something is worth reading, then get it directly from the",
        "repository, at the tag matching what a project actually has installed",
        f"(`<language>-v<version>`), never the default branch: https://github.com/{REPO}",
        "",
        "## Design",
        "",
        "- `docs/architecture/` — why the engine is shaped this way: the type",
        "  structure, the execution model, and which run mode to reach for.",
        "",
        "## Extending (docs/extending/)",
        "",
    ]
    for d in extending:
        lines.append(f"- `{d.name}` — {_extending_entry(d / 'README.md')}")

    lines += ["", "## Worked samples (docs/samples/)", ""]
    for d in samples:
        lines.append(f"- `{d.name}` — {_sample_entry(d / 'README.md')}")

    lines += [
        "",
        "## Testing",
        "",
        "- `docs/testing/` — the checklist for testing rule-based logic well.",
        "",
    ]

    out.parent.mkdir(parents=True, exist_ok=True)
    out.write_text("\n".join(lines))
    print(f"wrote {out} ({len(extending)} extending, {len(samples)} sample entries)")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
