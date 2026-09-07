# Example: Two-Tier Distributed Locking

Distributed locking with two different lock lifetimes — generalized from a real production system.

```mermaid
flowchart TB
    Caller(["👤 API Request<br/>(any instance)"])

    subgraph T1["🔒 Tier 1 — Session Lock (operation-scoped)"]
        direction TB
        LockSvc[["⚙️ Operation Lock Service<br/>TryAcquire(key, timeout)"]]
        Conn[/"🔌 Dedicated Connection<br/>(holds the lock open)"/]
        NativeLock[("🗄️ Native DB Lock<br/>(advisory / session-scoped)")]
    end

    subgraph T2["⏳ Tier 2 — Lease Row (session-scoped)"]
        direction TB
        EditSvc[["⚙️ Edit Lock Service<br/>EnsureCanMutate(resource, token)"]]
        LeaseRow[("🗄️ Lease Row<br/>UNIQUE(resource) + expires_at")]
        Reaper[["🧹 Lease Reaper<br/>(sweeps expired rows)"]]
    end

    FastExit("🟥 409 Conflict<br/>+ holder metadata")
    Proceed("🟢 Operation Proceeds")

    %% Link 0: Caller requests an operation-scoped mutex
    Caller -->|"[1]<br/>Mutating Op"| LockSvc
    %% Link 1: Caller requests a lease-scoped edit lock
    Caller -->|"[2]<br/>Row Write"| EditSvc
    %% Link 2: Lock service acquires on a dedicated connection
    LockSvc -->|"[3]<br/>Acquire On"| Conn
    %% Link 3: The connection issues the actual lock call
    Conn -->|"[4]<br/>SQL"| NativeLock
    %% Link 4: Busy session lock fast-exits
    LockSvc -.->|"[5]<br/>Busy<br/>(Fast-Exit)"| FastExit
    %% Link 5: Foreign lease fast-exits
    EditSvc -.->|"[6]<br/>Foreign Lease<br/>(Fast-Exit)"| FastExit
    %% Link 6: Lock acquired -> proceed
    NativeLock -->|"[7]<br/>Acquired"| Proceed
    %% Link 7: Lease free or already owned by caller -> proceed
    EditSvc -->|"[8]<br/>Free / Owned by Caller"| Proceed
    %% Link 8: Expired leases get swept, asynchronously, on a timer
    LeaseRow -.->|"[9]<br/>TTL Expiry<br/>(Background Sweep)"| Reaper

    style Caller fill:#4A9EFF,stroke:#2B7DE9,stroke-width:2px,color:#000
    style LockSvc fill:#FFB84D,stroke:#E69500,stroke-width:2px,color:#000
    style Conn fill:#96E6B3,stroke:#51CF66,stroke-width:2px,color:#000
    style NativeLock fill:#51CF66,stroke:#37B24D,stroke-width:2px,color:#000
    style EditSvc fill:#FFB84D,stroke:#E69500,stroke-width:2px,color:#000
    style LeaseRow fill:#51CF66,stroke:#37B24D,stroke-width:2px,color:#000
    style Reaper fill:#B47EFF,stroke:#9654E8,stroke-width:2px,color:#000
    style FastExit fill:#FF6B6B,stroke:#E64545,stroke-width:2px,color:#000
    style Proceed fill:#8CE99A,stroke:#2F9E44,stroke-width:2px,color:#000

    %% Link Index:
    %% 0: Caller -> Lock Service (mutating op)
    %% 1: Caller -> Edit Lock Service (row write)
    %% 2: Lock Service -> Connection (acquire on)
    %% 3: Connection -> Native Lock (SQL)
    %% 4: Lock Service -> Fast Exit (busy)
    %% 5: Edit Lock Service -> Fast Exit (foreign lease)
    %% 6: Native Lock -> Proceed (acquired)
    %% 7: Edit Lock Service -> Proceed (free or owned by caller)
    %% 8: Lease Row -> Reaper (TTL expiry sweep)

    linkStyle 0 stroke:#FFC078,stroke-width:3px
    linkStyle 1 stroke:#FFC078,stroke-width:3px
    linkStyle 2 stroke:#FFC078,stroke-width:2px
    linkStyle 3 stroke:#8CE99A,stroke-width:2px
    linkStyle 4 stroke:#FF8787,stroke-width:2px,stroke-dasharray:3 3
    linkStyle 5 stroke:#FF8787,stroke-width:2px,stroke-dasharray:3 3
    linkStyle 6 stroke:#8CE99A,stroke-width:3px
    linkStyle 7 stroke:#FFC078,stroke-width:3px
    linkStyle 8 stroke:#D0BFFF,stroke-width:2px,stroke-dasharray:5 5
```

> **Component Interactions**:
> 1. **Two Callers, Two Tiers**: The same incoming request path splits into two independent lock
>    services depending on what kind of operation it is — a fast, operation-scoped mutex (Tier 1) or a
>    longer-lived, human-editing-scoped lease (Tier 2). They are never substitutes for each other.
> 2. **Tier 1 — Connection-Held Mutex**: The session lock lives on a dedicated connection (a real
>    resource, hence Parallelogram) and is released automatically the instant that connection dies —
>    it cannot be orphaned by a crash.
> 3. **Tier 2 — TTL Lease**: The lease row has an expiry and is swept by a background reaper — this one
>    *can* be abandoned (a client that never releases it), which is exactly why the reaper exists.
> 4. **Fast-Exit, Not Queue**: Both tiers respond to contention with an immediate `409`, never a queue —
>    that's a deliberate design choice, not an accident, and it's why both fast-exit edges are dashed:
>    they're the "someone else already has this" signal, not the main synchronous flow.
>
> **Design Rationale**: Locking at the data source instead of an external coordinator means there's no
> separate lock-manager service to keep available — the database's own lock primitives (or a lease row
> it stores) are the single source of truth for who currently holds what.
