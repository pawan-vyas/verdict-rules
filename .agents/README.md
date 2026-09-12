<!-- Title: Verdict Agent Working Directory -->
# `.agents/` — agent working material

> Everything an AI agent needs to work in this repo *and* everything it
> produces that outlives a single session, kept inside the repo
> boundary rather than in a harness's own global store. `.agents/` is
> the generic, harness-neutral location ([agentskills.io](https://agentskills.io/specification)
> convention) — Claude Code reads `.claude/` instead, which is why the
> skills here are mirrored to both. Nothing under this directory is
> part of the published `verdict` package.

## Layout

| Directory | Holds | Lifetime |
| :-- | :-- | :-- |
| [`memory/`](memory/) | Durable agent recall — facts, confirmed preferences, hard-won conclusions an agent will need again | Indefinite, until it stops being true |
| [`plans/`](plans/) | Multi-session plans, specs, playbooks, and design work in progress | Until the plan is executed or abandoned |
| [`skills/`](skills/) | Vendored operations manuals an agent follows (`mermaid-diagrams`, `context-fence`), mirrored to `.claude/skills/` | Tool-owned — refreshed wholesale, never hand-edited |
| [`scratch/`](scratch/) | Throwaway working material — research notes, logs, generated reports, scratch scripts | **Gitignored.** Until it has been read |
| [`incidents/`](incidents/) | Mistakes worth not repeating: what failed silently, why the setup allowed it, what prevents a repeat | Indefinite |

Add a sibling directory here when a genuinely new *kind* of durable
agent material appears — not a subdirectory of one that already
roughly fits. Each new sibling gets its own `README.md` saying what
belongs in it and what doesn't, and a row in the table above.

## What does not live here

- **Standing rules** → [`AGENTS.md`](../AGENTS.md) at the repo root,
  imported by `CLAUDE.md` and read natively by most other harnesses.
- **Session state** → [`HANDOFF.md`](../HANDOFF.md), rewritten each
  session and carrying its own staleness marker.
- **Published documentation** → [`docs/`](../docs/) and each
  language's own `docs/`. Those are written for human readers of the
  package; this directory is not.
- **Anything transient** → [`scratch/`](scratch/) (gitignored). If it
  stops mattering when the session ends, it does not belong here.

Everything under `.agents/` is committed, like every other durable note
in this repo. The discipline this layout implements is specified in
[`skills/context-fence/SKILL.md`](skills/context-fence/SKILL.md).
