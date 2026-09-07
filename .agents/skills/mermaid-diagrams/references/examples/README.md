# Examples Catalog

Examples are organized by Mermaid diagram type first, then by scenario within each type — check
`SKILL.md`'s "Choose the Right Diagram Type First" table if you're not sure which type your content
actually calls for before browsing below. If your diagram resembles one of these scenarios, read that
file for a directly relevant template.

**Note on guidance-file inline examples**: `sequence-diagrams.md`, `state-diagrams.md`, `er-diagrams.md`,
`class-diagrams.md`, and `gitgraph-diagrams.md` (one directory up) each already contain one canonical
worked example inline, used to teach that type's syntax directly alongside its rules. The subdirectories
below hold *additional* examples for that type — browse here when the guidance file's own example isn't
close enough to your case.

## `flowchart/`

### Cloud / Infra Architecture

| File | Teaches |
|---|---|
| [flowchart/architecture-multi-tier-web-app.md](flowchart/architecture-multi-tier-web-app.md) | Classic 3-tier web app: frontend/backend/data tiers, sync vs. async arrows, cloud service integration (Key Vault, Blob Storage, Event Hub, Service Bus), volume mounts |
| [flowchart/architecture-s3-upload-async.md](flowchart/architecture-s3-upload-async.md) | Presigned-URL upload pattern, step-numbered request/response sequences, S3 event-trigger → Lambda → SNS async fan-out |
| [flowchart/architecture-mixed-shape-infra.md](flowchart/architecture-mixed-shape-infra.md) | **Flagship example.** Full Node Shape Vocabulary in one diagram: edge routing (hexagon), traffic distribution (flag), a genuine deploy-health-gate decision, resulting states vs. components, alerting fan-out |
| [flowchart/pattern-three-tier.md](flowchart/pattern-three-tier.md) | Minimal 3-tier skeleton — the smallest correct starting point |
| [flowchart/pattern-microservices-shared-cache.md](flowchart/pattern-microservices-shared-cache.md) | Multiple services sharing one cache + one message queue |
| [flowchart/pattern-cloud-service-integration.md](flowchart/pattern-cloud-service-integration.md) | One app service calling several distinct cloud services (secrets, storage, compute) |
| [flowchart/pattern-volume-mounts.md](flowchart/pattern-volume-mounts.md) | Direct vs. infrastructure-provisioning arrows on the same diagram |

### Real-World Patterns (generalized from production systems)

Mined from real, in-production architectures and fully generalized — no company/project names, no
identifying business logic.

| File | Teaches |
|---|---|
| [flowchart/real-world-durable-event-sourcing-loop.md](flowchart/real-world-durable-event-sourcing-loop.md) | Event-sourcing/durable-execution: a command entering the system, an append-only log as source of truth, a separate work queue, and async fan-out to observers |
| [flowchart/real-world-two-tier-distributed-locking.md](flowchart/real-world-two-tier-distributed-locking.md) | Distributed locking with two different lock lifetimes, fast-exit-on-contention, a background reaper |
| [flowchart/real-world-dual-path-job-scheduler.md](flowchart/real-world-dual-path-job-scheduler.md) | A scheduler offering untracked fire-and-forget vs. tracked execution with a dead-letter path |

### Non-Infra Technical Concepts

Not architecture diagrams at all — language-runtime mechanics, framework internals, and architectural
patterns, proving the vocabulary generalizes past cloud infrastructure. Synthetic, but every transition
shown is factually correct.

| File | Teaches |
|---|---|
| [flowchart/concept-python-generators-iterators-context-managers.md](flowchart/concept-python-generators-iterators-context-managers.md) | The iterator protocol, generator suspension/resume, `__enter__`/`__exit__`/`__aenter__`/`__aexit__` semantics, how `@contextmanager` drives a generator |
| [flowchart/concept-aspnet-core-kestrel-middleware-pipeline.md](flowchart/concept-aspnet-core-kestrel-middleware-pipeline.md) | ASP.NET Core middleware ordering, short-circuiting vs. calling `next()`, why auth-n must precede auth-z |
| [flowchart/concept-cqrs-pattern.md](flowchart/concept-cqrs-pattern.md) | CQRS: separate command/query paths, two deliberately-different data stores, eventual consistency |
| [flowchart/concept-iac-plan-apply-state-reconciliation.md](flowchart/concept-iac-plan-apply-state-reconciliation.md) | The IaC plan/apply reconciliation loop — config vs. state file vs. live reality |

Also see [../retrofitting.md](../retrofitting.md) for a real before/after worked example of *fixing* an
existing diagram (as opposed to drafting a new one).

## `sequence/`

| File | Teaches |
|---|---|
| *(see `../sequence-diagrams.md` for the canonical 2FA login worked example)* | participant/actor, activate/deactivate, alt/opt/loop, the activation-stack gotcha, phase highlighting |
| [sequence/oauth2-authorization-code-flow.md](sequence/oauth2-authorization-code-flow.md) | OAuth 2.0 authorization code flow — the redirect-based delegated-auth pattern most "Login with X" buttons use |
| [sequence/oauth21-pkce-flow.md](sequence/oauth21-pkce-flow.md) | OAuth 2.1 with PKCE — the public-client flow (SPAs, mobile), closing the auth-code-interception gap plain OAuth 2.0 relies on a client secret to close |
| [sequence/access-refresh-token-flow.md](sequence/access-refresh-token-flow.md) | Access + refresh token flow with rotation — silent token refresh, and why rotating the refresh token on every use matters |
| [sequence/sql-query-execution-flow.md](sequence/sql-query-execution-flow.md) | RDBMS query execution internals — parse, bind/analyze, plan/optimize, execute, buffer-pool cache hit vs. disk read |
| [sequence/tls-https-handshake.md](sequence/tls-https-handshake.md) | The TLS 1.3 handshake (1-RTT) — key agreement before trust, encryption before certificate verification, why `Finished` is an integrity check |

## `state/`

| File | Teaches |
|---|---|
| *(see `../state-diagrams.md` for the canonical Java `Thread` lifecycle worked example)* | composite states, choice/fork/join, `classDef` limitations |
| [state/angular-component-lifecycle.md](state/angular-component-lifecycle.md) | Angular component lifecycle hooks — real firing order and conditions, not a guessed sequence |

## `er/`

| File | Teaches |
|---|---|
| *(see `../er-diagrams.md` for the canonical customer/order/line-item/product worked example)* | crow's-foot cardinality notation, identifying vs. non-identifying relationships |
| [er/multi-tenant-saas-schema.md](er/multi-tenant-saas-schema.md) | Many-to-many via a join entity that carries its own data (a `MEMBERSHIP` role) — distinct from the canonical example's one-to-many-only structure |

## `class/`

| File | Teaches |
|---|---|
| *(see `../class-diagrams.md` for the canonical Strategy-pattern worked example)* | the relationship-type vocabulary, composition vs. aggregation |
| [class/observer-pattern.md](class/observer-pattern.md) | One-to-many aggregation with real cardinality (`"1" o-- "*"`) — a subject notifying many independently-lifecycled observers, distinct from Strategy's single-dependency shape |

## `gitgraph/`

| File | Teaches |
|---|---|
| *(see `../gitgraph-diagrams.md` for the canonical trunk-based-development worked example)* | commit/branch/checkout/merge/cherry-pick, deliberate use of commit types |

**Known gap, checked and confirmed empty**: state-diagram content was NOT found in `oawl`, `rosetta`, or
`mastercache` (searched via `grep -l 'stateDiagram'` across all three, current branches, zero matches) —
the two `state/` examples above are both synthetic, not mined, despite the original plan expecting to
mine real content there. Re-check if any of those repos' state-diagram usage lands on a different branch
later.
