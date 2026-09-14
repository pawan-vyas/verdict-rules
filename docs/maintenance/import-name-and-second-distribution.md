<!-- Title: The Import Name, and a Second Distribution -->
# The import name, and what a second distribution would look like

> The deliberate, settled choice behind `from verdict import ...`, and how
> it decides the shape of any future add-on package or promoted recipe.

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

If a scenario from [`../extending/`](../extending/README.md) ever earns
a place in the shipped package — a `ThresholdRule`, say — the answer is a
**submodule of this same distribution**, `verdict/extensions/`, giving
`from verdict.extensions import ThresholdRule`. That is available at any
time and is purely **additive**: nothing existing breaks, no user
changes anything, and it costs exactly the same whether it happens next
month or in three years. It is the reason the question above does not
need revisiting — the growth path stays open without any restructuring.
Whether a given recipe *should* graduate is a separate judgement, and
[`../future_plan.md`](../future_plan.md) sets that bar.

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
to offer, and adding one would mean taking a dependency — see
[`constraints.md`](constraints.md). The `[dependency-groups]` block in
`python/pyproject.toml` is a separate, development-only mechanism and is
not published.
