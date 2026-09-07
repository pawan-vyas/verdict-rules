# Pattern: Cloud Service Integration

One app service calling three distinct cloud services — the minimal template for "app talks to several
managed services." Key Vault and Blob Storage are both Cylinders (they hold data at rest — secrets,
files); VMs stay Rectangle (compute the app deploys onto/manages, not a store). Link
indexing/explanation optional here per the skill's own "simple, self-evident" exception.

```mermaid
graph TB
    App["🔧 Application"]

    App -->|"Read Secrets<br/>(Managed Identity)"| KV[("🔐 Key Vault")]
    App -->|"File Operations<br/>(HTTP/SDK)"| Blob[("☁️ Blob Storage")]
    App -->|"Deploy Artifacts<br/>(Azure Resource Manager)"| VM["🖥️ VMs"]

    style App fill:#FFB84D,stroke:#E69500,stroke-width:2px,color:#000
    style KV fill:#74C0FC,stroke:#4DABF7,stroke-width:2px,color:#000
    style Blob fill:#96E6B3,stroke:#51CF66,stroke-width:2px,color:#000
    style VM fill:#91C7FF,stroke:#4A9EFF,stroke-width:2px,color:#000
    linkStyle 0 stroke:#74C0FC,stroke-width:2px
    linkStyle 1 stroke:#96E6B3,stroke-width:2px
    linkStyle 2 stroke:#91C7FF,stroke-width:2px
```
