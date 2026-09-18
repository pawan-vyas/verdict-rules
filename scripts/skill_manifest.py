#!/usr/bin/env python3
"""Read skills/verdict/MANIFEST.toml. See that file's own header for shapes.

Runnable directly, for build.sh's own bash loop:

    python3 scripts/skill_manifest.py bundled        # source<TAB>destination
    python3 scripts/skill_manifest.py fetch          # same, fetch tier, groups expanded
    python3 scripts/skill_manifest.py fetch-catalog  # language<TAB>source<TAB>destination,
                                                       # resolved per language
"""

from __future__ import annotations

import sys
import tomllib
from pathlib import Path

DEFAULT_PATH = Path("skills/verdict/MANIFEST.toml")


def load(manifest_path: Path = DEFAULT_PATH) -> dict:
    with open(manifest_path, "rb") as f:
        return tomllib.load(f)


def _fetch_groups(manifest: dict) -> list[dict]:
    """`[topics]` shorthand plus explicit `[[fetch_group]]` rows, same {readme, pattern} shape."""
    groups = [
        {"readme": f"docs/{category}/{name}/README.md", "pattern": f"docs/{category}/{name}/{{lang}}.md"}
        for category, names in manifest.get("topics", {}).items()
        for name in names
    ]
    groups.extend(manifest.get("fetch_group", []))
    return groups


def _expand_fetch_groups(manifest: dict, repo_root: Path) -> list[tuple[str, str]]:
    languages: list[str] = manifest.get("languages", [])
    out: list[tuple[str, str]] = []
    for group in _fetch_groups(manifest):
        readme = group["readme"]
        out.append((readme, readme))
        pattern = group["pattern"]
        for lang in languages:
            candidate = pattern.replace("{lang}", lang)
            if (repo_root / candidate).exists():
                out.append((candidate, candidate))
    return out


def _one_off_entries(manifest: dict) -> list[tuple[str, str]]:
    """`[[fetch]]` rows plus the `docs` shorthand (destination == source)."""
    entries = [(e["source"], e["destination"]) for e in manifest.get("fetch", [])]
    entries.extend((d, d) for d in manifest.get("docs", []))
    return entries


def rows(tier: str, manifest_path: Path = DEFAULT_PATH) -> list[tuple[str, str]]:
    """(source, destination) pairs for one tier ("bundled" or "fetch")."""
    manifest = load(manifest_path)
    if tier == "bundled":
        return [(e["source"], e["destination"]) for e in manifest.get("bundled", [])]

    out = _one_off_entries(manifest)
    out.extend((s, f"{lang}/quickstart.md") for lang, s in manifest.get("quickstarts", {}).items())
    # manifest_path is skills/verdict/MANIFEST.toml; source paths are
    # relative to the repo root two levels up.
    repo_root = manifest_path.resolve().parent.parent.parent
    out.extend(_expand_fetch_groups(manifest, repo_root))
    return out


def bundled_sources(manifest_path: Path = DEFAULT_PATH) -> list[str]:
    """Every repo-relative source path the bundled tier declares."""
    return [src for src, _ in rows("bundled", manifest_path)]


def _fetch_language(source: str, languages: list[str]) -> str | None:
    """Which language a fetch source is specific to, if any (by leading path segment)."""
    first_segment = source.split("/", 1)[0]
    return first_segment if first_segment in languages else None


def fetch_catalog(manifest_path: Path = DEFAULT_PATH) -> list[tuple[str, str, str]]:
    """(language, source, destination) rows, resolved per language -- what
    the shipped fetch-docs.sh script reads."""
    manifest = load(manifest_path)
    languages: list[str] = manifest.get("languages", [])
    repo_root = manifest_path.resolve().parent.parent.parent

    catalog: list[tuple[str, str, str]] = []

    for source, destination in _one_off_entries(manifest):
        specific_to = _fetch_language(source, languages)
        for lang in [specific_to] if specific_to else languages:
            catalog.append((lang, source, destination))

    for lang, source in manifest.get("quickstarts", {}).items():
        catalog.append((lang, source, f"{lang}/quickstart.md"))

    for group in _fetch_groups(manifest):
        readme = group["readme"]
        pattern = group["pattern"]
        for lang in languages:
            catalog.append((lang, readme, readme))
            candidate = pattern.replace("{lang}", lang)
            if (repo_root / candidate).exists():
                catalog.append((lang, candidate, candidate))

    return catalog


def main() -> int:
    if len(sys.argv) != 2 or sys.argv[1] not in ("bundled", "fetch", "fetch-catalog"):
        print("usage: skill_manifest.py <bundled|fetch|fetch-catalog>", file=sys.stderr)
        return 2
    if sys.argv[1] == "fetch-catalog":
        for lang, source, destination in fetch_catalog():
            print(f"{lang}\t{source}\t{destination}")
        return 0
    for source, destination in rows(sys.argv[1]):
        print(f"{source}\t{destination}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
