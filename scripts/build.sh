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
# skills/verdict/ in git holds everything hand-written and bundled — SKILL.md
# and each language's own agent-notes.md. The one generated piece is
# REPOSITORY-MAP.md, built here from the repository's own docs/ so its
# one-line descriptions cannot drift from what those documents actually say.
# Nothing else is vendored: docs/extending/, docs/samples/, and
# docs/architecture/ stay in the source repository, named but not copied.
skill_src="$stage/skill-src"
mkdir -p "$skill_src"
cp -r skills/verdict/. "$skill_src/"
python3 scripts/generate_repository_map.py "$skill_src/references/REPOSITORY-MAP.md"

# Validate the assembled bundle before packaging it: SKILL.md and every
# agent-notes.md must not route to a references/ path the bundle does not
# contain, and links between bundled documents must resolve. A script rather
# than an inline block — shell nested inside a generated file is how
# .agents/incidents/005 happened.
python3 scripts/check_skill_bundle.py "$skill_src"

echo "skill assembled"

# 1) plugin package — top-level verdict/ so it extracts cleanly into a skills dir.
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
#    recursive since references/ nests one subdirectory per language). install.sh resolves its own
#    skill source by checking for a sibling ./skill/ dir first (this package's shape) before
#    falling back to ../skills/verdict/ (the dev-repo shape) — see install.sh's own header comment.
mkdir -p "$stage/tools/verdict-tools/skill/references"
cp scripts/install.sh "$stage/tools/verdict-tools/install.sh"
chmod +x "$stage/tools/verdict-tools/install.sh"
cp -r scripts/harness-templates "$stage/tools/verdict-tools/harness-templates"
cp "$skill_src/SKILL.md" "$stage/tools/verdict-tools/skill/SKILL.md"
cp -r "$skill_src/references/." "$stage/tools/verdict-tools/skill/references/"
( cd "$stage/tools" && zip -r -q "$ROOT/dist/verdict-tools.zip" verdict-tools -x '*/.DS_Store' )

echo "built:"
ls -1 dist
