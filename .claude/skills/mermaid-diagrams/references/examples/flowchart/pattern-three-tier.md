# Pattern: Three-Tier Architecture

The smallest correct starting point for a 3-tier app — useful as a template to extend, not as a
destination. Note `DB` is a Cylinder, not a Rectangle: even in a 2-node backend/data pair, a real data
store always gets Cylinder, no exceptions for "the diagram is small." Link indexing and a diagram
explanation are genuinely optional here (2-3 nodes, self-evident relationship, per the skill's own
exception) — for the full worked format, see the flagship example in
[architecture-mixed-shape-infra.md](architecture-mixed-shape-infra.md).

```mermaid
graph TB
    subgraph Frontend["🌐 Frontend"]
        UI["💻 Web UI"]
    end

    subgraph Backend["⚙️ Backend"]
        API["🔧 API Server"]
    end

    subgraph Data["💾 Data"]
        DB[("🗄️ Database")]
    end

    UI -->|"HTTP REST<br/>(User Requests)"| API
    API -->|"SQL Queries<br/>(Data Access)"| DB

    style UI fill:#4A9EFF,stroke:#2B7DE9,stroke-width:2px,color:#000
    style API fill:#FFB84D,stroke:#E69500,stroke-width:2px,color:#000
    style DB fill:#51CF66,stroke:#37B24D,stroke-width:2px,color:#000
    linkStyle 0 stroke:#74C0FC,stroke-width:3px
    linkStyle 1 stroke:#8CE99A,stroke-width:3px
```
