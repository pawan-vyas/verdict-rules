<!-- Title: Incident 002 — Build Output Committed Across Branches -->
# 002 · 436 build artefacts committed across two SDK branches

> **2026-09-12 · caught before any merge**

## What happened

`plan/dart-sdk` had **186** C# `bin/`/`obj/` files committed to it.
`plan/csharp-sdk` had **250**, including `node_modules` contents. Neither
branch has anything to do with the other's language.

## How it surfaced

A `git checkout` refused to switch branches:

```
error: The following untracked working tree files would be overwritten
```

Then a deliberate audit before a recap, counting tracked files matching build
patterns per branch. It was not caught by any test or check.

## Impact

No merge, so `main` was never polluted. Both branches were rebuilt from `main`
and force-pushed. The published packages were unaffected — `files` whitelists
and `.gitignore` at pack time meant nothing shipped.

## Root cause

**Each language's ignore rules lived only on that language's own branch.**

Three SDKs were being developed in one working tree, on separate branches. On
`plan/dart-sdk`, the repo's `.gitignore` had Dart's rules but not C#'s — so the
C# build output sitting in the same directory was untracked-and-unignored, and
`git add -A` swept it in.

Two compounding factors, both mine: using `git add -A` at all when the change
was known and listable, and adding each language's ignore rules on the branch
that introduced that language, which felt tidy and was exactly wrong.

## The fix

Every SDK's build-output rules now live in the **root `.gitignore` on `main`**,
so every branch inherits them regardless of which languages are checked out.
Both branches were rebuilt clean rather than patched, so the artefacts are not
in their history either.

## What prevents a repeat

- **Ignore rules for a language go on `main`, not on that language's branch.**
  A branch-local ignore rule protects only the branch that has it, which is the
  opposite of what is needed when several languages share a tree.
- **Prefer `git add <explicit paths>` over `git add -A`.** The second is fine
  when the tree is known clean and the change is small; it is not fine in a
  multi-language tree with build output present.
- Auditing tracked files against build patterns is cheap and worth doing before
  any branch is merged:
  `git ls-tree -r --name-only <ref> | grep -cE 'node_modules|/(bin|obj)/|dart_tool'`
