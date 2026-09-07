# Mermaid Diagram Guidelines — Reference Index

**Purpose:** Standards and best practices for creating consistent, readable Mermaid diagrams in
documentation, to be followed by AI agents generating diagrams and all team members.

This reference material is split by topic instead of living in one file — a single monolith would force
loading everything whenever any one topic was needed. Each file below is independently loadable; read
only the ones relevant to your current task.

## Topics

1. [Color Palette Standards](colors.md) — service node colors, environment/status-tier colors,
   networking colors, connection colors, dual-mode compatibility
2. [Node Shape Vocabulary](shapes.md) — the curated shape table, eight disambiguation cases found in
   real retrofits, the exhaustive Mermaid shape spec
3. [Arrow Labeling & Styling Guidelines](arrows.md) — quoting rules, step numbering, solid vs. dashed,
   dasharray conventions, arrow width and color
4. [Layout Best Practices](layout.md) — subgraphs, direction control, avoiding title overflow/crossing arrows
5. [Emoji Usage Guidelines](emoji.md) — one emoji per element, category mapping, placement
6. [Diagram Explanations](diagram-explanations.md) — the required quote-block explanation format
7. [Link Indexing & Documentation](link-indexing.md) — `%% Link N:` comments, the link index summary,
   `linkStyle` documentation
8. [Examples](examples/README.md) — worked examples and common patterns, catalogued by scenario type
9. [Retrofitting Existing Diagrams](retrofitting.md) — a different task than drafting new diagrams,
   with its own failure modes
10. [Validation & Export Tooling](validation.md) — why and how to run `scripts/validate_diagrams.js`
11. [Checklist for Creating Diagrams](checklist.md) — before/during/after creation checklist
12. [Anti-Patterns (What to Avoid)](anti-patterns.md) — the mistakes that actually break rendering or
    quality, with wrong/correct pairs

**Other diagram types have their own files** — the topics above are flowchart-specific. Almost none of it
applies to these:
- [`sequence-diagrams.md`](sequence-diagrams.md) — `sequenceDiagram`
- [`state-diagrams.md`](state-diagrams.md) — `stateDiagram-v2`
- [`er-diagrams.md`](er-diagrams.md) — `erDiagram`
- [`class-diagrams.md`](class-diagrams.md) — `classDiagram`
- [`gitgraph-diagrams.md`](gitgraph-diagrams.md) — `gitGraph`

**Not covered yet**: block diagrams (`block-beta`) and a handful of lower-priority Mermaid types (gantt,
pie, mindmap, timeline, journey) that are a weaker fit for this skill's focus on engineering/system
documentation. If content genuinely calls for one of these, use correct native Mermaid syntax for it —
see `SKILL.md`'s diagram-type table for the reasoning on when to reach for a non-flowchart type at all.

## References

- [Mermaid Official Documentation](https://mermaid.js.org/)
- [Mermaid Flowchart Syntax — Node Shapes](https://mermaid.js.org/syntax/flowchart.html) (source for the
  [Exhaustive Shape Reference](shapes.md#exhaustive-shape-reference-mermaid-v1116-current-stable-spec) —
  re-check this page if a node shape looks unavailable, the spec evolves)
- [Mermaid Live Editor](https://mermaid.live/)
- [Color Palette Tools](https://coolors.co/)

