#!/usr/bin/env bash
# Fetch verdict's deeper documentation at an installed version, into this
# skill's own references/ directory. See commands/verdict-fetch-docs.md.
#
# Usage: fetch-docs.sh <language> [<language> ...]
#   fetch-docs.sh python
#   fetch-docs.sh python js dart
#
# Detects each language's installed version from the current project
# (run from inside it). To override detection, pass <language>=<version>
# instead of a bare language name.
set -euo pipefail

SKILL_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
CATALOG="$SKILL_ROOT/scripts/fetch-catalog.tsv"
REPO="pawan-vyas/verdict-rules"

if [ "$#" -eq 0 ]; then
  echo "usage: fetch-docs.sh <language> [<language> ...]" >&2
  exit 2
fi

if [ ! -f "$CATALOG" ]; then
  echo "error: $CATALOG not found -- this skill was not built with a fetch catalog" >&2
  exit 1
fi

detect_version() {
  case "$1" in
    python)
      python3 -c "import importlib.metadata as m; print(m.version('verdict-rules'))" 2>/dev/null
      ;;
    js)
      node -p "require('verdict-rules/package.json').version" 2>/dev/null
      ;;
    dart)
      local lockfile
      lockfile="$(find . -maxdepth 3 -name pubspec.lock -print -quit 2>/dev/null)"
      [ -n "$lockfile" ] && awk '/^  verdict_rules:/{found=1} found && /version:/{print $2; exit}' "$lockfile" | tr -d '"'
      ;;
    csharp)
      local csproj inline
      csproj="$(grep -rl 'PackageReference Include="VerdictRules"' --include='*.csproj' . 2>/dev/null | head -1)"
      [ -n "$csproj" ] || return 0
      inline="$(grep -m1 'PackageReference Include="VerdictRules"' "$csproj" | grep -oE 'Version="[^"]+"' | sed -E 's/Version="([^"]+)"/\1/')"
      if [ -n "$inline" ]; then
        printf '%s' "$inline"
      else
        # Central Package Management: the csproj's own PackageReference carries no
        # inline version -- it lives in a Directory.Packages.props instead.
        local props
        props="$(grep -rl 'PackageVersion Include="VerdictRules"' --include='Directory.Packages.props' . 2>/dev/null | head -1)"
        [ -n "$props" ] && grep -m1 'PackageVersion Include="VerdictRules"' "$props" | grep -oE 'Version="[^"]+"' | sed -E 's/Version="([^"]+)"/\1/'
      fi
      ;;
  esac
}

for arg in "$@"; do
  if [[ "$arg" == *=* ]]; then
    lang="${arg%%=*}" version="${arg#*=}"
  else
    lang="$arg"
    version="$(detect_version "$lang" || true)"
  fi

  if [ -z "$version" ]; then
    echo "skip ${lang}: could not detect an installed version from the current project"
    continue
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
