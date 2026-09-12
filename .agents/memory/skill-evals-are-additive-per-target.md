<!-- Title: Skill Evals Are Additive Per Target -->
# Skill evals are per-target, and adding one is additive

> The skill-creator harness reads a **single assembled eval file** — exactly
> the shape that forces every language branch to edit one shared document. The
> committed truth is instead one JSON file per eval, under
> `skills/verdict-workspace/evals/<target>/`, assembled by
> `scripts/build_evals.py` into the file the harness wants.

## The arrangement

A **target** is a language (`python/`), or `cross-language/` for behaviour that
belongs to no single SDK. The assembled `evals/evals.json` is generated and
gitignored; the per-eval files are what is committed and reviewed.

Ids are assigned by the assembler, and an eval file carrying its own `id` is
rejected outright. That rejection is the load-bearing part: hand-written ids
are precisely what would make adding a target require renumbering another
target's evals. Ids are positional and shift freely, because nothing durable
keys off them — every artifact the harness writes is regenerable point-in-time
material, and `eval_name` is the lasting name.

## Why

Adding a language must stay a new directory, never an edit to something
shared — see [[adding-a-variant-is-a-new-file]] for the general rule. Three SDK
branches were in flight when this was written, and a single hand-maintained
eval list would have conflicted three ways over a file none of them actually
disagreed about.

## How to apply

- A language's evals are `evals/<language>/NN-<name>.json`, plus a manifest
  under `evals/<language>/files/`. Never edit another target's eval to make
  room for yours.
- **Every eval hands the agent a real project manifest.** The skill's first
  instruction is to establish the language by reading one; in an empty
  directory that instruction is unreachable, and the eval silently measures a
  guess instead. The pinned dependency version in that manifest matters for the
  same reason — fetch-at-the-installed-version cannot be exercised without a
  version to read.
- A behavioural change that introduces a new way to be *subtly* wrong earns an
  eval, not just a doc edit — see
  [[a-behaviour-change-is-a-documentation-change]]. An expectation is the only
  thing in this repo that checks whether an agent reading the skill arrives at
  the right design; the test suite only checks that the library is right.
