<!-- Title: Verdict Agent Memory -->
# `.agents/memory/` — agent memory

> The in-repo home for durable agent recall — the things an AI agent
> would otherwise write to a harness's own global memory store and lose
> the moment the session, machine, or tool changes. This is a
> deliberate repo-local override of the context-fence default
> (`docs/agent-memory/`): agent working material belongs under
> [`.agents/`](../README.md), not mixed in with this repo's published
> docs, which are written for human readers of the package.

## What belongs here

One file per durable fact or convention an agent learned and will need
again — preferences confirmed with the maintainer, non-obvious
constraints, conclusions that cost real work to reach. Name each file
for the fact it holds, in kebab-case, and open it with the same
blockquote framing every doc in this repo uses.

## What does not belong here

- **Standing rules** → [`AGENTS.md`](../../AGENTS.md), which every
  harness reads natively and [`CLAUDE.md`](../../CLAUDE.md) imports.
- **Plans, specs, playbooks** → [`../plans/`](../plans/) — the sibling
  for work that's *going to happen*, as opposed to facts that are
  *already true*.
- **Session state** → [`HANDOFF.md`](../../HANDOFF.md), which is
  rewritten each session and carries its own staleness marker.
- **Anything already recorded by the repo itself** — the code, the git
  history, a package's own `CHANGELOG.md`, or the docs under
  [`docs/`](../../docs/). Memory is for what those don't say.
- **Anything transient** — a scratch log, a one-off script, a note
  that stops mattering when this session ends. Those go in
  [`../scratch/`](../scratch/) (gitignored).

## Index

| Entry | Hook |
| :-- | :-- |
| [a-behaviour-change-is-a-documentation-change](a-behaviour-change-is-a-documentation-change.md) | A behaviour change is not done until every doc that described the old behaviour is corrected |
| [adding-a-variant-is-a-new-file](adding-a-variant-is-a-new-file.md) | Adding a variant is a new file, never an edit to a shared one |
| [an-adopters-needs-are-input-not-requirements](an-adopters-needs-are-input-not-requirements.md) | An adopter's needs are input to a decision, never the decision |
| [ascii-only-in-data-and-config-values](ascii-only-in-data-and-config-values.md) | No em-dash, or other non-ASCII typographic punctuation, in a data/config value |
| [avoid-harness-meaningful-filenames](avoid-harness-meaningful-filenames.md) | A doc named after a harness's own convention can be silently misread by it |
| [ci-automation-follows-each-languages-manual-release](ci-automation-follows-each-languages-manual-release.md) | CI/CD automation lands after a language's manual first release, never before |
| [context-shape-is-decided-by-when-its-known](context-shape-is-decided-by-when-its-known.md) | Whether a sample's context should be typed or dict-shaped has one test |
| [doc-hygiene-audit-protocol](doc-hygiene-audit-protocol.md) | Doc hygiene audit protocol |
| [docs-consolidate-code-stays-in-its-own-tree](docs-consolidate-code-stays-in-its-own-tree.md) | A fixture's docs and data can consolidate centrally; its code cannot |
| [features-land-in-every-language](features-land-in-every-language.md) | A capability goes into every language, or into none |
| [first-party-dependencies-are-permitted](first-party-dependencies-are-permitted.md) | The dependency rule is "no third-party", not literally zero |
| [no-organizations-individual-forever](no-organizations-individual-forever.md) | This package is published by an individual, on every registry |
| [public-surfaces-stay-professional](public-surfaces-stay-professional.md) | Nothing public carries competitive analysis or working narration |
| [publish-over-oidc-not-tokens](publish-over-oidc-not-tokens.md) | Publish over OIDC on every registry — provenance depends on it |
| [releases-are-language-scoped](releases-are-language-scoped.md) | Releases are language-scoped, deliberately — never one repo-wide version |
| [research-the-ecosystem-before-deciding-its-idiom](research-the-ecosystem-before-deciding-its-idiom.md) | Research an ecosystem's own conventions before deciding what its idiom is |
| [run-costly-subagent-batches-sequentially](run-costly-subagent-batches-sequentially.md) | A batch of costly subagents runs one at a time, never in parallel |
| [shared-docs-never-name-one-languages-file](shared-docs-never-name-one-languages-file.md) | A shared doc naming one language's file is a forced edit waiting to happen |
| [skill-evals-are-additive-per-target](skill-evals-are-additive-per-target.md) | Skill evals are per-target, and adding one is additive |
| [skill-version-is-independent-of-sdk-versions](skill-version-is-independent-of-sdk-versions.md) | `plugin.json`'s version is Claude Code's skill-update signal, nothing more |
| [terse-tabulated-summaries-over-prose](terse-tabulated-summaries-over-prose.md) | Tabulate a summary; don't write an essay |
| [verdict-is-a-protocol-spec](verdict-is-a-protocol-spec.md) | verdict is a protocol spec, not a literal API port |
| [verify-doc-links-with-a-real-checker-not-grep](verify-doc-links-with-a-real-checker-not-grep.md) | A large doc restructuring needs a real link/anchor checker, not grep |
| [when-you-decline-record-which-question-failed](when-you-decline-record-which-question-failed.md) | A declined candidate needs the right reason recorded, not just "no" |
