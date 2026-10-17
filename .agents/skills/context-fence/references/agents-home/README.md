# `.agents/` — agent working material

> Everything an AI agent needs to work in this repo, and everything it produces that outlives a single
> session, kept inside the repo boundary instead of in whatever store the harness would have chosen.
> `.agents/` is the harness-neutral location — Claude Code reads `.claude/` instead, which is why the
> skills here are mirrored to both. Nothing under this directory is part of what this project ships.

## Layout

| Directory | Holds | Lifetime |
| :-- | :-- | :-- |
| `memory/` | Durable recall — facts, confirmed preferences, conclusions that cost real work to reach | Until it stops being true |
| `plans/` | Multi-session plans, specs, playbooks, design work in progress | Until the plan is executed or abandoned |
| `incidents/` | Failures worth not repeating, each ending in what prevents a repeat | Indefinite |
| `skills/` | Vendored operations manuals an agent follows, mirrored to `.claude/skills/` | Tool-owned — refreshed wholesale, never hand-edited |
| `scratch/` | Throwaway working material — research, logs, generated reports, scratch scripts | **Gitignored.** Until it has been read |
| `fence.json` | Which homes this repo uses and what it has already decided about them | Indefinite |

A genuinely new **kind** of durable agent material gets a new sibling directory here, with its own
`README.md` saying what belongs in it and what doesn't, plus a row in the table above — not a
subdirectory of whichever existing home is the closest fit.

## What does not live here

- **Standing rules** → `AGENTS.md` at the repo root, which every harness reads natively.
- **Session state** → `HANDOFF.md`, rewritten each session and carrying its own staleness marker.
- **Published documentation** → `docs/`. That is written for human readers of this project; this
  directory is not.
- **Anything transient** → `scratch/` (gitignored). If it stops mattering when the session ends, it
  does not belong in any of the others.

Everything else under `.agents/` is committed, like every durable note in this repo. The discipline
this layout implements is specified in `skills/context-fence/SKILL.md`.
