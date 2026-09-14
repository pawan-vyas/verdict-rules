<!-- Title: A Package Is a Directory Under packages/ -->
# A package is a directory under `packages/`, and its changelog lives there

> Why a language directory holds *distributions* rather than being one,
> and the per-ecosystem workspace layout that follows from it.

A language directory holds *distributions*, rather than being one. Adding a
second package is therefore a new directory and nothing existing moves —
manifest, README, changelog, source and tests all travel together inside it.

```text
python/
  pyproject.toml            workspace root — declares members, is not a package
  AGENTS.md                 language-level: conventions for writing Python here
  examples/                 language-level: worked examples, may use any package
  packages/
    verdict-rules/          a distribution
      pyproject.toml
      README.md
      CHANGELOG.md
      docs/
      src/verdict/
      tests/
```

The shape follows each ecosystem's own convention for a multi-package
repository, rather than one shape imposed on all of them:

| | Convention | Members live in |
| :-- | :-- | :-- |
| **Python** | uv workspace | `packages/*`, declared in a root `pyproject.toml` |
| **npm** | npm workspaces | `packages/*`, declared in a root `package.json` |
| **.NET** | solution layout | `src/<Project>/` — already the shape C# had |
| **Dart** | pub workspace | `packages/*` |

The operational consequences of the Python workspace specifically (what
`uv build` does and doesn't build, where output lands) are release-workflow
details, not layout ones — see
[`releases/python.md`](releases/python.md#build-mechanics-specific-to-the-workspace).

**Dart is the one place the layout is adopted without the tooling.** Pub
workspaces require an SDK floor of `^3.6.0`, and the Dart SDK targets
`>=3.0.0`. Raising the floor to gain shared resolution across a single package
would cost reach for no benefit, so the directory layout is adopted now and a
root `pubspec.yaml` is added when a second package actually exists — itself an
additive change.
