# Checklist for Creating Diagrams

### Before Creating

- [ ] Identify all services and their relationships
- [ ] Determine service types (frontend, backend, data, etc.)
- [ ] For each node, decide its **role** (external actor, edge/gateway, compute, decision, outcome state, data store, callable function, attached config) — this drives shape, done before color
- [ ] Classify connections (direct vs infrastructure)
- [ ] Plan layout hierarchy (what goes in which tier)

### During Creation

- [ ] Assign shapes by role per the [Node Shape Vocabulary](shapes.md) — verify no two structurally different roles share a shape just because they share a color
- [ ] Use consistent color palette (dual-mode compatible)
- [ ] Apply service-specific colors to related arrows
- [ ] Use solid arrows for direct dependencies
- [ ] Use dashed arrows for infrastructure
- [ ] Add meaningful emojis (one per element)
- [ ] Keep subgraph titles concise
- [ ] Use `direction TB` or `LR` as needed
- [ ] Add link index comments before each connection

### After Creation

- [ ] Check link indexes match linkStyle statements
- [ ] Colors come from the documented dual-mode-compatible palette (not an arbitrary/off-palette choice)
- [ ] Add style definitions for all nodes
- [ ] Add linkStyle definitions for all connections, with the correct dasharray (`5 5` infra vs `3 3` async)
- [ ] Document link index mapping
- [ ] No node forced into a non-rectangle shape or off-palette color just to avoid looking plain — see [Disambiguation Notes](shapes.md#disambiguation-notes--cases-that-come-up-in-practice)
- [ ] Run `scripts/validate_diagrams.js` — see [Validation & Export Tooling](validation.md) — and confirm it passes before treating the diagram as done. This is the real check for "arrows visible, nothing overlapping/malformed" — trust the actual render over a manual read.

---

