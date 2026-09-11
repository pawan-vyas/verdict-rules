<!-- Title: Verdict Polyglot SDK Resume Notes -->
# Verdict — JS/TS + C# SDK Resume Notes

> Not a build-it-now implementation plan and not a commitment that
> either SDK will be built — just enough context to resume the work
> without re-deriving it. This repo is polyglot by design but **only
> Python ships today**; if and when a second language starts, this is
> the file to read first, then discard or fold into that language's own
> docs once its real decisions are made. Everything below is a
> first pass, deliberately marked as such.

## The core claim to preserve

Architecturally, nothing about verdict changes across languages:
`Rule` / `FunctionRule` / `AndRule` / `OrRule` / `RulesEngine` /
`RuleResult` / `RunResult`, the same execution-model guarantees
(sequential evaluation — never concurrent, so short-circuiting is a
real, reliable contract, not a best-effort optimization; vacuous-truth
polarities that differ by composite type, decided explicitly per shape
rather than inherited by accident), and the same one-adapter-module
extension pattern (a consumer's own domain vocabulary never leaks into
a `Rule` implementation; exactly one module per domain imports the
engine directly). Only the idiom changes per language. Every future SDK
should be judged against whether it actually preserves this claim, not
just whether it "feels similar."

## First-pass concept mapping

Not final — validate/refine when actually building each SDK against
real code, not against this compressed summary.

### TypeScript

- **`Rule`** → a structural `interface`. TypeScript's structural typing
  is the closest match to Python's `Protocol` of anything available in
  either target language — a plain object satisfying the interface's
  shape is a valid `Rule` with no explicit `implements` needed, the
  same "no registration, no base class" property the Python skill's own
  [`extension-recipes.md`](../../skills/verdict/references/python/extension-recipes.md)
  calls out as the whole point of `Rule` being structural there.
- **`evaluate()`** → `async`/`Promise`-based, same signature shape
  (`(context: Record<string, unknown>) => Promise<RuleResult>` or
  similar — the actual `Context` type needs its own real design pass,
  not just `unknown`/`any`).
- **`AndRule`/`OrRule`** → plain classes implementing that interface,
  same short-circuit loop-with-`await` shape as the Python
  implementation. **Never `Promise.all`** — for the identical reason
  [`docs/architecture.md`](../../docs/architecture.md) argues sequential
  evaluation is load-bearing: short-circuiting only means something if
  later work never starts, and concurrent scheduling would already have
  kicked off every sub-rule's coroutine before the first result comes
  back.
- **Packaging**: an npm package. Name availability is unchecked — a
  real, necessary follow-up task the moment this SDK actually starts,
  the exact same kind of check `verdict` itself needed on PyPI (which
  turned out to be taken, resolved as `verdict-rules` there — npm may
  or may not have the same conflict, don't assume either way).
- **Test runner**: likely Vitest, matching the ecosystem's current
  default — not a hard commitment, revisit when this actually starts.

### C#

- **`Rule`** → the one place this isn't a mechanical translation. C#
  has no free structural-typing equivalent to Python's `Protocol` (no
  duck-typing an arbitrary object into an interface shape at the
  language level), so `Rule` becomes a real `IRule` interface:
  ```csharp
  public interface IRule
  {
      string Name { get; }
      string? Group { get; }
      Task<RuleResult> EvaluateAsync(IDictionary<string, object> context);
  }
  ```
  C# consumers will need to explicitly `class MyRule : IRule` rather
  than just shaping an object correctly — a real, worth-naming
  difference from both Python and TypeScript's own stories, not a bug
  in the port.
- **`AndRule`/`OrRule`** → classes implementing `IRule`, same
  short-circuit-via-plain-loop shape (never `Task.WhenAll`, for the
  identical reason as TypeScript's `Promise.all` above).
- **Packaging**: a NuGet package. Name availability unchecked, same
  caveat as npm above.
- **Test framework**: likely xUnit, matching the ecosystem's current
  default — not a hard commitment.

### Both languages

Port
[`python/examples/graduation_verdict/`](../../python/examples/graduation_verdict/)'s
own chaos-suite technique — an independent, deliberately-dumb oracle
re-implementation checked against a deterministically-seeded random
input space (never the language's global/ambient random state; every
generator takes an explicit seeded instance so any single failing case
reproduces on its own from just its seed/index) — as each SDK's own
integration/regression net, not just hand-picked unit tests. This is
the single most reusable *pattern* from the Python side, entirely
independent of syntax, and it's the technique that actually proved the
Python engine correct across a wide space of generated
curricula/students in
[`test_chaos.py`](../../python/examples/graduation_verdict/test_chaos.py) —
worth deliberately re-deriving in each new language rather than
skipping because "the Python side already tested the shared design."

## Design source of truth to port from

Read these fresh in each new language's own context, rather than
translating this file's own necessarily-compressed summary above:
[`docs/architecture.md`](../../docs/architecture.md) (why sequential
evaluation, why `Rule` is structural, why `RuleResult.data` stays
opaque, the full type-structure class diagram) and
[`docs/extension.md`](../../docs/extension.md) (the five extension
recipes, especially Recipe 3's one-adapter-module boundary — the
pattern every consumer of every language's SDK should be steered
toward via that language's own skill reference, once one exists).

## Explicitly not decided yet — flag when picked back up

- npm package name, NuGet package ID — both need their own real
  availability check, not an assumption either is free.
- Per-language directory name alongside `python/`: `js/` vs
  `typescript/`; `csharp/` vs `dotnet/`. Whichever is picked, keep it
  consistent with that language's own ecosystem convention for
  referring to itself (e.g. `dotnet/` reads more natural to a C#
  audience than `csharp/` does, but `csharp/` is more literally
  parallel to `python/`/`typescript/`'s own naming — no strong
  preference recorded yet).
- Whether [`skills/verdict/SKILL.md`](../../skills/verdict/SKILL.md)'s
  language-routing note needs a deeper rewrite once a second language
  actually ships. The shape it assumes is a routing note plus a
  per-language reference directory alongside the existing
  `references/python/`; no real per-language reference content exists
  for languages that don't exist yet.
- Whether CI needs a matrix runner (Node + .NET installed alongside
  Python in one workflow) or fully separate workflow files per
  language. `.github/workflows/test-python.yml` already assumes the
  latter — it's path-filtered to `python/**` and its own header says it
  is named to leave room for `test-js.yml`/`test-csharp.yml` siblings,
  for the "purely additive, never a rewrite" reasoning. Carry that
  default forward unless a real reason to diverge shows up.
- Per-language tagging: `js-vX.Y.Z`/`csharp-vX.Y.Z`, matching the
  `python-vX.Y.Z` convention
  [`docs/maintenance.md`](../../docs/maintenance.md) establishes for
  Python and that `CHANGELOG.md` already groups entries by. Confirmed
  as the intended pattern, not yet exercised for real since neither SDK
  exists.
