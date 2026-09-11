<!-- Title: Python Namespacing Plan -->
# Python namespacing — position & plan

> How Python answers the question the other registries answer with
> scopes and prefixes, what is available today, and the one decision
> that has to be made before any extension distribution exists. **No
> implementation.** This branch is the development branch for the work,
> not a plan to be merged on its own.

## 1 · Three different things, only one of which is namespacing

| Mechanism | What it is | Gives us a namespace? |
| :-- | :-- | :-- |
| **Extras** — `pip install verdict-rules[x]` | Optional dependency groups declared inside one distribution's own metadata | **No.** No new distribution names, no protection over any name |
| **Import namespace packages** (PEP 420) | Several distributions contributing modules under one import root, e.g. `verdict.a` and `verdict.b` | Only at *import* time. Says nothing about who may publish a distribution |
| **Distribution namespaces** (PEP 752 / PEP 755) | The real equivalent of an npm scope, on PyPI itself | **Yes — but not yet available** |

`verdict-rules` today declares no extras and no dependencies
(`requires_dist: null`, `provides_extra: null`), so the first row is
moot for us regardless.

## 2 · What exists today

- **PEP 752** (implicit namespaces for package repositories) — **Accepted**.
- **PEP 755** (PyPI's grant policy) — **Draft**.
- **PyPI** — nothing deployed. `pypi.org/namespaces/` returns 404.

So there is **nothing to claim right now**. The family names are all
still free (`verdict-rules-core`, `verdict-rules-testing`,
`verdict-rules-flask` all 404 as of 2026-09-11), and unprotected, the
same as on pub.dev.

When it does ship, PEP 755's stated criteria matter for us:

- **Organization accounts only.** An individual user cannot apply. This
  is a real prerequisite, and it can be set up in advance.
- The namespace must exceed three characters, avoid common terms,
  clearly identify the owner, and be **actively used**.
- **Open vs restricted**: an open namespace still lets unaffiliated
  people publish matching names (marked with a neutral indicator); a
  restricted one does not, and pre-existing matching projects get a
  warning indicator instead.

## 3 · The decision that cannot wait

**`verdict-rules` installs `verdict/` as a regular package, with an
`__init__.py`.** That quietly forecloses PEP 420: a regular package
cannot host modules contributed by other distributions, so a future
`verdict-rules-testing` could not install into `verdict.testing`
without restructuring what is already shipped.

Two coherent positions:

1. **Keep `verdict/` a regular package.** Extensions take their own
   top-level import names (`verdict_testing`, and so on). Simple, no
   namespace-package subtleties, no restructuring. The import name stops
   signalling the family.
2. **Restructure `verdict/` into a namespace package.** Extensions
   install as `verdict.testing`, which reads better and matches how the
   scoped npm package will look. Costs a breaking change for importers
   if done after anything depends on the current layout — so it must
   happen *before* the first extension, or not at all.

This is worth settling now precisely because option 2 stops being
available cheaply the moment an extension exists.

## 4 · On reserving names early

For registries with no namespace concept, holding a name means
publishing a placeholder. That approach transfers badly to PyPI: PEP 755
asks that a namespace be *actively used*, and PyPI has its own position
on unused name reservation. Prefer waiting for the namespace feature
over publishing empty distributions.

## 5 · Sequence

1. Settle §3.
2. Create a PyPI organization account (§2 prerequisite).
3. Record the position in [`../../../docs/maintenance.md`](../../../docs/maintenance.md),
   next to the release procedure, so it is written down where a
   maintainer will find it.
4. Revisit when PEP 755 is accepted and PyPI deploys namespaces; apply
   then, and re-check name availability at that point.

## 6 · Not decided

- The §3 choice.
- Open vs restricted, if a grant is later approved.
- Whether any extension distribution is actually wanted yet — none is
  planned; this exists so the option is not lost by accident.
