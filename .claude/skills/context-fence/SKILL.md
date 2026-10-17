---
name: context-fence
description: >-
  Keep ALL durable agent context — memory, standing instructions, plans, specs, decisions, tasks,
  incidents, session handoffs — inside the repo boundary, so a model never scatters it into harness
  defaults (global config, plan mode, chat-only, /tmp) and loses it across sessions or tools. Six
  operations: RESUME (start-of-session front door — detects repo state, dispatches, then surfaces
  what's next), SETUP (raise the fence + resume from any handoff), ADOPT (one-time retrofit of a
  pre-fence repo), SWEEP (gather escaped context back in), HANDOFF (seal session state at the end),
  UPGRADE (offer newer fence conventions to an already-fenced repo, once, and record the answer).
  Use at the START of work in any repo — especially "set up / resume / continue / pick up where we
  left off / onboard this repo / sweep context / hand off / wrap up / migrate" — and before
  switching sessions or assistants. Tool-neutral (plain files + git); works across Claude Code,
  Copilot, Cursor, and any agent that reads AGENTS.md. The value is the invisible perimeter, not any
  one artifact.
metadata:
  version: "0.4.1"
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

> Every **durable** note — memory, standing rules, plans, specs, decisions, tasks, incidents,
> discussion conclusions, session handoffs — lives **inside the boundary**, committed (or at least on
> disk in the dir). **Never** write durable notes to `~/.claude/`, the home directory, a harness's own
> global memory or plan store, or `/tmp`. Only genuinely **transient** logs and throwaway scripts may
> live outside a tracked file, under `.agents/scratch/` (gitignored).

## Where each kind of note lives

`.agents/` is the harness-neutral root for everything an agent produces that outlives a session. It is
already where the [agentskills.io](https://agentskills.io/specification) convention puts vendored
skills, so the fence keeps the rest of an agent's working material beside them rather than inventing a
second root or borrowing `docs/` — which in most repos is published documentation written for human
readers of the project, and is the wrong place to mix agent recall into.

| Kind of note | Home |
| :-- | :-- |
| Standing rules | `AGENTS.md` (+ each tool's native rules file) |
| Session state — resume from here | `HANDOFF.md` |
| Durable facts, confirmed preferences, conclusions that cost real work to reach | `.agents/memory/` |
| Multi-session plans, specs, playbooks, design work in progress | `.agents/plans/` |
| Failures worth not repeating, and what prevents each one | `.agents/incidents/` |
| Vendored operations manuals an agent follows | `.agents/skills/`, mirrored to `.claude/skills/` |
| Throwaway logs, research, generated reports, scratch scripts | `.agents/scratch/` — **gitignored** |
| Published documentation, for human readers | `docs/` |
| Which homes this repo actually uses, and what it has already decided | `.agents/fence.json` |

Three rules keep that table meaningful:

- **The paths are defaults, not law.** A repo that deliberately keeps a home somewhere else records
  that in `.agents/fence.json`'s `homes` map, and **every operation reads the paths from there** rather
  than from this table. Resolve the map once at the start of an operation and treat it as the answer;
  don't re-derive a path at each site. A deviation recorded this way is a decision, not drift — never
  "correct" it back (see **UPGRADE**).
- **A genuinely new kind of durable note gets a new sibling under `.agents/`**, with its own
  `README.md` stating what belongs in it and what doesn't — never a subdirectory of whichever existing
  home is the closest fit. Homes stop meaning anything the moment they start absorbing near-misses.
- **Each home carries a `README.md`.** An empty directory doesn't teach anyone what goes in it, and a
  kind of note with no visible home simply never gets written down. Templates for all three:
  `references/agents-home/`.

**Every durable entry is indexed in `AGENTS.md`** — a link and a hook of roughly ten words, never the
content itself. The hook is written once, in the entry's own `hook:` frontmatter field, so the index
and the entry can never drift into disagreeing about what the entry says. Writing an entry and indexing it are **one** operation, not two: an unindexed note is
one the next agent won't find, which is the same outcome as never having written it. This is the same
progressive disclosure the vendored skills themselves rely on — an always-loaded layer that names what
exists, with the full content one hop away. `AGENTS.md` is kept from bloating by a character budget and
an explicit spill order (`pin: always` never spills; the rest spill oldest-touched-first into their
home's README, and the collapsed line states how many are not shown, so the index never silently lies
about coverage). Full rules: `references/durable-index.md`.

`AGENTS.md` is the cross-tool canonical rules file — read natively by Claude Code, GitHub Copilot,
Cursor, Google Antigravity, Codex, Windsurf, Zed, Jules and 20+ others (Linux Foundation standard).
Each tool's *richer native* rules layer on top and should point at it: `CLAUDE.md` (Claude),
`.github/copilot-instructions.md` (Copilot), `.cursor/rules/*.mdc` (Cursor), and for Google
Antigravity a workspace rule at `.agents/rules/context-fence.md` (Always On; `GEMINI.md` is
Antigravity's *global* layer, not a repo file). Keep the `AGENTS.md` fence section compact (several
tools cap rules files at ~12k chars) and use `@relative/path` imports for detail — `.agents/rules/`
and `CLAUDE.md` both support `@` imports. Raw conversation logs are exempt (harness's, not durable).

## The harness's own stores are the main leak

The boundary is easy to hold for files you chose to write. It leaks through features that write
*for* you, on a path you didn't pick. Three of these exist in nearly every harness, and the fence has
a rule for each:

- **Plan mode / scratch mode.** Anything that drafts a working document somewhere of the harness's
  choosing gets pointed at `.agents/plans/`. Where the path isn't configurable, copy the document into
  `.agents/plans/` as soon as it has real content and make every later edit to the in-repo copy — the
  authoritative version must never be the one in a harness-private directory.
- **Memory features.** Same rule, and it matters more, because a memory feature is built to be
  invisible: it accumulates without being asked and is read back automatically, so nothing ever *feels*
  wrong while every fact being learned lands outside the repo. Write to `.agents/memory/` instead — one
  file per fact, committed. If the harness insists on its own store and the path can't be changed,
  leave **only a pointer** there naming `AGENTS.md` and `.agents/memory/` as the real location. Never a
  second copy of the fact: duplicated facts drift, and the stale copy is the one that gets read back in
  the session where it matters. This supersedes any instruction to "mirror" durable notes into a
  harness store — a pointer is the mirror.
- **Temp directories.** Every harness offers somewhere of its own for working files — a session
  scratchpad, `/tmp`, a hidden cache. Use `.agents/scratch/` instead, for the same reason: one
  predictable location beside the repo it concerns, readable by the user in their own editor and by the
  next agent whatever harness it is running in. It is gitignored, which is the point. If something
  there turns out to be durable, promote it to `.agents/memory/` or `.agents/plans/` rather than
  leaving it to be deleted.

Each of the three has two possible outcomes, and which one you got is worth recording: **redirect** it
where the harness exposes a setting or a project-local path — the leak is then closed structurally and
cannot drift — or, where the path is fixed and private, put it on the **reconcile loop**: copy the
content in as soon as it is real, make every later edit to the in-repo copy, leave only a pointer
behind, and re-check the harness's path during SWEEP and before HANDOFF, because something will have
been written there since. A store you haven't checked is leaking, not redirected. Per-harness detail
and the full loop: `references/harness-stores.md`. Where a store can be neither redirected nor read,
record it in `HANDOFF.md` §4 as a named known leak rather than treating the fence as intact.

Two adjacent anti-patterns worth naming, since both defeat the fence while staying technically inside
it:

- **Don't open a parallel planning file.** In-flight work and next steps belong in `HANDOFF.md` §3;
  anything longer-lived belongs in `.agents/plans/` as a named plan. A fresh `TODO.md` or `NOTES.md` at
  the repo root is how context gets lost, not how it gets organized.
- **Don't use a public surface as a working surface.** Issue bodies, pull request descriptions, and
  commit messages are permanent and read as the project speaking in its own voice. They carry the task
  and the decision; the investigation behind it belongs in `.agents/scratch/`. This is the same leak in
  the opposite direction — working material pushed *out* instead of durable material left outside.

## Asking the user

Several operations below reach a point where a decision is genuinely the user's. When that happens,
**ask through the harness's interactive question tool** if it has one, and in the conversation if it
doesn't — the requirement is that the question is put explicitly, not that any particular mechanism is
used. Never bury it as prose at the end of a long reply, and never guess silently and proceed.

- Give the context **first**: what was found, why it forces a choice, what each option costs.
- Give a **recommendation**, not an even-handed survey.
- **Don't block.** Do everything that doesn't depend on the answer, then ask. Reserve a blocking
  question for a case where proceeding under any assumption would be unsafe or would waste the work if
  wrong.
- **If the harness cannot ask at all**, proceed with the documented default and leave an explicit
  `TODO (owner)` marker in the file that the decision affects, so the question survives the session.
- **Ask at most once.** Any answer that will still be true next session is durable context by this
  skill's own invariant, so record it — in `.agents/fence.json` for fence decisions, in
  `.agents/memory/` for anything else — and read it back instead of re-asking.

---

## Operation RESUME — start-of-session front door (run this first)

Run at the very start of a session, or on `/fence-resume`. This is the recommended entry point: rather
than making you (or the user) already know whether a repo needs `ADOPT`, `SETUP`, or just a freshness
check, RESUME detects which and dispatches — then, if the repo is already fenced, surfaces what to do
next instead of silently confirming "fresh" and stopping. Idempotent — safe to re-run.

1. **Classify.** Run the bundled `scripts/detect_state.sh` (it sits next to this SKILL.md; in plugin
   form: `${CLAUDE_PLUGIN_ROOT}/skills/context-fence/scripts/detect_state.sh`) from the target repo.
   It reports `is_git`, `boundary`, `has_agents_md`, `has_handoff_md`, a `state`, and the two version
   facts UPGRADE needs (`applied_version` — what `.agents/fence.json` says this repo was last fenced
   with, or `none` — and `pending_upgrade`), and `handoff_sealed` (`yes`/`no`/`none`). The script only reports facts — the dispatch policy below
   is the single place that decision lives, so it stays easy to change without touching the script.
2. **Dispatch by `state`** (an exhaustive table, not a chain of ifs — every state maps to exactly one
   next action):

   | `state` | Meaning | Action |
   | :-- | :-- | :-- |
   | `NON_GIT` | no git boundary | Same as SETUP step 1: ask about `git init` (or proceed with the directory as the boundary if you can't ask), then re-run classification. |
   | `NON_COMPLIANT` | no `AGENTS.md` and no `HANDOFF.md` | Run **ADOPT**, always — even on a repo that looks empty. ADOPT's discovery pass is cheap insurance (it degrades to "nothing to sweep" on a genuinely fresh repo) and it's the only path of the two that's guaranteed to never miss or clobber scattered pre-existing context on a populated one. |
   | `PARTIAL` | some fence files present, not all (e.g. `AGENTS.md` exists, `HANDOFF.md` doesn't) | Run **SETUP** — idempotent, fills in what's missing without touching what's already there. |
   | `COMPLIANT` | `AGENTS.md` + `HANDOFF.md` both present | Skip straight to `HANDOFF.md`'s own §0.1 freshness check; reconcile first if it reports stale. No need to re-scaffold anything. **Unless `handoff_sealed` is `no`** — then the handoff was scaffolded, not written: populate §1–§6 from the repo and seal it, which is the one case where `COMPLIANT` still has real work to do. |

3. **Check for pending conventions.** If `pending_upgrade` is `yes`, run **UPGRADE** — it applies
   what's safe to apply silently and asks once about the rest. This is deliberately a separate step
   rather than a fifth row in the table above: a repo's *currency* is orthogonal to how far its
   scaffolding got, so a `COMPLIANT` repo can still be several versions behind, and folding the two
   axes together would break the table's one-state-one-action property.
4. **Land ready, not just "fenced."** Once the dispatched step finishes, read `HANDOFF.md` §2 (where
   things are) and §3 (what to do next) and report them. RESUME's job is done when the next concrete
   step is in front of you — not merely when the files exist.

A `HANDOFF.md` you're resuming from may be missing sections — hand-edited, migrated from an older
schema, or written by a tool that didn't fully follow the template. **A missing section is never
blocking, and never license to invent one.** Report what is there, say plainly which sections were
absent, and carry on:

- **Missing §0.1** — check the frontmatter comment first (it carries the same freshness command); if
  that's missing too, compute the check from the formula in this SKILL.md's HANDOFF section. A missing
  §0.1 is a reason to be extra careful reconciling, not a reason to skip the check.
- **Missing §2 (or any other body section)** — proceed with what the file does have. Fill the gaps from
  the repo itself (`git log`, the working tree) rather than leaving the user with nothing, and note the
  file is incomplete so the next HANDOFF rebuilds it from the template.

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
   a compact "Working notes stay in the repo" section (the invariant), a **Durable context** index
   section per `references/durable-index.md` (create it empty on a fresh repo — it is what later
   entries get added to, and a section that doesn't exist is one nobody adds to), plus a one-liner: *"resume from
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
5. **Scaffold the homes.** Resolve the home paths first (`.agents/fence.json`'s `homes` map if the
   repo has one, otherwise the defaults in "Where each kind of note lives"), then ensure each exists
   with its `README.md` from `references/agents-home/`: `.agents/memory/`, `.agents/plans/`,
   `.agents/incidents/`, plus `.agents/README.md` for the layout itself. Create `HANDOFF.md` from
   `references/handoff-template.md` if absent, ensure `docs/` exists, and add the scratch home to
   `.gitignore`. On a repo that already has homes at non-default paths, honor the recorded paths and
   leave them where they are — relocation is UPGRADE's job, and only with the user's say-so.
   Finally, write `.agents/fence.json` (schema in **UPGRADE** below) stamped with this skill's
   `metadata.version`, so a later session can tell what this repo was fenced with.
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
7. **Adopt repo-local memory, plans, and scratch.** For the rest of this session, write every durable
   note in-repo, never to a global/harness store — and apply "The harness's own stores are the main
   leak" above to this harness specifically: point its plan mode at `.agents/plans/`, write what you
   learn to `.agents/memory/` rather than its memory feature, and use `.agents/scratch/` rather than
   its temp directory. Where a path can't be redirected, leave a pointer in the harness store and keep
   the in-repo copy authoritative — never two copies of the same fact. Work through
   `references/harness-stores.md` for the harness actually running: redirect each of the three stores
   where it can be redirected, put the rest on the reconcile loop, and record which is which.
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
4. **Sweep it in (SWEEP):** move the discovered durable context into the home that owns each piece,
   de-duplicating. ADOPT never needs UPGRADE — a repo being adopted is being fenced at the current
   version by definition, so scaffold the current homes and stamp `.agents/fence.json` with every
   convention marked accepted.
5. **Seal an initial HANDOFF (HANDOFF):** write the first `HANDOFF.md` from the template, populated
   from discovery (state = "adopted at &lt;date&gt;", next steps = whatever's in flight or the obvious
   backlog, verify = the gate commands you found).
6. **Commit** (`chore(fence): adopt repo into context-fence`) and report the map + any `TODO (owner)`
   gaps for the user to fill.

---

## Operation SWEEP — gather escaped context back in (mid-session / on demand)

Run periodically, before a handoff, or on `/fence-sweep`. Pulls durable context that has leaked
outside the boundary — or that lives only in this conversation — back into the repo.

0. **Check for pending conventions** — SWEEP is one of the three points where this skill reliably
   activates, so if `.agents/fence.json` reports a `pending_upgrade`, run **UPGRADE** first. Sweeping
   context into homes a repo hasn't adopted yet would create them behind the user's back.
1. **Find escapees:** (a) durable decisions/plans/specs/facts discussed in-conversation but not yet
   written down; (b) anything durable written to `~/.claude/`, the home dir, a harness's own memory or
   plan store, or `/tmp`; (c) relevant uncommitted working-tree changes.
2. **Bring them in:** write/move each durable item into the home that owns it per the resolved `homes`
   map — a fact to `.agents/memory/`, a plan to `.agents/plans/`, a failure worth not repeating to
   `.agents/incidents/`, session state to `HANDOFF.md`. Leave only transient material outside, under
   the scratch home.
2b. **Index what you wrote.** Every entry swept into a home gets its line in `AGENTS.md`'s durable
   context index in the same pass, and re-check the budget while you're there: spill or re-promote per
   `references/durable-index.md`. Also re-check any harness store on the reconcile loop — by
   definition something has been written there since the last pass.
3. **De-duplicate, and re-check what the session made untrue:** fold into existing files rather than
   creating parallel copies. A change to the code can silently falsify a durable note that describes
   it, and nothing fails when it does — the tests still pass while the note says something wrong. So
   sweep outward from what changed, not only inward from what was said. If the repo has no
   `HANDOFF.md` at all, skip the parts of this step that reconcile against it — creating one is SETUP's
   job, and a handoff sealed mid-sweep from a template would carry placeholders rather than real state. If what you're
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

0. **Check for pending conventions** — as in SWEEP, if `.agents/fence.json` reports a
   `pending_upgrade`, run **UPGRADE** first, so the state you seal reflects the conventions this repo
   has actually agreed to.
1. **Settle the tree** — commit outstanding work (or explicitly note what's intentionally left).
2. **Compute frontmatter** — run the bundled `scripts/handoff_meta.sh` (it sits next to this
   SKILL.md; in plugin form: `${CLAUDE_PLUGIN_ROOT}/skills/context-fence/scripts/handoff_meta.sh`)
   with the target repo as the working directory. It prints `state_at_commit` (= current HEAD; the
   handoff commit will be its child, so the marker stays N-1), `_short`, `branch`, `updated_utc`,
   `updated_local` (system tz; the offset names the zone, e.g. `+05:30` = IST).
3. **Write/refresh `HANDOFF.md`** from `references/handoff-template.md`, filled with real state — no
   placeholders. Set `sealed: true` and `handoff_schema: 2`; sealing is what this operation *is*, so a
   handoff it wrote is never unsealed. Keep standing rules OUT (they belong in `CLAUDE.md`). The body is freely overwritten
   on migration; the frontmatter is the staleness marker.
4. **Wire pointers** — ensure `CLAUDE.md` (and `AGENTS.md`/`.github/copilot-instructions.md` if the
   repo is multi-tool) points at `HANDOFF.md` and its §0.1 freshness protocol.
5. **Commit** (`docs(handoff): refresh session state @ <short>`) — its parent is the recorded
   `state_at_commit`, keeping the marker exactly N-1. Remind the user to push.

### Frontmatter schema (`handoff_schema: 2`)

```yaml
---
kind: session-handoff          # marks migratable state, NOT standing rules
handoff_schema: 2              # bump when these fields change
sealed: true                   # false = scaffolded, body not yet populated (see below)
updated_utc: <ISO-8601 Z>
updated_local: <ISO-8601 ±hh:mm>   # offset names the zone (e.g. +05:30 = IST)
branch: <git branch>
state_at_commit: <full sha>    # N-1: the HEAD this snapshot describes
state_at_commit_short: <short sha>
# Freshness: `git log --oneline "$(git log -1 --format=%H -- HANDOFF.md)"..HEAD` empty + clean tree =
# current. (Not state_at_commit..HEAD directly — that always includes the handoff commit itself.)
---
```

**`sealed` distinguishes a populated handoff from a scaffolded one.** `scripts/install.sh` can wire a
repo but cannot know what the project is, what just changed, or what the gate commands are — so it
writes a handoff with real frontmatter and a body that says so, rather than one full of placeholders.
Without this field that file is indistinguishable from a real handoff: the repo classifies `COMPLIANT`
and the freshness check reports "current", so an operation could trust §2–§3 without ever reading the
sentence telling it not to.

- **`sealed: false`** → treat §1–§6 as absent. Populate them from the repo before relying on them, and
  set `sealed: true` when you do. Never reason from the body of an unsealed handoff.
- **`sealed: true`** → a normal handoff; §0.1's freshness check is the thing to honor.
- **Field absent entirely** (a `handoff_schema: 1` file) → **treat it as sealed.** The field didn't
  exist when that file was written, and everything written before it existed was a real handoff
  authored by an agent or a human. This is why schema 1 → 2 needs no migration row: the only possible
  reading of an older file is the correct one, and the next HANDOFF reseal writes schema 2 anyway.

Body sections (from the template): §0 how-to + §0.1 freshness/alignment · §1 project one-paragraph +
SSOT · §1b external references (sibling repos / mounted folders the repo doesn't contain) · §2 current
state + commit range · §3 next steps (exact commands) · §4 known issues (repro + fix) · §5 verify/gate
commands · §5b tooling/skills · §6 key docs.

---

## Operation UPGRADE — offer newer conventions to an already-fenced repo

Run when RESUME/SWEEP/HANDOFF reports a `pending_upgrade`, or on `/fence-migrate`. A repo fenced by an
older version of this skill has older scaffolding and an older `AGENTS.md`, and **nothing on disk
notices** when a newer version adds a convention: the vendored manual gets refreshed, but the homes it
describes don't exist, the standing rule that would trigger them was never written, and the agent has
no reason to go looking. UPGRADE closes that gap — and closes it *once*, so a user who says no is not
asked again.

The three operations that trigger it are the three where this skill reliably activates. That is the
whole delivery mechanism: there is no background process and no install step, so the migration has to
ride along with normal use.

1. **Read the applied version.** `.agents/fence.json`'s `fence_version` is what this repo was last
   fenced with. **No file at all means pre-0.4.0** — evaluate every migration. Compare against this
   skill's own `metadata.version`.
2. **Select the pending migrations.** From `references/migrations.md`, take every row whose `since` is
   the literal `always`, plus every row whose `since` is a version **newer than** the applied version —
   then drop any whose `id` is already recorded in `fence.json`'s `decisions`. (`since: always` is not a
   version and is never compared as one; it marks a row that re-runs on every pass, which only
   `mechanical` rows may do.)
   Walk the selected rows in order with one generic loop — the registry is data, so adding a future
   convention is a new row there and nothing else changes here.
3. **Apply each row by its `kind`**, which resolves once to how that row is handled:

   | `kind` | Ask the user? | What to recommend when you ask | If you cannot ask | If declined |
   | :-- | :-- | :-- | :-- | :-- |
   | `mechanical` | No — just apply it | n/a | Apply it anyway | n/a |
   | `additive` | Yes, once | Accepting | **Skip it.** Change nothing, record nothing | Record `declined`; never scaffold it, never re-ask |
   | `relocation` | **Always**, never implicit | Accepting | **Skip it.** Change nothing, record nothing | Record the existing paths as this repo's `homes` — the current layout becomes the deliberate one |

   "Recommend accepting" describes what you say **inside the question**. It is never permission to
   proceed without one: an unanswered `additive` or `relocation` row is skipped, not defaulted to yes.

   `mechanical` covers tool-owned content that was already safe to overwrite — refreshing the vendored
   skill copies, rewording a pointer. `additive` creates a new home or adds a standing rule to the
   always-loaded `AGENTS.md`: an empty directory is cheap, but a rule spends the next agent's attention
   in every session, so it is the user's call. `relocation` moves content the repo already has, which
   is never done on an agent's own initiative.
4. **Ask once, together.** Present all pending non-mechanical rows in a single question per "Asking the
   user" above — what each adds, what it costs, and a recommendation — rather than one prompt per row
   or one per session. If the harness can't ask, apply only the `mechanical` rows, leave a
   `TODO (owner)` note in `HANDOFF.md` §3 naming what's pending, and record nothing as declined.
5. **Record every answer, including the declines.** Write a `decisions` entry for each row you got a
   terminal answer to — `accepted` or `declined`. Mechanical rows are never recorded; they are
   idempotent and re-run harmlessly. This file exists primarily *for the declines*: a missing home is
   indistinguishable on disk from a home the user refused, and without that distinction the question
   comes back every session, which is how a good default teaches people to ignore it.

   **Only advance `fence_version` once every selected row has a terminal decision.** `fence_version`
   means "this repo is fully resolved up to here" — not "a run happened." If you could not ask, or the
   user deferred, leave `fence_version` exactly as you found it, even though you already applied the
   mechanical rows — and when there *was* no prior value because you just created the file, write
   **`0.0.0`**, meaning "nothing resolved yet." Do not write the current version and do not omit the
   field: the first reads as fully resolved, and the second reads as `unknown`, which is not the
   trigger RESUME tests for. Both leave the repo looking settled while it is not. Advancing it past an undecided row silently switches off `pending_upgrade`, which
   is the *only* automatic trigger this skill has — the undecided rows would then survive nowhere but a
   `TODO (owner)` note that nothing re-checks. A repo that looks current while carrying unanswered
   questions is strictly worse than one that asks again next session.
6. **Report and commit** (`chore(fence): upgrade conventions to <version>`) — what was applied, what
   was declined, and that `/fence-migrate` reopens anything declined.

### `.agents/fence.json` (`fence_schema: 1`)

```json
{
  "fence_schema": 1,
  "fence_version": "0.4.0",
  "applied_utc": "2026-01-01T00:00:00Z",
  "homes": {
    "memory": ".agents/memory",
    "plans": ".agents/plans",
    "incidents": ".agents/incidents",
    "scratch": ".agents/scratch"
  },
  "decisions": {
    "agents-home": { "decision": "accepted", "at_version": "0.4.0" },
    "incidents":   { "decision": "declined", "at_version": "0.4.0" }
  }
}
```

`homes` is the resolved layout for this repo — read it once per operation and treat each field as the
answer for that kind of note, instead of testing for a deviation at every site that touches a path. A
repo that never deviates still carries the map, so there is exactly one way to look a path up.

**`homes` is a routing map, not an inventory of what exists on disk.** Always write all four keys,
including for a home that hasn't been created yet: the field answers "where does this kind of note go,"
and a partially-migrated repo still needs that answer the moment one shows up. A key is omitted only if
this repo has genuinely decided that kind of note has no home here.
`decisions` is append-only in practice: a row already recorded is never re-evaluated unless the user
explicitly runs `/fence-migrate`, which re-offers everything including prior declines.

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
