#!/usr/bin/env bash
# Mutation testing for the Dart package, via pub.dev's own mutation_test
# (https://pub.dev/packages/mutation_test), run against dart/packages/verdict_rules/lib/ only --
# never examples/, never test/ itself. See docs/maintenance/mutation-testing.md for why this exists,
# what a surviving mutant requires, and why this is deliberately not wired into any CI gate (it
# reruns the whole test suite once per mutant -- genuinely slow, and its own score moves slowly
# release to release).
#
# mutation_test is a dev_dependency of the package (its own README recommends
# `dart pub add --dev mutation_test` over global activation), so `dart pub get` alone makes it
# available -- nothing to globally activate first.
#
# Zero-config by design: invoked with no arguments, mutation_test assumes `dart test` as the test
# command and every `lib/**.dart` file as a mutation target -- exactly the scope this script wants,
# so there is nothing to pass it.
#
# Usage: scripts/run_mutation_dart.sh
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
PACKAGE_DIR="$ROOT/dart/packages/verdict_rules"

if ! command -v dart >/dev/null 2>&1; then
  echo "error: 'dart' is required but not found on PATH." >&2
  exit 1
fi

cd "$PACKAGE_DIR"

echo "verdict: fetching dependencies for $PACKAGE_DIR..." >&2
dart pub get

echo "verdict: running mutation_test against lib/ (this reruns the test suite once per mutant --" >&2
echo "         expect it to take real time)..." >&2
# -f all: write every report format mutation_test supports (html/md/xml/junit/xunit) under
# mutation-test-report/, so whichever format is more convenient to open afterward is already there.
# The per-language score this script prints comes from mutation_test's own terminal summary below --
# nothing here re-derives or re-parses it.
dart run mutation_test -f all
