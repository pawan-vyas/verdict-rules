#!/usr/bin/env sh
# The curl-pipeable facade for scripts/install.sh. Downloads the latest verdict-tools.zip release
# asset (not a repo clone), unzips it to a temp dir, and runs its install.sh. Interactive by default
# (prompts for scope, then harness/target if project scope); pass flags through to skip the prompts
# entirely, same as calling install.sh directly:
#
#   curl -fsSL https://raw.githubusercontent.com/pawan-vyas/verdict-rules/main/scripts/get.sh | sh
#   curl -fsSL .../get.sh | sh -s -- --scope=global
#   curl -fsSL .../get.sh | sh -s -- --harness=cursor,copilot --dir=/path/to/repo
#
# POSIX sh for the download/extract phase (curl | sh shouldn't assume bash is what's piping into);
# install.sh itself is bash (arrays, etc.), so it's always invoked via an explicit `bash` call below,
# not sourced or run via the shebang alone.
set -eu

REPO="pawan-vyas/verdict-rules"
API_URL="https://api.github.com/repos/$REPO/releases/latest"

# Reads a line from the real terminal even when this script's own stdin is the curl pipe. Falls
# back to plain stdin if /dev/tty isn't actually usable (not just present) — some sandboxed/detached
# exec environments have a /dev/tty node that fails to open despite passing a bare -r check.
prompt() {
  # $1 = prompt text, result left in $REPLY
  printf '%s' "$1" >&2
  if ! { [ -r /dev/tty ] && read -r REPLY < /dev/tty; } 2>/dev/null; then
    read -r REPLY
  fi
}

need() {
  command -v "$1" >/dev/null 2>&1 || { echo "error: '$1' is required but not found on PATH." >&2; exit 1; }
}

need curl
need unzip
need bash

tmpdir="$(mktemp -d)"
trap 'rm -rf "$tmpdir"' EXIT INT TERM

echo "verdict: fetching latest release info..." >&2
download_url="$(
  curl -fsSL "$API_URL" \
    | grep '"browser_download_url"' \
    | grep 'verdict-tools\.zip' \
    | sed -E 's/.*"browser_download_url": *"([^"]+)".*/\1/' \
    | head -n1
)"

if [ -z "$download_url" ]; then
  echo "error: couldn't find a verdict-tools.zip asset on the latest GitHub release." >&2
  echo "       Either no release has been cut yet, or the asset was renamed — check:" >&2
  echo "       https://github.com/$REPO/releases" >&2
  exit 1
fi

echo "verdict: downloading $download_url" >&2
curl -fsSL "$download_url" -o "$tmpdir/verdict-tools.zip"
unzip -q "$tmpdir/verdict-tools.zip" -d "$tmpdir"

install_sh="$tmpdir/verdict-tools/install.sh"
if [ ! -f "$install_sh" ]; then
  echo "error: downloaded archive didn't contain install.sh at the expected path." >&2
  exit 1
fi

# Non-interactive: any argument given to this script is passed straight through.
if [ "$#" -gt 0 ]; then
  exec bash "$install_sh" "$@"
fi

# Interactive: ask what's wanted rather than assuming.
echo "" >&2
echo "verdict installer" >&2
echo "" >&2
echo "  1) Global  — wire this machine once. Every project you open in Claude Code or Cursor" >&2
echo "               already has it; nothing written into any project." >&2
echo "  2) Project — wire one repo, for a project other people collaborate on. Travels with" >&2
echo "               the repo (committed) so any collaborator, in any harness, gets it too." >&2
echo "" >&2
prompt "Choose [1/2]: "
choice="$REPLY"

case "$choice" in
  1)
    exec bash "$install_sh" --scope=global
    ;;
  2)
    echo "" >&2
    echo "Available harnesses:" >&2
    bash "$install_sh" --list | sed 's/^/  - /' >&2
    echo "" >&2
    prompt "Harness(es), comma-separated, or 'all': "
    harness="$REPLY"
    prompt "Target repo path: "
    dir="$REPLY"
    exec bash "$install_sh" --harness="$harness" --dir="$dir"
    ;;
  *)
    echo "error: expected 1 or 2, got '$choice'." >&2
    exit 1
    ;;
esac
