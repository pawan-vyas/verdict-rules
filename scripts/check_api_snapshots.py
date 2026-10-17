#!/usr/bin/env python3
"""Diff every language's public-API snapshot against the cross-language concept map.

Each language's own real tool (see docs/maintenance/api-snapshots.md) tracks
that language's own surface against drift. This script is the layer above
that: it normalizes all four into one shape and checks they still describe
the *same* package.

Two assertions, both against docs/maintenance/api-concepts.yaml and
docs/maintenance/api-surface-allowlist.yaml:

1. Every concept row resolves in every language whose own cell in that row
   isn't empty. An empty cell means "not tracked for this language" (either
   the language genuinely has no such symbol, or -- see api-concepts.yaml's
   own comments -- a real tool limitation makes the symbol permanently
   invisible to that language's extractor even though it exists), never
   "missing by omission."
2. Every symbol a language's extractor actually reports is accounted for --
   by a concept row's own per-language spelling, by an allowlist entry, or
   by this script's one blanket exemption: a member named identically to
   its own type is a constructor, in every language that reports one at
   all (confirmed empirically: C#'s PublicAPI.txt reports one per type,
   named after the type; Python's griffe, JS's api-extractor, and Dart's
   dart_apitool extractors here do not report constructors as a member at
   all). Constructors exist on every type in every OOP language and carry
   no cross-language naming question the way a real method does, so they
   are exempted here rather than filling the concept map or the allowlist
   with one boilerplate row per type per language.
"""

from __future__ import annotations

import sys
from pathlib import Path

try:
    import yaml
except ImportError:
    print("::error::PyYAML is required: pip install pyyaml (or add it to the dev group)", file=sys.stderr)
    sys.exit(1)

REPO_ROOT = Path(__file__).resolve().parent.parent
CONCEPTS_PATH = REPO_ROOT / "docs/maintenance/api-concepts.yaml"
ALLOWLIST_PATH = REPO_ROOT / "docs/maintenance/api-surface-allowlist.yaml"

LANGUAGES = ("csharp", "python", "js", "dart")

sys.path.insert(0, str(REPO_ROOT / "scripts"))
import api_snapshot_csharp  # noqa: E402
import api_snapshot_dart  # noqa: E402
import api_snapshot_js  # noqa: E402
import api_snapshot_python  # noqa: E402

EXTRACTORS = {
    "csharp": api_snapshot_csharp.extract_symbols,
    "python": api_snapshot_python.extract_symbols,
    "js": api_snapshot_js.extract_symbols,
    "dart": api_snapshot_dart.extract_symbols,
}


def _load_yaml(path: Path):
    with path.open(encoding="utf-8") as f:
        return yaml.safe_load(f)


def _symbol_key(type_name: str, member: str | None) -> tuple[str, str | None]:
    return (type_name, member)


def _parse_symbol_list(raw: str) -> list[tuple[str, str | None]]:
    """"Type" or "Type.Member", comma-separated -- see api-surface-allowlist.yaml's own header."""
    out = []
    for piece in raw.split(","):
        piece = piece.strip()
        if not piece:
            continue
        if "." in piece:
            type_name, member = piece.split(".", 1)
        else:
            type_name, member = piece, None
        out.append(_symbol_key(type_name, member))
    return out


def load_concepts() -> list[dict]:
    data = _load_yaml(CONCEPTS_PATH)
    return data if isinstance(data, list) else []


def load_allowlist() -> dict[str, set[tuple[str, str | None]]]:
    data = _load_yaml(ALLOWLIST_PATH) or []
    allowed: dict[str, set[tuple[str, str | None]]] = {lang: set() for lang in LANGUAGES}
    for entry in data:
        for lang, raw in entry.get("symbol", {}).items():
            if lang not in allowed:
                print(f"::error::{ALLOWLIST_PATH}: unknown language '{lang}' in a symbol entry", file=sys.stderr)
                sys.exit(1)
            allowed[lang].update(_parse_symbol_list(raw))
    return allowed


def extract_all() -> dict[str, set[tuple[str, str | None]]]:
    extracted = {}
    for lang in LANGUAGES:
        rows = EXTRACTORS[lang]()
        extracted[lang] = {_symbol_key(r["type"], r["member"]) for r in rows}
    return extracted


def is_constructor(type_name: str, member: str | None) -> bool:
    return member is not None and member == type_name


def check_concepts_resolve(concepts: list[dict], extracted: dict[str, set]) -> list[str]:
    errors = []
    for row in concepts:
        concept_name = row.get("concept", "<unnamed concept>")
        type_field = row.get("type", {})
        type_map = type_field if isinstance(type_field, dict) else {lang: type_field for lang in LANGUAGES}
        member_field = row.get("member", "")
        member_map = (
            member_field if isinstance(member_field, dict) else {lang: member_field for lang in LANGUAGES}
        )

        for lang in LANGUAGES:
            type_name = (type_map.get(lang) or "").strip()
            if not type_name:
                continue  # not tracked for this language -- see api-concepts.yaml's own header comment

            member_name = (member_map.get(lang) or "").strip() or None
            key = _symbol_key(type_name, member_name)
            if key not in extracted[lang]:
                where = type_name if member_name is None else f"{type_name}.{member_name}"
                errors.append(
                    f"::error::concept '{concept_name}' expects {lang} to export '{where}', "
                    f"but it is not in that language's committed snapshot."
                )
    return errors


def check_every_symbol_accounted_for(
    concepts: list[dict], allowlist: dict[str, set], extracted: dict[str, set]
) -> list[str]:
    errors = []

    claimed: dict[str, set[tuple[str, str | None]]] = {lang: set() for lang in LANGUAGES}
    for row in concepts:
        type_field = row.get("type", {})
        type_map = type_field if isinstance(type_field, dict) else {lang: type_field for lang in LANGUAGES}
        member_field = row.get("member", "")
        member_map = (
            member_field if isinstance(member_field, dict) else {lang: member_field for lang in LANGUAGES}
        )
        for lang in LANGUAGES:
            type_name = (type_map.get(lang) or "").strip()
            if not type_name:
                continue
            member_name = (member_map.get(lang) or "").strip() or None
            claimed[lang].add(_symbol_key(type_name, member_name))

    for lang in LANGUAGES:
        for type_name, member_name in sorted(extracted[lang], key=lambda k: (k[0], k[1] or "")):
            key = _symbol_key(type_name, member_name)
            if key in claimed[lang]:
                continue
            if key in allowlist[lang]:
                continue
            if is_constructor(type_name, member_name):
                continue
            where = type_name if member_name is None else f"{type_name}.{member_name}"
            errors.append(
                f"::error::{lang} exports '{where}', which is not in any api-concepts.yaml row "
                f"or api-surface-allowlist.yaml entry for {lang}. Add it to one of the two."
            )
    return errors


def main() -> int:
    concepts = load_concepts()
    allowlist = load_allowlist()
    extracted = extract_all()

    errors = check_concepts_resolve(concepts, extracted) + check_every_symbol_accounted_for(
        concepts, allowlist, extracted
    )

    if errors:
        for e in errors:
            print(e)
        print(f"\n{len(errors)} problem(s).", file=sys.stderr)
        return 1

    total = sum(len(v) for v in extracted.values())
    print(
        f"Every concept resolves, and every one of {total} exported symbol(s) "
        f"across {len(LANGUAGES)} languages is accounted for."
    )
    return 0


if __name__ == "__main__":
    sys.exit(main())
