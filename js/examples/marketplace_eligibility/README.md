<!-- Title: Marketplace Eligibility Example -->
# Marketplace Eligibility

> A two-sided marketplace eligibility check, built as real, tested code
> — the flagship example exercising `Rule<TContext>` end to end: two
> typed contexts sharing no fields, one rule reused across both via a
> `ProjectingRule` adapter, and a dict-context catalog coexisting in
> the same codebase. See
> [`docs/samples/marketplace-eligibility/`](../../../docs/samples/marketplace-eligibility/README.md)
> for the original framing question this project answers, the design,
> and what a solution must demonstrate.

## Run it

```bash
# From js/
npm install
npm run build --workspace=verdict-rules
node examples/marketplace_eligibility/marketplace-eligibility.js
```

Prints every seller's and buyer's eligibility, then every compliance
event's independently-fired flags.

## Test it

```bash
# From js/
npm test --workspace=@verdict-rules/example-marketplace-eligibility
```

Also picked up by the workspace root's own `npm test`, alongside every
other package and example.

## Read more

- [`../../../docs/samples/marketplace-eligibility/`](../../../docs/samples/marketplace-eligibility/README.md) —
  the naive way this problem is usually approached, why it breaks down,
  and both diagrams behind the design actually used here.
- [`../../../fixtures/marketplace_eligibility/README.md`](../../../fixtures/marketplace_eligibility/README.md) —
  the shared cross-language fixture contract.
- [`../../../docs/extending/reusing-a-rule-across-contexts/README.md`](../../../docs/extending/reusing-a-rule-across-contexts/README.md) —
  the standalone `ProjectingRule` pattern this project's identity check
  is a full-scale instance of.

## Files

| File | What it is |
| --- | --- |
| [`../../../fixtures/marketplace_eligibility/sellers.json`](../../../fixtures/marketplace_eligibility/sellers.json) | Four sellers, each with an expected outcome. **Shared across every language.** |
| [`../../../fixtures/marketplace_eligibility/buyers.json`](../../../fixtures/marketplace_eligibility/buyers.json) | Four buyers, each with an expected outcome. **Shared.** |
| [`../../../fixtures/marketplace_eligibility/compliance_events.json`](../../../fixtures/marketplace_eligibility/compliance_events.json) | Four heterogeneous compliance events, each with three independent expected flags. **Shared.** |
| `marketplace-eligibility.js` | The real implementation, plus a runnable demo. |
| `test/marketplace-eligibility.test.js` | The full test suite, loading all three JSON files and asserting generically. |
