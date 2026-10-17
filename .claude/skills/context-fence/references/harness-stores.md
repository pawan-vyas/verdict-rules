# Redirecting a harness's own stores into the repo

> The boundary is easy to hold for files you chose to write. It leaks through features that write
> *for* you, on a path you didn't pick: plan modes, memory features, temp directories. This is the
> per-harness detail for closing those three — redirect where the harness allows it, and run the
> reconcile loop below where it doesn't.

Work through this during SETUP step 7, for whichever harness is actually running. Record what you did
in `.agents/fence.json` so the next session doesn't re-derive it.

## The two outcomes

**Redirect** — the harness exposes a setting, an env var, or a documented project-local path. Point it
at the in-repo home and you are done; the leak is closed structurally and nothing can drift.

**Reconcile** — the path is fixed and private. The harness will keep writing there no matter what, so
the fence is maintained by procedure instead of configuration. See the loop below.

## Per-harness

| Harness | Plans | Memory | Temp / scratch |
| :-- | :-- | :-- | :-- |
| **Claude Code** | Plan-mode output is conversational, not a file you own — write the accepted plan to `.agents/plans/` yourself once it is agreed. Reconcile. | Project memory is `CLAUDE.md`, already in-boundary. If a user-scope memory directory is in play, keep a pointer there and the fact in `.agents/memory/`. Redirect by convention. | Point work at `.agents/scratch/` explicitly; the session scratchpad it offers by default is outside. Reconcile. |
| **Cursor** | Project rules and notes live under `.cursor/`, in-boundary. Agent plans written elsewhere: reconcile. | `.cursor/rules/*.mdc` is in-boundary; keep facts in `.agents/memory/` and let the rule point at it. Redirect. | Reconcile. |
| **GitHub Copilot** | No separate plan store; work is in-conversation. Write agreed plans to `.agents/plans/`. Reconcile. | `.github/copilot-instructions.md` is in-boundary. Redirect. | Reconcile. |
| **Kiro** | Steering files under `.kiro/` are in-boundary. Redirect. | Same. Redirect. | Reconcile. |
| **Google Antigravity** | Workspace rules and workflows under `.agents/` are in-boundary — the plans home is already a sibling of them. Redirect. | `GEMINI.md` is the *global* layer and is out of boundary: keep a pointer there, the fact in `.agents/memory/`. Reconcile. | Reconcile. |
| **Anything else** | Assume reconcile until you have checked. A store you haven't confirmed is redirectable is one that is quietly accumulating outside the repo. | | |

The column that matters is not which harness you are in — it is whether a given store was redirected
or is on the reconcile loop. Treat an unchecked store as leaking.

## The reconcile loop

For every store that could not be redirected:

1. **Copy it in as soon as it has real content** — not at the end of the session, when the session may
   not get an end. The in-repo copy is authoritative from that moment.
2. **Make every later edit to the in-repo copy.** This is the step that actually fails in practice: it
   is easy to keep editing the harness's copy out of habit because that is where its own UI points, and
   the in-repo file rots into a stale fork of something nobody is reading.
3. **Leave a pointer in the harness's store** where it allows text at all — one line naming the in-repo
   path as the real location. Never a second copy of the content: duplicated material drifts, and the
   stale side is the one that gets read back in the session where it matters.
4. **Re-check the harness path during SWEEP and before HANDOFF.** Something will have been written
   there since — that is what "could not be redirected" means. Fold in anything new, and note in
   `HANDOFF.md` §5b which stores are on this loop, so the next session inherits the obligation instead
   of rediscovering it.

## When neither works

If a store cannot be redirected *and* cannot be read or copied from — a fully opaque memory feature,
for instance — say so explicitly rather than pretending the fence is intact. Record it in
`HANDOFF.md` §4 as a known leak with the harness named, and keep the durable content in
`.agents/memory/` regardless, so the in-repo copy stays the complete one even while the opaque store
holds a partial duplicate.
