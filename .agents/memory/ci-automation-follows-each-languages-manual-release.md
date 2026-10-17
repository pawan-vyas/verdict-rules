<!-- Title: CI Automation Follows Each Language's Manual Release -->
# CI/CD automation lands after a language's manual first release, never before

> A release workflow encodes registry-specific decisions — what OIDC
> Trusted Publishing looks like there, what artifact shape it expects,
> what a successful publish even looks like in that ecosystem's tooling.
> Building it before anyone has published by hand once encodes guesses
> about a process nobody has walked. **The manual release surfaces the
> real steps; the automation formalizes them afterward.**

All four shipping SDKs followed this order, and each has its own
`test-<lang>.yml` and `release-<lang>.yml` as a result — per
[`adding-a-variant-is-a-new-file`](adding-a-variant-is-a-new-file.md), a
new workflow file rather than an edit to a shared one or a matrix runner
installing every toolchain in one job.

**How to apply**, to a fifth language: publish its `0.0.1` by hand first,
then write its workflows from what that actually required. Don't crib a
release workflow from another language and assume the registry's
mechanics transfer — see
[`publish-over-oidc-not-tokens`](publish-over-oidc-not-tokens.md) for how
far they do, and
[`research-the-ecosystem-before-deciding-its-idiom`](research-the-ecosystem-before-deciding-its-idiom.md)
for the worked case where one of four registries turned out not to use
the mechanism being designed at all.
