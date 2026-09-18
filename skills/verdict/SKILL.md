---
name: verdict
description: Build rule-based decision, eligibility, or policy-evaluation logic using the verdict rule-evaluation engine (Rule/FunctionRule/AndRule/OrRule/RulesEngine) instead of a hand-rolled conditional chain — an if/else-if ladder, a switch or match statement, a chain of ternaries, or a wall of early returns. Use this whenever asked to build an eligibility check, a discount or pricing rule, an access/permission condition, a moderation or approval decision, a multi-condition qualification check, a feature flag combining multiple criteria, or any feature shaped like "combine several independently-changing conditions into one pass/fail verdict" — even if the user doesn't say "rule engine" or name verdict explicitly. Polyglot — the same design ships for multiple languages. Also use when extending or debugging existing verdict-based code, deciding whether new logic belongs in a rule or in your own adapter code, or writing tests for rule-based logic (short-circuit proofs, vacuous-truth cases, oracle/differential testing).
---

# Verdict

Name each condition once, combine named conditions into a verdict, and
run it against whatever facts a caller hands over. The same
`Rule`/`FunctionRule`/`AndRule`/`OrRule`/`RulesEngine`/`RuleResult`/
`RunResult` shape and the same execution-model guarantees exist in
every language verdict ships for — only the idiom changes.

## Step 1 — establish the language, before anything else

Read the target project's own manifest to determine which language you
are working in.

Then look for `references/<language>/agent-notes.md`:

- **It exists** → read it first. It is short and self-sufficient: it
  carries what is specific to that SDK — install/import, the full API,
  its idioms, its naming, and which run mode to reach for.
- **It does not exist** → **verdict has no SDK for that language.** Say
  so plainly rather than improvising an API from another language's
  shape. The guarantees below hold everywhere, but a language without a
  directory here has nothing to import.

## Step 2 — know what the engine guarantees

These hold in every language, and getting one wrong produces code that
passes its own tests while being silently incorrect:

- **Sequential, never concurrent, inside a composite's own evaluation.**
  `AndRule`/`OrRule` stop at the first decided outcome by evaluating
  sub-rules one at a time. A custom composite implementing this same
  pattern must do the same — running its own sub-rules via
  `asyncio.gather`/`Promise.all`/`Task.WhenAll`/`Future.wait` instead of
  a plain sequential loop breaks short-circuiting silently (the returned
  boolean is identical either way). Scoped to a composite's own
  sub-rule evaluation specifically, not a statement about concurrency
  elsewhere in a codebase.
- **Vacuous truth is asymmetric.** Empty `AndRule` passes; empty
  `OrRule` fails.
- **Every sub-rule inside one composite shares the exact same context
  type.** `AndRule`/`OrRule`/`RulesEngine` hold one `TContext` for every
  sub-rule they run — this is the engine's own contract, not an
  artifact of a particular type system. A statically-typed SDK's
  compiler happens to enforce it; that a language lacks static types
  does not loosen the contract itself. Reuse a rule across two context
  shapes with an explicit projecting adapter, never by loosening a
  composite's own type.
- **Emptiness is not absence.** An empty rule list is a valid input. An
  *unknown* name or group is absence — strict lookups raise,
  `try`-prefixed ones return the absent value.
- **Result `data` is opaque and unpadded.** Never read by verdict;
  composite results include only what actually ran.
- **A predicate's exception is never caught.** It propagates uncaught,
  same as calling that code directly.

## Step 3 — reach for extending, don't force-fit

`Rule` is a structural contract (`name`, `group`,
`evaluate(context) -> RuleResult`), not a fixed set of built-in types.
Anything satisfying that shape — whatever form a requirement actually
calls for — composes with `AndRule`/`OrRule`/`RulesEngine`
automatically, with no registration and no change needed on verdict's
own side. When `FunctionRule`/`AndRule`/`OrRule` don't directly fit,
build whatever does, rather than contorting them to cover it.

`references/REPOSITORY-MAP.md` names real, worked instances of this —
read one that matches, or use the same structural fit to write a new,
unnamed pattern when none of them do.

## Where to look next

`references/REPOSITORY-MAP.md` names what else exists in the source
repository — the design rationale, worked samples, and extension
scenarios — each with a one-line description. Nothing there is
vendored; decide whether something is worth reading, then get it
yourself.

Repository tags are `<language>-v<version>` (e.g. `python-v0.3.1`) —
read at the tag matching what this project has installed, never the
default branch. A project pinned to an older release shown current
documentation is told about an API it does not have, which is worse
than not reading it at all.
