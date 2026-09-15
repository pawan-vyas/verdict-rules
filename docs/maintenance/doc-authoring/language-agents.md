<!-- Title: Language AGENTS.md Authoring Template -->
# Language `AGENTS.md` authoring template

> The structure every language's own top-level `AGENTS.md` follows.
> This builds on the general standard in [`README.md`](README.md) —
> read that first. Like a package README, these files don't
> consolidate under `docs/`: each lives at that language's own
> top-level directory, so an agent working inside it finds standing
> rules one level up rather than needing to know this repo's own
> `docs/` layout first.

## Why one template across files an agent reads in isolation

An agent working inside one language's directory reads only *that*
language's own `AGENTS.md` plus the repo root's — it has no reason to
open a sibling language's file in the same session, so drift here isn't
caught the way a reader-comparing-two-registry-pages catches it in
[`package-readmes.md`](package-readmes.md). The three later SDKs
converged on the same shape anyway, independently, which is itself the
evidence the shape is worth having: the same skeleton, arrived at three
times without a template forcing it, means the skeleton is actually
useful rather than imposed. This template exists to keep a fourth
language from having to rediscover it, and to bring the one language
that predates the convergence into the same shape.

## The shared skeleton, in order

1. **Title** — `# AGENTS.md — <Language> SDK` (`# AGENTS.md — Python SDK`,
   `# AGENTS.md — C# SDK`). The `SDK` suffix isn't decorative: a bare
   `# AGENTS.md — C#` trips markdownlint's `MD020` — a trailing `#`
   reads as a stray closed-heading marker — so every language carries
   the same suffix rather than only the one language that's forced to.
2. **Opening line** — one sentence pointing at the repo-root
   [`AGENTS.md`](../../../AGENTS.md) for the cross-language rules
   (portability, the two constraints, dispatch-as-a-table, diagram
   authoring), stating plainly that this file only adds what's specific
   to this language. Never restate a repo-root rule here — see
   [`README.md`](README.md)'s rule against narrating a document instead
   of saying the thing once.
3. **`## The guarantees, in <Language> terms`** — the same four
   execution-model guarantees stated in every language's `AGENTS.md`,
   each restated with that language's own real type and method names:
   sequential evaluation (naming the concurrency primitive to never
   reach for), vacuous truth's polarity, emptiness-vs-absence (naming
   the throw/raise type and the `try`-prefixed primitive), and
   `RuleResult.data`/`.Data` staying opaque. This is the fast-context
   version for an agent about to write code, distinct from
   [`../../architecture/`](../../architecture/README.md)'s own deeper
   "why" — an agent needs this restated here so it doesn't have to open
   a second document before writing a single line.
4. **`## Be precise about structural typing`** *(only where the
   language has a genuine, nameable gap)* — a language whose structural
   typing is total (nothing to contrast) skips this section entirely
   rather than manufacturing one; a language with no structural typing
   at all folds the one relevant fact into `## Conventions` instead of
   giving it a whole section. Where a real gap exists (a language with
   structural typing for single-member shapes like a delegate or
   function type, but not for a multi-member interface), open with what
   the language *does* have before stating the gap — "do not write that
   this language has no structural typing, it does, for X" — so the
   section can't be skimmed into the opposite, wrong conclusion.
5. **`## Layout`** — the workspace/package directory structure, and
   what specifically becomes additive (a new directory, a new glob
   match) when a second distribution is added under it. State the one
   or two things about that language's own workspace tooling that are
   easy to get wrong (an explicit `--package` flag a bare build command
   silently ignores, where build output actually lands), not a full
   restatement of the ecosystem's own documentation.
6. **`## Conventions`** — that language's own style and idiom rules:
   naming case, nullability/typing strictness flags, docstring format,
   immutable-value-type shape. Entirely language-specific; no shared
   content to enforce here beyond the heading itself.
7. **Zero or more language-specific highlight sections** — a
   distribution-shape-is-permanent warning, a debuggability note, an
   explicitly-recorded open design question not to resolve unprompted,
   an observation worth watching once that language's own evals exist.
   Not required, not capped; add exactly as many as that language's own
   situation genuinely earns.
8. **`## Before calling a change done`** — the exact, runnable
   command(s) for that language's own tooling, verified to actually
   work as written (a bare `dotnet build` from a directory with no
   solution file fails outright — name the project explicitly, and say
   so if the ecosystem's default invocation doesn't just work).
9. **`## Tests prove behaviour, not just booleans`** — closing section,
   stated in substance identically everywhere: short-circuiting is
   proven with a call log, never the final boolean; vacuous-truth
   polarities and unknown-lookup throws/raises each get their own test.
   Links to the repo-root [`../../testing/`](../../testing/README.md).

## Adding a new language's `AGENTS.md`

Follow the skeleton above; add only the language-specific highlight
sections that language's own situation genuinely earns. A comparison to
a sibling language's own behavior is fine here — unlike a package's own
landing page (see [`package-readmes.md`](package-readmes.md)'s three
leaks), this file is read by someone who opened it because they are
already working in this repo, not an installer with no reason to know
another SDK exists.

## Related

- [`README.md`](README.md) — the general standard this template builds
  on.
- [`package-readmes.md`](package-readmes.md) — the sibling template for
  each package's own shipped `README.md`, and why that file draws the
  opposite conclusion about cross-language comparisons.
- [`../../architecture/`](../../architecture/README.md) — the deeper
  "why" behind the guarantees this file's own section restates briefly.
