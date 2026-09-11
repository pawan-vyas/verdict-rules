---
name: context-fence
description: >-
  Keep ALL durable agent context — memory, standing instructions, plans, specs, decisions, tasks,
  session handoffs — inside the repo boundary, so a model never scatters it into harness defaults
  (global config, chat-only, /tmp) and loses it across sessions or tools. Five operations: RESUME
  (start-of-session front door — detects repo state, dispatches, then surfaces what's next), SETUP
  (raise the fence + resume from any handoff), ADOPT (one-time retrofit of a pre-fence repo), SWEEP
  (gather escaped context back in), HANDOFF (seal session state at the end). Use at the START of
  work in any repo — especially "set up / resume / continue / pick up where we left off / onboard
  this repo / sweep context / hand off / wrap up / migrate" — and before switching sessions or
  assistants. Tool-neutral (plain files + git); works across Claude Code, Copilot, Cursor, and any
  agent that reads AGENTS.md. The value is the invisible perimeter, not any one artifact.
---

# context-fence

A model dropped into a repo runs on **harness defaults**: unless told otherwise it will scatter
durable context — memory, decisions, plans, tasks, handoffs — wherever the harness puts it (a global
store, the chat log only, `/tmp`). By the time anyone "hands off", the context has already leaked and
can't be recovered. **context-fence** is the discipline that raises an invisible perimeter at the
*start* of work so every durable note lands **inside the directory boundary**, and keeps it there.

- **The boundary is the directory** (the repo if git-inited, else the working dir). Git is version
  control layered on top — useful for the freshness check — **not** the fence itself.
- **The value is the perimeter, not the handoff artifact.** The handoff is just the exit seal of a
  discipline that must start at entry.

## The invariant (enforce it everywhere)

> Every **durable** note — memory, standing rules, plans, specs, decisions, tasks, discussion
> conclusions, session handoffs — lives **inside the boundary**, committed (or at least on disk in
> the dir): `docs/`, `HANDOFF.md`, `AGENTS.md` (+ each tool's native rules file), `.agents/skills/context-fence/`
> (the vendored operations manual — see Operation SETUP step 4), `.claude/`. **Never** write durable
> notes to `~/.claude/`, the home dir, a global memory store, or `/tmp`. Only **transient** logs and
> throwaway scripts may live outside — under `scratch/` (gitignored) or `/tmp`.

Three homes, one rule of thumb: **standing rules → `AGENTS.md`; the full manual → `.agents/skills/context-fence/`;
session state → `HANDOFF.md`.**
`AGENTS.md` is the cross-tool canonical rules file — read natively by Claude Code, GitHub Copilot,
Cursor, Google Antigravity, Codex, Windsurf, Zed, Jules and 20+ others (Linux Foundation standard).
Each tool's *richer native* rules layer on top and should point at it: `CLAUDE.md` (Claude),
`.github/copilot-instructions.md` (Copilot), `.cursor/rules/*.mdc` (Cursor), and for Google
Antigravity a workspace rule at `.agents/rules/context-fence.md` (Always On; `GEMINI.md` is
Antigravity's *global* layer, not a repo file). Keep the `AGENTS.md` fence section compact (several
tools cap rules files at ~12k chars) and use `@relative/path` imports for detail — `.agents/rules/`
and `CLAUDE.md` both support `@` imports. Raw conversation logs are exempt (harness's, not durable).

---

## Operation RESUME — start-of-session front door (run this first)

Run at the very start of a session, or on `/fence-resume`. This is the recommended entry point: rather
than making you (or the user) already know whether a repo needs `ADOPT`, `SETUP`, or just a freshness
check, RESUME detects which and dispatches — then, if the repo is already fenced, surfaces what to do
next instead of silently confirming "fresh" and stopping. Idempotent — safe to re-run.

1. **Classify.** Run the bundled `scripts/detect_state.sh` (it sits next to this SKILL.md; in plugin
   form: `${CLAUDE_PLUGIN_ROOT}/skills/context-fence/scripts/detect_state.sh`) from the target repo.
   It reports `is_git`, `boundary`, `has_agents_md`, `has_handoff_md`, and a `state`. The script only
   reports facts — the dispatch policy below is the single place that decision lives, so it stays easy
   to change without touching the script.
2. **Dispatch by `state`** (an exhaustive table, not a chain of ifs — every state maps to exactly one
   next action):

   | `state` | Meaning | Action |
   | :-- | :-- | :-- |
   | `NON_GIT` | no git boundary | Same as SETUP step 1: ask about `git init` (or proceed with the directory as the boundary if you can't ask), then re-run classification. |
   | `NON_COMPLIANT` | no `AGENTS.md` and no `HANDOFF.md` | Run **ADOPT**, always — even on a repo that looks empty. ADOPT's discovery pass is cheap insurance (it degrades to "nothing to sweep" on a genuinely fresh repo) and it's the only path of the two that's guaranteed to never miss or clobber scattered pre-existing context on a populated one. |
   | `PARTIAL` | some fence files present, not all (e.g. `AGENTS.md` exists, `HANDOFF.md` doesn't) | Run **SETUP** — idempotent, fills in what's missing without touching what's already there. |
   | `COMPLIANT` | `AGENTS.md` + `HANDOFF.md` both present | Skip straight to `HANDOFF.md`'s own §0.1 freshness check; reconcile first if it reports stale. No need to re-scaffold anything. |

3. **Land ready, not just "fenced."** Once the dispatched step finishes, read `HANDOFF.md` §2 (where
   things are) and §3 (what to do next) and report them. RESUME's job is done when the next concrete
   step is in front of you — not merely when the files exist.

If a `HANDOFF.md` you're resuming from doesn't actually contain a §0.1 section (hand-edited, migrated
from an older schema, written by a tool that didn't fully follow the template) — check its frontmatter
comment first (it carries the same freshness command); if that's missing too, compute the check
directly from the formula in this SKILL.md's HANDOFF section rather than treating the absence as
blocking. A missing §0.1 is a reason to be extra careful reconciling, not a reason to skip the check.

---

## Operation SETUP — raise the fence + resume (start of work in a repo)

Run to (re)raise the fence directly, or to repair/complete scaffolding — RESUME calls this
automatically when it detects a `PARTIAL` state, and you can also run it directly (or on
`/fence-setup`) any time you want to verify or fix the scaffolding without RESUME's dispatch logic.
Idempotent — safe to re-run.

1. **Find the boundary.** `git rev-parse --is-inside-work-tree`.
   - Inside a git repo → the repo root is the boundary.
   - **Not** a git repo → the working directory is the boundary; git's VCS/freshness layer is
     unavailable. If your harness can ask the user, ask: *"No git repo here — `git init` so the fence
     gets version control + handoff freshness checks, or proceed with the directory as the
     boundary?"* If it cannot ask, **default to proceeding with the directory as the boundary** and
     print a one-line warning that there's no VCS/freshness layer.
2. **Resume if there's prior state.** If `HANDOFF.md` exists, follow its own §0.1 freshness protocol
   (reconcile before trusting §2–§3 if it reports stale) — see **RESUME** above for the recommended
   entry point, which runs this same check and then also surfaces what to do next.
3. **Install the rule (AGENTS.md canonical).** Ensure `AGENTS.md` exists at the repo root and carries
   a compact "Working notes stay in the repo" section (the invariant) plus a one-liner: *"resume from
   `HANDOFF.md`; honor its §0.1; the full operations spec is vendored at `.agents/skills/context-fence/SKILL.md`"*
   (see step 4 — point at the **local** copy, not an external URL: a harness with no internet access
   and no plugin installed must still be able to read the whole spec). Then make each present tool's
   native rules layer on top and point at it — `CLAUDE.md` (`@AGENTS.md` import or a one-line pointer),
   `.github/copilot-instructions.md`, `.cursor/rules/*.mdc`, and for Google Antigravity a workspace
   rule `.agents/rules/context-fence.md` (Always On; keep it compact and `@AGENTS.md` + `@HANDOFF.md`
   the detail — rules cap at ~12k chars). Detect which tools the repo already uses (existing native
   files/dirs) and wire those; you don't need every tool's file, but `AGENTS.md` is always written
   because it's the universal one.
4. **Vendor the full manual, not just the rule — to *both* real discovery paths.** Copy this skill's
   own `SKILL.md`, `references/`, and `scripts/*.sh` (not `evals/` — that's dev-only) into **both**
   `.agents/skills/context-fence/` (the generic [agentskills.io](https://agentskills.io/specification)
   convention — Cursor and other spec-compatible tools scan this) **and** `.claude/skills/context-fence/`
   (Claude Code scans *only* this path, never `.agents/skills/` — confirmed against
   `code.claude.com/docs/en/skills`; skipping this copy means a Claude Code session opening this repo
   without the context-fence plugin installed gets no native skill discovery at all). Copy from
   wherever this skill is currently loaded (e.g. `${CLAUDE_PLUGIN_ROOT}/skills/context-fence` in
   plugin form, or this skill's own directory otherwise). This is what makes every operation actually
   *followable* by any agent in any harness with **zero installation** — a thin `AGENTS.md` pointer
   only tells the next agent a discipline named "ADOPT" exists, not what it does; the vendored copies
   are what tools can genuinely *discover and load*, not just be told about. Treat both vendored
   copies as **tool-owned**: unlike `AGENTS.md` (create-once, never touch), it's safe — and expected —
   to overwrite/refresh them on every SETUP run, so they always mirror whichever skill version last
   touched the repo rather than accumulating hand-edits. **Skip this step if the repo already contains
   `skills/context-fence/SKILL.md` at its own root** (i.e. this repo *is* the context-fence source) —
   vendoring a copy of the skill into itself is meaningless.
   - **Commands, where the harness has its own separate mechanism beyond skill discovery.** Some
     harnesses additionally support a lighter reusable-command/slash-command surface distinct from
     full skill loading (Copilot `.github/prompts/*.prompt.md`, Kiro steering-as-command, Antigravity
     `/workflow-name` workflows, opencode `.opencode/commands/*.md`) — mirror all five operations there
     too, pointing at the vendored `SKILL.md`. See `docs/harnesses/` for the exact convention per tool
     and `scripts/install.sh`'s `dropin/` templates for working examples. Harnesses that natively
     discover agentskills.io-format skills (Cursor, and by extension Claude Code via the path above)
     don't need this — they already load the whole `SKILL.md`, including this dispatch table, and can
     invoke it by name (e.g. `/context-fence`).
   - **Manual invocation, where neither exists.** Some harnesses read `AGENTS.md`/native rules but have
     no command mechanism and no agentskills.io-style skill discovery (Cline, Goose, pi, Aider, and
     others). For those, the fallback is explicit natural language: *"follow the RESUME operation in
     `.agents/skills/context-fence/SKILL.md`"* (or SETUP/ADOPT/SWEEP/HANDOFF). Document this per-tool in
     `docs/harnesses/<tool>.md` rather than leaving it implicit — a user shouldn't have to guess that
     typing out the operation name is the intended UX for a given tool.
5. **Scaffold the homes.** Ensure `docs/` exists; create `HANDOFF.md` from the template
   (`references/handoff-template.md`) if absent; add `scratch/` to `.gitignore`.
6. **Fold in the harness's own config — it's durable repo state.** A harness writes config into the
   repo; treat the *shareable* part as in-boundary and the *personal/secret* part as transient:
   - **Commit** project-scope config that records the repo's toolchain so a clone/teammate gets the
     same setup: `.claude/settings.json` (its `enabledPlugins` — e.g. enabling *this* plugin),
     `.cursor/`, `.agents/rules/`, `.github/copilot-instructions.md`. If a `.claude/settings.json`
     just appeared from installing a plugin, surface it and ask whether to commit (default **yes** —
     enabling the fence is itself durable repo state; committing it is the fence eating its own dog
     food). Skip only if it holds machine-specific paths or secrets.
   - **Gitignore** the personal/local/secret variants: `.claude/settings.local.json`, `*.local.json`,
     and any credential files. Add them to `.gitignore` if missing.
7. **Adopt repo-local memory.** For the rest of this session, write every durable note in-repo, never
   to a global/harness store. If the harness has its own memory feature, treat the repo's
   `CLAUDE.md`/`docs/` as authoritative and mirror anything durable into the repo.
8. **Report** what you created vs. verified. Don't duplicate content that already exists — update in
   place. `.agents/skills/context-fence/` is the one exception — always report if it was refreshed, since silently
   updating tool-owned content is fine but silently *not* updating it (stale manual) isn't.

---

## Operation ADOPT — retrofit an existing repo (one-time, discovery-heavy)

Run once when bringing a repo that predates the fence (or was worked without it) under the
discipline, or on `/fence-adopt`. It's SETUP + a discovery pass + SWEEP + an initial HANDOFF fused —
gather the scattered context first, then safely scaffold the homes so nothing is lost or overwritten.

1. **Discover, don't assume.** Survey what already exists before writing anything:
   - repo shape: languages, package/build files, test/gate commands (CI config, Makefile,
     package.json scripts, pyproject, etc.), the git remote and default branch.
   - existing durable notes anywhere: `README`, `docs/`, `*.md` design/spec/plan/TODO files, ADRs,
     wikis checked in, any `CLAUDE.md`/`AGENTS.md`/`GEMINI.md`/`.github/copilot-instructions.md`/
     `.cursor/rules` already present, and inline notes.
   - out-of-boundary context to reclaim (as in SWEEP): global stores, `/tmp`, chat-only decisions.
2. **Propose a map, then confirm if you can.** Summarize what you found and where each piece will
   land (SSOT doc, `docs/…`, `HANDOFF.md`, `AGENTS.md`). If the harness supports asking, confirm the
   project one-paragraph, the gate commands, and any external reference folders with the user before
   writing (this is the info a fresh agent most often gets wrong). If it can't ask, proceed and mark
   uncertain items as explicit `TODO (owner)` in the files.
3. **Raise the fence (SETUP)** without clobbering: create `AGENTS.md` + the tool-native pointers,
   `docs/` (if missing), `scratch/` gitignore — but **merge** into existing files, never overwrite.
4. **Sweep it in (SWEEP):** move the discovered durable context into its in-repo homes,
   de-duplicating.
5. **Seal an initial HANDOFF (HANDOFF):** write the first `HANDOFF.md` from the template, populated
   from discovery (state = "adopted at &lt;date&gt;", next steps = whatever's in flight or the obvious
   backlog, verify = the gate commands you found).
6. **Commit** (`chore(fence): adopt repo into context-fence`) and report the map + any `TODO (owner)`
   gaps for the user to fill.

---

## Operation SWEEP — gather escaped context back in (mid-session / on demand)

Run periodically, before a handoff, or on `/fence-sweep`. Pulls durable context that has leaked
outside the boundary — or that lives only in this conversation — back into the repo.

1. **Find escapees:** (a) durable decisions/plans/specs discussed in-conversation but not yet written
   to `docs/` or `HANDOFF.md`; (b) anything durable written to `~/.claude/`, the home dir, a global
   store, or `/tmp`; (c) relevant uncommitted working-tree changes.
2. **Bring them in:** write/move each durable item into its proper in-repo home (`docs/…`,
   `HANDOFF.md`, `docs/agent-memory/…`). Leave only transient logs/scripts outside (under `scratch/`).
3. **De-duplicate:** fold into existing files rather than creating parallel copies. If what you're
   sweeping in directly contradicts something `HANDOFF.md`'s §2/§3 already states (e.g. a "decide X"
   next-step that's now decided), fix that reference too — a HANDOFF.md left stating something you
   just proved wrong is worse than not sweeping at all. This doesn't require a full HANDOFF reseal
   (frontmatter stays as-is); it's still just folding content into an existing home.
4. **Optionally commit** (`chore(fence): sweep context into the repo`) and **report** what was swept
   and from where.

---

## Operation HANDOFF — seal session state (end of session / migration)

Run at the end of a work session or before migrating to a new session/tool, or on `/fence-handoff`.
Produces/refreshes a staleness-aware `HANDOFF.md`. **Run SWEEP first** so nothing durable is left out.

1. **Settle the tree** — commit outstanding work (or explicitly note what's intentionally left).
2. **Compute frontmatter** — run the bundled `scripts/handoff_meta.sh` (it sits next to this
   SKILL.md; in plugin form: `${CLAUDE_PLUGIN_ROOT}/skills/context-fence/scripts/handoff_meta.sh`)
   with the target repo as the working directory. It prints `state_at_commit` (= current HEAD; the
   handoff commit will be its child, so the marker stays N-1), `_short`, `branch`, `updated_utc`,
   `updated_local` (system tz; the offset names the zone, e.g. `+05:30` = IST).
3. **Write/refresh `HANDOFF.md`** from `references/handoff-template.md`, filled with real state — no
   placeholders. Keep standing rules OUT (they belong in `CLAUDE.md`). The body is freely overwritten
   on migration; the frontmatter is the staleness marker.
4. **Wire pointers** — ensure `CLAUDE.md` (and `AGENTS.md`/`.github/copilot-instructions.md` if the
   repo is multi-tool) points at `HANDOFF.md` and its §0.1 freshness protocol.
5. **Commit** (`docs(handoff): refresh session state @ <short>`) — its parent is the recorded
   `state_at_commit`, keeping the marker exactly N-1. Remind the user to push.

### Frontmatter schema (`handoff_schema: 1`)

```yaml
---
kind: session-handoff          # marks migratable state, NOT standing rules
handoff_schema: 1              # bump when these fields change
updated_utc: <ISO-8601 Z>
updated_local: <ISO-8601 ±hh:mm>   # offset names the zone (e.g. +05:30 = IST)
branch: <git branch>
state_at_commit: <full sha>    # N-1: the HEAD this snapshot describes
state_at_commit_short: <short sha>
# Freshness: `git log --oneline "$(git log -1 --format=%H -- HANDOFF.md)"..HEAD` empty + clean tree =
# current. (Not state_at_commit..HEAD directly — that always includes the handoff commit itself.)
---
```

Body sections (from the template): §0 how-to + §0.1 freshness/alignment · §1 project one-paragraph +
SSOT · §1b external references (sibling repos / mounted folders the repo doesn't contain) · §2 current
state + commit range · §3 next steps (exact commands) · §4 known issues (repro + fix) · §5 verify/gate
commands · §5b tooling/skills · §6 key docs.

---

## Harness compatibility

The fence is plain files + git, so the **discipline and the artifacts are universal** — any agent
that opens the repo can honor them. Only the *delivery* differs per harness. "Follow the rules" below
always means: read the vendored `SKILL.md` (see SETUP step 4) and do what it says — never "install the
skill first."

| Harness | Auto-loaded rules file (write the fence here) | Invoke the operations |
| :-- | :-- | :-- |
| **Claude Code / Cowork** | `CLAUDE.md` (native) **and** `AGENTS.md`; also natively discovers the vendored `.claude/skills/context-fence/` as a project skill | `/fence-*` slash commands (plugin), `/context-fence` (skill, project-scoped, no plugin needed), or follow the rules |
| **Cursor** | `.cursor/rules/*.mdc` **and** `AGENTS.md` (native); also natively discovers the vendored `.agents/skills/context-fence/` (and `.claude/skills/context-fence/`, for backward compat) | `/context-fence` (native skill discovery — no separate command mirror needed) |
| **GitHub Copilot** | `.github/copilot-instructions.md` **and** `AGENTS.md` (native) | `.github/prompts/*.prompt.md`, or follow the rules |
| **Kiro (AWS)** | `.kiro/steering/*.md` **and** `AGENTS.md` (native, always-included) | Kiro slash-commands/hooks, or follow the rules |
| **Google Antigravity** | `.agents/rules/*.md` (workspace, Always On); `GEMINI.md` = global. Root `AGENTS.md` support is **unconfirmed** — Google's own docs are inconsistent on this, write an explicit `.agents/rules/` pointer rather than relying on it | Antigravity **workflows** (`/fence-*` markdown), or follow the rules |
| **opencode / Codex CLI / OpenHands / Cline / pi / Goose** (FOSS terminal & IDE agents) | `AGENTS.md` (native in all six) | follow the rules; each has its own reusable-prompt mechanism — see `docs/harnesses/` |
| **Devin Desktop** (formerly Windsurf) / Zed / Jules / Aider / JetBrains | `AGENTS.md` | follow the rules |

Four takeaways: (1) **always write `AGENTS.md`, and always vendor the skill to both real discovery
paths** — `AGENTS.md` is the one file every tool reads, but on its own it only names the operations;
the vendored `SKILL.md` copies (see SETUP step 4) are what make them actually *followable* without the
skill being installed anywhere. This is the difference between "the next tool knows a discipline
exists" and "the next tool can actually run it" — the entire point of being able to hand a session off
from one tool to another. (2) **The `/fence-*` slash commands are a Claude Code plugin convenience,
not a requirement** — with `AGENTS.md` + the vendored skill in place, any agent performs
RESUME/SETUP/ADOPT/SWEEP/HANDOFF by reading and following the vendored `SKILL.md` directly, installed
or not. (3) **Harnesses that natively discover agentskills.io-format skills need no extra
wiring at all** — confirmed for Cursor (scans `.agents/skills/`, `.cursor/skills/`, and — for backward
compat — `.claude/skills/`, `.codex/skills/`) and Claude Code itself (scans `.claude/skills/`); both
load the *entire* `SKILL.md`, including this dispatch table, and can invoke it by name. (4) **Where a
tool has a separate, lighter reusable-command mechanism distinct from full skill discovery** (Copilot
prompt files, Kiro hooks, Antigravity workflows, opencode commands), mirror the five operations as
those too, per SETUP step 4 — nicer ergonomics on top of the same underlying capability. Where a tool
has neither (Cline, Goose, pi, and others), the fallback is typing out the operation name against the
vendored `SKILL.md` — document that explicitly per tool rather than leaving it implicit. The
`HANDOFF.md` staleness marker and the bundled `scripts/*.sh` are pure git + shell — identical
everywhere, and now runnable locally from the vendored skill's `scripts/` in any fenced repo. Per-tool
wiring detail, install notes, and current-traction context (some of these products rename/merge/get
acquired — worth re-checking before publishing anything long-lived) live in `docs/harnesses/` — one
file per tool, kept out of this SKILL.md so it stays compact.
