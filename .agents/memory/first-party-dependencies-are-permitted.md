<!-- Title: First-Party Dependencies Are Permitted -->
# The dependency rule is "no third-party", not literally zero

> The constraint is about **who maintains the code you pull in**, not the
> count. A package owned by the language or platform vendor itself is
> permitted; anything third-party is not, and still needs an explicit
> discussion before it lands.

## What counts as first-party

| Ecosystem | Permitted | Example |
| :-- | :-- | :-- |
| Dart | packages published by the Dart team | `package:meta` |
| C#/.NET | Microsoft-owned packages | `Microsoft.Bcl.*`, `System.*` |
| Python | the standard library only — there is no vendor-owned package tier on PyPI | `warnings`, `dataclasses` |
| JS/TS | nothing — npm has no vendor-owned tier, so this stays genuinely zero | — |

The asymmetry is real and worth knowing: Dart and .NET have a first-party
package tier, Python and JS/TS do not. So a capability reachable via
`package:meta` in Dart may have no equivalent permitted route in JS, and
the answer there is a zero-dependency mechanism or nothing.

## The cost of using it, which is why it isn't free

"Zero dependencies" is asserted across a couple of dozen tracked files —
don't trust a written count, search for it, since the number moves every
time a doc is added. It includes all four registry-visible package
descriptions (`VerdictRules.csproj`, `pubspec.yaml`, `package.json`,
`pyproject.toml`) and the four package READMEs that render on the registry
pages, which are the ones that cannot be corrected after the fact. Taking
a first-party
dependency in any language makes an unqualified "zero dependencies"
claim false for that language, on the registry page where the dependency
list is displayed directly beside the description.

**How to apply**: taking a first-party dependency is permitted but is a
*release-visible* change. It requires rewording the claim to "no
third-party dependencies" everywhere it appears — not just in the
package that took the dependency — in the same change. Judge whether
what the dependency buys is worth that, and prefer a zero-dependency
mechanism when one exists and is adequate (a self-declared attribute, a
stdlib facility, a doc convention).

Related: [`verdict-is-a-protocol-spec`](verdict-is-a-protocol-spec.md) —
the same reasoning that lets each language's idiomatic surface differ
also means a dependency available in one ecosystem has no obligation to
be mirrored in another.
