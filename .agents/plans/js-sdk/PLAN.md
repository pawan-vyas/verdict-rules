<!-- Title: JavaScript/TypeScript SDK Plan -->
# JavaScript/TypeScript SDK — triage & plan

> Planning for a second language alongside `python/`. **Nothing here is
> built, and nothing here is decided** beyond what the triage section
> records as fact. This exists so the real decisions can be made
> deliberately rather than re-derived under time pressure, and so the
> registry traps are known before anyone starts. **The pull request
> carrying this file is the development branch for this SDK** — it stays
> open and accumulates the implementation rather than being merged on
> its own. Registry research behind the naming decision is kept
> locally, outside this repo. Supersedes the JS/TS half of
> [`../polyglot-sdk-resume.md`](../polyglot-sdk-resume.md), which stays
> as the cross-language framing.

## 1 · Package name

`verdict` is unavailable on npm, as it is on every registry this package
targets. The chosen name is **`@verdict/core`** — scoped, so future
extension packages are additive inside a namespace we control. npm is
the only one of these registries with first-class namespacing, which is
why the scoped form is worth the extra characters here and not
elsewhere.

**Scope availability still needs confirming** before anything depends on
it — see the issue's first checklist item.

## 2 · Triage: publish path

Trusted Publishing via OIDC, mirroring what `release-python.yml`
already does for PyPI — no long-lived token in repo secrets.

- Supported on **GitHub Actions with GitHub-hosted runners**. Self-hosted
  runners are not supported.
- Workflow needs `permissions: id-token: write`.
- Requires **npm CLI v11+**.
- Provenance attestations are published **automatically** for a public
  repo publishing a public package; the `--provenance` flag is obsolete.

## 3 · Triage: prerequisites, in order

1. An npm account; an npm **org** as well if a scoped name is chosen.
2. **A manual first publish. This is the trap.** npm requires the
   package to already exist before a trusted publisher can be added —
   there is no equivalent to PyPI's "pending publisher", which is what
   the Python release path relies on. Publish a placeholder by hand at a
   version deliberately below the first real release (e.g. `0.0.0`) to
   create the package record, then configure the publisher, then let CI
   own every subsequent version. Plan for it rather than discovering it
   mid-release.
3. Configure the trusted publisher on the package's npm settings:
   org/username, repository, workflow filename (exact, with extension),
   optional environment. Up to 10 per package.
4. Decide `npm publish` vs `npm stage publish`. Publisher configurations
   created from 2026-09-03 onward automatically allow staged publishing,
   with direct publishing as a separate explicit opt-in.

## 4 · What must be preserved, not re-derived

The point of a second SDK is that the *design* is identical and only
the idiom changes. Judge any implementation against whether it actually
preserves this, not whether it feels similar:

- The same seven types: `Rule`, `FunctionRule`, `AndRule`, `OrRule`,
  `RulesEngine`, `RuleResult`, `RunResult`.
- **Sequential, never concurrent evaluation.** `AndRule`/`OrRule` must
  use a plain loop with `await`, **never `Promise.all`**. Short-circuiting
  only means something if later work never *starts*; concurrent
  scheduling would already have kicked off every sub-rule before the
  first result returned. This is the single easiest thing to "optimize"
  into incorrectness, because the tests still pass — the returned
  boolean is unchanged. See [`../../docs/architecture.md`](../../../docs/architecture.md).
- Vacuous-truth polarity decided explicitly per composite shape
  (`AndRule([])` passes, `OrRule([])` fails — deliberately asymmetric).
- `RuleResult.data` stays opaque: only what actually ran, never padded,
  never flattened.
- The one-adapter-module extension pattern
  ([`../../docs/extension.md`](../../../docs/extension.md), Recipe 3).

## 5 · First-pass concept mapping (validate against real code)

- **`Rule`** → a structural `interface`. TypeScript's structural typing
  is the closest available analogue to Python's `Protocol`: an object of
  the right shape satisfies it with no `implements`, no base class, no
  registration — the same property that makes `Rule` worth having.
- **`evaluate()`** → `async`, returning `Promise<RuleResult>`. The
  `Context` type needs its own real design pass; `Record<string, unknown>`
  is a placeholder, and `any` is not acceptable.
- **`AndRule`/`OrRule`** → plain classes implementing that interface,
  short-circuiting in a `for … of` loop with `await`.

## 6 · Proposed sequence (no work started)

1. Settle the open decisions in §8.
2. Scaffold the directory with `package.json`, `tsconfig.json`, and the
   language's own `AGENTS.md`, mirroring `python/AGENTS.md`'s role.
3. Port the seven types, then the test suite — including a
   short-circuit proof via a call counter and explicit vacuous-truth
   cases, per [`../../docs/testing.md`](../../../docs/testing.md).
4. Port the oracle/differential chaos suite from
   [`../../python/examples/graduation_verdict/`](../../../python/examples/graduation_verdict/) —
   an independent, deliberately naive re-implementation checked against
   a deterministically seeded input space, with every generator taking
   an explicit seeded instance so a failure reproduces from its seed
   alone. This is the most reusable technique from the Python side and
   is what actually proved that engine correct.
5. Add `test-js.yml`, path-filtered, as a **new file**.
6. Do the manual placeholder publish (§3.2), configure the trusted
   publisher, then add `release-js.yml` on a `js-v*` tag.
7. Add `skills/verdict/references/typescript/` and update the skill's
   language-routing note. Bump `.claude-plugin/plugin.json` in the same
   commit — `check-skill-version.yml` fails the push otherwise.

## 7 · Repo-side prerequisites

All already in place, and all additive — adding a language is new files,
never an edit to existing ones:

- `test-python.yml` is path-filtered to `python/**` and named to leave
  room for a `test-js.yml` sibling.
- `release-python.yml` triggers on `python-v*`, leaving `js-v*` free.
- `CHANGELOG.md` groups entries by language-scoped tag.
- `skills/verdict/references/` is already language-scoped.

One constraint that is **not** optional: every release workflow must
attach the full skill artifact set. `scripts/get.sh` resolves whatever
GitHub calls the latest release and pulls `verdict-tools.zip` from it,
so a release that omits it breaks the curl-pipe installer outright. See
`release-skill.yml`'s header.

## 8 · Explicitly not decided

- **Directory name**: `js/` vs `typescript/`.
- **Runtime targets**: Node only, or also Deno/Bun/browser.
- **Module format**: ESM-only, or dual CJS/ESM.
- **The `Context` type.** Needs a real design pass, not a placeholder.
- **Test runner.** Vitest is the ecosystem default; not a commitment.
- Whether the skill's language-routing note needs a deeper rewrite once
  a second language genuinely ships.
