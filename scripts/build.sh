#!/usr/bin/env bash
# Build the distributable artifacts from source:
#   dist/verdict-plugin.zip  — the Claude Code plugin (skills-dir / manual install)
#   dist/verdict.skill       — the standalone skill (Cowork / Save-skill)
#   dist/verdict-tools.zip   — install.sh + harness-templates/ + the vendorable skill payload, for
#                              every OTHER harness. Download+unzip this instead of cloning the whole
#                              repo when all you need is to run the installer.
# The repo itself remains the canonical install (marketplace or skills-dir clone); these are
# conveniences, published as GitHub Release assets tagged to plugin.json's version.
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"
rm -rf dist && mkdir -p dist
stage="$(mktemp -d)"
trap 'rm -rf "$stage"' EXIT

# 1) plugin package — top-level verdict/ so it extracts cleanly into a skills dir.
# No commands/ dir exists for this skill (no session-state operations to invoke).
mkdir -p "$stage/plugin/verdict"
cp -r .claude-plugin skills "$stage/plugin/verdict/"
# the plugin package needs only plugin.json, not the marketplace manifest
rm -f "$stage/plugin/verdict/.claude-plugin/marketplace.json"
# the plugin package ships the skill only, not the eval-loop working material alongside it
rm -rf "$stage/plugin/verdict/skills/verdict-workspace"
( cd "$stage/plugin" && zip -r -q "$ROOT/dist/verdict-plugin.zip" verdict -x '*/.DS_Store' )

# 2) standalone .skill — just the skill directory.
mkdir -p "$stage/skill"
cp -r skills/verdict "$stage/skill/verdict"
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
cp skills/verdict/SKILL.md "$stage/tools/verdict-tools/skill/SKILL.md"
cp -r skills/verdict/references/. "$stage/tools/verdict-tools/skill/references/"
( cd "$stage/tools" && zip -r -q "$ROOT/dist/verdict-tools.zip" verdict-tools -x '*/.DS_Store' )

echo "built:"
ls -1 dist
