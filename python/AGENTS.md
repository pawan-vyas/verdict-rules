# AGENTS.md — Python

Python-specific conventions for this directory. See the repo-root
[`../AGENTS.md`](../AGENTS.md) first for the cross-language rules
(portability, the two constraints, diagram authoring, doc conventions)
— this file only adds what's specific to Python.


## Layout

`python/` is a **uv workspace**, not a package. Distributions live under
`packages/`, so adding a second is a new directory and nothing existing moves:

```
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

Run the suite from `python/`: the root's dev group installs the workspace
member, so one `uv run pytest` covers both `packages/*/tests/` and `examples/`.

## What this package is

`verdict` is a small, zero-dependency, async-native rule-evaluation
engine for Python — see [`README.md`](packages/verdict-rules/README.md) for what it is and
its "Where to go next" table for the full doc suite
(`docs/quickstart.md`, `docs/architecture.md`, `docs/extension.md`,
`docs/maintenance.md`, `docs/testing.md`, `docs/samples/`).
[`examples/`](examples/README.md) holds full, tested mini-projects
behind the more comprehensive samples.

## Python coding conventions

- **Docstrings**: Google-style (`Args:`/`Returns:`/`Raises:`) on every
  public class, function, and method — see any function in
  `packages/verdict-rules/src/verdict/rule.py` for the exact shape to match.
- **`from __future__ import annotations`** at the top of every module;
  modern type hints throughout (`str | None`, `list[Rule]`, never
  `Optional[...]`/`List[...]` from `typing`).
- **Frozen dataclasses for immutable value types** (`@dataclass(frozen=True)`)
  — the shape `RuleResult`/`RunResult` already use; match it for any
  new value type.

The repo-root `AGENTS.md`'s cross-language conventions (no hardcoded
values, dispatch as a table not a ladder, no internal task references,
blockquote doc framing, testing patterns) apply here unchanged — this
file doesn't repeat them.
