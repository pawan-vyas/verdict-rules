<!-- Title: C# SDK Plan -->
# C# SDK — triage & plan

> Planning for a C# SDK alongside `python/`. **Nothing here is built,
> and nothing here is decided** beyond what the triage section records
> as fact. This exists so the real decisions can be made deliberately
> rather than re-derived later, and so the registry constraints are
> known before anyone starts. **The pull request carrying this file is
> the development branch for this SDK** — it stays open and accumulates
> the implementation rather than being merged on its own. Supersedes the
> C# half of [`../polyglot-sdk-resume.md`](../polyglot-sdk-resume.md),
> which stays as the cross-language framing.

## 1 · Package name

`Verdict` is unavailable on NuGet. The chosen ID is **`VerdictRules`**,
which also leaves `VerdictRules.*` free for any extension packages added
later.

NuGet has no true namespaces, but it does offer **ID prefix
reservation** — an application rather than an automatic grant. Worth
applying for once `VerdictRules` is published, since it is the only
mechanism that protects the family name.

## 2 · Triage: publish path

Trusted Publishing via OIDC, generally available on nuget.org
(GitHub Actions and GitLab CI). Unlike PyPI's direct publish, this is a
**token exchange**:

1. The workflow requests a short-lived OIDC token from GitHub.
2. `NuGet/login@v1` forwards it to nuget.org, which validates it against
   the registered policy and returns a **temporary API key**.
3. `dotnet nuget push` uses that key.

**The temporary key lives one hour**, and each OIDC token exchanges for
exactly one key, exactly once. Request it immediately before the push —
a long build sitting between login and push can expire it. This is a
real sequencing constraint on how the release job is structured, not a
footnote.

Workflow needs `permissions: id-token: write`.

## 3 · Triage: prerequisites, in order

1. A nuget.org account, and an organization if the policy should be
   org-owned.
2. **Register the policy**: nuget.org → username → Trusted Publishing.
   Repository Owner, Repository, and **Workflow File as a bare
   filename** (`release-csharp.yml`, *not* the `.github/workflows/`
   path), plus optional Environment.
3. **Set the policy scope deliberately.** Scopes control whether the
   policy may publish *new packages* or only *new versions of existing*
   ones, with a glob for the IDs covered. **This is where NuGet beats
   npm**: with the right scope, trusted publishing can perform the very
   first publish. npm cannot, and needs a manual placeholder release —
   see the sibling plan at `.agents/plans/js-sdk/PLAN.md` §3 (named
   plainly rather than linked, since the two plans land in separate
   pull requests and the relative path does not resolve until both
   have merged).
4. **Choose the policy owner** — individual or organization. If
   org-owned and the creating user later leaves that org, the policy
   goes inactive until they are re-added.
5. **Account for the 7-day pending window.** A new policy can start
   *temporarily* active for 7 days and goes inactive if no publish
   happens within it, because nuget.org needs the GitHub repository and
   owner IDs — captured from a real publish — to pin the policy against
   repo-recreation attacks. The window can be restarted at any time.
   Documented as typical for private repos; this repo is public, so it
   may not apply. Don't assume either way — check the policy status in
   the UI after creating it.

## 4 · What must be preserved, not re-derived

The point of another SDK is that the *design* is identical and only the
idiom changes:

- The same seven types: `Rule`, `FunctionRule`, `AndRule`, `OrRule`,
  `RulesEngine`, `RuleResult`, `RunResult`.
- **Sequential, never concurrent evaluation.** `AndRule`/`OrRule` must
  short-circuit in a plain `foreach` with `await`, **never
  `Task.WhenAll`** — for exactly the reason
  [`../../../docs/architecture.md`](../../../docs/architecture.md)
  gives: short-circuiting only means something if later work never
  *starts*, and `WhenAll` has already started every sub-rule before the
  first result returns. The returned boolean is unchanged either way,
  so the tests still pass. This is the easiest correctness property in
  the whole package to destroy silently.
- Vacuous-truth polarity decided explicitly per composite shape
  (`AndRule([])` passes, `OrRule([])` fails — deliberately asymmetric).
- `RuleResult.Data` stays opaque: only what actually ran, never padded,
  never flattened.
- The one-adapter-module extension pattern
  ([`../../../docs/extension.md`](../../../docs/extension.md), Recipe 3).

## 5 · The one genuinely non-mechanical design point

C# has no free structural-typing equivalent to Python's `Protocol` —
there is no way to satisfy an interface by shape alone. So `Rule` stops
being duck-typed and becomes a real interface:

```csharp
public interface IRule
{
    string Name { get; }
    string? Group { get; }
    Task<RuleResult> EvaluateAsync(IDictionary<string, object> context);
}
```

C# consumers must write `class MyRule : IRule` rather than merely
shaping an object correctly. **This is a real divergence from both
Python and TypeScript and should be documented as one**, not papered
over — it changes the extension story, not just the syntax. Python's
`Protocol` and TypeScript's structural interfaces both give "no
registration, no base class"; C# cannot, and pretending otherwise would
mislead.

The signature above is a first pass. `IDictionary<string, object>` for
context and the exact async naming both need a real design pass against
.NET conventions.

## 6 · Proposed sequence (no work started)

1. Settle the open decisions in §8.
2. Scaffold the directory with the project/solution files and the
   language's own `AGENTS.md`, mirroring `python/AGENTS.md`'s role.
3. Port the seven types, then the test suite — including a
   short-circuit proof via a call counter and explicit vacuous-truth
   cases, per [`../../../docs/testing.md`](../../../docs/testing.md).
4. Port the oracle/differential chaos suite from
   [`../../../python/examples/graduation_verdict/`](../../../python/examples/graduation_verdict/) —
   an independent, deliberately naive re-implementation checked against
   a deterministically seeded input space, with every generator taking
   an explicit seeded instance so any failure reproduces from its seed
   alone. Most reusable technique from the Python side, and what
   actually proved that engine correct.
5. Add `test-csharp.yml`, path-filtered, as a **new file**.
6. Register the trusted publishing policy (§3), then add
   `release-csharp.yml` on a `csharp-v*` tag, with the OIDC login step
   placed immediately before the push (§2).
7. Add `skills/verdict/references/csharp/` and update the skill's
   language-routing note. Bump `.claude-plugin/plugin.json` in the same
   commit — `check-skill-version.yml` fails the push otherwise.

## 7 · Repo-side prerequisites

All in place, all additive:

- `test-python.yml` is path-filtered to `python/**`, named to leave room
  for siblings.
- `release-python.yml` triggers on `python-v*`, leaving `csharp-v*` free.
- `CHANGELOG.md` groups entries by language-scoped tag.
- `skills/verdict/references/` is already language-scoped.

Not optional: every release workflow must attach the full skill artifact
set. `scripts/get.sh` resolves whatever GitHub calls the latest release
and pulls `verdict-tools.zip` from it, so a release that omits it breaks
the curl-pipe installer outright.

## 8 · Explicitly not decided

- **Directory name**: `csharp/` vs `dotnet/`. `dotnet/` reads more
  naturally to a .NET audience; `csharp/` is more literally parallel to
  `python/`. No preference recorded.
- **Target frameworks**, and how far back to support.
- **Nullable reference types** policy, and whether the public surface is
  annotated from day one.
- **The context type.** `IDictionary<string, object>` is a placeholder.
- **Test framework.** xUnit is the ecosystem default; not a commitment.
