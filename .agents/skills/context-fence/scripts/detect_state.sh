#!/usr/bin/env bash
# Classify how far a directory is into the context-fence discipline, for the RESUME operation to
# dispatch on. Run from anywhere inside the target repo/dir:  bash scripts/detect_state.sh
#
# Reports facts only — no policy. RESUME (in SKILL.md) owns the state -> operation dispatch table and
# UPGRADE owns what to do about a version gap; this script just tells them what's true on disk so
# those tables stay the single place each policy lives.
set -uo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SKILL_MD="$SCRIPT_DIR/../SKILL.md"

if git rev-parse --is-inside-work-tree >/dev/null 2>&1; then
  is_git=yes
  boundary="$(git rev-parse --show-toplevel)"
else
  is_git=no
  boundary="$(pwd)"
fi

has_agents_md=no
has_handoff_md=no
[ -f "$boundary/AGENTS.md" ] && has_agents_md=yes
[ -f "$boundary/HANDOFF.md" ] && has_handoff_md=yes

if [ "$is_git" = no ]; then
  state=NON_GIT
elif [ "$has_agents_md" = yes ] && [ "$has_handoff_md" = yes ]; then
  state=COMPLIANT
elif [ "$has_agents_md" = no ] && [ "$has_handoff_md" = no ]; then
  state=NON_COMPLIANT
else
  state=PARTIAL
fi

# --- version facts (for UPGRADE) -------------------------------------------------------------------
# skill_version: this skill payload's own metadata.version, read from the SKILL.md beside this script.
# applied_version: what .agents/fence.json says the repo was last fenced with; "none" if absent, which
# means the repo predates fence.json (pre-0.4.0) rather than that it is current.
skill_version="$(sed -n '/^metadata:/,/^[^ ]/p' "$SKILL_MD" 2>/dev/null \
  | sed -n 's/^[[:space:]]*version:[[:space:]]*"\{0,1\}\([^"]*\)"\{0,1\}[[:space:]]*$/\1/p' \
  | head -n 1)"
[ -n "$skill_version" ] || skill_version=unknown

# handoff_sealed: whether HANDOFF.md's body was actually populated, or merely scaffolded by
# install.sh (which can wire a repo but cannot know its state). A `handoff_schema: 1` file has no
# such field, and absent is defined as sealed: the field postdates every handoff written without it,
# and all of those were real. Reported as `none` when there is no HANDOFF.md to inspect at all, which
# is a different fact from an unsealed one and dispatches differently.
handoff_sealed=none
if [ "$has_handoff_md" = yes ]; then
  sealed_field="$(sed -n '/^---[[:space:]]*$/,/^---[[:space:]]*$/p' "$boundary/HANDOFF.md" 2>/dev/null \
    | sed -n 's/^[[:space:]]*sealed:[[:space:]]*\([A-Za-z]*\).*$/\1/p' | head -n 1)"
  case "$sealed_field" in
    false|no|False|FALSE|No|NO) handoff_sealed=no ;;
    *)                          handoff_sealed=yes ;;
  esac
fi

fence_json="$boundary/.agents/fence.json"
if [ -f "$fence_json" ]; then
  applied_version="$(sed -n 's/.*"fence_version"[[:space:]]*:[[:space:]]*"\([^"]*\)".*/\1/p' "$fence_json" | head -n 1)"
  [ -n "$applied_version" ] || applied_version=unknown
else
  applied_version=none
fi

# Numeric-field comparison rather than `sort -V`, which isn't portable across the BSD/GNU split this
# script has to run on unchanged.
version_lt() {
  awk -v a="$1" -v b="$2" '
    function part(v, i,   n, arr) { n = split(v, arr, "."); return (i <= n ? arr[i] + 0 : 0) }
    BEGIN {
      for (i = 1; i <= 4; i++) {
        if (part(a, i) < part(b, i)) { print "yes"; exit }
        if (part(a, i) > part(b, i)) { print "no";  exit }
      }
      print "no"
    }'
}

# An unfenced repo has no pending upgrade: ADOPT will fence it at the current version outright.
if [ "$state" = NON_COMPLIANT ] || [ "$state" = NON_GIT ]; then
  pending_upgrade=no
elif [ "$applied_version" = unknown ]; then
  # A fence.json with no readable fence_version: hand-edited, truncated, or written by something that
  # didn't follow the schema. Report it as pending rather than unknown — the cost of asking once more
  # is a question, and the cost of staying quiet is that nothing ever asks again.
  pending_upgrade=yes
elif [ "$skill_version" = unknown ]; then
  # Our own payload is unreadable, so there is nothing to compare against. This one genuinely is
  # unknown: claiming "pending" would invite an upgrade to a version we cannot name.
  pending_upgrade=unknown
elif [ "$applied_version" = none ]; then
  pending_upgrade=yes
else
  pending_upgrade="$(version_lt "$applied_version" "$skill_version")"
fi

printf 'is_git: %s\n'          "$is_git"
printf 'boundary: %s\n'        "$boundary"
printf 'has_agents_md: %s\n'   "$has_agents_md"
printf 'has_handoff_md: %s\n'  "$has_handoff_md"
printf 'state: %s\n'           "$state"
printf 'handoff_sealed: %s\n'  "$handoff_sealed"
printf 'skill_version: %s\n'   "$skill_version"
printf 'applied_version: %s\n' "$applied_version"
printf 'pending_upgrade: %s\n' "$pending_upgrade"
