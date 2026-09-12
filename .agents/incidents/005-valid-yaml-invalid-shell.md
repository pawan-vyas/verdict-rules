<!-- Title: Incident 005 — Valid YAML, Invalid Shell -->
# 005 · A release workflow shipped with an unterminated quote

> **2026-09-12 · caught on `main`, no release lost**

## What happened

`Release (Skill)` failed on the merge that bumped the skill to `0.2.1`. Its
`detect` job exited with code 2 — a shell *parse* failure, not a check
failing. The line was:

```bash
echo "Changelog section for ${tag} present.
```

The closing quote was missing, so bash never finished parsing the script.

## How it surfaced

A red workflow on `main`, after the merge. Every pre-merge check had passed:
the YAML was valid, `Release readiness` was green, the tests were green.

## Impact

The skill release did not happen. Nothing was published incorrectly — the job
failed before reaching the build or the tag — so this cost a re-run rather than
a lost version. `Release (Python)` was unaffected and correctly did nothing,
since its version was already tagged.

Worth noting what the shape could have been: the identical bug sat in
`release-python.yml` too, in the step that guards the PyPI publish. It had not
fired only because Python's version was unchanged in that merge.

## Root cause

**Two languages, one file, one checker.** A workflow is YAML that *contains*
shell, and those are parsed by different things. Everything in this repo
validated the YAML — `yaml.safe_load` in a dozen places — and nothing ever
validated the shell inside a `run:` block. A file could be perfectly
well-formed YAML and contain a script that cannot run.

The specific typo came from generating YAML with a Python script: the heredoc
ended `present."""`, and Python consumed one of those quotes as its own string
terminator. That is the proximate cause; the structural one is that nothing
would have caught it whatever its origin.

## The fix

`scripts/check_workflow_shell.py` extracts every `run:` block from every
workflow and parses it with `bash -n`, which checks syntax without executing.
It runs in `check-release-readiness.yml`, so it fires on every pull request.

Verified against the real bug rather than a synthetic one: reintroducing the
missing quote makes the checker exit 1 and name the exact job, step and line.

## What prevents a repeat

- **A workflow is two languages. Check both.** Validating the YAML says nothing
  about the shell inside it, and the gap is invisible until a job runs.
- **Prefer a script file over a long `run:` block.** The checker itself started
  as an inline block and immediately hit a second version of this problem — a
  step named `Every run: block is valid shell` broke the YAML, because `run:`
  inside an unquoted string is a mapping. Shell in YAML in a generated file is
  three layers of quoting, and each one can eat a character from the next.
- **A green pre-merge suite is not evidence a workflow runs.** Nothing except
  actually running it proves that, which is why the checker had to be verified
  by reintroducing the fault rather than by observing it pass.
