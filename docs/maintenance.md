<!-- Title: Verdict Maintenance Guide -->
# Verdict — Maintenance Guide

> For people changing *this package's own code* — not for people building
> rules on top of it (see [`extension.md`](extension.md) for that) and not
> for the design rationale behind what's already here (see
> [`architecture.md`](architecture.md) for that). This doc is about
> keeping the package correct and true to its own stated constraints as it
> grows.

## The two constraints that must never quietly slip

Both are called out in the root [`README.md`](../README.md) as
load-bearing, not incidental — a maintainer's first job on any change
is checking it doesn't erode either one:

1. **Zero external dependencies.** `pyproject.toml`'s `dependencies` list
   is empty on purpose. A change that reaches for a third-party package —
   even a small, well-regarded one — is a discussion-worthy exception,
   not a default. If a future need genuinely can't be met without one,
   that's a real design conversation (does it belong in this package at
   all, or in a consumer's own adapter?), not a routine dependency bump.
2. **No knowledge of any specific domain.** Nothing under a language's
   own package source (today: `python/src/verdict/`) should ever import
   or reference rate limiting, access grants,
   discounts, or any other consumer's vocabulary. Domain-specific logic
   belongs in the *consumer's own* adapter module — see
   [`extension.md`](extension.md#recipe-3--keep-your-own-domain-out-of-verdict-in-one-adapter-module)
   for two living examples of where that vocabulary actually goes. If a
   change to this package only makes sense described in terms of one
   consumer's problem, it's in the wrong file.

## How this package is released and consumed

This package is published to PyPI as `verdict-rules`, with a real
version pin standing between a change here and any consumer's
production code — the normal "cut a release, consumers upgrade when
ready" safety net applies, unlike an internal package vendored via an
editable local path.

Release procedure, once a change is ready to ship:

1. Bump `python/pyproject.toml`'s `version` (semver;
   `0.x` while the public API is still settling — a breaking change
   bumps `MINOR` pre-1.0, `MAJOR` after).

   **One carve-out, pre-1.0 only**: removing behaviour that was never
   intended and has no valid use may go in `PATCH`. The bar is
   deliberately high — not "we think nobody relies on it" but "there is
   no way to rely on it correctly." `0.1.1` is the precedent: an unknown
   group returned a vacuous pass, which could only ever fire on a typo
   or a stale name, because a group exists exactly when some rule
   declares it. Anything a caller could legitimately have depended on is
   a breaking change and takes `MINOR`, however unlikely that dependency
   seems.
2. Add a `## python-vX.Y.Z` entry to the repo-root `CHANGELOG.md`, in
   the same commit as the version bump — never backfilled later.
3. Commit, then tag `python-vX.Y.Z` (the `python-` prefix matters: tags
   are scoped per language, since each one releases independently to
   its own registry — see `CHANGELOG.md`'s own intro).
4. Push the tag. CI's `release-python.yml` does everything else: builds
   the sdist/wheel, publishes to PyPI via Trusted Publishing, builds the
   skill-distribution artifacts, and attaches all of it to a GitHub
   Release.

Pushing the tag is the only manual step, and deliberately so — it is the
point at which a human decides something is a release. Everything
downstream of it is automated, including the GitHub Release itself.

The release body is **this changelog's own section for that tag**, not
an auto-generated commit list. That is why step 2 is not optional: a tag
whose `## python-vX.Y.Z` section is missing fails the release rather than
publishing one with empty notes.

### The skill's version is a separate number, on its own schedule

`.claude-plugin/plugin.json`'s `version` is **not** part of the release
procedure above and is not meant to match `python/pyproject.toml`. It
measures the *skill* under `skills/verdict/`, and it exists for one
consumer: Claude Code's marketplace, which compares it against a user's
vendored copy to decide whether that copy is stale. No other harness
reads it — `scripts/install.sh` and `scripts/get.sh` vendor the skill
by copying files, with no version check anywhere.

So the two numbers drift apart in normal operation, and that drift is
correct. A library release with no skill change shouldn't touch
`plugin.json`; a wording tweak to `SKILL.md` with no library change
should bump `plugin.json` and nothing else.

**Bump `plugin.json`'s `version` in the same commit as any change to
`skills/verdict/`.** It's the only signal a marketplace user has that
an update exists, so a skill change shipped without a bump is invisible
to them. `.github/workflows/check-skill-version.yml` enforces this: it
fails a PR or a push to `main` that changes shipped skill content
without changing that version. (`skills/verdict-workspace/` is eval
working material, never ships, and is excluded.)

Release procedure for a skill-only change — the same shape as the
language procedure above, on its own tag prefix:

1. Bump `.claude-plugin/plugin.json`'s `version` in the same commit as
   the skill change itself (the check above requires this anyway).
2. Add a `## skill-vX.Y.Z` entry to `CHANGELOG.md`.
3. Commit, then tag `skill-vX.Y.Z` and push the tag.
   `release-skill.yml` verifies the tag matches `plugin.json`, runs
   `scripts/build.sh`, and attaches the artifacts to a GitHub Release.

That last step matters for one specific audience.
[`scripts/get.sh`](../scripts/get.sh) — the `curl … | sh` install path
— pulls `verdict-tools.zip` from whatever GitHub reports as the
*latest* release, so a skill change that never gets a release of its
own would stay invisible to those users until the next language
release happened to carry it. Marketplace and clone installs read the
repo directly and are already current the moment a change lands on
`main`. Both release workflows attach the **full** set of skill
artifacts for the same reason: whichever kind of release came last,
`latest` always has a usable `verdict-tools.zip`.

Two things stay true independent of the release step:

- `pyproject.toml`'s `version` field is the single source of truth for
  what shipped — CI asserts it matches the tag being pushed and fails
  loudly on drift, rather than silently publishing a mismatch.
- Before merging a change to a public type's shape, run this package's
  own test suite (`uv run pytest`, below) *and* grep every consumer
  codebase you know about for `from verdict import` — a version pin
  protects a consumer from an *unwanted* upgrade, not from a real bug in
  a version they do take — see "Consumer impact checklist" below.

A consumer can still choose to vendor this package via an editable
local path instead of a normal PyPI dependency (e.g. inside their own
monorepo, before it's ready to depend on a public release). That
carries a sharper version of the same risk: no version pin at all
between a change here and that consumer's next process restart, so the
same test-suite-plus-grep discipline above matters even more in that
setup, not less.

### The import name, and what a second distribution would look like

This package installs a directory named `verdict` containing an
`__init__.py`, which is what makes `from verdict import RulesEngine`
work. That is a deliberate, settled choice, and it decides the shape of
any future add-on package.

If a second distribution is ever published — a testing helper, say — it
takes its **own top-level import name**:

```python
from verdict import RulesEngine          # this package
from verdict_testing import FakeRule     # a hypothetical add-on
```

rather than extending this one's namespace as `verdict.testing`. The
alternative would require `verdict/` to have no `__init__.py`, since
only a directory without one can be shared across installed
distributions. Dropping it would break `from verdict import …` for
every existing user and every code sample in these docs, in exchange for
an import style no planned package needs. The separate-name convention
is also what most of the ecosystem already does — `pytest-cov` imports
as `pytest_cov`, and so on.

Note that PyPI offers nothing that reserves `verdict-rules-*` names, so
an add-on distribution's name is held the same way this one's is: by
publishing it. A registry-level namespace mechanism has been designed
but is not deployed; there is nothing to apply for today.

If a recipe from [`extension.md`](extension.md) ever earns a place in
the shipped package — a `ThresholdRule`, say — the answer is a
**submodule of this same distribution**, `verdict/extensions/`, giving
`from verdict.extensions import ThresholdRule`. That is available at any
time and is purely **additive**: nothing existing breaks, no user
changes anything, and it costs exactly the same whether it happens next
month or in three years. It is the reason the question above does not
need revisiting — the growth path stays open without any restructuring.
Whether a given recipe *should* graduate is a separate judgement, and
[`future_plan.md`](future_plan.md) sets that bar.

**A note for other languages.** This constraint does not transfer, and
assuming it does will mislead. In .NET a namespace is purely logical and
decoupled from the assembly that provides it, so many packages can
contribute types under one `Foo.Bar.*` root — which is why
`Microsoft.Extensions.*` looks the way it does. In Python the import
path *is* the directory layout, so which distribution installs a module
and what its import looks like are welded together. Each language's SDK
should follow its own ecosystem's convention rather than copying this
one's.

**Not to be confused with extras.** `pip install verdict-rules[x]`
syntax refers to *optional dependency groups* declared by a package
about itself — it installs the same distribution plus some extra
dependencies. It creates no new distribution and no new import name.
Crucially, an extra gates **dependencies, not code** — the whole
distribution installs either way, and the extra only adds third-party
packages on top. So an extra can never make part of this package
optional. Combined with the zero-dependency constraint, that rules
extras out entirely: there is nothing optional to gate. This package
declares `dependencies = []` and no optional groups, so it has no extras
to offer, and adding one would mean taking a dependency — a decision
this doc's first section covers. The `[dependency-groups]` block in
`python/pyproject.toml` is a separate, development-only mechanism and is
not published.

## Where to make a change

| I want to... | Touch this file |
|---|---|
| Add a new concrete `Rule` shape (a new composite, a weighted combinator) | `python/src/verdict/rule.py` — or a new module if it doesn't naturally fit alongside `FunctionRule`/`AndRule`/`OrRule`; export it from `python/src/verdict/__init__.py`'s `__all__` either way |
| Change what `RuleResult`/`RunResult` carries | `python/src/verdict/result.py` — both are frozen dataclasses, so adding a *required* field breaks every construction site in `rule.py` and `engine.py`, and in every consumer's own adapter (see checklist below) |
| Add a new `RulesEngine` run mode (a new selection axis beyond "by name"/"by group") | `python/src/verdict/engine.py` — needs its own index built in `__init__`, the same way `_by_name`/`_by_group` already are |
| Change the `Rule` `Protocol` itself (its required attributes/method signature) | `python/src/verdict/rule.py` — the highest-blast-radius change this package can make; every existing `Rule` implementation anywhere (including in consumers) must still satisfy the new shape |
| Update *why* something is built this way | `architecture.md` (this directory) |
| Update the quickstart's concepts/example | `python/docs/quickstart.md` |
| Update the narrative front door | the root `README.md` — a distinct doc from `python/docs/quickstart.md`, not a copy of it |

```mermaid
graph TB
    Change{"🔧 What kind of change?"}
    RuleShape["📄 New concrete Rule shape<br/>→ rule.py"]
    ResultShape["📄 RuleResult / RunResult shape<br/>→ result.py"]
    RunMode["📄 New RulesEngine run mode<br/>→ engine.py"]
    ProtocolShape["📄 Rule Protocol itself<br/>→ rule.py"]
    Additive("✅ Additive —<br/>ship it")
    Blast{"⚠️ Blast-radius change?"}
    Checklist[["🔍 Consumer-impact checklist"]]
    Consumers["📦 Every real<br/>production adapter"]

    %% Link 0: Change -> RuleShape
    Change -->|"[1]<br/>a new rule shape"| RuleShape
    %% Link 1: Change -> ResultShape
    Change -->|"[2]<br/>change an existing shape"| ResultShape
    %% Link 2: Change -> RunMode
    Change -->|"[3]<br/>a new run mode"| RunMode
    %% Link 3: Change -> ProtocolShape
    Change -->|"[4]<br/>change the interface"| ProtocolShape
    %% Link 4: RuleShape -> Additive
    RuleShape -->|"[5]<br/>purely additive"| Additive
    %% Link 5: RunMode -> Additive
    RunMode -->|"[6]<br/>purely additive"| Additive
    %% Link 6: ResultShape -> Blast
    ResultShape -->|"[7]<br/>ripples outward"| Blast
    %% Link 7: ProtocolShape -> Blast
    ProtocolShape -->|"[8]<br/>ripples outward"| Blast
    %% Link 8: Blast -> Checklist
    Blast -->|"[9]<br/>before merging"| Checklist
    %% Link 9: Checklist -> Consumers
    Checklist -->|"[10]<br/>grep + re-run both suites"| Consumers

    style Change fill:#B47EFF,stroke:#9654E8,stroke-width:2px,color:#000
    style RuleShape fill:#D0D0D0,stroke:#999999,stroke-width:2px,color:#000
    style ResultShape fill:#D0D0D0,stroke:#999999,stroke-width:2px,color:#000
    style RunMode fill:#D0D0D0,stroke:#999999,stroke-width:2px,color:#000
    style ProtocolShape fill:#D0D0D0,stroke:#999999,stroke-width:2px,color:#000
    style Additive fill:#51CF66,stroke:#37B24D,stroke-width:2px,color:#000
    style Blast fill:#B47EFF,stroke:#9654E8,stroke-width:2px,color:#000
    style Checklist fill:#FFB84D,stroke:#E69500,stroke-width:2px,color:#000
    style Consumers fill:#FFB84D,stroke:#E69500,stroke-width:3px,color:#000

    %% Link Index:
    %% 0: a new rule shape routes to rule.py
    %% 1: a shape change to RuleResult/RunResult routes to result.py
    %% 2: a new run mode routes to engine.py
    %% 3: a Protocol change routes to rule.py
    %% 4: a new rule shape is purely additive
    %% 5: a new run mode is purely additive
    %% 6: a result-shape change is a blast-radius change
    %% 7: a Protocol change is a blast-radius change
    %% 8: every blast-radius change goes through the checklist first
    %% 9: the checklist means grepping and re-testing every consumer's adapter
    linkStyle 0 stroke:#C9B3FF,stroke-width:2px
    linkStyle 1 stroke:#C9B3FF,stroke-width:2px
    linkStyle 2 stroke:#C9B3FF,stroke-width:2px
    linkStyle 3 stroke:#C9B3FF,stroke-width:2px
    linkStyle 4 stroke:#E0E0E0,stroke-width:2px
    linkStyle 5 stroke:#E0E0E0,stroke-width:2px
    linkStyle 6 stroke:#E0E0E0,stroke-width:2px
    linkStyle 7 stroke:#E0E0E0,stroke-width:2px
    linkStyle 8 stroke:#C9B3FF,stroke-width:3px
    linkStyle 9 stroke:#FFCB7A,stroke-width:3px
```

> **Reading the Diagram**:
> 1. **Most changes are additive**: a new concrete `Rule` shape or a new
>    `RulesEngine` run mode never touches an existing consumer's code —
>    `Rule`'s structural typing means nothing needs to know about a new
>    shape in advance for it to work.
> 2. **Two specific changes are not additive**: touching `RuleResult`/
>    `RunResult`'s shape, or the `Rule` `Protocol` itself, ripples into
>    every consumer that already depends on the old shape.
> 3. **The checklist is the gate, not a suggestion**: grep every real
>    adapter you know about and re-run their own test suites before
>    merging either kind of blast-radius change — an editable-path
>    consumption model (see above) means there's often no version pin
>    to catch a mistake here later.

## Consumer-impact checklist for a shape change

Because `Rule` is a structural `Protocol`, most extensions (a new rule
shape, a new composite) are purely additive and can't break an existing
consumer — nothing has to import from this package or subclass anything
to remain a valid `Rule`. The one class of change that *does* ripple
outward is altering something every consumer already depends on the
shape of:

- `RuleResult`'s or `RunResult`'s field names/types.
- `Rule`'s required attributes (`name`, `group`) or its `evaluate`
  signature.

Before merging a change in either category:

1. `grep -rn "from verdict import" --include="*.py" <path to your consumer codebase>` for every consumer you know about, and read every hit.
2. Confirm every real adapter module still constructs/consumes the
   changed type correctly — walk each one found above by hand; see
   [`extension.md`](extension.md#recipe-3--keep-your-own-domain-out-of-verdict-in-one-adapter-module)
   for what a well-formed adapter looks like.
3. Run this package's own test suite, then each consumer's own test
   suite for whatever real adapters exist — a change that's internally
   consistent here can still break a consumer's own assumptions about
   field names it reads out of `RuleResult.data`.
4. Run `uv run pytest examples/graduation_verdict/` (from `python/`) too
   — the one "consumer" always available without needing access to
   anyone else's codebase, and broad enough (heterogeneous rule shapes,
   a custom `Rule` type, all three run modes together) to catch an
   interaction bug the narrower checks above might miss. See its own
   [`../python/examples/graduation_verdict/docs/testing.md`](../python/examples/graduation_verdict/docs/testing.md)
   for why it plays this role.

## When `architecture.md` needs updating, and when it doesn't

- A new concrete `Rule` shape usually **doesn't** need the class diagram
  updated unless it changes the Composite-pattern shape itself (e.g. a
  combinator that *doesn't* implement `Rule` the same way `AndRule`/
  `OrRule` do, breaking the "arbitrarily deep nesting is free" claim).
- A new `RulesEngine` run mode **does** need the "Three ways to run
  rules" table extended — that table is meant to be exhaustive.
- A change to the execution model (e.g. introducing concurrent sub-rule
  evaluation somewhere) is architecture-doc-worthy by definition — that
  section exists specifically to explain why evaluation is sequential
  today, so reversing that decision anywhere needs its own updated
  rationale, not just a code diff.

## Testing a change

A new `Rule` shape added under "Where to make a change" above needs a
short-circuit-and-vacuous-case test (if it's a composite) or a plain
delegation test (if it isn't) — see [`testing.md`](testing.md) for the
full checklist by change type, the current suite's coverage, and why
line coverage alone doesn't prove the contracts that actually matter
here (short-circuiting, vacuous truth).

## Related docs

- [`../README.md`](../README.md) — the narrative front door.
- [`../python/docs/quickstart.md`](../python/docs/quickstart.md) — core
  concepts and the one worked example.
- [`architecture.md`](architecture.md) — type structure, execution
  model, and the reasoning behind each design choice.
- [`extension.md`](extension.md) — building on top of this package from
  a consumer's own code, without changing anything here.
- [`testing.md`](testing.md) — the full testing checklist and current
  suite coverage.
- [`../python/docs/samples/`](../python/docs/samples/1_README.md) —
  worked, domain-flavored examples of where a rule engine like this
  earns its keep.
- [`future_plan.md`](future_plan.md) — exploratory, not-yet-decided
  feature candidates and the test used to evaluate them; read before
  proposing a new core `Rule` shape.
