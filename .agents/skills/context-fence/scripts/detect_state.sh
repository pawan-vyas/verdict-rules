#!/usr/bin/env bash
# Classify how far a directory is into the context-fence discipline, for the RESUME operation to
# dispatch on. Run from anywhere inside the target repo/dir:  bash scripts/detect_state.sh
#
# Reports facts only — no policy. RESUME (in SKILL.md) owns the state -> operation dispatch table;
# this script just tells it what's true on disk so that table stays the single place the policy lives.
set -uo pipefail

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

printf 'is_git: %s\n'         "$is_git"
printf 'boundary: %s\n'       "$boundary"
printf 'has_agents_md: %s\n'  "$has_agents_md"
printf 'has_handoff_md: %s\n' "$has_handoff_md"
printf 'state: %s\n'          "$state"
