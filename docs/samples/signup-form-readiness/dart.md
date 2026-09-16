<!-- Title: Sample — Signup Form Readiness (Dart) -->
# Sample: Signup Form Readiness

> The problem, the design, and what a solution must demonstrate are
> language-agnostic and live in [`README.md`](README.md) — read that
> first. This page is the Dart implementation of it, inside a Flutter
> `StatefulWidget` — `verdict_rules` is a plain Dart package, so it
> needs no Flutter-specific fork or adapter to use here.

## The naive way (and why it breaks down)

The obvious first implementation keeps one boolean per field and
recomputes the combined check inline, inside every field's own change
handler:

```dart
bool isValidEmail(String email) =>
    RegExp(r'^[^\s@]+@[^\s@]+\.[^\s@]+$').hasMatch(email);

bool isStrongPassword(String password) =>
    password.length >= 8 &&
    RegExp(r'[A-Z]').hasMatch(password) &&
    RegExp(r'[0-9]').hasMatch(password);

bool emailValid = false;
bool passwordValid = false;
bool termsAgreed = false;

void onEmailChange(String email) {
  emailValid = isValidEmail(email);
  submitButtonEnabled = emailValid && passwordValid && termsAgreed;
}

void onPasswordChange(String password) {
  passwordValid = isStrongPassword(password);
  submitButtonEnabled = emailValid && passwordValid && termsAgreed;
}

void onTermsChange(bool checked) {
  termsAgreed = checked;
  submitButtonEnabled = emailValid && passwordValid && termsAgreed;
}
```

Three handlers, three separately-written copies of the same `&&`
expression — see the spec for what that actually costs once a fourth
field enters the picture.

## The `verdict` way

The decision itself — is this form ready? — is a plain function of a
context map, with no widget in it at all:

```dart
import 'package:verdict_rules/verdict_rules.dart';

bool isValidEmail(String email) =>
    RegExp(r'^[^\s@]+@[^\s@]+\.[^\s@]+$').hasMatch(email);

bool isStrongPassword(String password) =>
    password.length >= 8 &&
    RegExp(r'[A-Z]').hasMatch(password) &&
    RegExp(r'[0-9]').hasMatch(password);

final emailValid = FunctionRule(
  'email_valid',
  (ctx) async => RuleResult(
    ruleName: 'email_valid',
    passed: isValidEmail(ctx['email'] as String? ?? ''),
  ),
);

final passwordStrong = FunctionRule(
  'password_strong',
  (ctx) async => RuleResult(
    ruleName: 'password_strong',
    passed: isStrongPassword(ctx['password'] as String? ?? ''),
  ),
);

final termsAgreed = FunctionRule(
  'terms_agreed',
  (ctx) async => RuleResult(
    ruleName: 'terms_agreed',
    passed: ctx['termsAgreed'] == true,
  ),
);

final formReady = AndRule('form_ready', [emailValid, passwordStrong, termsAgreed]);

/// The one function every field's change handler calls -- never a
/// re-derived boolean expression, no matter how many fields exist.
Future<bool> checkFormReady(Map<String, Object?> context) async =>
    (await formReady.evaluate(context)).passed;
```

`evaluate()` returns a genuine `Future<RuleResult>` — evaluation is
[real async work, never assumed synchronous](../../architecture/README.md#execution-model-sequential-not-concurrent) —
so it has to be awaited outside `build()`; `Widget build(BuildContext)`
itself stays synchronous. Each field's `onChanged` calls one shared
async method instead, which `setState`s the button's enabled flag once
the check resolves:

```dart
import 'package:flutter/material.dart';

class SignupScreen extends StatefulWidget {
  const SignupScreen({super.key});

  @override
  State<SignupScreen> createState() => _SignupScreenState();
}

class _SignupScreenState extends State<SignupScreen> {
  final _emailController = TextEditingController();
  final _passwordController = TextEditingController();
  bool _termsAgreed = false;
  bool _isReady = false;

  @override
  void dispose() {
    _emailController.dispose();
    _passwordController.dispose();
    super.dispose();
  }

  Future<void> _updateFormReadiness() async {
    final ready = await checkFormReady({
      'email': _emailController.text,
      'password': _passwordController.text,
      'termsAgreed': _termsAgreed,
    });
    if (!mounted) return; // the widget may have been disposed mid-await
    setState(() => _isReady = ready);
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Create your account')),
      body: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            TextField(
              controller: _emailController,
              decoration: const InputDecoration(labelText: 'Email'),
              onChanged: (_) => _updateFormReadiness(),
            ),
            TextField(
              controller: _passwordController,
              obscureText: true,
              decoration: const InputDecoration(labelText: 'Password'),
              onChanged: (_) => _updateFormReadiness(),
            ),
            CheckboxListTile(
              title: const Text('I agree to the terms'),
              value: _termsAgreed,
              onChanged: (checked) {
                setState(() => _termsAgreed = checked ?? false);
                _updateFormReadiness();
              },
            ),
            const SizedBox(height: 16),
            ElevatedButton(
              // Reads exactly one field, passed -- nothing about the
              // button's own code knows what a valid email looks like.
              onPressed: _isReady ? () {} : null,
              child: const Text('Create account'),
            ),
          ],
        ),
      ),
    );
  }
}
```

Because `checkFormReady` never touches the widget tree, it's testable
by constructing a plain map and evaluating it directly:

```dart
test('all three valid -> ready', () async {
  final ready = await checkFormReady({
    'email': 'a@b.com',
    'password': 'Str0ngpass',
    'termsAgreed': true,
  });
  expect(ready, isTrue);
});
```

And the widget itself is provable end to end without ever inspecting
`checkFormReady` directly — only what the button's own `onPressed`
does as each field changes:

```dart
testWidgets('button stays disabled until every condition holds',
    (tester) async {
  await tester.pumpWidget(const MaterialApp(home: SignupScreen()));

  ElevatedButton button() =>
      tester.widget<ElevatedButton>(find.byType(ElevatedButton));

  expect(button().onPressed, isNull); // empty form

  await tester.enterText(find.byType(TextField).first, 'a@b.com');
  await tester.pumpAndSettle();
  expect(button().onPressed, isNull); // still missing password + terms

  // ...password and terms filled in the same way...

  expect(button().onPressed, isNotNull); // now genuinely ready
});
```

## Related

- [`README.md`](README.md) —
  the language-agnostic spec this page implements.
- [`premium-upsell-panel/dart.md`](../premium-upsell-panel/dart.md) —
  the `OrRule` mirror image, including a genuinely async branch.
