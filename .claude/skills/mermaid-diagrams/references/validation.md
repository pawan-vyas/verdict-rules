# Validation & Export Tooling

### Why Not Just Eyeball It

Manual review — bracket-counting, reading the syntax carefully — misses real Mermaid failures and produces
false positives. Two concrete examples that came up building this skill:
- The asymmetric/flag shape `id>"text"]` has **intentionally unbalanced brackets** (no `[` to match the `]`)
  — a naive bracket-counter flags it as broken when it's correct.
- A single mismatched quote inside an arrow label, a `linkStyle` index that doesn't exist, or a subgraph
  closed in the wrong place can all look fine on a quick read and still fail to render.

The only reliable check is the real Mermaid parser actually running, ideally with the real renderer behind it
so rendering crashes (not just parse errors) get caught too.

### The Validator

`scripts/validate_diagrams.js` (in this skill's directory) is a self-contained Node.js script that shells out
to `@mermaid-js/mermaid-cli` (`mmdc`), which runs Mermaid.js inside headless Chromium via Puppeteer — the same
engine that actually renders your diagram wherever it's viewed. No project setup beyond Node.js; `npx` fetches
`@mermaid-js/mermaid-cli` on first use and caches it.

```bash
# Validate every ```mermaid block in a markdown file (the common case — diagrams embedded in docs)
node scripts/validate_diagrams.js --markdown path/to/doc.md

# Validate a single standalone .mmd file
node scripts/validate_diagrams.js --file path/to/diagram.mmd

# Export instead of just validating (svg is fastest; png/pdf also supported)
node scripts/validate_diagrams.js --markdown path/to/doc.md --format png --keep-output ./rendered

node scripts/validate_diagrams.js --help
```

**Behavior**:
- Extracts every fenced `\`\`\`mermaid` block from a markdown file, tracking the source line number of each
  fence so failures point back to an exact location in the document.
- Renders each diagram independently via `mmdc`; a failure in one block doesn't stop the others from being
  checked.
- Prints the real parser/render error (not a guess) for anything that fails, truncated to a readable length.
- Exits **0** only if every diagram in the input rendered cleanly; exits **1** if any failed, or if a markdown
  file had zero `\`\`\`mermaid` blocks (treat that as a signal something's wrong with the input, not a pass).
- Bundles a Puppeteer config with `--no-sandbox` so it also works inside CI/container environments where the
  Chromium sandbox can't initialize — no extra flags needed on your end.
- `--keep-output <dir>` persists the rendered files (useful when you actually want the SVG/PNG/PDF, not just
  a validation pass); without it, output is written to a temp dir and discarded.

**When to run it**: after writing or editing any diagram, before presenting it as final — see the "Validate
Before You Present It" section in `SKILL.md`. This applies even to simple diagrams; the failure modes above
aren't specific to complex ones.

---

