# AGENTS.md — Python SDK

Python-specific conventions for this directory. See the repo-root
[`../AGENTS.md`](../AGENTS.md) first for the cross-language rules
(portability, the two constraints, diagram authoring, doc conventions)
— this file only adds what's specific to Python.

## The guarantees, in Python terms

- **Sequential evaluation.** `AndRule`/`OrRule` use a plain `for` loop
  with `await`. **Never `asyncio.gather`.** Short-circuiting only means
  something if later work never *starts*, and the returned boolean is
  identical either way — so this is the one mistake here that passes
  its own tests.
- **Vacuous truth.** `AndRule([])` passes, `OrRule([])` fails.
- **Emptiness is not absence.** Empty composites fold to their
  identity; unknown rule names and unknown groups raise `KeyError` from
  `run_named`/`run_group`, and return `None` from
  `try_run_named`/`try_run_group`. The `try_` forms are the
  **primitives** — the raising ones are assertions on top, so there is
  one lookup path rather than two that can drift.

  A lookup that **matches** always reports its real verdict, so a
  caller's fallback can never mask a failure. When testing code that
  uses one, cover the *present but failing* case — testing only the
  absent one looks complete and misses the direction where a bug is
  silent.
- **`RuleResult.data`** holds only what actually ran. Never padded,
  never flattened into the parent's level.

## Layout

`python/` is a **uv workspace**, not a package. Distributions live under
`packages/`, so adding a second is a new directory and nothing existing moves:

```text
python/
  pyproject.toml            workspace root — declares members, is not a package
  AGENTS.md                 this file
  examples/                 worked examples, may use any package
  packages/verdict-rules/   the distribution: manifest, README, CHANGELOG,
                            docs/, src/verdict/, tests/
```

Two things the workspace changes, both easy to get wrong:

- **`uv build` needs `--package verdict-rules`.** The root has no `[project]`,
  so a bare `uv build` tries to build the root and emits a garbage
  `packages-0.0.0` artifact rather than failing cleanly.
- **Build output lands in `python/dist/`**, the workspace root's, not the
  member's.

## Conventions

- **Docstrings**: Google-style (`Args:`/`Returns:`/`Raises:`) on every
  public class, function, and method — see any function in
  `packages/verdict-rules/src/verdict/rule.py` for the exact shape to match.
- **`from __future__ import annotations`** at the top of every module;
  modern type hints throughout (`str | None`, `list[Rule]`, never
  `Optional[...]`/`List[...]` from `typing`).
- **Frozen dataclasses for immutable value types** (`@dataclass(frozen=True)`)
  — the shape `RuleResult`/`RunResult` already use; match it for any
  new value type.

The repo-root [`AGENTS.md`](../AGENTS.md)'s cross-language conventions (no hardcoded
values, dispatch as a table not a ladder, no internal task references,
blockquote doc framing, testing patterns) apply here unchanged — this
file doesn't repeat them.

## Before calling a change done

```bash
cd python
uv sync
uv run pytest
```

The root's dev group installs the workspace member, so this one command
covers both `packages/*/tests/` and `examples/`.

## Tests prove behaviour, not just booleans

Short-circuiting is proven with a call log, never the final boolean.
Vacuous-truth polarities and unknown-lookup raises each get their own
test. See the repo-root [`../docs/testing/`](../docs/testing/README.md).
