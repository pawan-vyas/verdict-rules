# Pattern: Volume Mounts

The minimal template for "direct dependency vs. infrastructure provisioning" on the same diagram — a
service actively using a volume (solid) vs. a file share merely provisioning that volume's storage
(dashed). Both Volume and File Share are Cylinders (data at rest); Service stays Rectangle (compute).

```mermaid
graph TB
    Service["🔧 Service"]
    Volume[("📁 Volume")]
    Storage[("📂 File Share")]

    %% Direct dependency - solid
    Service -->|"Volume Mount<br/>(Runtime Access)"| Volume
    %% Infrastructure provision - dashed
    Storage -.->|"Provides Storage<br/>(Infrastructure)"| Volume

    style Service fill:#FFB84D,stroke:#E69500,stroke-width:2px,color:#000
    style Volume fill:#51CF66,stroke:#37B24D,stroke-width:2px,color:#000
    style Storage fill:#51CF66,stroke:#37B24D,stroke-width:2px,color:#000
    linkStyle 0 stroke:#FFC078,stroke-width:2px
    linkStyle 1 stroke:#D0D0D0,stroke-width:2px,stroke-dasharray:5 5
```
