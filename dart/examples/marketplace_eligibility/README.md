<!-- Title: Marketplace Eligibility Example -->
# Marketplace Eligibility

> A two-sided marketplace eligibility check, built as real, tested code
> -- the flagship example exercising `Rule<TContext>` end to end: two
> typed contexts sharing no fields, one rule reused across both via a
> `ProjectingRule` adapter, and a dict-context catalog coexisting in
> the same codebase. See
> [`docs/samples/marketplace-eligibility/`](../../../docs/samples/marketplace-eligibility/README.md)
> for the original framing question this project answers, the design,
> and what a solution must demonstrate.

## Run it

```bash
# From dart/examples/marketplace_eligibility/
dart pub get
dart run bin/marketplace_eligibility.dart
```

Prints every seller's and buyer's eligibility, then every compliance
event's independently-fired flags.

## Test it

```bash
# From dart/examples/marketplace_eligibility/
dart test
```

This is its own standalone package (its own `pubspec.yaml`, depending
on `verdict_rules` via a `path:` dependency) rather than a member of a
pub workspace -- see `dart/AGENTS.md` for why there is deliberately no
root `pubspec.yaml` yet.

## Read more

- [`../../../docs/samples/marketplace-eligibility/`](../../../docs/samples/marketplace-eligibility/README.md) --
  the naive way this problem is usually approached, why it breaks down,
  and both diagrams behind the design actually used here.
- [`../../../fixtures/marketplace_eligibility/README.md`](../../../fixtures/marketplace_eligibility/README.md) --
  the shared cross-language fixture contract.
- [`../../../docs/extending/reusing-a-rule-across-contexts/README.md`](../../../docs/extending/reusing-a-rule-across-contexts/README.md) --
  the standalone `ProjectingRule` pattern this project's identity check
  is a full-scale instance of.

## Files

| File | What it is |
| --- | --- |
| [`../../../fixtures/marketplace_eligibility/sellers.json`](../../../fixtures/marketplace_eligibility/sellers.json) | Four sellers, each with an expected outcome. **Shared across every language.** |
| [`../../../fixtures/marketplace_eligibility/buyers.json`](../../../fixtures/marketplace_eligibility/buyers.json) | Four buyers, each with an expected outcome. **Shared.** |
| [`../../../fixtures/marketplace_eligibility/compliance_events.json`](../../../fixtures/marketplace_eligibility/compliance_events.json) | Four heterogeneous compliance events, each with three independent expected flags. **Shared.** |
| `lib/src/contexts.dart` | The typed `SellerListingContext`/`BuyerPurchaseContext`/`IdentityFlag` classes. |
| `lib/src/projecting_rule.dart` | The generic context-adapter class. |
| `lib/src/marketplace_check.dart` | The real implementation -- rule factories, JSON loading. |
| `bin/marketplace_eligibility.dart` | The runnable demo. |
| `test/marketplace_check_test.dart` | The full test suite, loading all three JSON files and asserting generically. |
