#!/usr/bin/env bash
# Fetch verdict's deeper documentation at an installed version, into this
# skill's own references/ directory. See commands/verdict-fetch-docs.md.
#
# Usage: fetch-docs.sh <language> <version> [<language> <version> ...]
#   fetch-docs.sh python 0.3.0
#   fetch-docs.sh python 0.3.0 js 0.3.0 dart 0.3.0
#
# language and version are supplied in pairs. The caller determines both
# per language (from that project's own manifest/lockfile) before
# invoking this script -- it does no version detection itself, so it
# has no per-ecosystem logic to keep in sync with each language's own
# agent-notes.md.
set -euo pipefail

SKILL_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
CATALOG="$SKILL_ROOT/scripts/fetch-catalog.tsv"
REPO="pawan-vyas/verdict-rules"

if [ "$#" -eq 0 ] || [ $(( $# % 2 )) -ne 0 ]; then
  echo "usage: fetch-docs.sh <language> <version> [<language> <version> ...]" >&2
  exit 2
fi

if [ ! -f "$CATALOG" ]; then
  echo "error: $CATALOG not found -- this skill was not built with a fetch catalog" >&2
  exit 1
fi

while [ "$#" -gt 0 ]; do
  lang="$1" version="$2"
  shift 2
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
  echo "${lang}: fetched ${fetched}, skipped ${missing} (no file for this language/topic yet) at ${tag}"
done
