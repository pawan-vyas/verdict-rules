# Example: Job Scheduler With Dual Execution Paths

A job scheduler offering untracked vs. tracked execution paths — generalized from a real production system.

```mermaid
flowchart TB
    subgraph ControlPlane["⚙️ Scheduler Control Plane"]
        direction TB
        API["📡 Scheduler API"]

        subgraph Triggers["🔀 Schedule Triggers"]
            direction LR
            Recurring["🔧 Recurring Trigger<br/>(cron-style)"]
            OneShot["⏳ One-Shot Trigger<br/>(at-style)"]
        end

        subgraph Executors["⚡ Executors"]
            direction LR
            UntrackedExec["📡 Fire-and-Forget Executor"]
            TrackedExec["📊 Tracked Executor"]
        end

        History[("💾 Execution History<br/>(append-only)")]
        Schedules[("📋 Schedule Definitions")]
    end

    Callback(["🌐 Callback Endpoint<br/>(external)"])
    DeadLetter(["📬 Dead-Letter Sink<br/>(external)"])

    %% Link 0: API persists a new schedule
    API -->|"[1]<br/>Persist"| Schedules
    %% Link 1: API registers a recurring trigger
    API -->|"[2]<br/>Register<br/>(Recurring)"| Recurring
    %% Link 2: API registers a one-shot trigger
    API -->|"[3]<br/>Register<br/>(One-Shot)"| OneShot
    %% Link 3: Recurring trigger fires the untracked path directly
    Recurring -->|"[4a]<br/>Tick"| UntrackedExec
    %% Link 4: Recurring trigger can also fire the tracked path via the API
    Recurring -.->|"[4b]<br/>Tick<br/>(via API)"| API
    %% Link 5: One-shot trigger fires the untracked path directly
    OneShot -->|"[5a]<br/>Fire"| UntrackedExec
    %% Link 6: One-shot trigger can also fire the tracked path via the API
    OneShot -.->|"[5b]<br/>Fire<br/>(via API)"| API
    %% Link 7: API dispatches to the tracked executor
    API -->|"[6]<br/>Dispatch"| TrackedExec
    %% Link 8: Untracked executor calls the external callback directly
    UntrackedExec -->|"[7a]<br/>Call<br/>(No Retry Tracking)"| Callback
    %% Link 9: Tracked executor calls the same external callback
    TrackedExec -->|"[7b]<br/>Call<br/>(Tracked)"| Callback
    %% Link 10: Tracked executor records the outcome
    TrackedExec -->|"[8]<br/>Record"| History
    %% Link 11: A failed tracked call routes to the dead-letter sink, async
    TrackedExec -.->|"[9]<br/>On Failure<br/>(Fire & Forget)"| DeadLetter

    style API fill:#FFB84D,stroke:#E69500,stroke-width:2px,color:#000
    style Recurring fill:#FFB84D,stroke:#E69500,stroke-width:2px,color:#000
    style OneShot fill:#FFB84D,stroke:#E69500,stroke-width:2px,color:#000
    style UntrackedExec fill:#FF6B6B,stroke:#E64545,stroke-width:2px,color:#000
    style TrackedExec fill:#FFB84D,stroke:#E69500,stroke-width:2px,color:#000
    style History fill:#51CF66,stroke:#37B24D,stroke-width:2px,color:#000
    style Schedules fill:#51CF66,stroke:#37B24D,stroke-width:2px,color:#000
    style Callback fill:#4A9EFF,stroke:#2B7DE9,stroke-width:2px,color:#000
    style DeadLetter fill:#D0D0D0,stroke:#A0A0A0,stroke-width:2px,color:#000

    %% Link Index:
    %% 0: API -> Schedules (persist)
    %% 1: API -> Recurring Trigger (register)
    %% 2: API -> One-Shot Trigger (register)
    %% 3: Recurring -> Untracked Executor (tick, direct)
    %% 4: Recurring -> API (tick, via managed path)
    %% 5: One-Shot -> Untracked Executor (fire, direct)
    %% 6: One-Shot -> API (fire, via managed path)
    %% 7: API -> Tracked Executor (dispatch)
    %% 8: Untracked Executor -> Callback (no retry tracking)
    %% 9: Tracked Executor -> Callback (tracked)
    %% 10: Tracked Executor -> History (record outcome)
    %% 11: Tracked Executor -> Dead-Letter Sink (on failure)

    linkStyle 0 stroke:#FFC078,stroke-width:2px
    linkStyle 1 stroke:#FFC078,stroke-width:2px
    linkStyle 2 stroke:#FFC078,stroke-width:2px
    linkStyle 3 stroke:#FF8787,stroke-width:3px
    linkStyle 4 stroke:#FFC078,stroke-width:2px,stroke-dasharray:3 3
    linkStyle 5 stroke:#FF8787,stroke-width:3px
    linkStyle 6 stroke:#FFC078,stroke-width:2px,stroke-dasharray:3 3
    linkStyle 7 stroke:#FFC078,stroke-width:3px
    linkStyle 8 stroke:#FF8787,stroke-width:3px
    linkStyle 9 stroke:#FFC078,stroke-width:3px
    linkStyle 10 stroke:#8CE99A,stroke-width:2px
    linkStyle 11 stroke:#D0BFFF,stroke-width:2px,stroke-dasharray:3 3
```

> **Key Steps**:
> 1. **Registration**: The API persists the schedule definition once and registers it with the
>    appropriate trigger — recurring or one-shot — these are two separate stores (Schedule Definitions
>    vs. Execution History), not one generic "data" cylinder, because they hold fundamentally different
>    things: config vs. a historical record.
> 2. **Two Paths From Every Trigger**: Every trigger can fire down either the untracked path (direct,
>    solid, no bookkeeping) or route back through the API into the tracked path (dashed — it's a
>    secondary, managed detour, not the trigger's primary action).
> 3. **Untracked = Fewer Guarantees**: The untracked executor calls the external callback directly with
>    no retry tracking — that's the trade-off callers accept for lower latency and simplicity.
> 4. **Tracked = Recorded and Recoverable**: The tracked executor records every outcome and, on failure,
>    fires an async, fire-and-forget hand-off to a dead-letter sink for later inspection or retry.
>
> **Design Rationale**: Offering both paths lets callers choose their own reliability/latency trade-off
> per job, rather than forcing every job through the overhead of tracked execution when a best-effort
> fire-and-forget call is good enough.
