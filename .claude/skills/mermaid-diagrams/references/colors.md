# Color Palette Standards

### Dual-Mode Compatibility

All colors must work in **both dark and light modes**. Use medium saturation and lightness values.

### Service Node Colors

```
Primary Services (Bold, High Contrast):
- Frontend/UI: #4A9EFF (Medium Blue)
- Backend/API: #FFB84D (Medium Orange/Amber)
- Database: #51CF66 (Medium Green)
- Observability: #B47EFF (Medium Purple)
- Messaging/Proxy: #FF6B6B (Medium Coral)
- Gateway/Daemon: #FF8787 (Light Coral)
- Networking/VPC Infra: #66D9E8 (Medium Cyan)
```

**Purple's second meaning, made explicit**: across a real retrofit of ~40 diagrams, `#B47EFF` consistently
ended up meaning "decision/gate/approval" (health checks, permission checks, approval steps) in addition to
its documented "Observability" meaning — independently, in eight separate diagrams, all converging on the
same reuse. This is being formalized here rather than fought: it's safe because the **shape** already
disambiguates (a Rhombus is unambiguously a decision regardless of fill color; a plain Rectangle in the same
purple is unambiguously an Observability service) — color alone was never the only signal. So: `#B47EFF` is
correct for both an Observability node *and* a decision/gate Rhombus in the same diagram; don't invent a
second purple or feel obligated to recolor one of them apart, and don't treat this as an unresolved
conflict — it's a documented dual-use, same as any shape that gets reused across two color families.

**Networking/VPC infra** (new): use `#66D9E8` for infrastructure whose job is networking topology itself —
a VPC, subnets, route tables, a networking module in an IaC diagram — as distinct from Messaging/Proxy
(`#FF6B6B`, for message brokers, reverse proxies, pub/sub) and from Gateway/Daemon (`#FF8787`, for a
request-facing edge component). A real retrofit reached for Messaging-red on an entire "Networking Module"
subgraph for lack of anything better; this swatch closes that gap.

### Environment / Status Tier Colors

A separate convention from Service Node Colors above — for when a diagram needs to show a **tier or
severity gradient** (dev → staging → production; low → medium → high severity; healthy → degraded →
critical) rather than "what part of the system is this." A real retrofit pass found this pattern
reinvented independently in 6+ diagrams, every time by reusing a Service Node color for its hue alone
(Database-green for "dev," Backend-orange for "staging," Messaging-red for "prod") — legible in each
individual diagram, but it means the same hex value silently means two unrelated things depending on
context (is `#51CF66` "this is a database" or "this is the dev tier"?). Use these dedicated tier colors
instead, so the two conventions never collide:

```
Tier / Severity Gradient (use for env or status, never for service identity):
- Tier 1 — Dev / Low / Healthy:      #8CE99A (fill), #2F9E44 (stroke)
- Tier 2 — Staging / Medium / Degraded: #FFD43B (fill), #F08C00 (stroke)
- Tier 3 — Prod / High / Critical:   #FF6B6B (fill), #C92A2A (stroke)
```

Note Tier 3 does share Messaging/Proxy's fill hue (`#FF6B6B`) — that overlap is acceptable (both read as
"urgent/critical" to a viewer) but Tier 1 and Tier 2 use hexes that don't collide with any Service Node
color, so a full three-step gradient stays visually distinct from service identity in at least two of its
three steps. If a diagram uses both a tier gradient and Service Node colors together, prefer applying the
tier gradient to a subgraph background/border rather than swapping a service node's own identity color —
that keeps "what this node is" and "what tier it's in" as two independent signals instead of overwriting
one with the other.

### Connection Colors

```
Application Layer (Bold, 3px):
- HTTP REST: #4A9EFF (Blue - matches Frontend)
- WebSocket: #FF6B6B (Coral - matches Proxy)
- Database: #51CF66 (Green - matches Database)
- Monitoring: #B47EFF (Purple - matches Observability)

Azure/Cloud Services (Medium, 2px):
- Secrets/Auth: #74C0FC (Light Blue)
- Storage: #96E6B3 (Light Green)
- Compute: #91C7FF (Soft Blue)
- Networking: #99E9F2 (Light Cyan - matches Networking/VPC Infra)

Infrastructure (Subtle, dashed/solid):
- Volume Mounts (service-specific): Lighter shade of service color
- Generic Infrastructure: #D0D0D0 (Light Gray)
- Image Pulls: #A0A0A0 (Medium Gray)
```

**Storage-green is for genuine stored artifacts, not "any file"**: `#96E6B3` fits a container image, a
build artifact, or a registry — things actually fetched/pulled at runtime — but not a setup-documentation
file or a `.env.example` template that a human reads once. See [Node Shape Vocabulary → Disambiguation
Notes](shapes.md#disambiguation-notes--cases-that-come-up-in-practice) (cases #1 and #5) for the full
reasoning; the shape section is where this distinction is decided, this is just the color half of the
same call.

### Node Styling Template

```
style ServiceName fill:#COLOR,stroke:#DARKER_COLOR,stroke-width:2px,color:#000
```

**Example:**
```
style Backend fill:#FFB84D,stroke:#E69500,stroke-width:2px,color:#000
```

---

