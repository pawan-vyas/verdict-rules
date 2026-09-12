<!-- Title: Dart SDK Plan -->
# Dart SDK — triage & plan

> Planning for a Dart SDK alongside `python/`. **Nothing here is built,
> and nothing here is decided** beyond what the triage section records
> as fact. Unlike the JS/TS and C# plans, this one has no prior notes in
> the repo to build on — [`../polyglot-sdk-resume.md`](../polyglot-sdk-resume.md)
> predates any thought of Dart and covers only those two languages.
> **The pull request carrying this file is the development branch for
> this SDK** — it stays open and accumulates the implementation rather
> than being merged on its own. Registry research behind the naming
> decision is kept locally, outside this repo.

## 1 · Package name

`verdict` is unavailable on pub.dev. The chosen name is
**`verdict_rules`**, which is also the correct pub.dev form:
`[a-z0-9_]`, lowercase with underscores, and a valid Dart identifier.

pub.dev has **no namespaces at all** — names are flat, related packages
are distinguished by convention only, and there is no reservation
mechanism. A **verified publisher** is the only durable signal that a
`verdict_rules_*` package is ours, so it is worth setting up with the
first publish rather than later.

## 2 · Triage: publish path

Automated publishing from GitHub Actions via OIDC, configured on the
package's pub.dev admin page.

- Workflow needs `permissions: id-token: write`.
- `dart-lang/setup-dart` creates and configures the OIDC token.
- **A reusable workflow is provided and strongly encouraged**:
  `dart-lang/setup-dart/.github/workflows/publish.yml@v1`. Using it lets
  the Dart team own the publishing logic and enables pub.dev-side
  verification.
- Publishing triggers on a git tag matching a pattern configured on
  pub.dev.

## 3 · Triage: prerequisites, in order

1. A pub.dev account, and **uploader on the package or admin of its
   publisher** — required before automated publishing can be enabled at
   all.
2. **A manual first publish — the same trap as npm, and unlike NuGet.**
   pub.dev is explicit that only *existing* packages can be published
   automatically; the first version must go out via `dart pub publish`
   from a developer machine. Plan for it rather than discovering it.
3. **Configure the tag pattern** `dart-v{{version}}` on pub.dev's admin
   panel, and make the workflow's own tag filter agree. **Confirmed
   supported** — pub.dev requires only that the pattern contain
   `{{version}}`, and explicitly documents a prefix for repositories
   publishing more than one package (`my_package_name-v{{version}}`).
   The repo's `<lang>-vX.Y.Z` convention therefore holds with no
   exception for Dart.
4. **Decide how the reusable workflow coexists with the release
   invariant.** Every release workflow here must attach the full skill
   artifact set, because `scripts/get.sh` pulls `verdict-tools.zip` from
   whatever GitHub calls the latest release. A third-party reusable
   workflow cannot be edited to add that step, so the artifacts must be
   built and attached from a **sibling job** in our own workflow file.
   Workable — but it means `release-dart.yml` will not be structurally
   parallel to `release-python.yml`, and that should be a conscious
   choice rather than a surprise during implementation.

`pubspec.yaml` requires `name`, `version`, `description`, and an
`environment` SDK constraint. Omitting the SDK constraint is an error,
not a warning.

## 4 · What must be preserved, not re-derived

- The same seven types: `Rule`, `FunctionRule`, `AndRule`, `OrRule`,
  `RulesEngine`, `RuleResult`, `RunResult`.
- **Sequential, never concurrent evaluation.** `AndRule`/`OrRule` must
  short-circuit in a plain `for` loop with `await`, **never
  `Future.wait`** — the same exclusion as `Promise.all` and
  `Task.WhenAll` in the other SDKs, for the reason
  [`../../../docs/architecture.md`](../../../docs/architecture.md)
  gives: short-circuiting only means something if later work never
  *starts*. The returned boolean is identical either way, so the tests
  keep passing while the guarantee is gone.
- Vacuous-truth polarity decided explicitly per composite shape
  (`AndRule([])` passes, `OrRule([])` fails — deliberately asymmetric).
- `RuleResult.data` stays opaque: only what actually ran, never padded,
  never flattened.
- The one-adapter-module extension pattern
  ([`../../../docs/extension.md`](../../../docs/extension.md), Recipe 3).

## 5 · Design notes specific to Dart

**No structural typing**, so `Rule` cannot stay duck-typed — the same
divergence C# has, arrived at differently. Every Dart class defines an
**implicit interface**, and Dart 3 adds `abstract interface class`, so
the *declaration* site has options that neither Python's `Protocol` nor
C#'s `interface` map onto exactly. What does not change is the consumer
side: they must write `implements Rule` explicitly, where a Python or
TypeScript user need only shape an object correctly.

Which declaration form to use is a real design question, not a
formality — `abstract interface class Rule` signals intent more
precisely than a bare `abstract class`, and the choice affects whether
consumers can also `extend` it.

`evaluate()` returns `Future<RuleResult>`. The context type needs its
own design pass; `Map<String, Object?>` is a placeholder, and `dynamic`
is not acceptable.

## 6 · Proposed sequence (no work started)

1. Settle the open decisions in §8 — especially the tag-pattern
   question in §3.3, which blocks the release design.
2. Scaffold the directory with `pubspec.yaml` and the language's own
   `AGENTS.md`, mirroring `python/AGENTS.md`'s role.
3. Port the seven types, then the test suite — including a
   short-circuit proof via a call counter and explicit vacuous-truth
   cases, per [`../../../docs/testing.md`](../../../docs/testing.md).
4. Port the oracle/differential chaos suite from
   [`../../../python/examples/graduation_verdict/`](../../../python/examples/graduation_verdict/) —
   an independent, deliberately naive re-implementation checked against
   a deterministically seeded input space, every generator taking an
   explicit seeded instance so a failure reproduces from its seed alone.
5. Add `test-dart.yml`, path-filtered, as a **new file**.
6. Manual first publish (§3.2), configure automated publishing and the
   tag pattern (§3.3), then add `release-dart.yml` — with the skill
   artifacts in a sibling job (§3.4).
7. Add `skills/verdict/references/dart/` and update the skill's
   language-routing note. Bump `.claude-plugin/plugin.json` in the same
   commit — `check-skill-version.yml` fails the push otherwise.

## 7 · Repo-side prerequisites

In place and additive: `test-python.yml` is path-filtered to `python/**`
and named to leave room for siblings; `release-python.yml` triggers on
`python-v*`, leaving `dart-v*` free; `CHANGELOG.md` groups entries by
language-scoped tag; `skills/verdict/references/` is already
language-scoped.

## 8 · Stage 0 decisions — settled

Every clerical question from
[`../../../docs/adding-a-language.md`](../../../docs/adding-a-language.md)'s
stage 0, answered in writing. Re-verified 2026-09-12.

| Question | Decision |
| :-- | :-- |
| Package name | **`verdict_rules`** — free on pub.dev, correct form (`[a-z0-9_]`) |
| Directory | **`dart/`** |
| SDK constraint | **`>=3.0.0 <4.0.0`** |
| Flutter | **Pure Dart**, no Flutter dependency |
| `Rule` declaration | **`abstract interface class`** |
| Context type | **`Map<String, Object?>`** |
| `RuleResult.data` | **`Object?`** |
| API naming | **lowerCamelCase** — `runAll`, `runNamed`, `runGroup`, `ruleNames`, `groupNames`, `ruleName` |
| Publisher | **Individual account.** No verified publisher |
| First publish | **Manual `dart pub publish`** at `0.0.1` |
| Release tag | **`dart-v{{version}}`** |

### Why each, where it was not obvious

**SDK `>=3.0.0`** is the floor rather than a preference: `abstract
interface class` is a Dart 3 class modifier, so nothing earlier can
express the `Rule` contract the way we want. Going higher would cost
reach for features this package does not use.

**Pure Dart is forced, not chosen.** Declaring a Flutter dependency
would break the zero-dependency constraint. A pure Dart package with no
dependencies already reports Flutter and every platform as supported, so
Flutter users lose nothing.

**`abstract interface class` over `abstract class`** is Dart's own
recommendation for a pure contract, and the reason is substantive rather
than stylistic: `abstract class` permits outside libraries to *extend*,
which exposes the fragile base class problem — an internal call to
another method on `this` may land in a consumer's override. The
`interface` modifier forbids extension while still allowing
implementation, so an instance method calling another always reaches a
known implementation. Consumers write `implements Rule`, never
`extends Rule`.

**`Map<String, Object?>` over `Map<String, dynamic>`.** `dynamic`
disables type checking entirely, which is the wrong default for a
library's public surface. `Object?` costs the consumer an explicit cast
at each read and gives back real checking. It is not a compatibility
problem: `dynamic` is assignable to `Object?`, so a `Map<String,
dynamic>` straight out of `jsonDecode` still passes without a cast.

**lowerCamelCase is free.** The shared fixture pins rule *names* —
string values like `all_core_subjects_pass` — not method names, so
following Dart convention costs nothing against the contract.

**No verified publisher.** It is domain-verified identity, reserves no
names, and requires a domain we do not have a reason to attach. It can
be added later; note only that transferring a package *to* a publisher
is irreversible, so the default of publishing as an individual is the
one that keeps options open.

**The tag pattern is confirmed, not assumed.** This was the one open
registry question. pub.dev's tag pattern must contain `{{version}}` and
explicitly supports a prefix for repositories publishing more than one
package — the documented example is `my_package_name-v{{version}}`. So
`dart-v{{version}}` is accepted and the repo's `<lang>-vX.Y.Z`
convention holds without an exception for Dart.

## 9 · Still open — deferred deliberately

- **Test runner configuration details** beyond `package:test`, which is
  the only real option.

Settled since this plan was first written: Dart's first stable is
**`0.1.0`**, its own number. Versions are language-scoped, and parity
with the reference implementation is proven by the shared fixture rather
than encoded in a matching version number. The `0.0.x` releases before
it are not a fixed count — spend as many as proving the publishing
pipeline takes, since pub.dev keeps every published version forever.
