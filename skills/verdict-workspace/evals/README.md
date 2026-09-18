# Skill evals

> What this skill is measured against, and how a language adds its own
> measurements without touching anyone else's. The eval harness reads a
> single assembled file; the committed truth is one JSON file per eval,
> inside a directory named for what it targets.

## Layout

```text
evals/
  <target>/
    NN-<name>.json     one eval
    files/             input files those evals hand to the agent
  evals.json           assembled by scripts/build_evals.py — not committed
```

A **target** is what the eval exercises: a language (`python/`), or
`cross-language/` for behaviour that belongs to no single SDK.

## Adding evals

Adding a language is a **new directory**. Adding an eval is a **new
file**. Neither edits anything shared — no central list, no id registry,
and nothing for two language branches to conflict over. This is the same
property the package layout has, applied here for the same reason: the
alternative is one file that every in-flight branch modifies.

Ids are assigned by the assembler, not written by hand, which is what
makes that true. They are positional and shift when a target is added;
nothing durable keys off them, because every artifact the harness writes
is regenerable and `eval_name` is the lasting name.

```bash
python3 scripts/build_evals.py                  # every target
python3 scripts/build_evals.py --target python  # one target
```

## Hand a running eval the built skill, never the raw source

`skills/verdict/references/REPOSITORY-MAP.md` does not exist as a file
in this repository — `scripts/generate_repository_map.py` writes it at
build time, and it is never checked in. Pointing an evaluated agent at
`skills/verdict/` directly therefore reproduces a real-looking but false
failure: `SKILL.md`'s "Where to look next" names a file that genuinely
is not there, and a careful agent will say so.

Every real consumer gets the built skill — through `install.sh`, the
Claude plugin marketplace, or `dist/verdict.skill` unzipped — where
`REPOSITORY-MAP.md` is present, because `scripts/build.sh` generates it
before packaging. Run `bash scripts/build.sh` (or unzip the resulting
`dist/verdict.skill`) and hand an evaluated agent *that* tree, not
`skills/verdict/` itself. An eval whose "observations" report a missing
`REPOSITORY-MAP.md` almost always means this step was skipped, not that
the skill has a real gap.

## Why each eval declares a manifest file

The skill's first step is *establish the language by reading the target
project's own manifest*. In an empty working directory that step has
nothing to read, so an agent guesses — and the eval then measures the
guess rather than the instruction. Every eval therefore hands over a
minimal, ordinary manifest.

The pinned dependency version in it is load-bearing for the same reason:
the skill says to fetch deeper documentation at the version a project
actually has rather than from the default branch, which is not a
reachable instruction without a version to read.

**Running a language's evals before that language's first registry
publish** — the point of shipping evals alongside a `0.0.1` PR rather
than waiting for `0.1.0` — means the pinned version in its fixture
manifest cannot resolve through an ordinary registry install yet,
because nothing is published there. The manifest still declares the
same version the package itself carries, matching what a real consumer
will see once it *is* published; whoever runs the eval sandbox in the
meantime substitutes a local install (a `file:` dependency, a locally
built tarball, an editable install — whatever that ecosystem's own
tooling calls it) for that one step, without editing the fixture file
to say so.

## What the evals cover, and what they deliberately do not

The three scenario evals measure the thing the skill exists for —
turning a conditional chain into named, independently-changing rules
with real diagnostics. Two more cover behaviour that is easy to get
wrong *and* silent when it is:

- `python/04-absence-versus-emptiness` — an unknown rule name is not an
  empty rule set, and collapsing the two hides a typo as a policy
  outcome.
- `cross-language/01-unsupported-language` — a language with no SDK
  should produce a plain statement of that, not an invented import path.
- `js/05-cdn-conditional-ui` — the one scenario with no `package.json` at
  all: a plain HTML page loading `verdict-rules` from a CDN, no build
  step. Probes whether the pinned-version-plus-integrity-hash guidance
  in the package's own README actually gets followed when there's no
  npm-based manifest to establish the language from in the first place.
  This is JS-native — no other language ships a browser-global bundle —
  so it has no counterpart in another target's numbering by design.
- `dart/05-flutter-conditional-banner` — the "UI" role `js/05` plays for
  JS/TS, filled by Dart's own dominant UI framework rather than a
  language-native mechanism like JS's CDN script tag (Dart has no
  equivalent — pub.dev packages don't ship a browser global). Probes
  whether the response respects that `Widget build()` must stay
  synchronous, so `evaluate()`'s `Future` has to be resolved outside it
  (`initState`/`setState`, a `FutureBuilder`), and states that
  `verdict_rules` needs no Flutter-specific adapter since it is a pure
  Dart package.
- `python/07-reference-at-installed-version` — the skill ships no fetch
  script and no bundled architecture tier; everything an agent might
  reach for beyond `agent-notes.md` is named, one line each, in
  `references/REPOSITORY-MAP.md`, for the agent to go get itself.
  Probes that when a task genuinely calls for something
  `REPOSITORY-MAP.md` names (here, reusing one rule across two
  differently-shaped contexts — a real documented scenario, not an
  invented one), the agent (a) determines the actual installed version
  from the fixture manifest rather than assuming the latest release,
  and (b) if it does go fetch that document, does so at the matching
  `python-v<version>` tag rather than the default branch — the specific
  failure mode `SKILL.md`'s own "Where to look next" section exists to
  prevent. Getting the composable-shapes design right without fetching
  anything is an equally good outcome; inventing a shape that violates
  the shared-`TContext` guarantee, or fetching from `main`, are the
  failures this eval looks for.

**Not covered, and known**: that a fetch actually happens over the real
network, against a real released tag. Measuring that needs network
access inside the eval sandbox, which would make the result depend on
GitHub being reachable rather than on the skill being right —
`python/07-reference-at-installed-version` measures whether the agent
*would* fetch the right document at the right tag, not whether a `curl`
or `WebFetch` call actually succeeded. `scripts/check_skill_bundle.py`
covers the one link-integrity failure that is reachable without a
network round trip — a route from `SKILL.md` or an `agent-notes.md` to a
`references/` path that the assembled bundle does not actually contain.
