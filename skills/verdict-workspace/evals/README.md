# Skill evals

> What this skill is measured against, and how a language adds its own
> measurements without touching anyone else's. The eval harness reads a
> single assembled file; the committed truth is one JSON file per eval,
> inside a directory named for what it targets.

## Layout

```
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

**Not covered, and known**: that a fetch actually happens at the pinned
tag. Measuring it needs network access and a real released tag inside
the eval sandbox, which would make the result depend on GitHub being
reachable rather than on the skill being right.
`scripts/check_skill_bundle.py` covers the failure that was actually
reachable — a documented fetch path drifting away from the manifest.
