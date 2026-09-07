# Arrow Labeling & Styling Guidelines

### Core Label Requirements

- **MANDATORY**: Always wrap arrow labels in double quotes: `-->|"label text"|`
- Use clear, concise labels that describe the **protocol** and **purpose** of the connection
- Include relevant details:
  - **Protocol**: HTTP, WebSocket, SQL, OTLP, gRPC, GraphQL, RabbitMQ, etc.
  - **Operation type**: CRUD, Metrics Export, Read/Write, Events, etc.
  - **Additional context**: Managed Identity, Real-time Data, Persistence, etc.
- Use `<br/>` for multi-line labels to improve readability
- Avoid overly verbose labels; focus on key information and flow

**Why quotes are required**: Double quotes protect special characters (parentheses, brackets, commas) from being interpreted as Mermaid syntax. Without quotes, characters like `(`, `)`, `[`, `]` can cause rendering errors.

### Step Numbering

**When to include numbered steps:**
- Sequence diagrams showing temporal flow
- Request/response cycles
- Multi-step processes or workflows
- Any diagram where order matters
- **Rule of thumb:** If explaining the diagram requires "first", "then", "next", use numbered steps

**Numbered steps are optional for:**
- Static architecture overviews
- Simple connection diagrams (2-3 nodes)
- Infrastructure topology diagrams
- When the relationship is self-evident and non-sequential

### Step Indicator Format

**All patterns work reliably when wrapped in double quotes.**

Quotes protect special characters — no special spacing or workarounds needed.

**Option 1: Parentheses (simplest)**
```text
Browser -->|"(1) Request Upload"| API
Browser -->|"(1)<br/>Request Upload<br/>(JSON Payload)"| API
```

**Option 2: Brackets (visual distinction)**
```text
Browser -->|"[1] Request Upload"| API
Browser -->|"[1]<br/>Request Upload<br/>(JSON Payload)"| API
```

**Key principle**: Whatever characters you use `(`, `)`, `[`, `]`, `{`, `}`, etc. — **they work fine inside quotes**. No leading spaces needed, no special handling required.

**Step numbering options:**
- **Numbers**: `(1)`, `(2)`, `(3)` or `[1]`, `[2]`, `[3]`
- **Letters**: `(A)`, `(B)` or `[A]`, `[B]` (uppercase)
- **Letters**: `(a)`, `(b)` or `[a]`, `[b]` (lowercase)
- **Roman numerals**: `(i)`, `(ii)` or `[i]`, `[ii]`
- **Sub-steps**: `(1.1)`, `(1a)`, `(A.i)` or `[1.1]`, `[1a]`, `[A.i]`
- **Complex labels**: `"(1) Request<br/>(POST /api/upload)<br/>(Async)"` — all parentheses work

### What to Avoid

**❌ Never use Markdown list syntax:**

Markdown list prefixes (`-`, `*`, `1.`, `2.`) cause rendering errors showing `Unsupported markdown: list` in all arrow labels.

```text
%% WRONG - Markdown lists not supported
ServiceA -->|"- Step 1: Do something"| ServiceB
ServiceA -->|"1. First step"| ServiceB
```

**✅ Use brackets or parentheses instead:**

```text
%% CORRECT
ServiceA -->|"[1]<br/>First step"| ServiceB
ServiceA -->|" (1)<br/>First step"| ServiceB
```

---

## Arrow Styling Guidelines


### Connection Types

#### 1. Direct Dependencies (Solid Arrows)

Use solid arrows for **runtime critical** connections where one service directly depends on another.

**Examples:**
- Application → API
- API → Database
- API → Key Vault
- Container → Volume (when actively used)
- ACI → ACR (image pulls)

**Syntax:**
```
ServiceA -->|"Label"| ServiceB
```

**Styling:**
```
linkStyle INDEX stroke:#COLOR,stroke-width:2-3px
```

#### 2. Infrastructure Connections (Dashed Arrows)

Use dashed arrows for **infrastructure provisioning** or passive relationships.

**Examples:**
- File Share → Volumes (provides storage)
- Service → Volume (passive mount, not actively used)
- Monitoring relationships (non-critical)

**Syntax:**
```
ServiceA -.->|"Label"| ServiceB
```

**Styling:**
```
linkStyle INDEX stroke:#COLOR,stroke-width:2px,stroke-dasharray:5 5
```

**Note the dasharray**: `5 5` (wider dashes) — deliberately different from Async's `3 3` below, so the
two dashed-arrow meanings stay visually distinguishable even in a diagram that uses both. Same reasoning
you'd apply to shapes: two different meanings shouldn't render identically.

#### 3. Asynchronous/Event-Driven Connections (Dotted Arrows)

Use dotted arrows for **non-blocking, event-driven, or fire-and-forget** operations.

**Examples:**
- Backend → Event Hub (event publishing)
- Service → Message Queue (async producer)
- S3 → Lambda (event trigger)
- API → Job Queue (background processing)
- Service → Notification Service (pub/sub)

**Syntax:**
```
ServiceA -.->|"Label"| ServiceB
```

**Labeling Convention:**
Always indicate the async nature in the label:
- `"Event Publish<br/>(Fire & Forget)"`
- `"Async Trigger<br/>(On Upload)"`
- `"Pub/Sub<br/>(Messages)"`
- `"Queue Job<br/>(Background)"`

**Styling:**
```
linkStyle INDEX stroke:#COLOR,stroke-width:2px,stroke-dasharray:3 3
```

**Note the dasharray**: `3 3` (tighter dashes) — distinct from Infrastructure's `5 5` above. This isn't
arbitrary: a reader scanning a diagram with both infra-passive and async-event edges should be able to
tell them apart by the dash pattern alone, without re-reading every label.

### Arrow Width Guidelines

```
3px: Application-critical connections (user-facing data flow)
2px: Supporting services, infrastructure, Azure services
1px: Optional - rarely used
```

### Service-Specific Arrow Colors

**Rule:** Arrows originating from a service should use **a lighter shade** of that service's color.

**Example:**
- Backend service: `#FFB84D` (orange)
- Backend → Volume: `#FFC078` (lighter orange)
- MongoDB service: `#51CF66` (green)
- MongoDB → Volume: `#8CE99A` (lighter green)

**Color Lightening Formula:**
- Increase lightness by 15-20%
- Keep saturation similar or slightly reduce
- Test in both dark and light modes

---

