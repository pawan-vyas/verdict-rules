---
kind: session-handoff
handoff_schema: 1
updated_utc: <FILL: 2026-01-01T00:00:00Z>
updated_local: <FILL: 2026-01-01T05:30:00+05:30>
branch: <FILL: main>
state_at_commit: <FILL: full sha of current HEAD>
state_at_commit_short: <FILL: short sha>
# Freshness: run `git log --oneline "$(git log -1 --format=%H -- HANDOFF.md)"..HEAD`. Empty (+ clean
# tree) = current. Non-empty = stale — reconcile per §0.1 before trusting §2–§3. (Comparing against
# state_at_commit directly always shows the handoff commit itself as "drift" — see §0.1.)
---

# <PROJECT> — session handoff / resume brief

> The **migratable state container** for this project: session-to-session, not cross-session. It may
> be freely rewritten when a new session/tool takes over (see §0.1). Standing rules live in
> `CLAUDE.md` (and `AGENTS.md` / `.github/copilot-instructions.md` for other assistants). Verify
> against the code; the source of truth for what's built is <FILL: docs/… or the code itself>.

## 0 · How to use this file

You (the next agent) are continuing work on **<PROJECT>**. Read §1 for what it is, §2 for where we
are, §3 for what to do next, §4 for known issues, §5 for how to verify. Everything is committed on
`<branch>`.

**Keep every durable note in the repo** — plans, specs, decisions, handoffs go in `docs/` /
`HANDOFF.md` / `CLAUDE.md` / `.claude/`, committed; never in `~/.claude/`, home, or `/tmp`. Transient
logs/scripts → `scratch/` (gitignored). Update this file at session end so the next one resumes from
the repo alone.

## 0.1 · Freshness & alignment protocol (read before trusting §2–§3)

The frontmatter is a staleness marker. `state_at_commit` is the HEAD this body describes; the commit
that wrote this file is its child, so its own hash isn't self-recorded. **Don't diff against
`state_at_commit` directly** — the range `state_at_commit..HEAD` always contains at least the handoff
commit itself (the one that added this file), so it would read "stale by 1" the instant this file is
written, even with zero drift. Diff against the commit that last touched `HANDOFF.md` instead — that
range is empty exactly when nothing has happened since:

```
git log --oneline "$(git log -1 --format=%H -- HANDOFF.md)"..HEAD
git status --short
```

- **Empty log + clean tree** → current; proceed with §2–§3.
- **Non-empty, or dirty tree** → the repo moved. Do NOT trust §2–§3 blindly. Reconcile — same
  procedure whether you're mid-session (RESUME found drift) or migrating to a fresh session: (1) read
  `git log -p "$(git log -1 --format=%H -- HANDOFF.md)"..HEAD` + `git diff` to see what actually
  happened; (2) sweep any external or uncommitted context back into the repo; (3) rewrite §2–§3 to
  reality; (4) update the frontmatter — new `updated_*` and `state_at_commit` = current HEAD *before*
  committing; (5) commit (its parent = the recorded `state_at_commit`, keeping the marker N-1 again).
  Reconciling **always** bumps the frontmatter and commits — don't leave a reconciled body sitting
  next to a frontmatter that now understates what §2 describes; that's a worse state than the
  staleness it was fixing. Standing rules never go here.

## 1 · What this project is (one paragraph)

<FILL: one-paragraph overview + layout + the source-of-truth pointer.>

## 1b · External references (NOT part of this repo)

<FILL, or "none": sibling repos / mounted folders the repo doesn't contain — path + purpose + how
they relate. This is the context a fresh session or a different tool will NOT inherit.>

## 2 · Where we are (this session's work)

<FILL: what changed, grouped, with the commit range e.g. `abc1234..def5678`.>

## 3 · What to do next (prioritized)

<FILL: ordered next steps; each with the EXACT commands to run.>

## 4 · Known issues / blockers

<FILL: each with repro + the fix or workaround. Mark which are pre-existing vs introduced.>

## 5 · Verify (gate / test commands)

```
<FILL: the exact gate commands — build, lint, type, test.>
```

## 5b · Tooling / skills

<FILL: interpreters + versions, key deps, environment gotchas (sandbox vs host), and any
skills/plugins relied on.>

## 6 · Key docs

<FILL: pointers into docs/ — the source of truth, specs, plans.>
