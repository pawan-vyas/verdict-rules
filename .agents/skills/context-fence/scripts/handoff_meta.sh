#!/usr/bin/env bash
# Print the session-handoff frontmatter field values for the current git repo.
# Run from anywhere inside the repo:  bash scripts/handoff_meta.sh
# state_at_commit = current HEAD (the commit that writes HANDOFF.md will be its child, so this
# stays N-1 — the marker the freshness check compares against).
set -euo pipefail

if ! git rev-parse --is-inside-work-tree >/dev/null 2>&1; then
  echo "error: not inside a git repository" >&2
  exit 1
fi

printf 'state_at_commit: %s\n'        "$(git rev-parse HEAD)"
printf 'state_at_commit_short: %s\n'  "$(git rev-parse --short HEAD)"
printf 'branch: %s\n'                 "$(git rev-parse --abbrev-ref HEAD)"
printf 'updated_utc: %s\n'            "$(date -u +%Y-%m-%dT%H:%M:%SZ)"
# updated_local uses the system timezone; the numeric offset names the zone (e.g. +05:30 = IST).
# %:z (colon-separated offset) is a GNU-date-only extension — on BSD/macOS date it silently prints
# the literal characters ":z" instead of an offset. %z (no colon) is portable to both; insert the
# colon ourselves so the output still matches the documented ISO-8601 ±hh:mm frontmatter schema.
tz_offset="$(date +%z)"
tz_offset="${tz_offset:0:3}:${tz_offset:3:2}"
printf 'updated_local: %s\n'          "$(date +%Y-%m-%dT%H:%M:%S)${tz_offset}"

# Optional context to eyeball while writing the body:
if remote="$(git remote get-url origin 2>/dev/null)"; then
  printf '# remote: %s\n' "$remote"
fi
printf '# tree_clean: %s\n' "$([ -z "$(git status --porcelain)" ] && echo yes || echo NO)"
