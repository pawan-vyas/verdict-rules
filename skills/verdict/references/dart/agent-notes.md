# Dart — agent notes

Short by design. Everything about *what verdict is* lives in
`references/docs/`, which is the repository's own documentation rather
than a summary that could drift from it. This file carries only what is
specific to the Dart SDK, and to writing Dart that uses it.

## Install and import

```sh
dart pub add verdict_rules
```

Works unchanged in a Flutter project too -- `flutter pub add verdict_rules` there.

```dart
import 'package:verdict_rules/verdict_rules.dart';
```

The package name is **`verdict_rules`** (snake_case, no hyphen) —
pub.dev's own package-identifier rules don't allow hyphens, unlike
npm's `verdict-rules`. There is no naming split the way Python has
(`pip install verdict-rules` → `import verdict`): the pub.dev name and
the import path segment are the same word.

## The API, in one screen

```dart
abstract interface class Rule {           // nominal -- must `implements Rule` explicitly
  String get name;
  String? get group;
  Future<RuleResult> evaluate(Map<String, Object?> context);
}
typedef RulePredicate = Future<RuleResult> Function(Map<String, Object?> context);

FunctionRule(name, predicate, {group})     // wraps a plain async predicate
AndRule(name, rules, {group})              // passes only if every sub-rule passes
OrRule(name, rules, {group})               // passes as soon as one does

final engine = RulesEngine(rules);
await engine.runAll(context);              // every rule, never short-circuits
await engine.runNamed(name, context);      // one rule; throws ArgumentError if absent
await engine.runGroup(group, context);     // one group;  throws ArgumentError if absent
await engine.tryRunNamed(name, context);   // -> RuleResult?
await engine.tryRunGroup(group, context);  // -> RunResult?
engine.ruleNames, engine.groupNames        // Iterable<String> of what exists
```

`RuleResult` and `RunResult` are plain immutable classes with `const`
constructors, not interfaces to satisfy — construct them directly:
`RuleResult(ruleName: name, passed: true, detail: '', data: null)` and
`RunResult(passed: true, results: [...])`.

## Mistakes that show up in generated Dart specifically

- **`Future.wait` in a composite.** It starts every sub-rule's
  coroutine before the first result returns and destroys the
  short-circuit guarantee. `AndRule`/`OrRule` evaluate sub-rules in a
  plain `for` loop with `await`, one at a time — the returned boolean
  is identical either way, so this is the one mistake here that passes
  its own tests.
- **Forgetting `implements Rule`.** Dart *does* have structural typing
  — for function types. Any function matching `RulePredicate` is a
  rule through `FunctionRule`, with nothing declared and no type to
  name; a tear-off works directly, as `FunctionRule('quorum',
  hasQuorum)`. What Dart lacks is structural typing for a
  *multi-member* interface: an object carrying `name`, `group` and
  `evaluate` is not thereby a `Rule`, where Python's `Protocol` and
  TypeScript's structural `interface` would accept it as-is. A rule
  shape owning its own name and group must say `implements Rule`
  explicitly. That's why `FunctionRule` carries more weight in this
  SDK than in Python/JS — it's the escape hatch back to shape-based
  rules, and most rules should use it rather than declaring a type.
- **`extends Rule` instead of `implements Rule`.** `Rule` is declared
  `abstract interface class` specifically to forbid extension — this
  is a compile error, so an agent won't get it silently wrong, but
  it's worth knowing why: forbidding extension means an instance
  method calling another method on `this` always reaches a known
  implementation, never landing in a consumer's override.
- **A predicate returning a bare `bool`.** `FunctionRule`'s predicate
  must return `Future<RuleResult>`, not `true`/`false`.
- **Checking `result.detail != null`.** `RuleResult.detail` is a
  non-nullable `String`, defaulting to `''` — never `null`. The check
  that means something is `result.detail.isEmpty`.
- **Catching `ArgumentError` where a rule shape should be reached for
  instead.** Unlike JS's dedicated `UnknownLookupError` or Python's
  `KeyError`, Dart's unknown-lookup failure is a plain `ArgumentError`
  — the same generic exception Dart's own standard library throws for
  countless unrelated argument-validation failures elsewhere, so
  catching it by type here is far less precise than in JS or Python.
  Checking `engine.ruleNames.contains(name)` (or `groupNames`) before
  calling is clearer, and is exactly what those two getters exist for.
- **`Map<String, dynamic>` instead of `Map<String, Object?>`** for
  context. `dynamic` disables type checking entirely; `Object?` still
  accepts a map straight out of `jsonDecode` with no cast, while
  keeping real type checking everywhere else the map is used.
- **`ruleName` set to something other than the rule's own `name`.** A
  caller walking a `RunResult` attributes outcomes by that field.

## Testing what matters

[`references/docs/testing/`](../../../../docs/testing/README.md) (fetch it) is the full checklist,
with [`references/docs/testing/dart.md`](../../../../docs/testing/dart.md)
naming which test proves which contract. The parts that are easy to
skip:

- Prove short-circuiting with a **call log**, not the final boolean. A
  composite that evaluates everything still returns the right answer.
- Give each **vacuous-truth polarity** its own test. They are asymmetric.
- Test both halves of a lookup: the strict form throwing **and** the
  `try` form returning `null`.
- If code uses a fallback, test the **present-but-failing** case — not
  just the absent one. That is the direction where a bug is silent.
- For a rule set **built from stored/config data at runtime** rather
  than hand-written, hand-picked fixtures stop scaling as the
  configuration space grows — reach for property-based testing or an
  oracle/differential approach (an independent, deliberately simpler
  reference implementation checked against many random configurations)
  instead of adding fixtures one at a time as bugs are found.

## Fetching the deeper documents

[`references/docs/architecture/README.md`](../../../../docs/architecture/README.md)
ships bundled — the design rationale is always available with no fetch
needed. [`references/docs/testing/`](../../../../docs/testing/README.md),
`docs/extending/`, and `docs/samples/` are fetch-tier, same as for
every language: too much to ship on every install, pulled at the
version actually installed. This SDK's own quickstart is fetch-tier
too, same as Python's and JS's:

```bash
VERSION=$(awk '/^  verdict_rules:/{found=1} found && /version:/{print $2; exit}' pubspec.lock | tr -d '"')
TAG="dart-v${VERSION}"
BASE="https://raw.githubusercontent.com/pawan-vyas/verdict-rules/${TAG}"

curl -fsSL "${BASE}/docs/testing/README.md" -o references/docs/testing/README.md
curl -fsSL "${BASE}/dart/packages/verdict_rules/doc/quickstart.md" \
     -o references/dart/quickstart.md
```

Reading `pubspec.lock` rather than `pubspec.yaml`'s own `dependencies:`
is deliberate: a caret constraint like `^0.0.2` names a range, not the
version actually resolved and installed, and fetching the wrong
version's documentation is worse than fetching none.

The manifest at `MANIFEST.toml` lists every fetchable document: most as
a literal `[[fetch]]` source/destination pair, the rest as a
`[[fetch_group]]` whose `pattern` needs `{lang}` replaced with this
language before fetching — see
[`commands/verdict-fetch-docs.md`](../../commands/verdict-fetch-docs.md)
for the exact expansion. Record the tag you fetched at in
`references/dart/.version` so a later reader can tell whether the
documents still match what is installed.

If `curl` fails because the tag does not exist, stop — do not fall back
to the default branch. Documentation for a version the project does not
have is worse than none.
