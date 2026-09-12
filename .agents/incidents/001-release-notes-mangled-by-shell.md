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

## What prevents a repeat

Never interpolate multi-line or user-authored text into a shell command in a
workflow — write a file and pass a path. Both workflows carry a comment saying
why, because the code looks harmless.

Verified by running the same changelog through both paths locally: the
interpolated one yields an empty line, the file yields the correct text.
