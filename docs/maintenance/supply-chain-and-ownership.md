<!-- Title: Supply-Chain Integrity and Ownership, Per Registry -->
# Supply-chain integrity and ownership, per registry

> The cross-registry comparison — what each one offers for verifying a
> published artifact, side by side — and the ownership posture that holds
> across all of them. Each registry's own mechanics live with that
> language's release doc: [`releases/python.md`](releases/python.md),
> [`releases/js.md`](releases/js.md),
> [`releases/csharp.md`](releases/csharp.md),
> [`releases/dart.md`](releases/dart.md), each under its own
> `## Provenance`.

## What each registry offers

This package has **zero runtime dependencies**, so it cannot transmit a
compromised dependency to anyone. That removes the most common
supply-chain risk and none of the others: a published artifact could
still be replaced, or published by something that is not this project's
CI. Provenance is what makes that checkable by a consumer rather than
merely asserted here.

Verified against each registry's own reference rather than assumed:

| Registry | Mechanism | Automatic? | Consumer can verify |
| :-- | :-- | :-- | :-- |
| **PyPI** | PEP 740 attestations | Yes, under Trusted Publishing | `pypi attestation`; confirmed on this project's own `0.2.0` |
| **npm** | SLSA provenance attestations | Yes, under Trusted Publishing | `npm audit signatures` |
| **NuGet** | Repository signature (`.signature.p7s`) | Yes, applied by nuget.org on upload | Present in a downloaded `.nupkg` |
| **pub.dev** | `archive_sha256` per version | Yes, published with the package | **Nothing to run** — detects a modified archive, says nothing about who produced it |

**pub.dev is the weakest of the four, and the gap is worth naming rather
than leaving it to be inferred from an empty cell.** There is no
attestation or signing mechanism to opt into, and no consumer-side
command. That is a property of the ecosystem.

Every release verifies its own registry's mechanism in a
`verify-published` job rather than trusting that a push exiting zero
means the right thing is live — each language's release doc names the
exact endpoint and field it asserts.

## Ownership and namespaces

This package is published by an **individual** on every registry, and
stays that way — no organization, no verified publisher, no team account.
Each registry offers something org-shaped that looks like a prerequisite
until you check what it buys, and in every case the answer is nothing
needed here: everything ships as **one package per language**, so there
is no family of names to protect.

The one exception is not an organization but a name guard, and it is
NuGet-specific — see
[`releases/csharp.md`](releases/csharp.md#id-prefix-reservation).
