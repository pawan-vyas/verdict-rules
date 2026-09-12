<!-- Title: Publish Over OIDC, Never Tokens -->
# Publish over OIDC on every registry — provenance depends on it

> Using Trusted Publishing rather than a stored API token is not only about
> having no credential to leak. On PyPI and npm it is also **the thing that
> produces provenance at all**. A release published with a long-lived token
> succeeds, looks identical, and silently carries no attestation.

## Why this package still has a supply-chain story despite zero dependencies

Zero runtime dependencies means this package cannot *transmit* a compromised
dependency. It does not mean it cannot *be* one. An artifact can still be
replaced, or published by something that is not our CI, and a consumer has
no way to tell — unless provenance says where it came from.

## What each registry gives, verified rather than assumed

| Registry | Mechanism | Automatic? |
| :-- | :-- | :-- |
| **PyPI** | PEP 740 attestations | ✅ with Trusted Publishing |
| **npm** | SLSA provenance attestations | ✅ with Trusted Publishing |
| **NuGet** | Repository signature | ✅ applied by nuget.org itself |
| **pub.dev** | `archive_sha256` only | — no provenance mechanism exists |

Checked against real artifacts, not documentation claims: our own
`verdict-rules 0.2.0` on PyPI carries an attestation naming publisher
`GitHub`, the repository, and `release-python.yml`; a downloaded
`.nupkg` contains `.signature.p7s`; npm's `dist.attestations` carries a
SLSA `predicateType` on packages that have it.

## The trap

**npm's `--provenance` flag is obsolete**, and its absence is not a
signal. Attestations come from running under Trusted Publishing with
`id-token: write`. A workflow that publishes with `NODE_AUTH_TOKEN`
instead produces a perfectly good release and no provenance, with nothing
warning you. Same shape on PyPI.

So when writing any language's release workflow: **OIDC, `id-token: write`,
no stored publish token.** If a registry cannot do it, say so explicitly in
that language's plan rather than leaving it looking like an oversight.

## pub.dev is genuinely weaker, and that is not ours to fix

A `sha256` detects a modified archive but says nothing about who produced
it. There is no attestation or signing mechanism to opt into, so a Dart
consumer has no equivalent of `npm audit signatures`. Publish over OIDC
anyway — it removes the stealable credential — and record the gap rather
than implying parity that does not exist.

Related: [`releases-are-language-scoped`](releases-are-language-scoped.md).
