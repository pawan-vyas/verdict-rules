#!/usr/bin/env python3
"""Normalize JS/TS's committed api-extractor report into flat (type, member) rows.

``js/packages/verdict-rules/etc/verdict-rules.api.md`` is
`@microsoft/api-extractor <https://api-extractor.com/>`_'s own committed
report -- a TypeScript-pseudocode block, one ``export class``/``export
interface``/``export type`` declaration per public symbol, each class/
interface listing its own members inside a ``{ ... }`` body.
``scripts/check_api_snapshots.py`` does not want to know any of that shape --
it wants one flat row per public symbol, the same shape every other
language's own extractor produces. This module is the only thing in the
JS/TS tree that reads api-extractor's own format; everything downstream of
:func:`extract_symbols` works with plain dicts.

This module only *parses* the committed report -- it never invokes
api-extractor itself. Regenerating the report (``npx api-extractor run
--local`` from ``js/packages/verdict-rules/``) is a step a change to the
public surface takes deliberately, the same way updating a changelog is;
this module does not paper over a stale report by re-running the tool
behind the scenes.

A regex-based line parser is enough here, deliberately -- api-extractor's
``.api.md`` body is a constrained, tool-generated pseudocode dialect (one
member per line, a fixed 4-space body indent, a closing brace alone on its
own unindented line), not arbitrary TypeScript. Building a real TS parser
to read a format this constrained would be solving a harder problem than
the one in front of it.
"""

from __future__ import annotations

import re
from pathlib import Path

SNAPSHOT_PATH = (
    Path(__file__).resolve().parent.parent
    / "js"
    / "packages"
    / "verdict-rules"
    / "etc"
    / "verdict-rules.api.md"
)

# The report's body lives in the single ```ts ... ``` fenced block.
_CODE_FENCE_RE = re.compile(r"^```(?:ts)?\s*$")

# `// @public`, `// (undocumented)`, `// (No @packageDocumentation ...)` --
# every annotation api-extractor emits is a line starting with `//`, never
# code. Skipped outright: the report doesn't use release tags to partition
# this package's surface (see api-extractor.json's own `ae-missing-release-tag`
# note), so there is nothing to branch on here, only noise to drop.
_COMMENT_RE = re.compile(r"^\s*//")

# `export class Foo<T> implements Rule<T> {` / `export interface Foo<T> {`
# -- the only two kinds of member-bearing declarations this package emits.
_TYPE_HEADER_RE = re.compile(
    r"^export\s+(?:class|interface)\s+(?P<name>[A-Za-z_$][\w$]*)\b.*\{$"
)

# `export type Foo = ...;` -- a one-line declaration with no body to recurse
# into; matches api-concepts.yaml's own `member: ""` rows exactly.
_TYPE_ALIAS_RE = re.compile(r"^export\s+type\s+(?P<name>[A-Za-z_$][\w$]*)\b.*;$")

# A member line's own name, after stripping the `static`/`readonly`/`get`
# modifiers api-extractor prints ahead of a field or accessor:
# `readonly name: string;`, `get groupNames(): readonly string[];`,
# `evaluate(context: T): ...;`, `static readonly VACUOUS_RESULT = true;`.
#
# The optional `<...>` after the name matches a member that is itself
# generic -- `evaluate<TContext>(...)` on the evaluators. Without it the
# name is followed by `<` rather than `(`, so the member is invisible to
# this parser and the gate silently stops asserting real public surface.
# `=` is a terminator for the same reason: a static field with an
# initializer is written `NAME = value`, with neither `:` nor `(`.
_MEMBER_NAME_RE = re.compile(
    r"^(?:static\s+)?(?:readonly\s+)?(?:get\s+|set\s+)?"
    r"(?P<name>\[[^\]]+\]|[A-Za-z_$][\w$]*)\s*(?:<[^>]*>)?\s*[:(?=]"
)


def _is_public_member(line: str) -> bool:
    """Whether a body line names a member to track, vs. structural noise.

    `#private;` is api-extractor's own marker for a class's private fields
    (this package's rules/by-name maps) -- already excluded from the report's
    public surface by the tool itself, filtered here only because the marker
    line has no member name to extract. `constructor(...)` is deliberately
    excluded too: every class in every one of this package's four languages
    has one, in a shape that is already language-specific by construction
    (positional vs. named args, `__init__` vs a constructor list) -- it is
    not a cross-language concept api-concepts.yaml tracks anywhere, the same
    choice the Python extractor makes by never special-casing `__init__`.
    """
    if line.startswith("#private"):
        return False
    if line.startswith("constructor("):
        return False
    return True


def extract_symbols() -> list[dict[str, str | None]]:
    """Return every public symbol in the package's committed snapshot, normalized.

    Each item is a dict: {'type': 'TypeName', 'member': 'MemberName' or None}.
    member=None means the row describes the type itself (a type-level
    concept with no further member, matching api-concepts.yaml's own
    member: '' rows). Reads the COMMITTED snapshot file on disk --
    does not invoke the language's own CLI tool itself (that already ran
    once to generate the committed file; this function just parses it).
    """
    lines = SNAPSHOT_PATH.read_text().splitlines()

    rows: list[dict[str, str | None]] = []
    in_code_block = False
    current_type: str | None = None

    for raw_line in lines:
        if _CODE_FENCE_RE.match(raw_line):
            in_code_block = not in_code_block
            continue
        if not in_code_block:
            continue

        line = raw_line.strip()
        if not line or _COMMENT_RE.match(line):
            continue

        if current_type is not None:
            if line == "}":
                current_type = None
                continue
            if _is_public_member(line):
                member_match = _MEMBER_NAME_RE.match(line)
                if member_match is not None:
                    rows.append({"type": current_type, "member": member_match["name"]})
            continue

        type_header_match = _TYPE_HEADER_RE.match(line)
        if type_header_match is not None:
            current_type = type_header_match["name"]
            rows.append({"type": current_type, "member": None})
            continue

        type_alias_match = _TYPE_ALIAS_RE.match(line)
        if type_alias_match is not None:
            rows.append({"type": type_alias_match["name"], "member": None})
            continue

    return rows


if __name__ == "__main__":
    for row in extract_symbols():
        print(row)
