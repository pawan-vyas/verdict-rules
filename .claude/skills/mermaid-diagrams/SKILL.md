---
name: mermaid-diagrams
description: >
  Use this skill whenever creating, generating, retrofitting, or improving any Mermaid diagram —
  flowcharts, architecture diagrams, sequence diagrams, state diagrams, ER diagrams, class diagrams, or
  git graphs. Triggers on "draw a diagram", "create an architecture diagram", "visualize this flow",
  "make a mermaid chart", "draw a sequence diagram", "draw a state machine", "show the entity
  relationships", "uplift/fix these existing diagrams", or whenever a diagram would supplement a
  technical explanation — even if the user never says "mermaid" explicitly. First helps pick the correct
  Mermaid diagram type for the content instead of defaulting everything to a flowchart, then enforces
  team color palette standards, arrow styling, a node-shape vocabulary matched to infra role (never
  everything-is-a-rectangle), emoji usage, layout, link indexing, diagram-explanation formatting, and
  retrofitting guidance at scale. Always consult this skill before writing or editing any Mermaid diagram.
metadata:
  version: "3.4"
---

# Mermaid Diagram Skill

This skill ensures all generated Mermaid diagrams follow team-defined standards for color, layout, labeling,
and documentation. Read this fully before writing any diagram code. Distinct infra roles get distinct
shapes — apply the Node Shape Vocabulary below on every diagram, not just complex ones; defaulting every
node to a plain rectangle is the single most common quality failure this skill exists to prevent.

For detailed reference on colors, full examples, and common patterns, start at:
→ `references/README.md` (the topic index — routes to `colors.md`, `shapes.md`, `arrows.md`, `layout.md`,
`emoji.md`, `diagram-explanations.md`, `link-indexing.md`, `examples/README.md`, `retrofitting.md`,
`validation.md`, `checklist.md`, `anti-patterns.md`)

## 0. Choose the Right Diagram Type First

Before applying anything below, decide what you're actually diagramming. **Don't default to a flowchart
with subgraphs just because that's the most thoroughly documented part of this skill** — the shape/color
vocabulary below is specifically for flowcharts, and forcing state-machine, entity-relationship, or
class-relationship content into flowchart boxes-and-arrows throws away structure Mermaid has real,
purpose-built syntax for.

| What are you actually diagramming? | Use | Read this file first |
|---|---|---|
| Components/services and how control or data flows between them (architecture, infra topology, request routing, data pipelines) | `flowchart`/`graph` | the flowchart-specific sections below |
| A specific interaction over time between named actors/services — request/response pairs, branching logic, async waits | `sequenceDiagram` | `references/sequence-diagrams.md` |
| The lifecycle of one entity — the states it can be in and what triggers a transition between them | `stateDiagram-v2` | `references/state-diagrams.md` |
| The shape of data — how tables/entities relate, with real cardinality (one-to-many, etc.) | `erDiagram` | `references/er-diagrams.md` |
| Class/type relationships in OOP — inheritance, composition, interfaces, aggregation | `classDiagram` | `references/class-diagrams.md` |
| Git branching/commit/merge history, a release or branching strategy | `gitGraph` | `references/gitgraph-diagrams.md` |

**Coverage note**: block diagrams (`block-beta`) and a handful of lower-priority Mermaid types (gantt,
pie, mindmap, timeline, journey) aren't covered yet — a weaker fit for this skill's focus on engineering/
system documentation. If content genuinely calls for one of these anyway, use correct native Mermaid
syntax for it rather than forcing a flowchart. One emoji per element and the Diagram Explanation
quote-block apply across every diagram type above except ER/class/gitGraph (see each file for why — short
version: their element names are conventionally plain schema/type/branch identifiers, not the kind of
thing an emoji attaches to naturally). Always validate with `scripts/validate_diagrams.js` regardless of
diagram type — the right *primitive* always matters more than styling depth.

**Uplifting or fixing existing diagrams, not drafting a new one?** That's a different task with its own
failure modes — see `references/retrofitting.md` before you start. The short version: don't force a node
into a non-rectangle shape just because a diagram already has several rectangles nearby; judge each node
on its own role, and check how sibling diagrams in the same file already handled the same concept before
improvising a new answer.

**Drawing a `sequenceDiagram`?** Most of the flowchart-specific rules below (node shapes, `style`,
`linkStyle`, subgraphs, link indexing) don't apply — read `references/sequence-diagrams.md` instead, which
covers what actually transfers (emoji, diagram explanation, the validator, solid/dashed intent) and what
doesn't.

---

## Quick Reference: Core Rules (Flowchart-Specific)

### 1. Arrow Labels — ALWAYS Double-Quoted

```
%% CORRECT
A -->|"HTTP REST<br/>(CRUD Operations)"| B
A -.->|"Async Trigger<br/>(Fire & Forget)"| B

%% WRONG
A -->|HTTP REST| B           %% no quotes = rendering errors
A -->|"- Step 1: Do X"| B   %% no markdown lists in labels
A -->|"1. First step"| B    %% no markdown lists in labels
```

Use `<br/>` for multi-line labels. Use `(N)` or `[N]` for step numbering, never `1.` or `-`.

### 2. Node Shapes — Match Shape to Role, Not Just Rectangles

**The single biggest lever for diagram quality.** A diagram where every node is `["Text"]` reads as flat and
generic no matter how good the colors are — the shape itself should tell the reader what kind of thing a node
is before they even read the label. Pick a shape by asking what role the node plays, not by habit:

| Ask | Shape | Syntax | Use For |
|---|---|---|---|
| Is it outside the system, not something we run? | Stadium | `id(["Text"])` | User, browser, third-party endpoint (e.g. Telegram, email) |
| Is it the first edge/routing layer a request hits? | Hexagon | `id{{"Text"}}` | CDN, API gateway, edge router |
| Does it split/forward traffic to multiple downstream targets? | Flag (asymmetric) | `id>"Text"]` | Load balancer, reverse proxy |
| Is it running our code / doing work? | Rectangle | `id["Text"]` | Application service, container, VM |
| Is it a rule/config that lives inside a parent node, not standalone? | Trapezoid (or its mirror) | `id[/"Text"\]` / `id[\"Text"/]` | Listener rule, target group, attached policy — use the mirror if the base shape is already taken in the same diagram |
| Is it data/an artifact entering a process, not a processing step? | Parallelogram | `id[/"Text"/]` | Build artifact, uploaded file, inbound event |
| Is this a genuine fork in the logic? | Rhombus | `id{"Text"}` | Health check, conditional, approval gate |
| Is it a resulting state rather than a component? | Rounded | `id("Text")` | "Live", "Discarded", "Rejected" |
| Does it hold data at rest? | Cylinder | `id[("Text")]` | Database, bucket, queue/topic, cache |
| Is it a named, reusable callable unit? | Subroutine | `id[["Text"]]` | Lambda/serverless function, webhook handler |

**Rule of thumb**: if two nodes play structurally different roles (a gateway vs. the service behind it; a
decision vs. the state it leads to; a data store vs. the thing writing to it), they should almost never share
the same shape — even if they'd otherwise share a color family. Shape carries the "what kind of thing is this,"
color carries "what part of the system does it belong to." Use both, don't make color do all the work.

**Don't force a shape just to avoid a rectangle.** If a node doesn't clearly earn one of the shapes above,
a plain Rectangle in generic-infra-gray is a correct, boring answer — better than pushing it into a
Parallelogram/Cylinder/Trapezoid that doesn't actually fit just for visual variety. This exact
overcorrection (docs/templates forced into Parallelogram, manual steps forced into Trapezoid, insight
callouts forced into Cylinder) was the single most common real-world mistake found retrofitting this
vocabulary against ~40 existing diagrams — see `references/shapes.md` → "Disambiguation Notes" for
the full list of eight named cases, including this one.

See `references/shapes.md` for the full rationale, the eight disambiguation cases, and a worked example.

### 3. Arrow Types

| Connection Type         | Syntax          | `linkStyle` dasharray | When to Use                                    |
|------------------------|-----------------|------------------------|------------------------------------------------|
| Direct dependency      | `-->`           | (none — solid)          | Runtime-critical, user-facing data flow        |
| Infrastructure/passive | `-.->` (dashed) | `5 5`                   | Provisioning, passive mounts, monitoring       |
| Async/event-driven     | `-.->` (dashed) | `3 3`                   | Fire-and-forget, queues, pub/sub, event triggers |

Both infra and async use the same `-.->` dashed arrow at the syntax level — the *only* thing that keeps
them visually distinct on the page is the `linkStyle` dasharray, so don't skip it: `stroke-dasharray:5 5`
for infra, `stroke-dasharray:3 3` for async. Always indicate async nature in the label too:
`"Event Publish<br/>(Fire & Forget)"`, `"Queue Job<br/>(Background)"`.

### 4. Arrow Widths
- **3px** — Application-critical / user-facing connections
- **2px** — Supporting services, infrastructure, cloud services
- **1px** — Rarely used

### 5. Service Node Colors (Dual-mode compatible)

| Service Type        | Fill Color | Stroke Color |
|--------------------|------------|--------------|
| Frontend / UI      | `#4A9EFF`  | `#2B7DE9`    |
| Backend / API      | `#FFB84D`  | `#E69500`    |
| Database           | `#51CF66`  | `#37B24D`    |
| Observability (also: decision/gate Rhombus — shape disambiguates) | `#B47EFF`  | `#9654E8`    |
| Messaging / Proxy  | `#FF6B6B`  | `#E64545`    |
| Gateway / Daemon   | `#FF8787`  | `#E64545`    |
| Networking / VPC Infra | `#66D9E8` | `#15AABF`  |

**Cloud / Infrastructure Colors:**
- Secrets / Auth: `#74C0FC`
- Storage: `#96E6B3` (genuine stored artifacts only — container images, registries, build outputs; not docs/templates a human reads, see `references/shapes.md` disambiguation case #5 for the dividing line)
- Compute: `#91C7FF`
- Generic infra: `#D0D0D0` (also the default for reference content that doesn't earn a service color — docs, templates, config files)
- Image pulls: `#A0A0A0`

**Environment/status-tier gradient (separate from service colors — use for dev/staging/prod or a severity
scale, never to indicate what a node *is*):**
- Tier 1 — Dev / Low / Healthy: `#8CE99A` / `#2F9E44`
- Tier 2 — Staging / Medium / Degraded: `#FFD43B` / `#F08C00`
- Tier 3 — Prod / High / Critical: `#FF6B6B` / `#C92A2A`

See `references/colors.md` → "Environment / Status Tier Colors" for why this needs its own palette
instead of reusing a service color for its hue.

**Node style template:**
```
style NodeName fill:#COLOR,stroke:#DARKER_COLOR,stroke-width:2px,color:#000
```

**Arrow color rule:** Arrows from a service use a **lighter shade** of that service's fill color (increase lightness ~15-20%).

### 6. Emojis — One Per Element, at the Start

Use one emoji per node or subgraph title. Place it at the beginning of the label.

```
%% CORRECT
Frontend["💻 Frontend Portal"]
subgraph ACI["☁️ Azure Container Instance"]

%% WRONG
Frontend["💻🌐🚀 Frontend Portal"]  %% emoji overload
```

**Common emoji mapping:**
- ☁️ Cloud / Azure / AWS  &nbsp; 🖥️ VMs/Servers  &nbsp; 💻 Frontend/Client
- 🔧 Backend/API  &nbsp; ⚙️ App Layer  &nbsp; 🗄️ Database  &nbsp; 💾 Data Layer
- 📁 Volumes  &nbsp; 📂 File Share  &nbsp; 💿 Persistent Volumes
- 🌐 Web Tier  &nbsp; 🔌 Proxy  &nbsp; 📡 Messaging/API
- 🔐 Key Vault  &nbsp; 🔑 Auth/Keys  &nbsp; 🛡️ Security
- 📊 Observability  &nbsp; 📈 Monitoring  &nbsp; 🚀 CI/CD
- 📦 Container Registry  &nbsp; 📬 Service Bus  &nbsp; 📢 Notifications

### 7. Layout

- Use `graph TB` (top-to-bottom) or `graph LR` (left-to-right) as top-level declaration.
- Use hierarchical `subgraph` blocks to tier services logically.
- Add `direction TB` or `direction LR` *inside* subgraphs for local control.
- Keep subgraph titles short — move extra details into node labels using `<br/>`.

### 8. Link Indexing & Comments

Always add a comment line **above** each connection (never inline after it):

```
%% Link 0: Frontend -> Backend (HTTP REST)
Frontend -->|"[1]<br/>HTTP REST API<br/>(CRUD Operations)"| Backend
%% Link 1: Backend -> Database (SQL)
Backend -->|"[2]<br/>SQL Queries<br/>(CRUD)"| Database
```

End the diagram with a link index summary:
```
%% Link Index:
%% 0: Frontend -> Backend (HTTP REST)
%% 1: Backend -> Database (CRUD)
```

Then add `linkStyle` declarations **with comments on separate lines above** (never inline):
```
%% Application Layer (bold, 3px)
linkStyle 0 stroke:#4A9EFF,stroke-width:3px
linkStyle 1 stroke:#51CF66,stroke-width:3px
```

**Critical:** Never place `%% comment` after a `linkStyle` on the same line — it breaks Mermaid parsing.

---

## Diagram Explanation (Required for Non-Trivial Diagrams)

Every complex diagram (>3 nodes, multi-tier, or non-obvious flow) **must** be followed by a quote-block explanation:

```markdown
> **[Heading]**:
> 1. **[Step Name]**: What happens and *why*
> 2. **[Step Name]**: What happens and *why*
>
> **[Design Notes]**: Key architecture decisions, caveats, or patterns used
```

**Heading templates:**
- Flow/sequence: `**Data Flow**:`, `**Key Steps**:`, `**Bootstrap Flow**:`
- Architecture: `**Component Interactions**:`, `**Design Rationale**:`

Guidelines:
- Bold all step names: `**Step Name**:`
- Explain *why*, not just *what*
- 3–7 steps, ~1–2 sentences each
- Follow visual order (top-to-bottom or left-to-right)

**When always required:** Sequence diagrams, complex architecture (>5 services or multiple tiers), bootstrap/init flows, non-obvious relationships.  
**Optional:** Simple 2–3 node diagrams with self-evident relationships.

---

## Anti-Patterns — Never Do These

```
%% Inline comment after connection — BREAKS RENDERING
Frontend -->|"Label"| Backend %% This breaks

%% Markdown list syntax in labels — BREAKS RENDERING
A -->|"- Step 1"| B
A -->|"1. First step"| B

%% Low contrast colors — avoid
style Node fill:#f0f0f0  %% too light for dark mode

%% Emoji overload — avoid
Frontend["💻🌐🚀🎨 Frontend"]  %% max 1 emoji

%% Inline comment after linkStyle — BREAKS RENDERING
linkStyle 0 stroke:#4A9EFF %% HTTP connection

%% Long subgraph titles — avoid
subgraph G["Very Long Title That Gets Cut Off (Extra Info)"]

%% Everything is a rectangle — avoid (this is the default failure mode)
Gateway["Load Balancer"]
Service["App Service"]
Store["Database"]
%% CORRECT: shape carries meaning too
Gateway>"Load Balancer"]
Service["App Service"]
Store[("Database")]

%% The overcorrection is just as bad — forcing a wrong shape to avoid a rectangle
Docs[/"Setup Documentation"/]    %% WRONG: Parallelogram implies data entering a process
Docs("Setup Documentation")       %% CORRECT: static reference content is Rounded
ManualStep[/"Run migration script"\]  %% WRONG: Trapezoid implies attached config, not an action
ManualStep["Run migration script"]     %% CORRECT: it's a plain step, an honest Rectangle
```

---

## Validate Before You Present It

**Do not treat a diagram as done just because it looks right.** Mermaid syntax has enough sharp edges
(shape-bracket mismatches, invalid `linkStyle` indexes, bad subgraph nesting) that manual bracket-counting or
eyeballing misses real failures and flags false positives on valid-but-unusual syntax (the asymmetric/flag
shape `id>"text"]` intentionally has unbalanced brackets — that's correct, not a bug). Ground truth is the
real Mermaid parser + a real headless-Chromium render, not a mental model of the grammar.

Run every diagram — or the whole markdown file it lives in — through the bundled validator before presenting
it as final:

```bash
node ~/.claude/skills/mermaid-diagrams/scripts/validate_diagrams.js --markdown path/to/doc.md
node ~/.claude/skills/mermaid-diagrams/scripts/validate_diagrams.js --file path/to/diagram.mmd
```

It extracts every `\`\`\`mermaid` block from a markdown file (or takes a standalone `.mmd`), actually renders
each one via `@mermaid-js/mermaid-cli`, and exits non-zero if any fail — with the real parser error and the
source line it came from. Requires Node.js; first run downloads `@mermaid-js/mermaid-cli` via `npx` (cached
after that). Full usage: `node scripts/validate_diagrams.js --help`.

**When to run it**: after writing or editing any diagram, before telling the user it's ready — not just for
complex ones. It's cheap (a few seconds per diagram) and catches things a careful read-through won't.

---

## Output Checklist

Before finalizing any diagram:

- [ ] All arrow labels wrapped in double quotes
- [ ] `<br/>` used for multi-line labels (not `\n`)
- [ ] Step numbers use `(N)` or `[N]`, not `1.` or `-`
- [ ] Node shapes match infra role (§2) — not everything defaulted to a rectangle, and no node forced into a non-rectangle shape that doesn't actually fit its role either
- [ ] Solid arrows for direct dependencies, dashed for infra/async
- [ ] Dashed arrows use the right dasharray (`5 5` infra, `3 3` async) — not identical for both
- [ ] Async arrows labeled with fire-and-forget/queue/pub-sub context
- [ ] Service nodes styled with dual-mode-compatible colors
- [ ] Arrow colors are lighter shades of source node colors
- [ ] One emoji per element, at the start
- [ ] Link index comments above each connection
- [ ] `linkStyle` comments on separate lines above (not inline)
- [ ] Link index summary at end of diagram
- [ ] No inline comments after connections or linkStyle statements
- [ ] Diagram followed by quote-block explanation (if non-trivial)
- [ ] Subgraph titles are concise
- [ ] Ran `scripts/validate_diagrams.js` and it passed — not just visually reviewed

---

## Need Full Examples?

For complete working diagram templates — cloud architecture, common patterns, real mined examples, and
non-infra technical-concept diagrams (language runtime mechanics, web framework pipelines, architectural
patterns like CQRS) — read:
→ `references/examples/README.md` (catalog: scenario type → which example file to read)
