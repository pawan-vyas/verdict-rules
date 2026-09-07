# Concept: CQRS (Command Query Responsibility Segregation)

A pure software-architecture pattern, not infrastructure — synthetic, but accurate to how CQRS is
actually implemented in practice.

```mermaid
flowchart TB
    Client(["🧑 Client"])
    Dispatch{{"📨 API / Mediator<br/>(routes by command or query)"}}

    subgraph Write["✍️ Command Side (Write)"]
        direction TB
        CmdHandler[["⚙️ Command Handler<br/>e.g. PlaceOrderHandler"]]
        Domain["🧠 Domain Model<br/>(validates, applies business rules)"]
        WriteStore[("🗄️ Write Store<br/>(normalized, transactional)")]
        DomainEvent[/"📣 Domain Event<br/>e.g. OrderPlaced"/]
    end

    subgraph ReadSide["📖 Query Side (Read)"]
        direction TB
        Projector[["🔄 Projector<br/>(keeps read store in sync)"]]
        ReadStore[("🗄️ Read Store<br/>(denormalized, query-optimized)")]
        QueryHandler[["⚙️ Query Handler<br/>e.g. GetOrderSummaryHandler"]]
    end

    %% Link 0: Client sends either a command or a query through the same entry point
    Client -->|"[1]<br/>Command or Query"| Dispatch
    %% Link 1: A command is routed to its handler
    Dispatch -->|"[2]<br/>Command → Handler"| CmdHandler
    %% Link 2: The handler asks the domain model to apply business rules
    CmdHandler -->|"[3]<br/>Execute"| Domain
    %% Link 3: The domain model persists the resulting state change
    Domain -->|"[4]<br/>Persist"| WriteStore
    %% Link 4: The domain model raises an event describing what happened
    Domain -->|"[5]<br/>Raise"| DomainEvent
    %% Link 5: The event is published for anyone downstream to react to, async
    DomainEvent -.->|"[6]<br/>Publish<br/>(Fire & Forget)"| Projector
    %% Link 6: The projector updates the read-optimized store from the event
    Projector -->|"[7]<br/>Project / Update"| ReadStore
    %% Link 7: A query is routed to its handler
    Dispatch -->|"[8]<br/>Query → Handler"| QueryHandler
    %% Link 8: The query handler reads directly from the read store -- no domain logic involved
    QueryHandler -->|"[9]<br/>Read Directly"| ReadStore
    %% Link 9: The query result returns straight to the client
    QueryHandler -->|"[10]<br/>Result"| Client

    style Client fill:#4A9EFF,stroke:#2B7DE9,stroke-width:2px,color:#000
    style Dispatch fill:#FF8787,stroke:#E64545,stroke-width:2px,color:#000
    style CmdHandler fill:#FFB84D,stroke:#E69500,stroke-width:2px,color:#000
    style Domain fill:#FFB84D,stroke:#E69500,stroke-width:2px,color:#000
    style WriteStore fill:#51CF66,stroke:#37B24D,stroke-width:2px,color:#000
    style DomainEvent fill:#96E6B3,stroke:#51CF66,stroke-width:2px,color:#000
    style Projector fill:#B47EFF,stroke:#9654E8,stroke-width:2px,color:#000
    style ReadStore fill:#51CF66,stroke:#37B24D,stroke-width:2px,color:#000
    style QueryHandler fill:#FFB84D,stroke:#E69500,stroke-width:2px,color:#000

    %% Link Index:
    %% 0: Client -> Dispatch (command or query)
    %% 1: Dispatch -> Command Handler
    %% 2: Command Handler -> Domain Model (execute)
    %% 3: Domain Model -> Write Store (persist)
    %% 4: Domain Model -> Domain Event (raise)
    %% 5: Domain Event -> Projector (publish, async)
    %% 6: Projector -> Read Store (project/update)
    %% 7: Dispatch -> Query Handler
    %% 8: Query Handler -> Read Store (read directly)
    %% 9: Query Handler -> Client (result)

    linkStyle 0 stroke:#FF8787,stroke-width:3px
    linkStyle 1 stroke:#FFC078,stroke-width:3px
    linkStyle 2 stroke:#FFC078,stroke-width:3px
    linkStyle 3 stroke:#8CE99A,stroke-width:3px
    linkStyle 4 stroke:#8CE99A,stroke-width:2px
    linkStyle 5 stroke:#D0BFFF,stroke-width:2px,stroke-dasharray:3 3
    linkStyle 6 stroke:#D0BFFF,stroke-width:2px
    linkStyle 7 stroke:#FFC078,stroke-width:3px
    linkStyle 8 stroke:#8CE99A,stroke-width:3px
    linkStyle 9 stroke:#74C0FC,stroke-width:3px
```

> **Component Interactions**:
> 1. **One Entry Point, Two Paths**: The dispatcher (often a mediator) routes a command or a query to
>    its own dedicated handler — the two paths never cross past this point.
> 2. **Commands Go Through the Domain, Queries Don't**: A command always runs through domain logic that
>    can reject it (validation, business rules); a query handler reads straight from the read store with
>    no domain logic in the way, which is exactly why it's typically much simpler and faster.
> 3. **Two Stores on Purpose**: The write store is normalized and transactional, optimized for
>    correctness under concurrent writes. The read store is a separate, denormalized projection,
>    optimized for exactly the queries the application actually needs — they are drawn as two distinct
>    Cylinders deliberately, not one shared database serving both jobs.
> 4. **The Read Store Can Lag**: The domain event that keeps the read store in sync is published
>    fire-and-forget (the one dashed edge in this diagram) — the read store is *eventually* consistent
>    with the write store, not immediately. A client that queries immediately after issuing a command may
>    briefly see stale data; real systems handle this with optimistic UI updates, version stamps, or by
>    simply accepting the lag where it doesn't matter.
>
> **Design Rationale**: CQRS is worth the complexity of two models specifically when read and write
> workloads have very different shapes (e.g., a small number of complex writes vs. a huge number of simple
> reads) — for simple CRUD with symmetric read/write needs, a single shared model is usually simpler and
> CQRS would be over-engineering.
