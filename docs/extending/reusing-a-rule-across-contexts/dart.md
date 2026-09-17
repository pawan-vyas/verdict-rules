<!-- Title: Extending — Reusing a Typed Rule Across Contexts (Dart) -->
# Reusing a typed rule across contexts: Dart

> The concept, the diagram, and the trade-off are in
> [`README.md`](README.md) — read that first. This page is the concrete
> Dart code.

```dart
import 'package:verdict_rules/verdict_rules.dart';

/// Adapts a Rule<TInner> to run inside a composite built on TOuter.
///
/// Not part of verdict_rules itself -- a consumer-defined adapter, exactly
/// as free to exist as a new rule shape is, with no changes needed on
/// verdict_rules' side to support it.
class ProjectingRule<TOuter, TInner> implements Rule<TOuter> {
  @override
  final String name;

  @override
  final String? group;

  final Rule<TInner> _inner;
  final TInner Function(TOuter) _project;

  ProjectingRule(Rule<TInner> inner, TInner Function(TOuter) project)
      : name = inner.name,
        group = inner.group,
        _inner = inner,
        _project = project;

  @override
  Future<RuleResult> evaluate(TOuter context) =>
      _inner.evaluate(_project(context));
}
```

Written once against its own narrow context, the same rule now projects
into two unrelated composites:

```dart
class UserFlag {
  final bool isVerified;
  const UserFlag({required this.isVerified});
}

Future<RuleResult> isVerifiedUser(UserFlag ctx) async =>
    RuleResult(ruleName: 'is_verified_user', passed: ctx.isVerified);

class OrderContext {
  final double total;
  final bool isVerified;
  const OrderContext({required this.total, required this.isVerified});
}

class SignupContext {
  final String email;
  final bool isVerified;
  const SignupContext({required this.email, required this.isVerified});
}

final verifiedRule = FunctionRule('is_verified_user', isVerifiedUser);

final checkoutVerified = ProjectingRule<OrderContext, UserFlag>(
  verifiedRule,
  (ctx) => UserFlag(isVerified: ctx.isVerified),
);
final signupVerified = ProjectingRule<SignupContext, UserFlag>(
  verifiedRule,
  (ctx) => UserFlag(isVerified: ctx.isVerified),
);

final checkout = AndRule<OrderContext>('eligible', [checkoutVerified]);
final signup = AndRule<SignupContext>('eligible', [signupVerified]);

final result = await checkout.evaluate(const OrderContext(total: 75, isVerified: true));
result.passed; // true -- delegated straight through to isVerifiedUser
```

`ProjectingRule<TOuter, TInner>` satisfies `Rule<TOuter>` through an
explicit `implements` clause — Dart has no structural typing for a
multi-member interface like `Rule` — the same way every other rule
shape in this package does. Nothing about `AndRule<TContext>` or
`RulesEngine<TContext>` needed to change to accept it.

## Related

- [`README.md`](README.md) — the language-agnostic spec this page
  implements.
- [`../new-rule-shape/dart.md`](../new-rule-shape/dart.md) — the same
  pattern (a consumer-defined type satisfying `Rule` via an explicit
  `implements` clause) applied to combination logic instead of context
  adaptation.
