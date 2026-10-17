<!-- Title: How To Run The Full Skill Eval -->
# Running all the skill evals: stage, one agent each, grade by hand

> The recipe that ran all 34 evals against the published `0.4.0`
> packages, so the next run does not re-derive the harness. Each eval is
> its own subagent, launched one at a time, graded by reading its reply
> against the eval's expectations. The first run found four skill gaps and
> two evals whose premise the library had outgrown.

## Before starting

- **Pin the fixtures to the latest published version** in every language
  (`evals/<target>/files/`, plus the Dart lockfiles and the Python eval
  that names the version). They had drifted from `0.0.1` to `0.3.1` while
  four releases went out. Check each registry shows the version first.
- `python3 scripts/build_evals.py`, then `bash scripts/build.sh`, and
  unzip `dist/verdict.skill` into `.agents/scratch/eval/skill/`. Hand
  agents the built skill, never `skills/verdict/`
  (see [the evals README](../../skills/verdict-workspace/evals/README.md)).
- Stage `.agents/scratch/eval/runs/NN-<eval_name>/project/` with the
  eval's `files`, and `request.md` with its prompt. The local-pin eval also
  needs a vendored checkout **including `docs/`**; copying only the package
  directory leaves the agent without the docs and it says so.

## The agent brief

Keep it in a file (`.agents/scratch/eval/INSTRUCTIONS.md`) so each launch
is one short call. It must say: act as an assistant in `project/`, start
from the installed skill's `SKILL.md`, read the request, write the full
reply to `RUN/response.md`; **read nothing else in the repo** (no source,
docs, memory, evals or expectations); and **never run a write to an
external service** (`gh issue create`, POSTs), printing the command and
body instead. Without that last line a report-filing eval can open a real
issue.

## Grading

Read each `response.md` beside its expectations. Separate three causes of
a failed expectation: the skill is wrong (fix the skill, bump
`plugin.json`, changelog), the eval is wrong (its premise no longer holds,
so a correct agent fails it), or the agent was merely thorough or terse
(note it, do not chase it). A reply that says "this is documented
behaviour, nothing to report" to an eval asserting a defect means the eval
is stale, not the skill.

## Cost

About a minute per agent; 34 run in well under an hour. Sequential per
[`run-costly-subagent-batches-sequentially`](run-costly-subagent-batches-sequentially.md).
