#!/usr/bin/env bash
# Mutation testing for the Python package, via mutmut (https://github.com/boxed/mutmut), run against
# python/packages/verdict-rules/src/verdict/ only -- never examples/, never tests/ itself. See
# docs/maintenance/mutation-testing.md for why this exists, what a surviving mutant requires, and why
# this is deliberately not wired into any CI gate (it reruns the whole test suite once per mutant --
# genuinely slow, and its own score moves slowly release to release).
#
# mutmut is a dev dependency of the python/ workspace (see python/pyproject.toml's dev dependency
# group), so `uv sync` alone makes it available -- nothing to install globally first. Its scope
# (source_paths = ["src/verdict"]) and test command (pytest against tests/) are configured in
# python/packages/verdict-rules/pyproject.toml's own [tool.mutmut] section, which mutmut reads
# relative to the directory it runs from -- hence the cd below, rather than passing paths on the
# command line.
#
# Usage: scripts/run_mutation_python.sh
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
PACKAGE_DIR="$ROOT/python/packages/verdict-rules"

if ! command -v uv >/dev/null 2>&1; then
  echo "error: 'uv' is required but not found on PATH." >&2
  exit 1
fi

cd "$PACKAGE_DIR"

echo "verdict: running mutmut against src/verdict/ (this reruns the test suite once per mutant --" >&2
echo "         expect it to take real time)..." >&2
uv run mutmut run

echo "verdict: mutation score (killed/total):" >&2
uv run mutmut export-cicd-stats
python3 -c '
import json
stats = json.load(open("mutants/mutmut-cicd-stats.json"))
killed, total, survived = stats["killed"], stats["total"], stats["survived"]
score = (killed / total * 100) if total else 0.0
print(f"verdict: {killed}/{total} mutants killed ({score:.1f}%) -- {survived} survivor(s)")
'
