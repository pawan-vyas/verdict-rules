<!-- Title: Incident 001 — Release Notes Mangled By Shell -->
# 001 · Release notes published with every code span empty

> **2026-09-11 · shipped in `python-v0.1.1` · GitHub release body only**

## What happened

The release workflow was changed to build its body from `CHANGELOG.md` rather
than GitHub's auto-generated commit list. It published successfully. The body
read:

```
- ** now raises  for an unknown group**, matching .
```

Every backtick-quoted code span was gone — replaced with nothing.

## How it surfaced

By reading the published release page. **Nothing failed.** The workflow was
green, the PyPI upload was correct, and every automated check passed.

## Impact

Cosmetic but public, on the project's most visible artefact. PyPI was
unaffected. Repaired in place with `gh release edit --notes-file`, which is
possible for a GitHub release and would not have been for the package itself.

## Root cause

The notes were passed as a **step output**, interpolated into a later step's
shell:

```yaml
--notes "${{ steps.notes.outputs.notes }}"
```

GitHub Actions substitutes the raw string into the script, so bash read the
changelog's backticks as **command substitution**. `` `RulesEngine.run_group()` ``
was *executed*, and its empty output is what reached the release page.

The structural problem: the construct being mangled — inline code — is what a
changelog is mostly made of, and the failure mode is silent by nature. Shell
interpolation of untrusted-shaped text is the general form.

## The fix

Write the notes to a file in the generating step and pass `--notes-file`, so
the bytes reach `gh` untouched. Applied to both release workflows.

## Recurrence, 2026-09-12 — same cause, different tool

It happened again, outside a workflow. A pull request body was passed to
`gh pr create --body "…"` as a double-quoted shell string containing markdown.
The backticks around `` `## 1.2.3` `` and `` `v` `` were read as command
substitution, and the published description rendered as *"documents , optionally
-prefixed"*.

The lesson had been written as a workflow rule, so it did not transfer. The
actual rule is broader: **any shell string carrying authored text is a hazard,
whatever the tool.** `gh` has `--body-file`, `gh release` has `--notes-file`,
and a workflow has a file path — all three exist for this reason.

## What prevents a repeat

- **Never interpolate authored text into a shell string.** Write a file and
  pass its path: `--body-file`, `--notes-file`, or a redirect. This applies to
  workflow `run:` blocks, `gh` invocations, and anything else that takes prose
  on a command line.
- **Markdown is authored text.** A changelog entry, a release note, a PR body,
  an issue body — all of them routinely contain backticks, and backticks in a
  double-quoted shell string are executed.
- The failure is silent both times: the command succeeds, and only the rendered
  output is wrong.

Verified by running the same changelog through both paths locally: the
interpolated one yields an empty line, the file yields the correct text.
