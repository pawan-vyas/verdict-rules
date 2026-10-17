#!/usr/bin/env python3
"""Normalize Dart's committed dart_apitool snapshot into flat (type, member) rows.

``dart/packages/verdict_rules/api-snapshot.json`` is
`dart_apitool <https://pub.dev/packages/dart_apitool>`_'s own committed model
(``dart-apitool extract --input . --output api-snapshot.json``) -- a nested
JSON document keyed ``packageApi``, carrying one entry per public
``interfaceDeclarations`` (class/interface/mixin), each with its own nested
``executableDeclarations`` (methods, getters, setters, constructors) and
``fieldDeclarations`` (instance fields/properties), plus a package-level
``typeAliasDeclarations`` list for top-level ``typedef``s like this package's
own ``Context`` and ``RulePredicate``. ``scripts/check_api_snapshots.py`` does
not want to know any of that shape -- it wants one flat row per public symbol,
the same shape every other language's own extractor produces. This module is
the only thing in the Dart tree that reads dart_apitool's own format;
everything downstream of :func:`extract_symbols` works with plain dicts.

This module only *parses* the committed snapshot -- it never invokes
dart_apitool itself. Regenerating the snapshot (``dart-apitool extract
--input . --output api-snapshot.json`` from
``dart/packages/verdict_rules/``) is a step a change to the public surface
takes deliberately, the same way updating a changelog is; this module does
not paper over a stale snapshot by re-running the tool behind the scenes.

Constructors are deliberately excluded from the member rows this produces --
every class in every one of this package's four languages has one, in a
shape that is already language-specific by construction (positional vs.
named args, ``new`` vs. a constructor list) -- it is not a cross-language
concept api-concepts.yaml tracks anywhere, the same choice the JS extractor
makes by never special-casing ``constructor(...)``.

A structural gap worth knowing before trusting this snapshot for anything
``toString()``-shaped: dart_apitool's own element collector drops every
``@override``-annotated member outright (see its
``APIRelevantElementsCollector._isElementAllowedToBeCollected``, which
returns ``false`` whenever ``element.metadata.hasOverride`` is true, treating
an override as "already part of the source" rather than new surface). Every
``toString()`` this package defines on ``FunctionRule``/``AndRule``/
``OrRule``/``RulesEngine`` carries ``@override`` (it overrides ``Object``'s
own), so dart_apitool's model -- and therefore this snapshot and this
extractor -- carries no row for any of them. Confirmed empirically: ``dart-
apitool diff --old pub://verdict_rules/0.3.1 --new .`` reports "No changes
detected!" even though 0.4.0's entire CHANGELOG entry is those four
``toString()`` additions (issue #98). This is not a bug in this module --
there is nothing in the committed snapshot to parse -- it is a real
limitation of dart_apitool's own public-API model that whoever wires Dart's
row into api-concepts.yaml's "has a readable string/debugger representation"
concepts needs to account for.
"""

from __future__ import annotations

import json
from pathlib import Path
from typing import Any

SNAPSHOT_PATH = (
    Path(__file__).resolve().parent.parent
    / "dart"
    / "packages"
    / "verdict_rules"
    / "api-snapshot.json"
)

# dart_apitool's own type for a constructor declaration -- excluded from the
# member rows this module emits, see the module docstring for why.
_CONSTRUCTOR_TYPE = "constructor"


def _is_public_member(executable_declaration: dict[str, Any]) -> bool:
    """Whether an interface's own executableDeclarations entry is a member to track.

    Everything already in the committed snapshot is already public --
    dart_apitool's own collector only ever records a public element in the
    first place -- so the only thing left to filter out here is a
    constructor, which this module tracks as "not a cross-language concept"
    rather than "not public".
    """
    return executable_declaration.get("type") != _CONSTRUCTOR_TYPE


def _interface_member_rows(interface_declaration: dict[str, Any]) -> list[dict[str, str | None]]:
    """One row per public method/getter/property dart_apitool recorded on one class."""
    type_name = interface_declaration["name"]
    rows: list[dict[str, str | None]] = []

    for executable_declaration in interface_declaration.get("executableDeclarations", []):
        if _is_public_member(executable_declaration):
            rows.append({"type": type_name, "member": executable_declaration["name"]})

    for field_declaration in interface_declaration.get("fieldDeclarations", []):
        rows.append({"type": type_name, "member": field_declaration["name"]})

    return rows


def _extract_from_dump(snapshot: dict[str, Any]) -> list[dict[str, str | None]]:
    """Normalize one already-parsed ``dart-apitool extract`` document into flat rows.

    Split out from :func:`extract_symbols` so a CI step can reuse the same
    normalization against a freshly-generated extraction (kept in memory,
    never written over the committed file) to confirm the committed snapshot
    still matches the live code -- without giving :func:`extract_symbols`
    itself a path parameter, since its contract is specifically to read the
    committed file and nothing else. A fresh extraction's own
    ``packageApi.packagePath`` is a machine-specific temp directory dart_apitool
    creates per run (never the same value twice, even for two extractions of
    identical source) -- irrelevant here since this function, like
    :func:`extract_symbols`, never reads that field at all.
    """
    package_api = snapshot["packageApi"]

    rows: list[dict[str, str | None]] = []

    for interface_declaration in package_api.get("interfaceDeclarations", []):
        rows.append({"type": interface_declaration["name"], "member": None})
        rows.extend(_interface_member_rows(interface_declaration))

    # Top-level typedefs (Context, RulePredicate) -- a type-level concept
    # with no further member, same as a class with nothing but its own name.
    for type_alias_declaration in package_api.get("typeAliasDeclarations", []):
        rows.append({"type": type_alias_declaration["name"], "member": None})

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
