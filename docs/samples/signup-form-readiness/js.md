<!-- Title: Sample — Signup Form Readiness (JS/TS) -->
# Sample: Signup Form Readiness

> The problem, the design, and what a solution must demonstrate are
> language-agnostic and live in [`README.md`](README.md) — read that
> first. This page is the JS/TS implementation of it, loaded straight
> from a CDN with no build step — the same no-bundler path the package
> README documents.

## The naive way (and why it breaks down)

The obvious first implementation keeps one boolean per field and
recomputes the combined check inline, inside every field's own change
handler:

```js
function isValidEmail(email) {
  return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email);
}

function isStrongPassword(password) {
  return password.length >= 8 && /[A-Z]/.test(password) && /[0-9]/.test(password);
}

const form = { emailValid: false, passwordValid: false, termsAgreed: false };

function onEmailChange(email) {
  form.emailValid = isValidEmail(email);
  submitButton.disabled = !(form.emailValid && form.passwordValid && form.termsAgreed);
}

function onPasswordChange(password) {
  form.passwordValid = isStrongPassword(password);
  submitButton.disabled = !(form.emailValid && form.passwordValid && form.termsAgreed);
}

function onTermsChange(checked) {
  form.termsAgreed = checked;
  submitButton.disabled = !(form.emailValid && form.passwordValid && form.termsAgreed);
}
```

Three handlers, three separately-written copies of the same `&&`
expression — see the spec for what that actually costs once a fourth
field enters the picture.

## The `verdict-rules` way

The decision itself — is this form ready? — is a plain function of a
context object, with no DOM in it at all:

```js
import { AndRule, FunctionRule } from "https://cdn.jsdelivr.net/npm/verdict-rules@0.0.6/+esm";

function isValidEmail(email) {
  return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email);
}

function isStrongPassword(password) {
  return password.length >= 8 && /[A-Z]/.test(password) && /[0-9]/.test(password);
}

const emailValid = new FunctionRule("email_valid", async (ctx) => ({
  ruleName: "email_valid",
  passed: isValidEmail(ctx.email ?? ""),
}));

const passwordStrong = new FunctionRule("password_strong", async (ctx) => ({
  ruleName: "password_strong",
  passed: isStrongPassword(ctx.password ?? ""),
}));

const termsAgreed = new FunctionRule("terms_agreed", async (ctx) => ({
  ruleName: "terms_agreed",
  passed: ctx.termsAgreed === true,
}));

const formReady = new AndRule("form_ready", [emailValid, passwordStrong, termsAgreed]);

// The one function every field's change handler calls -- never a
// re-derived boolean expression, no matter how many fields exist.
async function checkFormReady(context) {
  return (await formReady.evaluate(context)).passed;
}

export { formReady, checkFormReady };
```

Every field's own handler now does exactly two things: read the
current DOM state into a context, and hand the whole question to
`checkFormReady`. None of them know what a valid email or a strong
password looks like:

```js
const emailInput = document.querySelector("#email");
const passwordInput = document.querySelector("#password");
const termsCheckbox = document.querySelector("#terms");
const submitButton = document.querySelector("#submit");

async function updateFormReadiness() {
  const context = {
    email: emailInput.value,
    password: passwordInput.value,
    termsAgreed: termsCheckbox.checked,
  };
  submitButton.disabled = !(await checkFormReady(context));
}

emailInput.addEventListener("input", updateFormReadiness);
passwordInput.addEventListener("input", updateFormReadiness);
termsCheckbox.addEventListener("change", updateFormReadiness);

// The button starts disabled -- an empty form is never ready.
updateFormReadiness();
```

A minimal, deliberately plain page wiring all of it together — no
framework, no build step, one `<style>` block:

```html
<!doctype html>
<html lang="en">
  <head>
    <meta charset="utf-8" />
    <title>Create your account</title>
    <style>
      body { font-family: system-ui, sans-serif; max-width: 22rem; margin: 3rem auto; }
      label { display: block; margin-top: 1rem; }
      input { width: 100%; padding: 0.4rem; box-sizing: border-box; }
      button { margin-top: 1.5rem; padding: 0.5rem 1rem; }
    </style>
  </head>
  <body>
    <h1>Create your account</h1>
    <label>Email <input id="email" type="email" /></label>
    <label>Password <input id="password" type="password" /></label>
    <label><input id="terms" type="checkbox" /> I agree to the terms</label>
    <button id="submit" disabled>Create account</button>

    <script type="module" src="./form.mjs"></script>
  </body>
</html>
```

Because `checkFormReady` never touches the DOM, it's testable by
constructing a plain object and evaluating it directly — no simulated
`input` event, no rendered button, no `.disabled` attribute to read
back:

```js
import assert from "node:assert/strict";

assert.equal(
  await checkFormReady({ email: "a@b.com", password: "Str0ngpass", termsAgreed: true }),
  true,
);
assert.equal(
  await checkFormReady({ email: "not-an-email", password: "Str0ngpass", termsAgreed: true }),
  false, // invalid email alone is enough to keep the button disabled
);
```

### A pinned `<script>` tag works identically

The example above loads the ESM `+esm` CDN path, which has no stable
bytes to hash (see the package README's own
[CDN section](../../../js/packages/verdict-rules/README.md#use-it-from-a-browser-with-no-build-step)
for why). The same rule construction works identically behind a
pinned, integrity-checked global `<script>` tag instead, for a page
that isn't using `type="module"` at all:

```html
<script
  src="https://cdn.jsdelivr.net/npm/verdict-rules@0.0.6/dist/verdict-rules.global.js"
  integrity="sha384-8UUn2T+f6wOMdw/f8XSZN9acZmqxSjQnfzSkXsZz8V05rPEl62oCYhpEw3wsYKEb"
  crossorigin="anonymous"></script>
<script>
  const { AndRule, FunctionRule } = VerdictRules;
  // ...the same emailValid/passwordStrong/termsAgreed/formReady as above.
</script>
```

## Related

- [`README.md`](README.md) —
  the language-agnostic spec this page implements.
- [`../../../js/packages/verdict-rules/README.md`](../../../js/packages/verdict-rules/README.md#use-it-from-a-browser-with-no-build-step) —
  the pinning/integrity rules both CDN variants above follow.
- [`premium-upsell-panel/js.md`](../premium-upsell-panel/js.md) — the
  `OrRule` mirror image, including a genuinely async branch.
