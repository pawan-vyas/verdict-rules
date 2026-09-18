#!/usr/bin/env bash
# Fetch verdict's deeper documentation at a given version, into this skill's
# own references/ directory. See commands/verdict-fetch-docs.md.
#
# Usage: fetch-docs.sh <language>=<version> [<language>=<version> ...]
#   fetch-docs.sh python=0.3.1
#   fetch-docs.sh python=0.3.1 js=0.3.1 dart=0.3.1
#
# Fetches only -- does not detect an installed version. See
# references/<language>/agent-notes.md for how to determine it.
set -euo pipefail

SKILL_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
CATALOG="$SKILL_ROOT/scripts/fetch-catalog.tsv"
REPO="pawan-vyas/verdict-rules"

usage() {
  echo "usage: fetch-docs.sh <language>=<version> [<language>=<version> ...]" >&2
}

if [ "$#" -eq 0 ]; then
  usage
  exit 2
fi

if [ ! -f "$CATALOG" ]; then
  echo "error: $CATALOG not found -- this skill was not built with a fetch catalog" >&2
  exit 1
fi

for arg in "$@"; do
  if [[ "$arg" != *=* ]]; then
    echo "error: '$arg' has no version -- determine it first (see references/${arg}/agent-notes.md), then pass ${arg}=<version>" >&2
    usage
    exit 2
  fi
  lang="${arg%%=*}" version="${arg#*=}"

  if [ -z "$version" ]; then
    echo "error: '$arg' has no version after '='" >&2
    usage
    exit 2
  fi

  tag="${lang}-v${version}"

  if ! git ls-remote --tags --exit-code "https://github.com/${REPO}.git" "refs/tags/${tag}" >/dev/null 2>&1; then
    echo "skip ${lang}: tag ${tag} does not exist -- not falling back to the default branch"
    continue
  fi

  base="https://raw.githubusercontent.com/${REPO}/${tag}"
  fetched=0 missing=0

  while IFS="$(printf '\t')" read -r row_lang source destination; do
    [ "$row_lang" = "$lang" ] || continue
    target="$SKILL_ROOT/references/${destination}"
    mkdir -p "$(dirname "$target")"
    code="$(curl -fsSL -w '%{http_code}' -o "$target" "${base}/${source}" 2>/dev/null || echo 000)"
    if [ "$code" = "200" ]; then
      fetched=$((fetched + 1))
    else
      rm -f "$target"
      missing=$((missing + 1))
    fi
  done < "$CATALOG"

  mkdir -p "$SKILL_ROOT/references/${lang}"
  echo "$tag" > "$SKILL_ROOT/references/${lang}/.version"
  echo "${lang}: fetched ${fetched}, skipped ${missing} at ${tag}"
done
