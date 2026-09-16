<!-- Title: Sample — Premium Upsell Panel (JS/TS) -->
# Sample: Premium Upsell Panel

> The problem, the design, and what a solution must demonstrate are
> language-agnostic and live in [`README.md`](README.md) — read that
> first. This page is the JS/TS implementation of it, loaded straight
> from a CDN with no build step — the pinned global `<script>` variant
> this time, rather than [`signup-form-readiness/js.md`](../signup-form-readiness/js.md)'s
> ESM import, to show both work identically.

## The naive way (and why it breaks down)

The obvious first implementation is a conditional ladder checked
straight in the render path, with the slow network call wherever it
happened to be added:

```js
async function shouldShowUpsellPanel(user) {
  if (await checkWinbackOffer(user.id)) {
    return true;
  }
  if (user.planTier === "free") {
    return true;
  }
  if (user.usageThisMonth >= user.usageCap) {
    return true;
  }
  return false;
}
```

The billing check happens to run first here, so it runs on **every
single page load** — including a free-tier user who qualifies on plan
alone and never needed the network round trip at all. See the spec for
what that costs beyond the wasted call itself.

## The `verdict-rules` way

```html
<script
  src="https://cdn.jsdelivr.net/npm/verdict-rules@0.0.6/dist/verdict-rules.global.js"
  integrity="sha384-8UUn2T+f6wOMdw/f8XSZN9acZmqxSjQnfzSkXsZz8V05rPEl62oCYhpEw3wsYKEb"
  crossorigin="anonymous"></script>
<script>
  const { OrRule, FunctionRule } = VerdictRules;

  async function isFreeTier(ctx) {
    return { ruleName: "is_free_tier", passed: ctx.planTier === "free" };
  }

  async function hasHitUsageCap(ctx) {
    return { ruleName: "has_hit_usage_cap", passed: ctx.usageThisMonth >= ctx.usageCap };
  }

  async function qualifiesForWinback(ctx) {
    // The expensive path: only reached if both cheaper checks above failed.
    const qualifies = await ctx.billingService.checkWinbackEligibility(ctx.userId);
    return { ruleName: "qualifies_for_winback", passed: qualifies };
  }

  const shouldShowUpsell = new OrRule("should_show_upsell", [
    new FunctionRule("is_free_tier", isFreeTier),
    new FunctionRule("has_hit_usage_cap", hasHitUsageCap),
    new FunctionRule("qualifies_for_winback", qualifiesForWinback), // cheapest-last, on purpose
  ]);

  async function checkShouldShowUpsell(context) {
    return (await shouldShowUpsell.evaluate(context)).passed;
  }
</script>
```

The comment on the last rule is the whole fix, made visible: cost order
is now a stated decision at the point it matters, not something the
next person to edit this file has to reconstruct from scratch.

The panel's own loading indicator wraps exactly the one `evaluate()`
call — nothing more, and nothing less:

```html
<div id="upsell-panel" hidden>
  <p>Upgrade to Premium and unlock unlimited usage.</p>
</div>
<div id="upsell-loading" hidden>Checking your account&hellip;</div>

<script>
  async function updateUpsellPanel(user, billingService) {
    const panel = document.querySelector("#upsell-panel");
    const loading = document.querySelector("#upsell-loading");

    loading.hidden = false;
    const shouldShow = await checkShouldShowUpsell({ ...user, billingService });
    loading.hidden = true;

    panel.hidden = !shouldShow;
  }
</script>
```

For a free-tier user, `evaluate()` resolves on the same microtask it
started on — the loading indicator flips visible and back to hidden in
the same tick, never actually rendered. Only a user who fails both
cheap checks experiences a visible wait, and only then because a real
network call had to happen.

Proving the skip, not just asserting it — a call-counting fake stands
in for the real billing service:

```js
class FakeBillingService {
  calls = 0;
  async checkWinbackEligibility(userId) {
    this.calls += 1;
    return userId === "winback-eligible";
  }
}

const billingService = new FakeBillingService();
const shown = await checkShouldShowUpsell({
  planTier: "free",
  usageThisMonth: 0,
  usageCap: 100,
  userId: "u1",
  billingService,
});
// true, 0 -- free tier alone already qualifies; the billing service is never called
console.log(shown, billingService.calls);
```

A minimal, deliberately plain page wiring all of it together:

```html
<!doctype html>
<html lang="en">
  <head>
    <meta charset="utf-8" />
    <title>Account</title>
    <style>
      body { font-family: system-ui, sans-serif; max-width: 22rem; margin: 3rem auto; }
      #upsell-panel { padding: 1rem; border: 1px solid #ccc; border-radius: 0.5rem; }
      #upsell-loading { color: #666; font-style: italic; }
    </style>
  </head>
  <body>
    <h1>Your account</h1>
    <div id="upsell-loading" hidden>Checking your account&hellip;</div>
    <div id="upsell-panel" hidden>
      <p>Upgrade to Premium and unlock unlimited usage.</p>
    </div>

    <script
      src="https://cdn.jsdelivr.net/npm/verdict-rules@0.0.6/dist/verdict-rules.global.js"
      integrity="sha384-8UUn2T+f6wOMdw/f8XSZN9acZmqxSjQnfzSkXsZz8V05rPEl62oCYhpEw3wsYKEb"
      crossorigin="anonymous"></script>
    <script src="./upsell.js"></script>
  </body>
</html>
```

## Related

- [`README.md`](README.md) —
  the language-agnostic spec this page implements.
- [`shipping-fee-waiver/js.md`](../shipping-fee-waiver/js.md) — the same
  `OrRule` shape in a backend checkout decision.
- [`signup-form-readiness/js.md`](../signup-form-readiness/js.md) — the
  `AndRule` mirror image, loaded via the ESM `+esm` CDN path instead of
  this page's pinned global `<script>` — both variants work identically.
