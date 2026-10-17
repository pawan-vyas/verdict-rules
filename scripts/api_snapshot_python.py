#!/usr/bin/env python3
"""Normalize Python's committed griffe snapshot into flat (type, member) rows.

``python/packages/verdict-rules/api-snapshot.json`` is griffe's own nested
JSON dump (``griffe dump``) of the ``verdict`` package -- a tree keyed by
module, with each top-level re-export recorded as an *alias* pointing at the
submodule that actually defines it (``verdict/__init__.py`` re-exports
``FunctionRule`` from ``verdict.rule``, so griffe records the alias rather
than duplicating the class body at the package root). ``scripts/check_api_snapshots.py``
does not want to know any of that shape -- it wants one flat row per public
symbol, the same shape every other language's own extractor produces. This
module is the only thing in the Python tree that reads griffe's own format;
everything downstream of :func:`extract_symbols` works with plain dicts.

This module only *parses* the committed snapshot -- it never invokes griffe
itself. Regenerating the snapshot (``uv run griffe dump -o api-snapshot.json
verdict`` from ``python/``) is a step a change to the public surface takes
deliberately, the same way updating a changelog is; this module does not
paper over a stale snapshot by re-running the tool behind the scenes.
"""

from __future__ import annotations

import json
from pathlib import Path
from typing import Any

SNAPSHOT_PATH = (
    Path(__file__).resolve().parent.parent
    / "python"
    / "packages"
    / "verdict-rules"
    / "api-snapshot.json"
)

# Every dunder except __repr__ is implementation machinery (__init__ chief
# among them); __repr__ is tracked because issue #98 made "this type has a
# readable string/debugger representation" a cross-language concept (see
# api-concepts.yaml). A single-leading-underscore name (_rules, _by_name, ...)
# is this package's own private state, filtered the same way a leading
# underscore already means "private" throughout this package's own source.
_DUNDERS_TRACKED_AS_PUBLIC = {"__repr__"}


def _is_public_member(name: str) -> bool:
    """Whether a griffe member name belongs in the public-surface snapshot."""
    if name.startswith("__") and name.endswith("__"):
        return name in _DUNDERS_TRACKED_AS_PUBLIC
    return not name.startswith("_")


def _resolve_alias(root: dict[str, Any], node: dict[str, Any]) -> dict[str, Any]:
    """Follow a griffe alias's dotted ``target_path`` to its real definition.

    ``target_path`` is dotted from the package root (e.g.
    ``verdict.rule.FunctionRule``); its first segment is always the package
    name ``root`` itself already represents, so only the remaining segments
    walk ``root``'s own ``members`` tree. Loops in case an alias ever points
    at another alias rather than a concrete definition.
    """
    while node["kind"] == "alias":
        _package, *segments = node["target_path"].split(".")
        target = root
        for segment in segments:
            target = target["members"][segment]
        node = target
    return node


def _extract_from_dump(snapshot: dict[str, Any]) -> list[dict[str, str | None]]:
    """Normalize one already-parsed ``griffe dump`` document into flat rows.

    Split out from :func:`extract_symbols` so a CI step can reuse the same
    normalization against a freshly-generated dump (kept in memory, never
    written over the committed file) to confirm the committed snapshot still
    matches the live code -- without giving :func:`extract_symbols` itself a
    path parameter, since its contract is specifically to read the committed
    file and nothing else.
    """
    root = next(iter(snapshot.values()))  # top level is keyed by package name ("verdict")

    rows: list[dict[str, str | None]] = []
    for export_name in root["exports"]:
        definition = _resolve_alias(root, root["members"][export_name])

        rows.append({"type": export_name, "member": None})
        for member_name in definition.get("members", {}):
            if _is_public_member(member_name):
                rows.append({"type": export_name, "member": member_name})

    return rows


def extract_symbols() -> list[dict[str, str | None]]:
    """Return every public symbol in the package's committed snapshot, normalized.

    Each item is a dict: {'type': 'TypeName', 'member': 'MemberName' or None}.
    member=None means the row describes the type itself (a type-level
    concept with no further member, matching api-concepts.yaml's own
    member: '' rows). Reads the COMMITTED snapshot file on disk --
    does not invoke the language's own CLI tool itself (that already ran
    once to generate the committed file; this function just parses it).
    """
    return _extract_from_dump(json.loads(SNAPSHOT_PATH.read_text()))


if __name__ == "__main__":
    for row in extract_symbols():
        print(row)
