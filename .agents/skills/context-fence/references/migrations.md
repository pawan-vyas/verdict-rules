# Migration registry

> The ordered list of conventions this skill has introduced, as data. **Operation UPGRADE** selects
> every row newer than a repo's applied `fence_version` and walks them in one generic loop, so adding
> a convention in a future release means adding a row here — not editing the operation.

Columns: `id` is what gets recorded in `.agents/fence.json`'s `decisions`. `since` is the skill version
that introduced it, or the literal `always` for a row that re-runs on every pass — `always` is not a
version and is never compared as one, and only a `mechanical` row may use it. `kind` decides how it is applied — see UPGRADE's `kind` table
(`mechanical` applies silently, `additive` asks once, `relocation` always asks and never moves
anything on the agent's own initiative).

| `id` | `since` | `kind` | What it does |
| :-- | :-- | :-- | :-- |
| `vendor-refresh` | `always` | `mechanical` | Re-copy the vendored skill payload to `.agents/skills/context-fence/` and `.claude/skills/context-fence/`, so the repo's manual matches the version that last touched it. |
| `fence-state` | 0.4.0 | `mechanical` | Create `.agents/fence.json` with the resolved `homes` map, so the other rows have somewhere to be recorded. It writes `fence_version` **only** once every row selected in this pass has a terminal decision — see UPGRADE step 5; a version stamped over undecided rows switches off the automatic trigger. On a first run that leaves rows undecided, the new file gets `fence_version: "0.0.0"` (nothing resolved yet), never the current version and never an absent field. |
| `agents-home` | 0.4.0 | `relocation` | Move agent working material under `.agents/`: memory from `docs/agent-memory/` (or wherever the repo kept it) to `.agents/memory/`, and root `scratch/` to `.agents/scratch/`, updating `.gitignore` and any pointers. Declining records the existing paths in `homes`, which makes the current layout the repo's deliberate one. |
| `plans-home` | 0.4.0 | `additive` | Create `.agents/plans/` + its `README.md`, and add the standing rule that a harness's plan mode is pointed at it. |
| `incidents-home` | 0.4.0 | `additive` | Create `.agents/incidents/` + its `README.md` (which carries the admission bar and the index table), and add the `AGENTS.md` line that triggers reading the index before anything irreversible. |
| `durable-index` | 0.4.0 | `additive` | Add the **Durable context** index section to `AGENTS.md` and backfill a line for every entry already sitting in a home, so nothing that predates the convention stays invisible. |
| `harness-stores` | 0.4.0 | `additive` | Add the standing rules for the harness's own memory store (keep a pointer, never a copy) and temp directory (use the scratch home) to `AGENTS.md`. |

## Adding a row

1. Append it here with the version you are about to release as `since` — never an already-released
   version, or repos that upgraded to that version will never be offered it. Use `always` only for a
   `mechanical` row that is safe to re-run every single pass.
2. Pick the `kind` honestly. The question is not how useful the convention is, it is what it costs the
   repo: creating an unused empty directory is `additive`; moving files someone already committed is
   `relocation`; anything that only touches tool-owned content is `mechanical`.
3. If it adds a home, add its `README.md` template under `agents-home/` and a row to the homes table in
   `SKILL.md`.
4. Bump `metadata.version` in `SKILL.md` and `version` in `.claude-plugin/plugin.json` in the same
   commit, and add a `CHANGELOG.md` entry. The registry is only as trustworthy as the version it is
   keyed on — a convention that ships without a bump is never offered to anyone.
