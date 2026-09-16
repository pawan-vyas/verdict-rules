<!-- Title: Verdict Architecture — Dart -->
# Verdict architecture — Dart

> The concrete Dart realization of the shared design in
> [`README.md`](README.md) — read that first. Real type names, real
> method names, the class diagram, and the specific mistakes Dart's
> own idiom makes tempting.

## Design philosophy, in Dart

Three small files, each with exactly one job:

- **`rule.dart`** — what a rule *is*: the `Rule` interface, and three
  concrete shapes (`FunctionRule`, `AndRule`, `OrRule`).
- **`engine.dart`** — how a set of rules gets *run*: `RulesEngine` and
  its five lookup/run modes.
- **`result.dart`** — what evaluation *produces*: `RuleResult`/`RunResult`,
  deliberately plain, immutable classes.

`Rule` is declared `abstract interface class` rather than
`abstract class` deliberately: consumers **implement** it, never
**extend** it. Forbidding extension means an instance method calling
another method on `this` always reaches a known implementation, never
landing in a consumer's override — the fragile-base-class problem this
design closes off by construction rather than by convention. This is a
real, nominal-typing requirement, not just syntax: an object carrying
`name`, `group`, and `evaluate` is not thereby a `Rule` the way Python's
`Protocol` or TypeScript's structural `interface` would accept it —
declaring one means saying `implements Rule` explicitly. `FunctionRule`
is the escape hatch back to shape-based rules that Dart's own structural
typing *does* give for free: any function matching `RulePredicate` is a
rule through it, with nothing declared and no type to name.

## Type structure, concretely

```mermaid
classDiagram
    class Rule {
        <<Interface>>
        +name: String
        +group: String?
        +evaluate(context: Map~String, Object?~)* Future~RuleResult~
    }
    class FunctionRule {
        -predicate: RulePredicate
        +evaluate(context: Map~String, Object?~) Future~RuleResult~
    }
    class AndRule {
        -rules: List~Rule~
        +evaluate(context: Map~String, Object?~) Future~RuleResult~
    }
    class OrRule {
        -rules: List~Rule~
        +evaluate(context: Map~String, Object?~) Future~RuleResult~
    }
    class RulesEngine {
        -byName: Map~String, Rule~
        -byGroup: Map~String, List~Rule~~
        +runAll(context) Future~RunResult~
        +runNamed(name, context) Future~RuleResult~
        +runGroup(group, context) Future~RunResult~
        +tryRunNamed(name, context) Future~RuleResult?~
        +tryRunGroup(group, context) Future~RunResult?~
        +ruleNames Iterable~String~
        +groupNames Iterable~String~
    }
    class RuleResult {
        +ruleName: String
        +passed: bool
        +detail: String
        +data: Object?
    }
    class RunResult {
        +passed: bool
        +results: List~RuleResult~
    }

    Rule <|.. FunctionRule
    Rule <|.. AndRule
    Rule <|.. OrRule
    AndRule o-- Rule : sub-rules
    OrRule o-- Rule : sub-rules
    RulesEngine o-- Rule : holds
    RulesEngine ..> RuleResult : produces
    RulesEngine ..> RunResult : produces
    RulesEngine ..> ArgumentError : throws
    RunResult --> RuleResult : contains
```

See [`README.md`](README.md)'s "Type structure" section for why each of
these relationships is shaped the way it is — the reasoning applies
here unchanged; this diagram is just Dart's own type syntax for it. One
real difference from Python and JS/TS: `RuleResult.detail` is a
non-nullable `String` defaulting to `''`, not an optional field —
checking it against `null` is always true and proves nothing; the
check that means something is `.isEmpty`.

## Execution model, concretely

`AndRule`/`OrRule` evaluate their sub-rules with a plain `for` loop and
`await`, one at a time — never `Future.wait` or any other concurrent
scheduling. Reaching for `Future.wait` here is the single most tempting
mistake in a Dart composite: it still returns the same boolean, but
destroys the short-circuit guarantee described in
[`README.md`](README.md), because every sub-rule's future has already
been created — and its own work already started — before the first
result comes back.

## Three ways to run rules, concretely

The decision tree for which of these to reach for, and why, is in
[`README.md`](README.md#three-ways-to-run-rules-and-when-each-is-the-right-one)
— the same shape for every language. This SDK's own method names:

| Mode | Dart method |
| --- | --- |
| A composite's own evaluate | `AndRule`/`OrRule`.`evaluate()` |
| Run everything the engine holds | `RulesEngine.runAll()` |
| Look up one rule — strict / non-throwing | `runNamed()` / `tryRunNamed()` |
| Look up one named group — strict / non-throwing | `runGroup()` / `tryRunGroup()` |

## Extensibility, concretely

A custom rule shape must say `implements Rule` explicitly — Dart has no
free structural typing for a multi-member interface the way Python's
`Protocol` and TypeScript's structural `interface` do. What it has
instead is structural typing for *function types*: any function
matching `RulePredicate` (`Future<RuleResult> Function(Map<String,
Object?>)`) is a rule through `FunctionRule`, with nothing declared and
no type to name — a tear-off works directly, as
`FunctionRule('quorum', hasQuorum)`. Most rules should reach for that
rather than declaring a type at all.

Unlike JS/TS, this package needs **no custom error type**: unknown
lookups throw Dart's own `ArgumentError` from `dart:core`, the same
type Dart's standard library already uses for invalid-argument
failures generally — no `UnknownLookupError` equivalent to export, and
no gap in the standard library the way JavaScript's genuinely lacks a
comparable built-in lookup-error type. The tradeoff is precision:
catching `ArgumentError` here is far less specific than JS's
`instanceof UnknownLookupError` or Python's `except KeyError`, since
`ArgumentError` is also what countless unrelated argument-validation
failures throw elsewhere. Checking `engine.ruleNames.contains(name)`
(or `groupNames`) before calling is clearer than catching, and is
exactly what those two getters exist for.

## Testing, concretely

`AndRule('x', []).evaluate({})` passes and `OrRule('x', []).evaluate({})`
fails, being the identities of the folds they perform. `runNamed('typo',
{})` and `runGroup('typo', {})` both throw `ArgumentError`.
`tryRunNamed`/`tryRunGroup` return `null` instead of throwing, and
`null` always means *absent*, never *vacuously passed*.

## Related docs

- [`README.md`](README.md) — the shared, language-agnostic design this
  page is Dart's own realization of.
- [`../maintenance/`](../maintenance/README.md) — changing this package
  itself.
- [`../extending/`](../extending/README.md) — building on top of this
  package from a consumer's own code, with no changes here.
- [`../testing/dart.md`](../testing/dart.md) — how this package's own
  test suite is organized, and current coverage.
