<!-- Title: No Organizations, Individual Forever -->
# This package is published by an individual, on every registry

> **Settled, not open.** No registry organization, no verified
> publisher, no team account — on PyPI, npm, NuGet or pub.dev, now or
> later. This is an individual contribution and stays one. Don't
> re-raise it as a prerequisite when planning a language.

## What this closes

Each registry offers some org- or identity-shaped thing, and each one
looks like a prerequisite until you check what it actually buys:

- **PyPI Organization** — would be required to apply for a namespace if
  PyPI ever deploys PEP 752/755. We are not applying. Everything ships
  from one distribution, so there is no family of names to protect.
- **pub.dev verified publisher** — domain-verified identity only. It
  reserves no names and requires a domain we have no reason to attach.
  Note that transferring a package *to* a publisher is irreversible, so
  publishing as an individual is also the choice that keeps the option
  open rather than closing it.
- **npm organization** — was only needed for a scoped name, and the
  name is unscoped.
- **NuGet organization** — a trusted-publishing policy can be owned by
  an individual. Org ownership additionally means the policy goes
  inactive if the creating user ever leaves the org, which is a failure
  mode worth not having.

## The one exception, which is not an organization

**NuGet ID prefix reservation for `VerdictRules.*` will be applied
for.** It is an application tied to the package owner, not an
organization, and it exists because NuGet is the one registry where a
plausible-looking `VerdictRules.Extensions` could be published by
someone else and read as ours. We will never publish that package —
everything ships from one — which is exactly why nobody should be able
to.

Related: [`releases-are-language-scoped`](releases-are-language-scoped.md),
[`skill-version-is-independent-of-sdk-versions`](skill-version-is-independent-of-sdk-versions.md).
