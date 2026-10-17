#!/usr/bin/env bash
# Mutation testing for the C# package, via Stryker.NET (https://stryker-mutator.io/docs/stryker-net/)
# -- `dotnet stryker` -- run against csharp/src/VerdictRules/ only -- never examples/, never the test
# project itself -- through csharp/tests/VerdictRules.Tests/ by way of `dotnet test`. See
# docs/maintenance/mutation-testing.md for why this exists, what a surviving mutant requires, and why
# this is deliberately not wired into any CI gate (it reruns the whole test suite once per mutant --
# genuinely slow, and its own score moves slowly release to release).
#
# dotnet-stryker is a local dotnet tool, restored from csharp/.config/dotnet-tools.json -- `dotnet
# tool restore` alone makes it available -- nothing to install globally first. Stryker is invoked
# from csharp/tests/VerdictRules.Tests/ (its own working directory convention: it locates the project
# to mutate via the test project's own ProjectReference, so no separate --project/--test-project
# flags are needed for this package's single-project shape).
#
# Usage: scripts/run_mutation_csharp.sh
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
CSHARP_DIR="$ROOT/csharp"
TEST_PROJECT_DIR="$CSHARP_DIR/tests/VerdictRules.Tests"

if ! command -v dotnet >/dev/null 2>&1; then
  echo "error: 'dotnet' is required but not found on PATH." >&2
  exit 1
fi

cd "$CSHARP_DIR"
echo "verdict: restoring local dotnet tools (dotnet-stryker)..." >&2
dotnet tool restore

cd "$TEST_PROJECT_DIR"
echo "verdict: running dotnet stryker against src/VerdictRules/ (this reruns the test suite once" >&2
echo "         per mutant -- expect it to take real time)..." >&2
dotnet stryker -r Progress -r Json

report_dir="$(find StrykerOutput -maxdepth 1 -mindepth 1 -type d | sort | tail -n 1)"
report_json="$report_dir/reports/mutation-report.json"

if [ -z "$report_dir" ] || [ ! -f "$report_json" ]; then
  echo "error: could not locate a mutation-report.json under StrykerOutput/." >&2
  exit 1
fi

echo "verdict: mutation score (killed/total):" >&2
python3 -c "
import json
with open('$report_json') as f:
    data = json.load(f)

killed = survived = total = 0
for info in data['files'].values():
    for mutant in info['mutants']:
        status = mutant['status']
        if status in ('CompileError', 'Ignored'):
            continue
        total += 1
        if status == 'Killed':
            killed += 1
        elif status in ('Survived', 'NoCoverage'):
            survived += 1

score = (killed / total * 100) if total else 0.0
print(f'verdict: {killed}/{total} mutants killed ({score:.1f}%) -- {survived} survivor(s)')
"
