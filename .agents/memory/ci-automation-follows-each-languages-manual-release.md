<!-- Title: CI Automation Follows Each Language's Manual Release -->
# CI/CD automation lands after each language's manual first release, never before

> Python's automation (`test-python.yml`, `release-python.yml`,
> `check-release-readiness.yml` — gate on tests, tag automatically, publish
> over OIDC) was built once Python had already published by hand. The same
> order applies to JS/TS, C#, and Dart: each gets its own reusable-workflow
> automation only after that language's own `0.0.1` is manually published to
> its registry, not before, and not as a shared piece of work that blocks on
> all three finishing together.

## Why manual-first, not automation-first

A release workflow encodes real, registry-specific decisions — what OIDC
Trusted Publishing actually looks like for that registry, what artifact
shape it expects, what a successful publish even looks like in that
ecosystem's own tooling (see
[`publish-over-oidc-not-tokens`](publish-over-oidc-not-tokens.md)). Building
that automation before anyone has published by hand once means encoding
guesses about a process nobody has actually walked yet. The manual release
is what surfaces the real steps; the automation formalizes them afterward.

## The OIDC-bootstrap-order decision is deferred, not assigned

Which language's automation gets built *first*, or which one becomes the
reference implementation the other two crib from, is **not decided in
advance** — it lands once all three languages have finished their own
`0.0.1` work and that work has been audited, not before. An earlier
assumption that Dart would go first has been explicitly overridden: there
is no standing default, and the next language to automate is whichever the
maintainer actually picks once there is real, comparable `0.0.1` state
across all three to judge from — not whichever was guessed at earliest.

**How to apply**: don't start scaffolding a release workflow for JS/TS, C#,
or Dart until that language has a real manual publish behind it. Don't
treat "Dart first" (or any other order) as settled — ask, at the point all
three have reached `0.0.1` and been audited, rather than assuming.
