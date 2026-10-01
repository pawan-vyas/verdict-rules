#!/usr/bin/env python3
"""Check every package's README against the shared authoring template -- and
prove its first example actually runs against what the package really ships.

docs/maintenance/doc-authoring/package-readmes.md fixes one skeleton across
every language's own README.md so a reader comparing two packages minutes
apart, on two different registry pages, is reading one project in N
languages rather than relearning the shape each time. Nothing mechanical
enforced that skeleton before this script -- a README could drift out of
order, lose its install command, or (the failure this is really for) show an
example that no longer matches the API actually being published, and every
other release-readiness check would stay green regardless. A test suite
proves the library is right; it says nothing about whether the page telling
someone how to install it is still telling the truth.

Three checks per discovered package, in order of how cheap they are to get
wrong without anything else noticing:

1. Section order -- the first ``## Install``, the mandatory first-example
   section after it, and ``## What it guarantees`` / ``## Where to go
   next`` as the final two headings in that order (the template's own
   mechanically-checkable subset; (4)/(5) are free-form by design).
2. The install snippet names the real registry command for *this* package's
   *actual* name, read from its own manifest -- never a hardcoded table
   mapping package to command, so a rename can't silently go unchecked.
3. The actual artifact its own release workflow builds -- a real wheel, a
   real packed tarball, a real .nupkg, a real path dependency -- installed
   into a fresh, isolated scratch directory, with the README's own first
   runnable code block executed against it. This is the one check that
   catches a README describing an API the shipped package doesn't actually
   have; a stale prose description would pass every other check in this
   repo silently.

Packages are discovered, not listed, the same way scripts/check_changelogs.py
discovers changelogs: a directory containing both a recognised manifest and
a README.md beside it is a package, so adding a language needs no edit here.
"""

from __future__ import annotations

import json
import re
import subprocess
import sys
import tempfile
from dataclasses import dataclass
from pathlib import Path
from typing import Callable

# Directories that look like a package but are not published from here --
# the same list scripts/check_changelogs.py uses, for the same reason.
SKIP = ("node_modules", "/dist/", "/bin/", "/obj/", ".agents/", "/build/")


def _search(pattern: str, text: str) -> str | None:
    match = re.search(pattern, text, re.M)
    return match.group(1) if match else None


def _run(cmd: list[str], cwd: Path | None = None) -> None:
    """Run a build/install/execute step, failing loudly with real output.

    Every step here is a different language's toolchain -- the one thing
    they have in common is that a silent `CalledProcessError` traceback
    names the subprocess module, not the actual npm/uv/dotnet/dart failure
    a maintainer needs to read. Capturing and re-raising with the command
    and both streams attached keeps the real error in the failure message
    this script prints, not buried in a Python traceback.
    """
    result = subprocess.run(cmd, cwd=cwd, capture_output=True, text=True)
    if result.returncode != 0:
        where = f" (in {cwd})" if cwd else ""
        raise RuntimeError(
            f"`{' '.join(cmd)}`{where} exited {result.returncode}\n"
            f"--- stdout ---\n{result.stdout[-4000:]}\n"
            f"--- stderr ---\n{result.stderr[-4000:]}"
        )


# ---------------------------------------------------------------------------
# Heading / section parsing -- shared by every language, since markdown
# structure is not language-specific.
# ---------------------------------------------------------------------------


def headings(text: str) -> list[tuple[int, str]]:
    """[(level, heading text)] in document order, skipping fenced code.

    A naive ``^#{1,2}\\s`` scan would also match a shell comment or a
    Python docstring heading-looking line inside one of the README's own
    code fences -- this package's examples are full of ``#`` comments.
    """
    result: list[tuple[int, str]] = []
    in_code = False
    for line in text.splitlines():
        if line.strip().startswith("```"):
            in_code = not in_code
            continue
        if in_code:
            continue
        match = re.match(r"^(#{1,2})\s+(.*)$", line)
        if match:
            result.append((len(match.group(1)), match.group(2).strip()))
    return result


def extract_section(text: str, heading: str) -> str:
    """The body of the first H1/H2 section with this exact heading text."""
    lines = text.splitlines()
    start = None
    for i, line in enumerate(lines):
        if re.match(rf"^#{{1,2}}\s+{re.escape(heading)}\s*$", line):
            start = i + 1
            break
    if start is None:
        return ""
    end = len(lines)
    for i in range(start, len(lines)):
        if re.match(r"^#{1,2}\s+", lines[i]):
            end = i
            break
    return "\n".join(lines[start:end])


CODE_BLOCK = re.compile(r"```[^\n]*\n(.*?)```", re.S)


def first_code_block(section_text: str) -> str | None:
    match = CODE_BLOCK.search(section_text)
    return match.group(1) if match else None


def check_section_order(readme: Path, text: str) -> list[str]:
    """The template's own mechanically-checkable subset of the skeleton."""
    h2s = [heading for level, heading in headings(text) if level == 2]
    problems: list[str] = []

    if not h2s or h2s[0] != "Install":
        problems.append(
            f"{readme}: first H2 section is {(h2s[0] if h2s else '(none)')!r}, expected 'Install'"
        )

    if len(h2s) < 4:
        problems.append(
            f"{readme}: only {len(h2s)} H2 section(s) -- the skeleton needs 'Install', at "
            f"least one first-example section, 'What it guarantees', and 'Where to go next'"
        )
        return problems

    if h2s[-2:] != ["What it guarantees", "Where to go next"]:
        problems.append(
            f"{readme}: last two H2 sections are {h2s[-2:]!r}, expected "
            f"['What it guarantees', 'Where to go next']"
        )

    return problems


# ---------------------------------------------------------------------------
# Per-language ownership: each entry knows its own package name, its own
# registry's install syntax, and how to build + install + run its own real
# artifact. Nothing here dispatches back into a shared per-language branch --
# a fifth language is one more entry, never an edit to the other four's.
# ---------------------------------------------------------------------------


@dataclass(frozen=True)
class LanguageCheck:
    label: str
    # Manifest path -> published package name, or None if this manifest
    # belongs to an unpublished example/test project (skip it).
    package_name: Callable[[Path], str | None]
    # Package name -> the exact install command the README must show.
    install_command: Callable[[str], str]
    # (manifest, first-example code, scratch dir, package name) -> raises
    # on any failure; a fresh, isolated directory per call.
    run_first_example: Callable[[Path, str, Path, str], None]


def _language_root(manifest: Path) -> Path:
    """manifest/packages-or-src/<pkg>/<manifest> -> manifest/<language>/.

    True for every manifest this script discovers: each sits two directories
    below the language root that its own release workflow's `working-
    directory:` points at (python/, js/, csharp/).
    """
    return manifest.parents[2]


# --- Python -----------------------------------------------------------------


def _python_name(manifest: Path) -> str | None:
    return _search(r'^name\s*=\s*"(.+?)"', manifest.read_text())


def _run_python_example(manifest: Path, code: str, scratch: Path, name: str) -> None:
    root = _language_root(manifest)
    version = _search(r'^version\s*=\s*"(.+?)"', manifest.read_text())

    # Exactly scripts/release-python.yml's own build step: `uv build
    # --package <name>` from the workspace root.
    _run(["uv", "build", "--package", name], cwd=root)

    dist = root / "dist"
    wheels = sorted(dist.glob(f"*-{version}-*.whl"))
    if not wheels:
        raise RuntimeError(f"uv build did not produce a wheel for version {version} in {dist}")
    wheel = wheels[-1]

    # A fresh venv, not this repo's own dev environment -- the point is what
    # a consumer who only has the built wheel actually gets.
    venv = scratch / "venv"
    _run([sys.executable, "-m", "venv", str(venv)])
    _run([str(venv / "bin" / "pip"), "install", "--quiet", str(wheel)])

    script = scratch / "example.py"
    script.write_text(code)
    _run([str(venv / "bin" / "python"), str(script)], cwd=scratch)


# --- JS/TS --------------------------------------------------------------


def _js_name(manifest: Path) -> str | None:
    data = json.loads(manifest.read_text())
    if data.get("private"):
        return None  # an example project, never published
    return data.get("name")


def _run_js_example(manifest: Path, code: str, scratch: Path, name: str) -> None:
    package_dir = manifest.parent
    root = _language_root(manifest)
    version = json.loads(manifest.read_text())["version"]

    # Exactly release-js.yml's own build step: install the workspace, build
    # just this package, then pack it the same way the release job does.
    _run(["npm", "ci"], cwd=root)
    _run(["npm", "run", "build", f"--workspace={name}"], cwd=root)

    pack_dir = scratch / "pack"
    pack_dir.mkdir()
    _run(["npm", "pack", "--pack-destination", str(pack_dir)], cwd=package_dir)
    tarballs = sorted(pack_dir.glob(f"*{version}.tgz"))
    if not tarballs:
        raise RuntimeError(f"npm pack did not produce a tarball for version {version} in {pack_dir}")
    tarball = tarballs[-1]

    # A fresh project, not this repo's own workspace -- install only the
    # packed tarball, the same artifact npm publish would have pushed.
    project = scratch / "project"
    project.mkdir()
    _run(["npm", "init", "-y", "--silent"], cwd=project)
    package_json = project / "package.json"
    pkg = json.loads(package_json.read_text())
    pkg["type"] = "module"  # the README's example is ESM with a top-level await
    package_json.write_text(json.dumps(pkg, indent=2))
    _run(["npm", "install", "--silent", str(tarball)], cwd=project)

    example = project / "example.ts"
    example.write_text(code)
    # Node's own built-in TypeScript type-stripping runs the README's .ts
    # example verbatim -- no separate tsc/tsx dependency to install just to
    # prove the example works, and no divergence from what the README shows.
    # The flag is passed explicitly (stable since Node 22.6, default-on in
    # newer ones where it is simply a harmless no-op) rather than relying on
    # whichever Node version happens to be unflagged by default.
    _run(["node", "--experimental-strip-types", str(example)], cwd=project)


# --- Dart ---------------------------------------------------------------


def _dart_name(manifest: Path) -> str | None:
    text = manifest.read_text()
    if re.search(r'publish_to:\s*["\']?none["\']?', text):
        return None  # an example project, never published
    return _search(r"^name:\s*(\S+)", text)


def _run_dart_example(manifest: Path, code: str, scratch: Path, name: str) -> None:
    package_dir = manifest.parent.resolve()

    # pub.dev publishes Dart source directly -- there is no separate build
    # artifact the way a wheel/tarball/nupkg is one, so the closest faithful
    # approximation of "install only the real package" is a path dependency
    # at the package's own source directory, the standard local-package
    # testing pattern the Dart toolchain itself provides.
    project = scratch / "project"
    project.mkdir()
    (project / "pubspec.yaml").write_text(
        "name: readme_first_example_check\n"
        "environment:\n"
        '  sdk: ">=3.0.0 <4.0.0"\n'
        "dependencies:\n"
        f"  {name}:\n"
        f"    path: {package_dir}\n"
    )
    bin_dir = project / "bin"
    bin_dir.mkdir()
    (bin_dir / "example.dart").write_text(code)

    _run(["dart", "pub", "get"], cwd=project)
    _run(["dart", "run", "bin/example.dart"], cwd=project)


# --- C# -------------------------------------------------------------------


def _csharp_name(manifest: Path) -> str | None:
    text = manifest.read_text()
    if re.search(r"<IsPackable>\s*false\s*</IsPackable>", text):
        return None  # an example or test project, never published
    return _search(r"<PackageId>(.+?)</PackageId>", text) or manifest.stem


def _pick_runtime_tfm(csproj_text: str) -> str:
    """The non-netstandard target -- what `dotnet new console` needs.

    netstandard2.1 has no runtime of its own a console app can target; this
    package multi-targets it alongside a real runtime TFM specifically so a
    library consumer on either gets a native build, but the scratch console
    app proving the README's example needs to run on the actual one.
    """
    multi = _search(r"<TargetFrameworks>(.+?)</TargetFrameworks>", csproj_text)
    tfms = [t.strip() for t in multi.split(";") if t.strip()] if multi else []
    if not tfms:
        single = _search(r"<TargetFramework>(.+?)</TargetFramework>", csproj_text)
        tfms = [single] if single else []
    for tfm in tfms:
        if not tfm.startswith("netstandard"):
            return tfm
    if tfms:
        return tfms[0]
    raise RuntimeError(f"no <TargetFramework(s)> found in the manifest")


def _run_csharp_example(manifest: Path, code: str, scratch: Path, name: str) -> None:
    root = _language_root(manifest)
    rel_manifest = manifest.relative_to(root)
    text = manifest.read_text()
    version = _search(r"<Version>(.+?)</Version>", text)
    tfm = _pick_runtime_tfm(text)

    # Exactly release-csharp.yml's own build step.
    dist = scratch / "dist"
    _run(["dotnet", "pack", str(rel_manifest), "-c", "Release", "-o", str(dist)], cwd=root)
    nupkgs = sorted(dist.glob(f"*.{version}.nupkg"))
    if not nupkgs:
        raise RuntimeError(f"dotnet pack did not produce a .{version}.nupkg in {dist}")

    # A fresh console app, restoring from *only* the local packed nupkg --
    # no nuget.org round trip needed, since the package itself has zero
    # runtime dependencies to resolve.
    project = scratch / "app"
    _run(["dotnet", "new", "console", "-o", str(project), "--no-restore", "-f", tfm])
    (project / "NuGet.Config").write_text(
        '<?xml version="1.0" encoding="utf-8"?>\n'
        "<configuration>\n"
        "  <packageSources>\n"
        "    <clear />\n"
        f'    <add key="local" value="{dist}" />\n'
        "  </packageSources>\n"
        "</configuration>\n"
    )
    _run(
        ["dotnet", "add", "package", name, "--version", version, "--source", str(dist)],
        cwd=project,
    )
    (project / "Program.cs").write_text(code)
    _run(["dotnet", "run"], cwd=project)


MANIFESTS: dict[str, LanguageCheck] = {
    "pyproject.toml": LanguageCheck(
        label="Python",
        package_name=_python_name,
        install_command=lambda name: f"pip install {name}",
        run_first_example=_run_python_example,
    ),
    "package.json": LanguageCheck(
        label="JS/TS",
        package_name=_js_name,
        install_command=lambda name: f"npm install {name}",
        run_first_example=_run_js_example,
    ),
    "pubspec.yaml": LanguageCheck(
        label="Dart",
        package_name=_dart_name,
        install_command=lambda name: f"dart pub add {name}",
        run_first_example=_run_dart_example,
    ),
    ".csproj": LanguageCheck(
        label="C#",
        package_name=_csharp_name,
        install_command=lambda name: f"dotnet add package {name}",
        run_first_example=_run_csharp_example,
    ),
}


def discover() -> list[tuple[Path, Path, LanguageCheck, str]]:
    """(manifest, README, check, package name) for every publishable package.

    A package is a directory holding both a recognised manifest and a
    README.md beside it -- adding a language, or a second package within
    one, needs no edit here, the same discovery contract
    scripts/check_changelogs.py already establishes for CHANGELOG.md.
    """
    found = []
    for manifest_name, check in MANIFESTS.items():
        pattern = f"*{manifest_name}" if manifest_name.startswith(".") else manifest_name
        for manifest in sorted(Path(".").rglob(pattern)):
            text = str(manifest)
            if any(skip in f"/{text}" for skip in SKIP):
                continue
            readme = manifest.parent / "README.md"
            if not readme.exists():
                continue
            name = check.package_name(manifest)
            if not name:
                continue  # an example/test manifest, not a published package
            found.append((manifest, readme, check, name))
    return found


def check_package(manifest: Path, readme: Path, check: LanguageCheck, name: str) -> list[str]:
    failures: list[str] = []
    text = readme.read_text()

    # 1 -- section order.
    failures.extend(check_section_order(readme, text))

    h2s = [heading for level, heading in headings(text) if level == 2]

    # 2 -- the install snippet names this package's own real install command.
    if "Install" in h2s:
        expected = check.install_command(name)
        if expected not in extract_section(text, "Install"):
            failures.append(f"{readme}: '## Install' has no command matching {expected!r}")
    else:
        failures.append(f"{readme}: no '## Install' section to check for an install command")

    # 3 -- the README's first example actually runs against the real artifact.
    if len(h2s) < 2:
        failures.append(
            f"{readme}: no first-example section between 'Install' and the closing "
            f"sections to run"
        )
        return failures

    first_example_heading = h2s[1]
    code = first_code_block(extract_section(text, first_example_heading))
    if not code:
        failures.append(f"{readme}: '## {first_example_heading}' has no fenced code block to run")
        return failures

    try:
        with tempfile.TemporaryDirectory(prefix="verdict-readme-check-") as tmp:
            check.run_first_example(manifest, code, Path(tmp), name)
        print(f"OK {check.label}: '## {first_example_heading}' runs against the real packed artifact")
    except Exception as exc:  # noqa: BLE001 -- any toolchain's failure belongs in the report, not a traceback
        failures.append(
            f"{readme}: the first example under '## {first_example_heading}' failed against "
            f"the real built package -- {exc}"
        )

    return failures


def main() -> int:
    packages = discover()
    if not packages:
        print("::error::No packages found -- every package needs a README.md beside its manifest.")
        return 1

    failures: list[str] = []
    for manifest, readme, check, name in packages:
        print(f"-- {check.label} ({name}) --")
        package_failures = check_package(manifest, readme, check, name)
        if not package_failures:
            print(f"OK {check.label}: {readme} follows the template and its first example runs")
        failures.extend(package_failures)

    for failure in failures:
        print(f"::error::{failure}")

    if failures:
        print(f"\n{len(failures)} package README problem(s).", file=sys.stderr)
        return 1

    print("\nEvery package README follows the shared skeleton and its first example runs.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
