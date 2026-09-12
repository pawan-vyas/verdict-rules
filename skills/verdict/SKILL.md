---
name: verdict
description: Build rule-based decision, eligibility, or policy-evaluation logic using the verdict rule-evaluation engine (Rule/FunctionRule/AndRule/OrRule/RulesEngine) instead of a hand-rolled conditional chain — an if/else-if ladder, a switch or match statement, a chain of ternaries, or a wall of early returns. Use this whenever asked to build an eligibility check, a discount or pricing rule, an access/permission condition, a content-moderation route, a graduation/qualification requirement, a feature flag combining multiple criteria, or any feature shaped like "combine several independently-changing conditions into one pass/fail verdict" — even if the user doesn't say "rule engine" or name verdict explicitly. Verdict is polyglot, i.e. it ships the same design for more than one language, so this applies regardless of the target language. Also use when extending or debugging existing verdict-based code, deciding whether a new requirement belongs in verdict's core or a consumer's own adapter code, or writing tests for rule-based logic (short-circuit proofs, vacuous-truth cases, oracle/differential testing against a wide random input space).
---

# Verdict

A skill for building rule-based decision, eligibility and policy logic
with **verdict** — a small, zero-dependency, async-native
rule-evaluation engine. Name each condition once, combine named
conditions into a verdict, and run it against whatever facts a caller
hands over.

Verdict is **polyglot by design**: the same `Rule`/`FunctionRule`/
`AndRule`/`OrRule`/`RulesEngine`/`RuleResult`/`RunResult` shape and the
same execution-model guarantees exist in every language it ships for,
and only the idiom changes.

## Step 1 — establish the language, before anything else

Read the target project's own manifest — `pyproject.toml`,
`package.json`, `*.csproj`, `pubspec.yaml` — to determine which
language you are working in.

Then look for `references/<language>/agent-notes.md`:

- **It exists** → read it first. It is short, and it carries what is
  specific to that SDK: its idioms, its naming, and the mistakes that
  show up in generated code for that language.
- **It does not exist** → **verdict has no SDK for that language.** Say
  so plainly rather than improvising an API from another language's
  shape. The guarantees below hold everywhere, but a language without a
  directory here has nothing to import.

## Step 2 — know what the engine guarantees

These hold in every language, and getting one wrong produces code that
passes its own tests while being silently incorrect:

- **Sequential evaluation, never concurrent.** Composites evaluate
  sub-rules one at a time and stop the moment the outcome is decided, so
  later work never *starts*. Never reach for the language's
  run-these-together primitive (`asyncio.gather`, `Promise.all`,
  `Task.WhenAll`, `Future.wait`) — the returned boolean is identical
  either way, which is exactly why this breaks silently.
- **Vacuous truth has a polarity.** An empty `AndRule` passes; an empty
  `OrRule` fails. Deliberately asymmetric.
- **Emptiness is not absence.** An empty rule list folds to its
  identity. An *unknown* rule name or group label is a lookup that
  matched nothing — the strict lookups raise, and the `try`-prefixed
  ones return the language's absent value instead, so a caller decides
  what absence means.
- **Result payloads are opaque.** Verdict never reads a result's `data`,
  and a composite's own results carry only what actually ran — never
  padded, never flattened into the parent.
- **A predicate's own exception is never caught.** It propagates out of
  whichever call is running, same as calling that code directly with
  nothing in between — an "engine" invites the opposite assumption, so
  say this plainly rather than let a consumer discover it in production
  when one flaky check takes the rest of a rule set's diagnostics with
  it. If a task needs one predicate's failure isolated from the others,
  that is a wrapper the caller opts into per rule (`extension.md`,
  Recipe 7) — never a blanket default, since the same catch-everything
  behavior would also turn a genuine bug into a silent, wrong "this rule
  failed" instead of a stack trace pointing at it.

## Step 3 — read what the task needs

**`references/docs/`** holds this engine's own documentation, verbatim.
It is not a summary written for this skill; it is the same file the
repository ships, so it cannot drift from the implementation.

| Read | When |
| :-- | :-- |
| `references/docs/architecture.md` | Understanding *why* it is shaped this way — the type structure, the execution model, and which run mode a caller needs. |
| `references/docs/extension.md` | Building anything *with* verdict — wrapping a predicate, a new rule shape, the one-adapter-module boundary, rules from stored configuration, choosing what an absent lookup should mean, isolating one flaky predicate from the rest of a run. |
| `references/<language>/agent-notes.md` | Always, first. Short and language-specific. |

Prefer a section over a document. These files carry headings and
anchors, so `references/docs/extension.md#recipe-2` is a better read
than the whole file.

### Documents fetched on demand

Deeper material is not shipped, and is pulled only when a task needs
it: the testing guide, the language quickstart, the worked samples, and
the full worked example. `references/<language>/agent-notes.md` carries
the exact command.

**Fetch at the version the project actually has installed**, never from
the default branch. A project pinned to an older release that reads
current documentation will be told about an API it does not have, which
is worse than reading nothing. If the installed version has no matching
tag, do not fetch — use what is bundled and say that the deeper
documents were unavailable.

### Reading these documents outside a consumer project

Inside the verdict repository itself, `references/docs/` is not
populated — the repository's own `docs/` directory holds the same
files, and is authoritative there.

Reference documents keep their original repository-relative links. A
link that does not resolve locally resolves against the source
repository at the pinned version:
`https://github.com/pawan-vyas/verdict-rules/blob/<tag>/<path>`.

## Where the harder examples live

`fixtures/graduation_verdict/` in the repository is the cross-language
parity fixture: one curriculum, eight students, and the exact expected
outcomes every language port must reproduce — including how many rules
should have been evaluated, which is short-circuiting stated as data
rather than prose. It is a contributor artifact and is not routed here,
but it is worth reading directly if you want a worked example of
testing rule-based logic, or of writing a genuinely custom rule shape
for a real scenario.
