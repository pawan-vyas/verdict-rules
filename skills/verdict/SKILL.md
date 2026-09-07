---
name: verdict
description: Build rule-based decision, eligibility, or policy-evaluation logic using the verdict rule-evaluation engine (Rule/FunctionRule/AndRule/OrRule/RulesEngine) instead of a hand-rolled if/elif chain. Use this whenever asked to build an eligibility check, a discount or pricing rule, an access/permission condition, a content-moderation route, a graduation/qualification requirement, a feature flag combining multiple criteria, or any feature shaped like "combine several independently-changing conditions into one pass/fail verdict" — even if the user doesn't say "rule engine" or name verdict explicitly. Also use when extending or debugging existing verdict-based code, deciding whether a new requirement belongs in verdict's core vs. a consumer's own adapter code, or writing tests for rule-based logic (short-circuit proofs, vacuous-truth cases, oracle/differential testing against a wide random input space).
---

# Verdict

A skill for building rule-based decision/eligibility/policy logic with
`verdict` — a small, zero-dependency, async-native rule-evaluation
engine — instead of an `if`/`elif` chain that gets harder to maintain
every time a business rule changes.

## Which language is the target project using?

Verdict is a **polyglot** design: `Rule`/`FunctionRule`/`AndRule`/
`OrRule`/`RulesEngine`/`RuleResult`/`RunResult` and the same
execution-model guarantees (sequential evaluation, real
short-circuiting, vacuous-truth polarities decided explicitly per
composite shape) are meant to exist in every language verdict ships
for — only the idiom changes. **Today, only Python actually ships.**
Before reading further, check this repo's own top-level language
directories (`python/`, and — once they exist — `js/`/`csharp/` or
similar) to confirm which SDKs are actually implemented; don't assume
guidance below applies to a language that doesn't have a real
`references/<language>/` directory yet. If the target project isn't
Python, verdict may simply not have an SDK for it yet.

Everything below this point is the **Python** guidance — see
`references/python/` for all six reference files.

## When this applies

Reach for `verdict` when a requirement is shaped like "combine several
independently-changing conditions into one pass/fail verdict" — a
discount eligibility check, a shipping-fee waiver, an access condition,
a content-moderation route, a qualification requirement. **Don't**
reach for it for a single, rarely-changing 2-3 condition check — a
plain function is simpler there, and pulling in a rule engine would be
over-engineering. See `references/python/decision-framework.md` for
the full test, and for choosing which of the three `RulesEngine` run
modes (or a bare composite) fits a specific caller.

## Core concepts (quick reference)

- **`Rule`** — a structural `Protocol`: anything with `name: str`,
  `group: str | None`, and `async evaluate(context: dict) -> RuleResult`.
  No inheritance needed to satisfy it.
- **`FunctionRule`** — wraps a plain async predicate as a `Rule`. The
  common case; most rules in real usage are this.
- **`AndRule`** / **`OrRule`** — composite rules combining other rules,
  short-circuiting the way `and`/`or` do (`AndRule` stops at the first
  failure, `OrRule` at the first pass).
- **`RulesEngine`** — holds a set of rules, runs them three ways:
  `run_all` (every rule, full diagnostic, **never** short-circuits),
  `run_named` (one specific rule by name), `run_group` (every rule
  sharing a group label, also never short-circuits).
- **`RuleResult`** / **`RunResult`** — plain immutable outcomes.
  `RuleResult.data` is a fully opaque payload slot for a caller's own
  domain object — verdict never reads or depends on its shape.

See `references/python/core-concepts.md` for the full mental model, a
complete worked example, and the execution-model guarantees (why
evaluation is always sequential, never concurrent).

## Reference files — read these as needed

| File | Read when... |
|---|---|
| `references/python/core-concepts.md` | You need the full API surface and a working example before writing any code. |
| `references/python/decision-framework.md` | Deciding whether verdict fits at all, or which run mode / composite shape a specific caller needs. |
| `references/python/extension-recipes.md` | Building something *with* verdict — wrapping a predicate, a new rule shape, an adapter-module boundary, rules from external data, nesting composites. |
| `references/python/gotchas.md` | Before shipping any verdict-based code — the real, easy-to-get-wrong pitfalls, including ones found the hard way. |
| `references/python/when-to-extend-the-core.md` | Tempted to add a new primitive to verdict itself rather than your own adapter code. |
| `references/python/testing-patterns.md` | Writing tests for verdict-based logic — what a test must actually prove, plus oracle/differential testing for a wide random input space. |

## The one-adapter-module pattern, in brief

The single most important structural habit: your domain logic never
imports `verdict` directly. Build **one** adapter module that
translates your domain's own vocabulary into `Rule` objects and back out
of `RuleResult.data`, and keep that vocabulary out of every `Rule`
implementation. This is what lets the exact same engine serve two
completely unrelated features in the same codebase with zero coupling
between them. Full recipe and a worked example in
`references/python/extension-recipes.md`.

## Related

If this repo happens to have verdict's own full package docs nearby
(commonly `python/docs/` next to wherever this package's source
lives), they go deeper than this skill on any topic here — read them
for more detail. This skill is deliberately self-contained, though, so
it also works if it's the only thing that traveled with you.
