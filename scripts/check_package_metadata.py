#!/usr/bin/env python3
"""Every published package carries complete, real discoverability metadata.

A registry page built from a manifest with a blank `description`, a missing
`license`, or an unattributed `author` is not a release-readiness problem
anyone notices locally — `dotnet pack`, `npm publish`, and `uv build` all
happily produce an artifact from an incomplete manifest, and the gap is only
visible once it is live on PyPI, npm, NuGet, or pub.dev, where it cannot be
patched without a new version. This catches it before the merge that would
ship it.

Packages are discovered exactly the way ``check_changelogs.py`` discovers
them — scanning for a recognised manifest filename paired with a
``CHANGELOG.md`` — reused directly from there rather than re-implemented, so
the two scripts can never disagree about what counts as a package in this
repo, and a fifth language needs an edit to neither.

Each ecosystem's manifest supports a different metadata surface, so what
"complete" means is a per-format field table, not one universal field list:
pyproject.toml's ``[project]`` table (description, license, authors, a
repository/homepage URL), package.json (description, license, author,
repository, keywords), a .csproj's PackageReference properties (Description,
Authors, a license expression or file, RepositoryUrl, PackageTags), and
pubspec.yaml (description, a homepage or repository URL, topics) — pub.dev
has no manifest field for license at all; it reads the LICENSE file bundled
in the package instead, so this check does not invent one. The vendored
Claude Code plugin manifest (plugin.json) gets the same treatment with its
own smaller field set (description, license, author, keywords), because
``check_changelogs.py``'s own discovery already treats it as a package.

A field fails three ways: absent from the manifest entirely, present but
empty, or present with an obvious placeholder value (``TODO``, ``TBD``, or
the field's own name used as its value, e.g. ``description = "description"``)
-- never a network call, so this runs safely on every pull request.
"""

from __future__ import annotations

import json
import re
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))

from check_changelogs import MANIFESTS as DISCOVERY_MANIFESTS  # noqa: E402
from check_changelogs import discover  # noqa: E402

PLACEHOLDER_TOKENS = {"todo", "tbd", "fixme", "xxx", "n/a", "na", "changeme", "placeholder"}


def _is_placeholder(field: str, value: str) -> bool:
    """A value that is technically non-empty but says nothing real."""
    cleaned = value.strip()
    return cleaned.lower() in PLACEHOLDER_TOKENS or cleaned.lower() == field.lower()


def _search(pattern: str, text: str) -> str | None:
    match = re.search(pattern, text, re.M)
    return match.group(1) if match else None


# --- per-ecosystem field extraction ----------------------------------------
#
# Each function reads one manifest's raw text (or, for the JSON formats,
# parses it once) and returns every required (field label, value-or-None)
# pair for that format. This is the variant's own file-local knowledge of
# its ecosystem's metadata surface -- adding a sixth field to one format
# never touches another format's function.


def _pyproject_fields(text: str) -> list[tuple[str, str | None]]:
    description = _search(r'^description\s*=\s*"(.*?)"', text)

    license_ = _search(r'^license\s*=\s*"(.*?)"', text)
    if license_ is None:
        license_ = _search(r'^license\s*=\s*\{[^}]*?(?:text|file)\s*=\s*"(.*?)"', text)

    authors_block = _search(r"authors\s*=\s*\[([\s\S]*?)\]", text)
    authors = None
    if authors_block:
        names = re.findall(r'name\s*=\s*"(.*?)"', authors_block)
        authors = ", ".join(names) if names else None

    urls_block = _search(r"\[project\.urls\]([\s\S]*?)(?=\n\[|\Z)", text)
    url = None
    if urls_block:
        url = _search(r'^(?:Homepage|Repository)\s*=\s*"(.*?)"', urls_block)

    return [
        ("description", description),
        ("license", license_),
        ("authors", authors),
        ("repository/homepage url", url),
    ]


def _package_json_fields(text: str) -> list[tuple[str, str | None]]:
    data = json.loads(text)

    author = data.get("author")
    if isinstance(author, dict):
        author = author.get("name")

    repository = data.get("repository")
    if isinstance(repository, dict):
        repository = repository.get("url")

    keywords = data.get("keywords")
    keywords_value = ", ".join(keywords) if isinstance(keywords, list) and keywords else None

    return [
        ("description", data.get("description")),
        ("license", data.get("license")),
        ("author", author),
        ("repository", repository),
        ("keywords", keywords_value),
    ]


def _plugin_json_fields(text: str) -> list[tuple[str, str | None]]:
    data = json.loads(text)

    author = data.get("author")
    if isinstance(author, dict):
        author = author.get("name")

    keywords = data.get("keywords")
    keywords_value = ", ".join(keywords) if isinstance(keywords, list) and keywords else None

    return [
        ("description", data.get("description")),
        ("license", data.get("license")),
        ("author", author),
        ("keywords", keywords_value),
    ]


def _pubspec_description(text: str) -> str | None:
    lines = text.splitlines()
    for i, line in enumerate(lines):
        match = re.match(r"^description:\s*(.*)$", line)
        if not match:
            continue
        rest = match.group(1).strip()
        if rest and rest[0] not in ">|":
            return rest.strip("\"'")
        # A folded (>-) or literal (|-) block scalar: the real text is on
        # the indented lines that follow, not on the `description:` line.
        block = [
            following.strip()
            for following in lines[i + 1 :]
            if following.strip()
            if following[:1] in (" ", "\t")
        ]
        return " ".join(block) if block else None
    return None


def _pubspec_list(text: str, key: str) -> str | None:
    lines = text.splitlines()
    for i, line in enumerate(lines):
        if not re.match(rf"^{key}:\s*$", line):
            continue
        items = []
        for following in lines[i + 1 :]:
            item = re.match(r"^\s*-\s*(.+)$", following)
            if item:
                items.append(item.group(1).strip())
            elif not following.strip():
                continue
            else:
                break
        return ", ".join(items) if items else None
    return None


def _pubspec_fields(text: str) -> list[tuple[str, str | None]]:
    url = _search(r"^(?:homepage|repository):\s*(\S.*)$", text)
    return [
        ("description", _pubspec_description(text)),
        ("homepage/repository url", url),
        ("topics", _pubspec_list(text, "topics")),
    ]


def _csproj_fields(text: str) -> list[tuple[str, str | None]]:
    license_ = _search(r"<PackageLicenseExpression>(.*?)</PackageLicenseExpression>", text)
    if license_ is None:
        license_ = _search(r"<PackageLicenseFile>(.*?)</PackageLicenseFile>", text)

    return [
        ("Description", _search(r"<Description>(.*?)</Description>", text)),
        ("Authors", _search(r"<Authors>(.*?)</Authors>", text)),
        ("license (PackageLicenseExpression/File)", license_),
        ("RepositoryUrl", _search(r"<RepositoryUrl>(.*?)</RepositoryUrl>", text)),
        ("PackageTags", _search(r"<PackageTags>(.*?)</PackageTags>", text)),
    ]


# The dispatch table itself -- keyed by exactly the same manifest-filename
# vocabulary as check_changelogs.py's own MANIFESTS table, so the two stay
# impossible to drift apart on "what kinds of manifest exist here."
FIELD_CHECKS = {
    "pyproject.toml": _pyproject_fields,
    "package.json": _package_json_fields,
    "pubspec.yaml": _pubspec_fields,
    ".csproj": _csproj_fields,
    "plugin.json": _plugin_json_fields,
}


def _manifest_kind(manifest: Path) -> str:
    """Which FIELD_CHECKS entry a manifest path belongs to.

    Mirrors check_changelogs.py's own name-vs-suffix matching exactly, so a
    manifest that discover() recognised is always recognised here too.
    """
    for name in DISCOVERY_MANIFESTS:
        if name.startswith(".") and manifest.suffix == name:
            return name
        if not name.startswith(".") and manifest.name == name:
            return name
    raise ValueError(f"{manifest}: not a recognised manifest filename -- check_changelogs.py should not have found it")


def main() -> int:
    failures: list[str] = []
    checked = 0

    packages = discover()
    if not packages:
        print("::error::No packages found -- every package needs a CHANGELOG.md beside its manifest.")
        return 1

    for label, manifest, _changelog, _version in packages:
        kind = _manifest_kind(manifest)
        fields = FIELD_CHECKS[kind](manifest.read_text())

        for field, value in fields:
            checked += 1
            if value is None or not value.strip():
                failures.append(f"{label} ({manifest}): '{field}' is missing or empty.")
            elif _is_placeholder(field, value):
                failures.append(f"{label} ({manifest}): '{field}' looks like a placeholder ({value!r}).")
            else:
                print(f"OK {label}: {field} = {value[:70]!r}")

    for failure in failures:
        print(f"::error::{failure}")

    if failures:
        print(f"\n{len(failures)} of {checked} metadata field(s) incomplete.", file=sys.stderr)
        return 1

    print(f"\nEvery package's metadata is complete ({checked} field(s) across {len(packages)} package(s)).")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
