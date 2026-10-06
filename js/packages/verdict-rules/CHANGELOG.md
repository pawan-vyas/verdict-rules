# Changelog

Release history for the `verdict-rules` JS/TS package. Format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/); versioning follows
[semantic versioning](https://semver.org/), scoped to this package — it
releases independently of the other language SDKs and of the AI-agent skill.

Tagged `js-vX.Y.Z`.

## [0.4.0] - 2026-10-03

- **The `marketplace_eligibility` example reads its policy thresholds from
  `fixtures/marketplace_eligibility/thresholds.json`** rather than declaring
  its own literals, so the four ports cannot drift from each other or from
  the data their suites assert against.
- **Doc comments in this package's own source state current behaviour only.**
  `PredicateOutcome` and `AndRule` each explained themselves by contrast with
  an earlier design, which `0.3.1` had claimed was already done. Each keeps its
  rationale in present tense; the claim now holds.
- **Added `CompositeRule<TContext>` and `isCompositeRule`** — an interface a
  rule built from other rules satisfies, exposing `subRules`: the parts it
  was built from, readable before anything is evaluated, plus a type guard
  that narrows to it. `AndRule`/`OrRule`/`NotRule` implement it; a
  `FunctionRule` does not, so "structure or terminal check" is answerable
  without naming concrete classes. A negation reports a one-element array
  rather than a differently-named single rule, so a walk needs no knowledge
  of which composite it holds, and a vacuous composite reports an empty one.
  Narrow with `isCompositeRule`, never an `instanceof` chain over the three
  built-ins: a chain silently walks past any other composite, a consumer's
  own included, reporting the rules inside it as absent rather than failing.
  The guard is structural, so a consumer's composite narrows exactly as a
  built-in one does. This matters more here than in the other SDKs: the
  built-ins store their parts in a `#private` field, so `subRules` is the
  only way to read them at all. `Rule` is unchanged.
- `FunctionRule`, `AndRule`, `OrRule`, `NotRule` and `RulesEngine` now
  define `toString()` and `[Symbol.for('nodejs.util.inspect.custom')]`. A
  rule or engine printed via `console.log` or in the Node REPL previously
  showed only the bare class dump; it now shows `FunctionRule "name"`
  (or `"name" (group)`), `AndRule "name" (group) — N sub-rule(s)`,
  `OrRule "name" (group) — N sub-rule(s)`, `NotRule "name"`, or
  `RulesEngine — N rule(s), M group(s)`.
- **`RuleResult.subResults`** — a composite's own children, in
  evaluation order, holding exactly what it evaluated: never padded to
  the full sub-rule list, never flattened into the parent. An empty
  `subResults` *is* the leaf signal, structurally.
- **`RuleResult.leaves`/`failingLeaves`**, and the same pair on
  `RunResult`, flattened across every rule a run evaluated. A consumer
  keying an audit trail on the refusing rule can read
  every `ruleName` in `result.failingLeaves` without knowing the tree's
  shape -- the whole list, since a failed `AndRule` reports the failing
  leaves of the one sub-rule that stopped it, which is a single leaf only
  when that sub-rule is itself a leaf.
  `failingLeaves` is an independent recursion, not a filter over
  `leaves`: a passed result contributes none even past an earlier
  short-circuited branch that failed, and a failed result with no
  failing children is itself the leaf.
- **`RuleResult.decidedBy`** — which of `subResults` explain *this*
  result's own verdict. One level, non-recursive; not the same question
  `failingLeaves` answers. A prototype getter, derived from
  `decidedByIndices`, which is the stored field a constructor call passes:
  the positions of the deciding children within `subResults`, not the
  children themselves. Holding the same results under two fields makes the
  stored graph a DAG, and `JSON.stringify` expands a shared node once per
  path — serialized size doubled per nesting level, measured at 13 MB for
  sixteen levels. An index naming a child the result does not have throws
  `RangeError` at construction.
- **A result serializes**, and its instances are frozen. `JSON.stringify`
  emits the stored fields only, the derived getters being non-enumerable.
  Note that an unset `data` produces no key at all rather than `null`,
  since `JSON.stringify` omits `undefined`-valued properties.
- **`NotRule`** — passes exactly when its one wrapped rule fails.
- **`SequentialEvaluator`/`ShortCircuitEvaluator`** — the sequencing
  `AndRule`/`OrRule` compose, now public so a custom composite composes
  the same primitive rather than hand-rolling a loop.
- **A predicate returns a `PredicateOutcome`, not a `RuleResult`.**
  Migration: `return { ruleName: n, passed: x }` becomes
  `return { passed: x }`. The `FunctionRule` wrapping it owns the name,
  so a predicate can no longer set a `ruleName` that silently disagrees
  with the rule it belongs to — one returning a `RuleResult`-shaped
  object throws `TypeError` rather than appearing to work.
- **A composite's children live in `subResults`, not `data`.**
  Migration: the `result.data as RuleResult[]` walk every caller wrote
  becomes `result.subResults`, or `result.failingLeaves` if the goal was
  the refusing leaf. `data` stays opaque.
- **`AndRule`/`OrRule` leave their own `detail` empty.** The failing
  sub-rule and its own detail are in
  `subResults`/`decidedBy`/`failingLeaves`. Migration, reproducing 0.3's
  own `AndRule` text for a flat composite:

  ```ts
  result.failingLeaves.map((l) => `'${l.ruleName}' failed: ${l.detail}`).join(" | ")
  ```

- **`RunResult.failingLeaves` disagreed with
  `RuleResult.failingLeaves`** in both directions. It filtered the
  flattened leaves by `!passed` instead of forwarding to each result's own
  getter, and a result's verdict is not a function of its leaves'
  verdicts: a failed `NotRule` wraps a child that *passed*, so filtering
  found a passing leaf and reported no failure on a failed run, while a
  passed `OrRule` holding a recovered-from failed branch reported that
  branch as a failure on a passing run. Now forwards per result.
  `RunResult.leaves` was always correct and is unchanged. Reported from
  downstream use of the Dart package; the same defect was present in all
  four SDKs.
- **A custom rule that put its children in `data` now reads as a
  *leaf*.** A behaviour change rather than a fix, and the silent one: a
  hand-rolled composite — a negation written as
  `new RuleResult(name, !inner.passed, { data: [inner] })`, say — still
  compiles and still evaluates, but its child disappears from `leaves`
  and `decidedBy`, which report it as a single terminal check.
  Migration: pass children as `subResults`, with `decidedByIndices`
  naming the ones that explain the verdict — or wrap `NotRule`, if the
  custom rule was only ever a negation.
- **A composite's sub-rules and a result's children are copied on
  construction, not aliased.** A caller that kept the array it passed
  could change a composite's sub-rules — and its verdict — after
  construction, and pushing a result onto the very array it was built
  from produced a result containing itself. `Object.freeze` does not
  help: it seals the instance, not an array the instance points at.
- **`RuleResult` and `RunResult` are classes with public constructors**,
  replacing the plain interfaces and the unexported `buildRuleResult`/
  `buildRunResult` factories. `new RuleResult(ruleName, passed, { detail,
  data, subResults, decidedByIndices })` is how a custom rule or composite builds
  its own result — the same constructor-based shape Python, Dart, and C#
  already use. Both are exported as values, not type-only.
- **`leaves`/`failingLeaves` are computed accessors rather than stored
  fields.** A leaf result's own leaves list is `[itself]`, so storing it
  placed a reference to the result inside the result. Any tree-shaped
  traversal of that structure — `JSON.stringify`, a structured logger, a
  reflection-based mapper — recursed until it gave up. Computing on access
  keeps the object graph acyclic, so a result now serializes. `leaves`/
  `failingLeaves` are absent from `JSON.stringify` output, being
  accessors; they are derivable from `subResults`/`passed` at any time.
- A custom composite must construct its result rather than spreading one.
  `{ ...result, detail: "..." }` yields a plain object without the
  accessors, which is not a `RuleResult`.

## [0.3.1] - 2026-09-18

- Every doc comment and inline comment in the package's own source
  trimmed to state current behavior only -- design rationale,
  alternatives-considered framing, and cross-references to the deeper
  docs for "the full reasoning" cut, not relocated.

## [0.3.0] - 2026-09-18

- `Rule<TContext>` is now generic over the context it reads from, along
  with `FunctionRule`, `AndRule`, `OrRule`, and `RulesEngine` — no
  default type parameter. Dict-context is `Rule<Context>`, written out
  explicitly. `TContext` is inferred from a predicate's own parameter
  type.
- [`docs/architecture/js.md`](https://github.com/pawan-vyas/verdict-rules/blob/js-v0.3.0/docs/architecture/js.md) gained a "Generic context, concretely"
  section.

## [0.0.7] - 2026-09-17

- **The CDN section no longer hardcodes a version+hash pair for the
  pinned `<script>` example** — the exact problem the `0.0.6` fix
  itself only patched, not removed (this same pin went stale twice
  now). The ESM import switches to `@latest`, which never needed the
  integrity hash in the first place (documented in the same section:
  the CDN's on-the-fly transform has no stable bytes to hash, so there
  was nothing for a hash to protect there). The pinned `<script>` +
  `integrity` example is now an explicit placeholder pointing at
  [jsdelivr's own package page](https://www.jsdelivr.com/package/npm/verdict-rules),
  which generates the exact tag with the correct hash for whichever
  version is picked — a static value here would only ever be correct
  for the version that existed when it was written.
- **unpkg and esm.sh are now linked directly**, alongside jsdelivr,
  rather than named without a link.

## [0.0.6] - 2026-09-16

- **`## Install` now also shows the bare import statement**
  (`import { AndRule, FunctionRule, RulesEngine } from "verdict-rules"`),
  matching the Python package page's own Install section -- previously
  the import only appeared inside the first-example code block further
  down the page.
- **The first-example heading is now "A first rule"**, not the generic
  "Use" -- the shared package-README template explicitly names a
  generic "Usage"-style heading as the thing to avoid, and Python's own
  page already used the correct idiom.
- **Fixed the CDN example's stale version pin**: `verdict-rules@0.0.2`
  in both the `<script src>` and the `+esm` import -- three releases
  behind the package's actual current version. The integrity hash was
  already correct (verified against the live CDN for real): every
  release between `0.0.2` and `0.0.5` was documentation/metadata-only,
  so the bundle's own bytes never changed, only the version number
  needed updating.

## [0.0.5] - 2026-09-16

- **The structural-typing example swaps `overEighteen`/`age` for
  `isBusinessHours`/`hour`** -- kept distinct from the main example's
  own `age` field, which already lives on the same page.
- **[`docs/quickstart.md`](https://github.com/pawan-vyas/verdict-rules/blob/js-v0.0.5/js/packages/verdict-rules/docs/quickstart.md)'s complete example now nests a composite**
  (`AndRule` containing an `OrRule`, one branch of which is itself a
  further `AndRule`) and demonstrates `runGroup` alongside a passing
  and a failing call, rather than repeating the same flat two-rule
  `AndRule` + `runNamed` shape the package README's own first example
  already covers. Package identical to `0.0.4` otherwise.

## [0.0.4] - 2026-09-15

- **`keywords` drops `policy` and adds `decision-engine` and `async`**,
  now `rules-engine`, `rule-evaluation`, `eligibility`, `decision`,
  `decision-engine`, `async`. `policy` names a rule together with what
  happens when it's enforced -- this package only ever evaluates and
  never acts on the result, so a "policy" keyword invited a search for
  something this package doesn't do. `decision` stays: it's this
  package's own output, a judgment reached, not an act of enforcing
  anything. Package identical to `0.0.3` otherwise.

## [0.0.3] - 2026-09-15

- **Drops the "Where to go next" row pointing back at the top-level
  `README.md`.** `docs/architecture/` is already linked in the same
  table and already covers "why it's shaped this way," in more depth
  than a narrative pointer would — a second row saying the same thing
  again was dead weight, not a different fact.

## [0.0.2] - 2026-09-15

- **The npm landing page's first example now demonstrates `RulesEngine`**,
  wrapping the rule and running it by name instead of stopping at the
  composite's own `.evaluate()` — `RulesEngine` is one of the five named
  primitives, and the first example is meant to show all of them.
  `package-readmes.md`'s own authoring template now mandates this for
  every language's first example and [`docs/quickstart.md`](https://github.com/pawan-vyas/verdict-rules/blob/js-v0.0.2/js/packages/verdict-rules/docs/quickstart.md)'s worked
  example alike, so the two never demonstrate a different subset of the
  API from each other.
- **Drops the `## Development` section.** It duplicated a subset of
  `CONTRIBUTING.md`'s own JS/TS section on a page a consumer landed on
  to install the package, not to change it — `package-readmes.md` no
  longer mandates this section for any language.
- **Cuts the blockquote to a single fact.** The second sentence pointed
  to the top-level `README.md` while narrating what this file itself
  was for — the document talking about itself, the same failure
  `package-readmes.md` already names for every other doc in this repo.
  That pointer moves to `## Where to go next`'s own first row instead.

## [0.0.1] - 2026-09-14

Initial publish.

- `Rule` (a structural `interface`), `FunctionRule`, `AndRule`, `OrRule`,
  `RulesEngine`, `RuleResult`, `RunResult`.
- Sequential, never concurrent evaluation, so short-circuiting is a real
  contract rather than a best-effort optimisation.
- Vacuous-truth polarity decided per composite: `AndRule([])` passes,
  `OrRule([])` fails.
- Unknown rule names and unknown groups throw rather than returning a vacuous
  pass — emptiness folds to an identity, absence is an error.
- `RulesEngine.tryRunNamed` / `tryRunGroup`, returning `undefined` rather than
  throwing when nothing matches — the primitives the throwing forms are built
  on. `undefined` means absent, never failed.
- `RulesEngine.ruleNames` / `groupNames` for enumerating an engine.
- `UnknownLookupError`, carrying `kind` and `key`, so an unknown lookup is
  catchable by type rather than by matching message text.
- Ships as ESM, CommonJS, and an ES2019 global bundle for a plain `<script>`
  tag or a CDN URL. Node 18+, zero runtime dependencies, types included.
