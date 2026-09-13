<!-- Title: The Release Pipeline, Shared Across Every Language -->
# The release pipeline, shared across every language

> The part of a release that is identical no matter which language or
> target is shipping — job ordering, when each check runs, and why
> nothing is tagged or published by hand. A given target's own concrete
> steps (which file to bump, which workflow notices it) live in that
> target's own sibling file in this directory — today
> [`verdict-agent-skill.md`](verdict-agent-skill.md) and
> [`python.md`](python.md). A new language
> adds its own `<language>.md` here; nothing in this file changes for it
> to do so.

## Nothing is tagged or published by hand

Merging a version bump is the decision to release; there is no
separate tag step. That is not only convenience. A hand-pushed tag can
point at any commit, including one whose tests never ran — and since
publishing is irreversible on every registry this repo targets, an
untested release is a version number burned. Making the test matrix a
dependency of the publish job removes that possibility rather than
relying on discipline.

The ordering matters and is enforced by job dependencies: **test →
build → publish → tag → release**. The tag is created *after* a
successful publish, so a tag existing means that version really shipped.
Re-running is safe: the detect job compares the manifest version against
existing tags and does nothing if one already matches, so merging to
`main` repeatedly cannot double-release.

The parts of a release that are identical for every language — the
changelog section, the skill artifacts, the tag, the GitHub release —
live in `release-github.yml`, a reusable workflow the per-language ones
call. Adding a language is a thin caller, not another copy.

**Publishing itself deliberately stays in each language's own workflow**,
and must. PyPI Trusted Publishing does not work with reusable workflows —
a documented limitation, not a configuration problem — so moving the
publish step would break authentication outright. That constraint costs
nothing, because publishing is the one genuinely per-language part of a
release: PyPI publishes over OIDC directly, npm cannot bootstrap a first
publish at all, NuGet exchanges a token for a one-hour key, and pub.dev
drives its own reusable workflow from a tag pattern. There is no shared
step there to factor out — only the tail after it.

## Verifying a release, and when

Checks are worth having at the earliest point they can possibly run, because
the tail of a release is too late to learn anything: the registry upload
happens before the tag and the GitHub release, and no registry this repo
targets lets you take a version back.

So the checks are spread across three moments rather than gathered into one:

**Before merging** — `check-release-readiness.yml`, on every pull request.
Every manifest version has a matching section in *its own* changelog, each
changelog is ordered newest-first, and `scripts/build.sh` produces all three
skill artifacts with an intact payload.
A missing changelog section fails here, while the change is still a proposal.

**Before publishing** — the `detect` job re-checks the changelog section, so
anything that reached `main` another way still cannot publish. Without it, a
missing section would be discovered *after* the registry upload, leaving a live
version with no tag and no release.

**After publishing** — the release job verifies what is deterministic: the
notes rendered with their code spans intact (a regression guard for
`.agents/incidents/001`), every expected asset attached, and `scripts/get.sh`
still resolving `verdict-tools.zip` with a real payload inside it.

**By hand, once propagation settles** — two things a job should not wait on:

1. The version is live and **installable** from the registry.
2. **Provenance is present** where the registry offers it — see
   [`../supply-chain-and-ownership.md`](../supply-chain-and-ownership.md).
   Its absence is silent, which is exactly why it is worth looking at.
