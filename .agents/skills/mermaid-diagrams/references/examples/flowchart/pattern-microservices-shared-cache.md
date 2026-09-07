# Pattern: Microservices with Shared Services

Three services sharing one cache and one message queue — the minimal template for "several services, two
shared dependencies." Both shared dependencies are Cylinders (data at rest); the services themselves stay
Rectangle (they're compute, doing work). Link indexing/explanation optional here per the skill's own
"simple, self-evident" exception — see [architecture-mixed-shape-infra.md](architecture-mixed-shape-infra.md)
for the full worked format on a larger diagram.

```mermaid
graph LR
    A["🔧 Service A"] -->|"Cache Access<br/>(Read/Write)"| Cache[("💾 Redis")]
    B["🔧 Service B"] -->|"Cache Access<br/>(Read/Write)"| Cache
    C["🔧 Service C"] -->|"Cache Access<br/>(Read/Write)"| Cache

    A -.->|"Pub/Sub<br/>(Events, Fire & Forget)"| MQ[("📡 Message Queue")]
    B -.->|"Pub/Sub<br/>(Events, Fire & Forget)"| MQ
    C -.->|"Pub/Sub<br/>(Events, Fire & Forget)"| MQ

    style A fill:#FFB84D,stroke:#E69500,stroke-width:2px,color:#000
    style B fill:#FFB84D,stroke:#E69500,stroke-width:2px,color:#000
    style C fill:#FFB84D,stroke:#E69500,stroke-width:2px,color:#000
    style Cache fill:#51CF66,stroke:#37B24D,stroke-width:2px,color:#000
    style MQ fill:#FF6B6B,stroke:#E64545,stroke-width:2px,color:#000
    linkStyle 0 stroke:#8CE99A,stroke-width:3px
    linkStyle 1 stroke:#8CE99A,stroke-width:3px
    linkStyle 2 stroke:#8CE99A,stroke-width:3px
    linkStyle 3 stroke:#FF8787,stroke-width:2px,stroke-dasharray:3 3
    linkStyle 4 stroke:#FF8787,stroke-width:2px,stroke-dasharray:3 3
    linkStyle 5 stroke:#FF8787,stroke-width:2px,stroke-dasharray:3 3
```
