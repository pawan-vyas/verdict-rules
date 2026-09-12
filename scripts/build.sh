#!/usr/bin/env bash
# Build the distributable artifacts from source:
#   dist/verdict-plugin.zip  — the Claude Code plugin (skills-dir / manual install)
#   dist/verdict.skill       — the standalone skill (Cowork / Save-skill)
#   dist/verdict-tools.zip   — install.sh + harness-templates/ + the vendorable skill payload, for
#                              every OTHER harness. Download+unzip this instead of cloning the whole
#                              repo when all you need is to run the installer.
# The repo itself remains the canonical install (marketplace or skills-dir clone); these are
# conveniences, published as GitHub Release assets under whatever tag triggered the release
# (today: python-vX.Y.Z); the asset filenames carry no version. plugin.json's own version is a
# Claude Code marketplace signal for whether a vendored skill copy needs updating — it tracks the
# skill's content, not any language's release, and the other harnesses' vendoring never reads it.
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"
rm -rf dist && mkdir -p dist
stage="$(mktemp -d)"
trap 'rm -rf "$stage"' EXIT

# Assemble the skill into the staging area, never in place.
#
# skills/verdict/ in git holds only hand-written content. The documents the
# skill ships are copied here from the repository's own docs/, so there is one
# copy of each fact and nothing to keep in sync. Doing it in staging rather
# than in the source tree matters: all three artifacts below copy
# skills/verdict/ wholesale, and build output living inside a source directory
# is how unrelated files end up in a package (see .agents/incidents/002).
skill_src="$stage/skill-src"
mkdir -p "$skill_src"
cp -r skills/verdict/. "$skill_src/"

# The bundled tier, from MANIFEST. A trailing slash on both sides copies a
# directory. Anything marked `fetch` is deliberately absent — SKILL.md explains
# how it is pulled on demand, pinned to the consumer's installed version.
bundled=0
while IFS="$(printf '\t')" read -r tier src dst; do
  case "$tier" in ''|'#'*) continue;; esac
  [ "$tier" = "bundled" ] || continue
  target="$skill_src/references/$dst"
  mkdir -p "$(dirname "$target")"
  if [ "${src%/}" != "$src" ]; then
    mkdir -p "$target"
    cp -r "$src". "$target"
  else
    cp "$src" "$target"
  fi
  bundled=$((bundled + 1))
done < skills/verdict/MANIFEST

[ "$bundled" -gt 0 ] || { echo "error: MANIFEST declared no bundled documents." >&2; exit 1; }

# Validate the assembled bundle before packaging it: SKILL.md must not route
# to anything the manifest omitted, and links between bundled documents must
# resolve where the manifest put them. A script rather than an inline block —
# shell nested inside a generated file is how .agents/incidents/005 happened.
python3 scripts/check_skill_bundle.py "$skill_src" skills/verdict/MANIFEST

echo "skill assembled: $bundled bundled document(s)"

# 1) plugin package — top-level verdict/ so it extracts cleanly into a skills dir.
# commands/ carries the one slash-command surface this skill has: fetching the
# documents the bundle deliberately does not carry. Harnesses without commands
# use the equivalent recipe in references/<language>/agent-notes.md.
mkdir -p "$stage/plugin/verdict"
cp -r .claude-plugin "$stage/plugin/verdict/"
mkdir -p "$stage/plugin/verdict/skills"
cp -r "$skill_src" "$stage/plugin/verdict/skills/verdict"
# the plugin package needs only plugin.json, not the marketplace manifest
rm -f "$stage/plugin/verdict/.claude-plugin/marketplace.json"
# the plugin package ships the skill only, not the eval-loop working material alongside it
rm -rf "$stage/plugin/verdict/skills/verdict-workspace"
( cd "$stage/plugin" && zip -r -q "$ROOT/dist/verdict-plugin.zip" verdict -x '*/.DS_Store' )

# 2) standalone .skill — just the skill directory.
mkdir -p "$stage/skill"
cp -r "$skill_src" "$stage/skill/verdict"
( cd "$stage/skill" && zip -r -q "$ROOT/dist/verdict.skill" verdict -x '*/.DS_Store' )

# 3) standalone tools package — install.sh + harness-templates/ + skill/ (SKILL.md + references/,
#    recursive since references/ nests one subdirectory per language — today just python/). No
#    scripts/*.js exist for this skill (unlike mermaid-diagrams' bundled validator), so nothing to
#    copy there. install.sh resolves its own skill source by checking for a sibling ./skill/ dir
#    first (this package's shape) before falling back to ../skills/verdict/ (the dev-repo shape) —
#    see install.sh's own header comment.
mkdir -p "$stage/tools/verdict-tools/skill/references"
cp scripts/install.sh "$stage/tools/verdict-tools/install.sh"
chmod +x "$stage/tools/verdict-tools/install.sh"
cp -r scripts/harness-templates "$stage/tools/verdict-tools/harness-templates"
cp "$skill_src/SKILL.md" "$stage/tools/verdict-tools/skill/SKILL.md"
cp "$skill_src/MANIFEST" "$stage/tools/verdict-tools/skill/MANIFEST"
cp -r "$skill_src/references/." "$stage/tools/verdict-tools/skill/references/"
( cd "$stage/tools" && zip -r -q "$ROOT/dist/verdict-tools.zip" verdict-tools -x '*/.DS_Store' )

echo "built:"
ls -1 dist
