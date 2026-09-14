<!-- Title: Supply-Chain Integrity and Ownership, Per Registry -->
# Supply-chain integrity and ownership, per registry

> What each registry actually provides for verifying a published
> artifact, and how ownership/namespaces are handled across all of them.
> A registry's own provenance detail lives with that language's release
> doc once one exists — see
> [`releases/python.md`](releases/python.md)'s own "Provenance"
> section for PyPI. The registries below with no shipped language yet
> keep their detail here until they do; the paragraph moves to that
> language's own `releases/<language>.md` the same way PyPI's did,
> rather than staying in this shared file.

## Supply-chain integrity, per registry

This package has **zero runtime dependencies**, so it cannot transmit a
compromised dependency to anyone. That removes the most common supply-chain
risk and none of the others: a published artifact could still be replaced,
or published by something that is not our CI. Provenance is what makes that
checkable by a consumer rather than merely asserted by us.

What each registry actually provides, verified rather than assumed:

| Registry | Mechanism | Automatic? | Verified |
| :-- | :-- | :-- | :-- |
| **PyPI** | PEP 740 attestations | ✅ with Trusted Publishing | **Yes — on our own `0.2.0`, see [`releases/python.md`](releases/python.md)** |
| **npm** | SLSA provenance attestations | ✅ with Trusted Publishing | Field present on packages that have it |
| **NuGet** | Repository signature (`.signature.p7s`) | ✅ applied by nuget.org | Present in a downloaded `.nupkg` |
| **pub.dev** | `archive_sha256` per version | ✅ published with the package | Exposed by the package API |

**npm needs nothing extra either**, but it needs the *right* setup: the
`--provenance` flag is obsolete, and attestations are published
automatically when the release runs under Trusted Publishing with
`id-token: write`. A release workflow that used a long-lived token instead
would silently produce no provenance.

**NuGet signs every package itself.** A repository signature is applied by
nuget.org on upload, so a consumer can verify the package came from
nuget.org unmodified. *Author* signing is a separate thing requiring a
code-signing certificate, and buys little for a package already
repository-signed and published from CI — not worth pursuing.

**pub.dev is the weakest of the four, and the gap is worth naming.** It
publishes a `sha256` per version, which detects a modified archive but says
nothing about *who produced it*. There is no attestation or signing
mechanism to opt into. Automated publishing over OIDC is still worth using
— it means no long-lived credential exists to steal — but a Dart consumer
has no equivalent of `pypi attestation` or `npm audit signatures` to run.
That is a property of the ecosystem, not something to design around.

## Ownership and namespaces across registries

This package is published by an **individual** on every registry, and
stays that way — no organization, no verified publisher, no team
account. Each registry offers something org-shaped that looks like a
prerequisite until you check what it buys, and in every case the answer
is nothing we need: everything ships from **one package per language**,
so there is no family of names to protect.

One exception, which is not an organization: **NuGet ID prefix
reservation for `VerdictRules.*` is worth applying for** once the base
package exists. NuGet is the one registry where a plausible-looking
`VerdictRules.Extensions` could be published by somebody else and read
as ours. We will never publish that package — which is precisely why
nobody else should be able to. The reservation is tied to the package
owner, not to an organization. This moves into `releases/csharp.md`
once C# ships, the same way PyPI's provenance detail moved into
[`releases/python.md`](releases/python.md).
