<!-- Title: Verdict Agent Memory -->
# `.agents/memory/` — agent memory

> The in-repo home for durable agent recall — the things an AI agent
> would otherwise write to a harness's own global memory store and lose
> the moment the session, machine, or tool changes. This is a
> deliberate repo-local override of the context-fence default
> (`docs/agent-memory/`): agent working material belongs under
> [`.agents/`](../README.md), not mixed in with this repo's published
> docs, which are written for human readers of the package.

## What belongs here

One file per durable fact or convention an agent learned and will need
again — preferences confirmed with the maintainer, non-obvious
constraints, conclusions that cost real work to reach. Name each file
for the fact it holds, in kebab-case, and open it with the same
blockquote framing every doc in this repo uses.

## What does not belong here

- **Standing rules** → [`AGENTS.md`](../../AGENTS.md), which every
  harness reads natively and `CLAUDE.md` imports.
- **Plans, specs, playbooks** → [`../plans/`](../plans/) — the sibling
  for work that's *going to happen*, as opposed to facts that are
  *already true*.
- **Session state** → [`HANDOFF.md`](../../HANDOFF.md), which is
  rewritten each session and carries its own staleness marker.
- **Anything already recorded by the repo itself** — the code, the git
  history, [`CHANGELOG.md`](../../CHANGELOG.md), or the docs under
  [`docs/`](../../docs/). Memory is for what those don't say.
- **Anything transient** — a scratch log, a one-off script, a note
  that stops mattering when this session ends. Those go in `scratch/`
  (gitignored) or `/tmp`.
