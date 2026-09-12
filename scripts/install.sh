#!/usr/bin/env bash
# Wire the verdict skill into one or more agentic coding harnesses, without needing Claude Code's
# plugin/marketplace mechanism (most harnesses don't have an equivalent). Supports both scopes a real
# install needs, mirroring what Claude Code's own marketplace already does natively (global plugin
# install vs. per-project vendoring) for harnesses that don't have that distinction built in yet:
#
#   --scope=project (default) — wire ONE repo. Vendors the skill into that repo (so it travels with
#     it — any collaborator, any harness, gets it) and writes that harness's own native rules-pointer
#     file into the repo. This is what you want for a collaborative project.
#   --scope=global — wire your own machine, once. Vendors the skill to your home directory, so EVERY
#     project you open in a skill-discovering harness (confirmed: Claude Code, Cursor) already has it
#     — no per-project step, nothing to commit. This is what you want if you're the only one working
#     on a given repo and don't want per-project vendoring cluttering it. `--harness`/`--dir` don't
#     apply here (see below).
#
# Usage:
#   scripts/install.sh --list
#   scripts/install.sh --harness=cursor [--dir=/path/to/repo] [--dry-run]
#   scripts/install.sh --harness=cursor,copilot,kiro
#   scripts/install.sh --harness=all
#   scripts/install.sh --scope=global [--dry-run]
#
# Table-driven, not a per-harness if/else ladder: valid harness names are discovered at runtime by
# listing scripts/harness-templates/ subdirectories. Adding a harness = adding a new template
# directory; this script never needs a code change for that.
#
# Two copy modes, chosen by which subdirectory a harness's files live under:
#   dropin/  — harness's native convention is "a directory of independently-named files"
#              (Cursor, Kiro, Antigravity, Cline, OpenHands). Safe to create a uniquely-named file
#              without touching anything else already there. Idempotent: skipped if the destination
#              already exists (never overwrites).
#   append/  — harness's native convention is a single shared file (Claude's CLAUDE.md, GitHub
#              Copilot's copilot-instructions.md) that may already hold unrelated content. Created
#              if absent; if present, the pointer is appended only if not already there (detected via
#              a small HTML-comment marker), never overwriting existing content.
#
# In project scope, the full skill payload (SKILL.md + references/, nested one subdirectory per
# language verdict ships for — today just references/python/) is vendored as an agentskills.io-
# conformant skill folder (see https://agentskills.io/specification) at BOTH
# .agents/skills/verdict/ (the generic convention — Cursor and other agentskills.io tools scan this)
# AND .claude/skills/verdict/ (Claude Code scans ONLY .claude/skills/, confirmed against
# code.claude.com/docs/en/skills — it does not read .agents/skills/ at all). Both are refreshed on
# every run. This is what makes the skill followable by ANY agent in ANY harness with zero
# installation. Per-harness dropin/append pointer files (native rules-pointer, where the harness
# supports one) are ergonomic layers on top of that, not a substitute.
#
# Global scope vendors to ~/.claude/skills/verdict/ and ~/.agents/skills/verdict/ — the only two
# home-directory skill-discovery paths confirmed by research (see docs/harnesses/cursor.md,
# code.claude.com/docs/en/skills). Other harnesses' global config (Kiro's ~/.kiro/steering/, pi's
# ~/.pi/agent/AGENTS.md, etc.) isn't a confirmed agentskills.io-style skill directory, so global
# scope doesn't attempt to write there — those harnesses still need per-project --scope=project
# wiring.
#
# This script runs in two different layouts and resolves its own skill source accordingly, so the
# same file works unmodified in both — no separate copy to keep in sync:
#   - Dev repo:            scripts/install.sh, with ../skills/verdict/ as the source.
#   - Standalone tools zip: install.sh at the package root, with ./skill/ as the source (see
#     scripts/build.sh, which packages dist/verdict-tools.zip in exactly this shape — download that
#     instead of cloning the whole repo if you only need the installer, not this repo's own docs/dev
#     material).
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
TEMPLATES_DIR="$SCRIPT_DIR/harness-templates"
if [ -d "$SCRIPT_DIR/skill" ]; then
  SKILL_SRC="$SCRIPT_DIR/skill"                                # standalone tools package
else
  SKILL_SRC="$(cd "$SCRIPT_DIR/.." && pwd)/skills/verdict"     # dev repo
fi
MARKER="verdict:marker"

SCOPE="project"
TARGET_DIR="."
DRY_RUN=0
HARNESSES=""
LIST_ONLY=0

usage() {
  cat <<'EOF'
Usage: install.sh --scope=project [--harness=<name[,name...]|all>] [--dir=PATH] [--dry-run]
       install.sh --scope=global [--dry-run]
       install.sh --list

  --scope=project  Wire one repo (default). Always vendors the skill into it (.agents/skills/ +
                   .claude/skills/); --harness additionally writes that harness's native pointer file(s).
  --scope=global   Wire your machine once. Vendors the skill to ~/.claude and ~/.agents skill dirs;
                   ignores --harness/--dir (only Claude Code and Cursor have a confirmed global path).
  --harness=NAME   One or more harness names (comma-separated), or "all". Optional — omit it to only
                   vendor the skill payload with no harness-specific pointer files.
  --dir=PATH       Target repo root for --scope=project. Defaults to the current directory.
  --dry-run        Print what would be created/appended without writing anything.
  --list           Print the available harness names and exit.
EOF
}

available_harnesses() {
  find "$TEMPLATES_DIR" -mindepth 1 -maxdepth 1 -type d -exec basename {} \; | sort
}

for arg in "$@"; do
  case "$arg" in
    --scope=*) SCOPE="${arg#--scope=}" ;;
    --harness=*) HARNESSES="${arg#--harness=}" ;;
    --dir=*) TARGET_DIR="${arg#--dir=}" ;;
    --dry-run) DRY_RUN=1 ;;
    --list) LIST_ONLY=1 ;;
    -h|--help) usage; exit 0 ;;
    *) echo "error: unknown argument: $arg" >&2; usage >&2; exit 1 ;;
  esac
done

if [ "$LIST_ONLY" -eq 1 ]; then
  available_harnesses
  exit 0
fi

if [ "$SCOPE" != "project" ] && [ "$SCOPE" != "global" ]; then
  echo "error: --scope must be 'project' or 'global', got '$SCOPE'" >&2
  usage >&2
  exit 1
fi

note() { printf '%s\n' "$*"; }
would() { if [ "$DRY_RUN" -eq 1 ]; then printf '[dry-run] %s\n' "$*"; fi; }

# Vendored skill copies are tool-owned: always refreshed, never hand-edited.
# vendor_one() does the actual copy into one absolute destination path.
vendor_one() {
  local dest="$1"
  would "vendor SKILL.md + references/ into $dest/ (refreshed if already present)"
  if [ "$DRY_RUN" -eq 0 ]; then
    # references/ nests one subdirectory per language (today: references/python/), and that set of
    # languages changes as new SDKs ship -- a plain `cp -r` only ever adds or overwrites, it never
    # removes, so a stale destination accumulates orphaned files a source removal should have cleaned
    # up. rm -rf + recreate makes this a real sync, not a merge.
    rm -rf "$dest/references"
    mkdir -p "$dest/references"
    cp "$SKILL_SRC/SKILL.md" "$dest/SKILL.md"
    cp -r "$SKILL_SRC/references/." "$dest/references/"
    # cp preserves an existing destination file's permission bits rather than resetting them, so an
    # already-present, wrongly-permissioned destination (e.g. a stray 600) would otherwise survive a
    # refresh. Force it explicitly every time instead of relying on umask defaults for new files only.
    # references/ nests real subdirectories -- a flat `chmod 644 references/*` would catch those
    # directories too and strip their execute/traverse bit (644 on a dir = not listable), so files and
    # directories need separate, recursive treatment, not one flat glob.
    chmod 644 "$dest/SKILL.md"
    find "$dest/references" -type d -exec chmod 755 {} +
    find "$dest/references" -type f -exec chmod 644 {} +
    note "vendored $dest/ (SKILL.md + references/)"
  fi
}

if [ "$SCOPE" = "global" ]; then
  if [ -n "$HARNESSES" ] || [ "$TARGET_DIR" != "." ]; then
    note "note: --harness/--dir are ignored in --scope=global (it always targets both confirmed global skill directories)."
  fi
  vendor_one "$HOME/.claude/skills/verdict"
  vendor_one "$HOME/.agents/skills/verdict"
  note "Done. Vendored globally — every project you open in Claude Code or Cursor now discovers verdict without any per-project step. Run --scope=project in a specific repo if you want it to travel with that repo for collaborators too (see docs/harnesses/README.md)."
  exit 0
fi

# --scope=project from here down. --harness is optional: omitting it just vendors the skill payload
# with no harness-specific pointer files.
if [ ! -d "$TARGET_DIR" ]; then
  echo "error: target directory does not exist: $TARGET_DIR" >&2
  exit 1
fi
TARGET_DIR="$(cd "$TARGET_DIR" && pwd)"

if [ "$HARNESSES" = "all" ]; then
  HARNESSES="$(available_harnesses | paste -sd, -)"
fi

vendor_payload() {
  if [ -f "$TARGET_DIR/skills/verdict/SKILL.md" ]; then
    note "Target already contains skills/verdict/SKILL.md (this is the verdict source repo) — skipping skill vendoring."
    return
  fi
  vendor_one "$TARGET_DIR/.agents/skills/verdict"
  vendor_one "$TARGET_DIR/.claude/skills/verdict"
}

copy_dropin_tree() {
  local harness="$1"
  local src_root="$TEMPLATES_DIR/$harness/dropin"
  [ -d "$src_root" ] || return 0
  while IFS= read -r -d '' file; do
    local relpath="${file#"$src_root"/}"
    local dest="$TARGET_DIR/$relpath"
    if [ -f "$dest" ]; then
      note "[$harness] $relpath already present — skipped."
      continue
    fi
    would "[$harness] create $relpath"
    if [ "$DRY_RUN" -eq 0 ]; then
      mkdir -p "$(dirname "$dest")"
      cp "$file" "$dest"
      note "[$harness] created $relpath"
    fi
  done < <(find "$src_root" -type f -print0)
}

append_tree() {
  local harness="$1"
  local src_root="$TEMPLATES_DIR/$harness/append"
  [ -d "$src_root" ] || return 0
  while IFS= read -r -d '' file; do
    local relpath="${file#"$src_root"/}"
    local dest="$TARGET_DIR/$relpath"
    if [ ! -f "$dest" ]; then
      would "[$harness] create $relpath"
      if [ "$DRY_RUN" -eq 0 ]; then
        mkdir -p "$(dirname "$dest")"
        cp "$file" "$dest"
        note "[$harness] created $relpath"
      fi
      continue
    fi
    if grep -qF "$MARKER" "$dest" 2>/dev/null; then
      note "[$harness] $relpath already has the verdict pointer — skipped."
      continue
    fi
    would "[$harness] append verdict pointer to $relpath"
    if [ "$DRY_RUN" -eq 0 ]; then
      { printf '\n'; cat "$file"; } >> "$dest"
      note "[$harness] appended verdict pointer to $relpath"
    fi
  done < <(find "$src_root" -type f -print0)
}

vendor_payload

if [ -n "$HARNESSES" ]; then
  known="$(available_harnesses)"
  IFS=',' read -ra requested <<< "$HARNESSES"

  for h in "${requested[@]}"; do
    h="$(printf '%s' "$h" | xargs)"
    if ! printf '%s\n' "$known" | grep -qxF "$h"; then
      echo "error: unknown harness '$h'. Valid names:" >&2
      printf '%s\n' "$known" >&2
      exit 1
    fi
  done

  for h in "${requested[@]}"; do
    h="$(printf '%s' "$h" | xargs)"
    copy_dropin_tree "$h"
    append_tree "$h"
  done
else
  note "No --harness given — vendored the skill only (.agents/skills/verdict/ and .claude/skills/verdict/), no harness-specific pointer files written. Pass --harness=<name[,name...]|all> to also wire native rules files for a specific harness."
fi

note "Done. The vendored skill (.agents/skills/verdict/ and .claude/skills/verdict/) carries the workflow, the execution-model guarantees and the extension recipes locally — enough to write correct verdict code with no network. Deeper material (the testing guide, quickstart, samples and worked example) is pulled on demand at the version a project has installed; see the skill's own SKILL.md. Per-harness pointers wire up richer native surfaces where one exists."
