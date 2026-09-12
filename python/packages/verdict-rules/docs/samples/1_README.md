<!-- Title: Verdict Samples -->
# Verdict — Samples

> Worked, runnable-shaped examples of where a small rule engine like this
> earns its keep — each one a self-contained problem statement plus code,
> using nothing beyond what [`quickstart.md`](../quickstart.md) already
> introduces (`Rule`, `FunctionRule`, `AndRule`, `OrRule`,
> `RulesEngine`). None of these are tied to any particular consumer's
> codebase; they're generic domains chosen because the same shape of
> problem — "combine several independently-changing conditions into one
> verdict" — shows up in most systems eventually.

| Sample | The question it answers | What it demonstrates |
|---|---|---|
| [`dynamic-discounts.md`](2_dynamic-discounts.md) | Does this cart qualify for a promotion? | Building an `AndRule` from marketing-configured conditions; `run_all()` for a "why not?" breakdown |
| [`shipping-fee-waiver.md`](3_shipping-fee-waiver.md) | Does this order ship free? | `OrRule`'s short-circuit — ordering independent qualifying paths cheapest-first |
| [`loyalty-tier-promotion.md`](4_loyalty-tier-promotion.md) | Should this customer be promoted to the next tier? | `RulesEngine.run_all()` over a composite, for a progress/checklist UI, not just one boolean |
| [`content-moderation-routing.md`](5_content-moderation-routing.md) | Auto-publish, queue for review, or auto-reject? | `run_group()` — partitioning one engine's rules into named groups |
| [`data-driven-rule-sets.md`](6_data-driven-rule-sets.md) | How do we avoid redeploying every time a business rule changes? | Building `Rule` objects from stored configuration at runtime, evaluated by one shared engine — the pattern this package is designed for |
| [`graduation-requirement-verdict.md`](7_graduation-requirement-verdict.md) | Does this student qualify to graduate? | The full breadth at once — heterogeneous `Rule` shapes, a custom `Rule` type, all three run modes. Backed by a real, tested project in [`examples/graduation_verdict/`](../../../../examples/graduation_verdict/README.md), not just this markdown page |
| [`admin-eligibility-lookup.md`](8_admin-eligibility-lookup.md) | Why did this specific customer not qualify, when the check name is user-typed? | `try_run_named` for an unknown check name, distinguished from a check with zero conditions configured yet — two absence-shaped situations kept apart from each other and from a genuine verdict |

Each of samples 1-6 and 8 is deliberately small enough to read
start-to-finish in a couple of minutes; sample 7 is the exception, both
in length and in coming right before the shorter sample after it — see
its own doc for why. For the underlying concepts these lean on, see
[`../quickstart.md`](../quickstart.md), [`../architecture.md`](../../../../../docs/architecture.md)
(why it's shaped this way), and [`../extension.md`](../../../../../docs/extension.md)
(the recipes these samples are instances of).
