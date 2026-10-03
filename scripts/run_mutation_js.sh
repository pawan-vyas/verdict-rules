#!/usr/bin/env bash
# Mutation testing for the JS/TS package, via StrykerJS (https://stryker-mutator.io/docs/stryker-js/)
# and its TAP runner plugin, run against js/packages/verdict-rules/src/ only -- never examples/, never
# test/ itself. See docs/maintenance/mutation-testing.md for why this exists, what a surviving mutant
# requires, and why this is deliberately not wired into any CI gate (it reruns the whole test suite
# once per mutant -- genuinely slow, and its own score moves slowly release to release).
#
# @stryker-mutator/core and @stryker-mutator/tap-runner are devDependencies of the package (see
# js/packages/verdict-rules/package.json), installed via the js/ npm workspace -- `npm install` from
# the workspace root alone makes them available, nothing to install globally first.
#
# The package's own test suite imports its compiled dist/, not src/ directly (see
# js/packages/verdict-rules/scripts/build.mjs), so stryker.config.json's own `buildCommand` rebuilds
# dist/ inside each sandbox after mutation, before the TAP runner points node's own built-in test
# runner (`node --test --test-reporter=tap`) at it. See js/packages/verdict-rules/stryker.config.json
# for the full wiring.
#
# Usage: scripts/run_mutation_js.sh
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
WORKSPACE_DIR="$ROOT/js"
PACKAGE_DIR="$WORKSPACE_DIR/packages/verdict-rules"

if ! command -v npm >/dev/null 2>&1; then
  echo "error: 'npm' is required but not found on PATH." >&2
  exit 1
fi

echo "verdict: installing dependencies for $WORKSPACE_DIR..." >&2
(cd "$WORKSPACE_DIR" && npm install)

cd "$PACKAGE_DIR"

echo "verdict: running StrykerJS against src/ (this reruns the test suite once per mutant --" >&2
echo "         expect it to take real time)..." >&2
# StrykerJS's own clear-text reporter (enabled in stryker.config.json) already prints the
# per-file and total mutation score table below -- nothing here re-derives or re-parses it.
npx stryker run
