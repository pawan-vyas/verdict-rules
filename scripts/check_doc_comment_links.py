#!/usr/bin/env python3
"""Every file reference inside a source-level doc comment must resolve.

A docstring, an XML `///` doc comment, or a JSDoc `/** */` block routinely
points a reader somewhere else — "see ``README.md``", "`docs/architecture/`
has the full write-up". Unlike a link in a published doc, nothing checks
these today: they live inside source files, so no markdown link checker ever
sees them, and a rename or a moved directory leaves them dangling silently.
The reader who hits a stale one is mid-API-exploration in their editor, which
is the worst place to discover a repo's docs have drifted from its code.

Packages are discovered the same way as `scripts/check_changelogs.py` — a
directory with a recognised manifest paired with its own `CHANGELOG.md` is a
package, never a hardcoded table — so adding a language needs no edit here,
only a new row in `PACKAGE_SOURCE` saying where that ecosystem keeps source
relative to its manifest, and in `DOC_COMMENT_LINES`/`SYMBOL_PATTERN` saying
how to read a doc comment and a type name out of that language's syntax.

Two checks per reference found inside a doc comment:

1. A markdown-style link (``[text](path)``) or a backtick-quoted relative
   file path must resolve to a real file, tried against the package's own
   root and the repo root (a reference's own path structure implies which
   one it meant — `docs/architecture/` only exists at the repo root, while
   `README.md` only exists beside the package).
2. A backtick-quoted symbol name explicitly introduced by "see" (`see
   \\`RuleResult\\``) must resolve to a type genuinely declared somewhere in
   that language's own package source. This is deliberately narrow — a bare
   capitalized word in prose is not a claim of cross-reference — so it never
   flags a false positive at the cost of missing some real ones.
"""

from __future__ import annotations

import ast
import re
import sys
from pathlib import Path

# Mirrors check_changelogs.py's MANIFESTS table, extended with the two facts
# this check additionally needs per ecosystem: where source normally lives
# relative to the manifest, and what extension its doc comments are written
# in. A fifth language is one new row -- nothing here is edited.
PACKAGE_SOURCE: dict[str, tuple[str, str]] = {
    "pyproject.toml": ("src", ".py"),
    "package.json": ("src", ".ts"),
    "pubspec.yaml": ("lib", ".dart"),
    ".csproj": (".", ".cs"),
}

# Directories that hold generated or vendored code, never hand-written doc
# comments -- walking into them wastes time and can false-positive on
# generated cross-references that were never meant to resolve by hand.
SKIP = ("node_modules", "/dist/", "/bin/", "/obj/", ".agents/", "/build/", "/.dart_tool/")


def discover() -> list[tuple[str, Path, str]]:
    """Every (label, package_root, source_extension) with its own changelog.

    Reusing the changelog as the discovery anchor (rather than scanning for
    manifests directly) keeps this script's notion of "a package" identical
    to check_changelogs.py's: the published units, not every example, test
    project, or eval fixture that happens to carry its own manifest too.
    """
    found = []
    for changelog in sorted(Path(".").rglob("CHANGELOG.md")):
        if any(skip in f"/{changelog}" for skip in SKIP):
            continue
        directory = changelog.parent
        for name, (source_subdir, extension) in PACKAGE_SOURCE.items():
            manifests = (
                sorted(directory.glob(f"*{name}")) if name.startswith(".")
                else ([directory / name] if (directory / name).exists() else [])
            )
            if not manifests:
                continue
            found.append((str(directory), directory / source_subdir, extension))
            break
    return found


def source_files(package_root: Path, extension: str) -> list[Path]:
    return sorted(
        p for p in package_root.rglob(f"*{extension}")
        if not any(skip in f"/{p}" for skip in SKIP)
    )


# --- Extracting doc-comment lines, one extractor per source syntax --------
#
# Each returns every (line_number, raw_source_line) pair that is part of a
# doc comment -- never a regular comment or code line -- so a reference
# found downstream always points at real doc-comment content.


def _python_doc_comment_lines(path: Path) -> list[tuple[int, str]]:
    source = path.read_text()
    lines = source.splitlines()
    tree = ast.parse(source, filename=str(path))
    nodes: list[ast.AST] = [tree] + [
        n for n in ast.walk(tree)
        if isinstance(n, (ast.ClassDef, ast.FunctionDef, ast.AsyncFunctionDef))
    ]
    out: list[tuple[int, str]] = []
    for node in nodes:
        if ast.get_docstring(node, clean=False) is None:
            continue
        doc_expr = node.body[0].value  # type: ignore[attr-defined]
        start = doc_expr.lineno
        end = getattr(doc_expr, "end_lineno", start)
        out.extend((ln, lines[ln - 1]) for ln in range(start, end + 1))
    return out


def _slash_doc_comment_lines(path: Path) -> list[tuple[int, str]]:
    """C# `///` and Dart `///` share the same line-prefixed doc-comment shape."""
    return [
        (i, line)
        for i, line in enumerate(path.read_text().splitlines(), start=1)
        if line.strip().startswith("///")
    ]


_JSDOC_BLOCK = re.compile(r"/\*\*.*?\*/", re.S)


def _jsdoc_comment_lines(path: Path) -> list[tuple[int, str]]:
    text = path.read_text()
    lines = text.splitlines()
    out: list[tuple[int, str]] = []
    for block in _JSDOC_BLOCK.finditer(text):
        start = text.count("\n", 0, block.start()) + 1
        end = text.count("\n", 0, block.end()) + 1
        out.extend((ln, lines[ln - 1]) for ln in range(start, end + 1))
    return out


# Keyed by extension, exhaustive over PACKAGE_SOURCE's own extensions -- a
# fifth language adds a row here, never edits an existing one.
DOC_COMMENT_LINES = {
    ".py": _python_doc_comment_lines,
    ".cs": _slash_doc_comment_lines,
    ".dart": _slash_doc_comment_lines,
    ".ts": _jsdoc_comment_lines,
}


# --- Extracting type-level symbol names, for the "see `Name`" check -------
#
# Deliberately type-level only (class/interface/struct/record/enum/mixin),
# not every method -- a method-name index would be large, noisy, and still
# incomplete (inherited members, generic instantiations), which is exactly
# the shape of false positive this check is designed to avoid.
SYMBOL_PATTERN = {
    ".py": re.compile(r"^\s*(?:class|(?:async\s+)?def)\s+(\w+)", re.M),
    ".cs": re.compile(r"\b(?:class|interface|struct|record|enum)\s+(\w+)"),
    ".dart": re.compile(r"\b(?:class|mixin|enum|extension)\s+(\w+)"),
    ".ts": re.compile(r"\b(?:class|interface|type|function)\s+(\w+)"),
}


def symbol_index(package_root: Path, extension: str) -> set[str]:
    pattern = SYMBOL_PATTERN[extension]
    names: set[str] = set()
    for path in source_files(package_root, extension):
        names.update(pattern.findall(path.read_text()))
    return names


# --- Finding references inside a doc-comment line --------------------------

_MARKDOWN_LINK = re.compile(r"\[[^\]\n]*\]\(([^)\s]+)\)")
# Double-backtick (reST ``code``) tried first: Python docstrings pace
# adjacent double-backtick spans right next to each other (` ``and``/``or`` `),
# and matching single backticks greedily there would split each pair at its
# inner boundary and misread the separator between them as its own span.
_BACKTICK = re.compile(r"``([^`]+)``|`([^`\s]+)`")
_FILE_EXTENSIONS = (
    ".md", ".py", ".ts", ".tsx", ".js", ".dart", ".cs", ".csproj",
    ".json", ".yaml", ".yml", ".toml", ".txt",
)
# `{1,2}` so both a single Markdown backtick and a doubled reST backtick
# (Python's own inline-code convention) are recognised the same way. Capture
# the raw content first and let symbol_references() reject anything that is
# really a file path ("see ``README.md``") -- a path already gets checked
# by path_references(), and treating it as a symbol claim too would flag the
# ordinary "see <file>" phrasing used throughout this repo as dangling.
_SEE_RAW = re.compile(r"\bsee\s+`{1,2}([^`]+)`{1,2}", re.I)
_SYMBOL_SHAPE = re.compile(r"^([A-Z][A-Za-z0-9_]*)(?:\.[A-Za-z0-9_]+)?$")


def _looks_like_path(candidate: str) -> bool:
    if candidate.startswith(("http://", "https://", "mailto:")):
        return False
    return "/" in candidate or candidate.lower().endswith(_FILE_EXTENSIONS)


def path_references(line: str) -> list[str]:
    found = [m.group(1) for m in _MARKDOWN_LINK.finditer(line)]
    found += [
        (m.group(1) or m.group(2)) for m in _BACKTICK.finditer(line)
        if _looks_like_path(m.group(1) or m.group(2))
    ]
    return found


def symbol_references(line: str) -> list[str]:
    out = []
    for match in _SEE_RAW.finditer(line):
        content = match.group(1)
        if _looks_like_path(content):
            continue  # "see `README.md`" is a file reference, not a type claim
        shape = _SYMBOL_SHAPE.match(content)
        if shape:
            out.append(shape.group(1))
    return out


def resolves(reference: str, package_root: Path) -> bool:
    target = reference.split("#", 1)[0].lstrip("/")
    if not target:
        return True  # a bare anchor into the same file, nothing to resolve
    return (package_root / target).exists() or Path(target).exists()


def main() -> int:
    failures: list[str] = []
    packages = discover()
    if not packages:
        print("::error::No packages found -- every package needs a CHANGELOG.md beside its manifest.")
        return 1

    for label, package_root, extension in packages:
        extract_lines = DOC_COMMENT_LINES.get(extension)
        if extract_lines is None or not package_root.exists():
            continue

        files = source_files(package_root, extension)
        symbols: set[str] | None = None  # built lazily, only if a package ever uses "see `X`"
        checked = 0

        for path in files:
            for line_no, line in extract_lines(path):
                for reference in path_references(line):
                    checked += 1
                    if not resolves(reference, package_root):
                        failures.append(
                            f"{path}:{line_no}: doc comment references '{reference}', "
                            f"which does not exist (tried relative to {package_root}/ and the repo root)"
                        )

                for name in symbol_references(line):
                    if symbols is None:
                        symbols = symbol_index(package_root, extension)
                    checked += 1
                    if name not in symbols:
                        failures.append(
                            f"{path}:{line_no}: doc comment says 'see `{name}`', but no such "
                            f"type is declared anywhere in {package_root}/"
                        )

        print(f"OK {label} ({len(files)} {extension} file(s), {checked} reference(s) checked)")

    for failure in failures:
        print(f"::error::{failure}")

    if failures:
        print(f"\n{len(failures)} dangling doc-comment reference(s).", file=sys.stderr)
        return 1

    print("Every doc-comment reference resolves.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
