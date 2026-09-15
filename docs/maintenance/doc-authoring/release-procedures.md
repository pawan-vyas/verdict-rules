<!-- Title: Release Procedure Authoring Template -->
# Release procedure authoring template

> The structure every `docs/maintenance/releases/<language>.md` follows
> — the concrete, per-registry steps that plug into the shared pipeline
> [`../releases/README.md`](../releases/README.md) documents once for
> every language. This builds on the general standard in
> [`README.md`](README.md) — read that first.

## Why one template across four different registries

Every registry in this repository publishes differently — one publishes
directly over OIDC, one exchanges a token, one requires the publishing
run itself be tag-triggered, one cannot bootstrap its own first release
at all — which is exactly why
[`../releases/README.md`](../releases/README.md) keeps publishing
mechanics out of the shared file and pushes them here, one file per
target. The four real registry mechanics documented so far converged on
the same shape regardless: what to bump, what CI does automatically
once that lands, and a closing account of what provenance the registry
actually offers. That convergence is the evidence the shape is worth
keeping, not an accident of copying the first file written.

## The shared skeleton, in order

1. **Title + blockquote** — pointing at
   [`../releases/README.md`](../releases/README.md) for the shared
   pipeline shape this file's own steps plug into.
2. **One paragraph of context** — the registry name, and the safety-net
   framing a real version pin gives a consumer, contrasted with however
   that language vendors an internal/local dependency (an editable
   install, a project reference, a path dependency, a `file:`
   dependency) to make the contrast concrete rather than abstract.
3. **Numbered release procedure** — exactly three steps for every
   language: bump that manifest's own version field, add a changelog
   entry in the same commit (never backfilled), merge to `main`. State
   plainly that the merge *is* the whole release for every version after
   the first — and if it genuinely isn't (see item 5), say so
   immediately rather than let the numbered list imply something the
   next section contradicts.
4. **One paragraph naming what CI does automatically** — the
   detect → test → build → publish → tag → release chain, in that order,
   each step gated on the one before it. Never repeat the shared
   pipeline's own reasoning for *why* that order exists —
   [`../releases/README.md`](../releases/README.md) already covers it
   once; link back to it if a specific step needs pointing at.
5. **One paragraph on the version field as source of truth** — that CI
   asserts the manifest version matches the tag being pushed and fails
   loudly on drift, rather than silently publishing a mismatch.
6. **Zero or more registry-specific highlight sections** — a
   publish-is-a-token-exchange mechanic, a first-publish trap unique to
   that registry, a two-phase workflow shape forced by a tag-trigger
   requirement, an explicitly-recorded gotcha in how the Trusted
   Publisher's own settings must match the workflow file exactly. Not
   required, not capped; add exactly as many as that registry's own
   mechanics genuinely earn. A comparison to how a sibling registry
   solves the same problem is fine here and often the clearest way to
   state it (NuGet can bootstrap a first publish where npm and pub.dev
   cannot) — this file is read by a maintainer already comparing
   registries, not an installer with no reason to know another one
   exists.
7. **`## Provenance`** — what mechanism that registry offers (or the
   honest absence of one, stated plainly rather than left looking
   accidental), whether it's automatic under Trusted Publishing or needs
   something extra, and a real, checkable reference (an actual shipped
   version's own attestation, a real API endpoint that returns the
   field). Close with a pointer to
   [`../supply-chain-and-ownership.md`](../supply-chain-and-ownership.md)
   for how this registry's mechanism compares to every other one this
   project targets.

## What's genuinely registry specific

The skeleton above is fixed; the content of items 3 and 6 is where a
registry's own real mechanics belong — a manual first-publish trap, a
token-exchange step, a two-phase tag-triggered shape, a Trusted
Publisher setting that must match a workflow declaration exactly. None
of that content is shared, and none of it should be generalized away
into vague language that would apply to any registry; state the real
mechanic, named, the way each existing file does.

## Adding a new language's release procedure

Add `docs/maintenance/releases/<language>.md` to the existing directory
— nothing in [`../releases/README.md`](../releases/README.md) or a
sibling language's own file changes to make room for it. Research that
registry's actual publishing model before writing the procedure, the
same way [`../adding-a-language.md`](../adding-a-language.md)'s own
Stage 0 asks every other registry question to be answered in writing
before any code exists.

## Related

- [`README.md`](README.md) — the general standard this template builds
  on.
- [`../releases/README.md`](../releases/README.md) — the shared release
  pipeline this file's own steps plug into.
- [`../supply-chain-and-ownership.md`](../supply-chain-and-ownership.md) —
  where a registry's provenance detail moves permanently once that
  language actually ships, per that doc's own stated rule.
