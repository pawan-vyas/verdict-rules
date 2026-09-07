# Node Shape Vocabulary

### Why Shape Matters As Much As Color

Every example in earlier versions of this skill defaulted every node to a plain rectangle `["Text"]`,
distinguishing them only by fill color. That collapses a whole dimension of meaning: **color should say
which part of the system a node belongs to; shape should say what kind of thing it is.** A load balancer and
the service behind it are structurally different (one routes, one computes) even when they're the same color
family — they should not look identical. Diagrams that vary shape by role read as intentional, layered
architecture instead of a wall of uniform boxes.

**Rule**: before styling a node, ask "what role does this play?" — not "what color is this team's stuff?" —
then pick the shape from the table below, and only then pick the color.

### Curated Working Vocabulary (Use This By Default)

This is the shape set actually used in practice — small enough to stay memorable, and it's the exact
vocabulary that produced the benchmark diagram in [Example 3](examples/README.md#example-3-mixed-shape-infra-scenario)
below. Default to these 11 shapes; reach for the exhaustive spec (further down) only when none of these fit.

| Ask | Shape | Syntax | Use For |
|---|---|---|---|
| Is it outside the system, not something we run? | Stadium | `id(["Text"])` | User, browser, third-party endpoint (Telegram, email, webhook target) |
| Is it the first edge/routing layer a request hits? | Hexagon | `id{{"Text"}}` | CDN, API gateway, edge router |
| Does it split/forward traffic to multiple downstream targets? | Flag (asymmetric) | `id>"Text"]` | Load balancer, reverse proxy |
| Is it running our code / doing work? | Rectangle | `id["Text"]` | Application service, container, VM |
| Is it a rule/config living inside a parent node, not standalone? | Trapezoid | `id[/"Text"\]` (or its mirror, Trapezoid-alt: `id[\"Text"/]`) | Listener rule, target group, attached policy — use the mirror if the base Trapezoid is already used elsewhere in the same diagram |
| Is it data/an artifact entering a process, not a processing step? | Parallelogram | `id[/"Text"/]` | Build artifact, uploaded file, inbound event |
| Is this a genuine fork in the logic? | Rhombus | `id{"Text"}` | Health check, conditional, approval gate |
| Is it a resulting state rather than a component? | Rounded | `id("Text")` | "Live", "Discarded", "Rejected" |
| Does it hold data at rest? | Cylinder | `id[("Text")]` | Database, bucket, queue/topic, cache |
| Is it a named, reusable callable unit? | Subroutine | `id[["Text"]]` | Lambda/serverless function, webhook handler |
| Is it a pure fan-out/fan-in point with no identity of its own? | Circle | `id(("Text"))` | Shared bus junction, load-balanced pool entry — **short labels only, see caveat below** |

**Circle caveat — scales badly with label length**: a circle must have equal radius in every direction, so
it grows in *both* dimensions to fit long text, not just wider like a rectangle does. A single-word label is
fine; a real infra label like `"Backend Target Group (PathPattern /api/*)"` renders a circle at roughly
230×230px against ~250×90px for a rectangle-family shape with the same text — it will visibly dwarf every
other node in the diagram. Reserve circle for genuinely short labels (a single word or a number), or avoid it
entirely for infra nodes that carry descriptive text. Confirmed by actually rendering both and comparing
`viewBox` dimensions — don't just eyeball it, check with the validator (see
[Validation & Export Tooling](validation.md)).

**Design principle behind this set**: structurally different roles never share a shape, even within the same
color family. In the benchmark example, CloudFront (hexagon) and the ALB behind it (flag) are both "networking
orange" — same color family, different shape — because one is edge routing and the other is traffic
distribution. Two genuinely different jobs, one visual family, two silhouettes.

**Nested sub-resources — considered, not the default**: for a node that's a literal sub-resource of a parent
already in the diagram (a listener rule/target group that's part of the load balancer resource itself, not a
separate thing traffic passes through), reusing the parent's *exact* shape was tried in practice as a way to
show containment. It worked visually, but made the child too easy to mistake for another instance of the
parent at a glance, and coupled the child's shape to the parent's — if the parent's shape ever changes, so
must the child's. **Settled preference: keep Trapezoid (or its mirror, Trapezoid-alt) for this role instead**,
distinct from the parent but still visually "attached config," and pick whichever of the two trapezoid
mirrors isn't already in use elsewhere in the same diagram.

**Known gap — dashboards, reports, docs, and any other read-only/reference content**: a node that's neither
a decision, a data store, nor an actor — a dashboard, a rendered report, a status page, but also a plain
setup-documentation file or a `.env.example` template someone reads rather than a process step someone
runs — has no clean fit in the 11 above. The exhaustive spec below has a purpose-built shape for the
dashboard case specifically (`curv-trap` / "Display"), but it's part of the newer `@{shape:}` syntax with
the multi-renderer compatibility caveat noted there. If the target renderer is confirmed to support v11.3+,
use `curv-trap` for genuine dashboards. Otherwise — and for the broader "reference content" case generally,
in classic-shorthand-only contexts — default to **Rounded**, reasoning it as "a resulting view/state to look
at or a piece of static reference material" rather than a component doing work. **Don't reach for
Parallelogram**: that shape means data or an artifact actively *entering* a process, not a passive
already-computed result or a template a human reads. See the Disambiguation Notes right below for the
full worked case (this exact substitution — Parallelogram used for a doc/template file — is the single
most common real-world misfire found in this vocabulary's first large-scale retrofit pass).

### Disambiguation Notes — Cases That Come Up in Practice

The table above and the "Nested sub-resources"/"Known gap" notes cover the common cases well, but a
retrofit pass across ~40 real diagrams (uplifting an existing doc set, not designing from scratch) and a
separate stress-test against a novel CI/CD scenario surfaced eight specific ambiguities worth naming
explicitly, because a model under time pressure across many diagrams tends to resolve them the same wrong
way each time rather than independently guessing differently. If you're unsure which shape/color fits a
node and it isn't one of these eight, that's fine — an honest Rectangle in plain infra-gray is a correct,
boring answer, and boring-but-accurate always beats interesting-but-wrong.

**The one anti-pattern behind most of these**: forcing a node into a non-rectangle shape or an
off-palette color *specifically to avoid it looking like a plain rectangle*, when the node's actual role
doesn't fit that shape/color any better than Rectangle would have. Shape and color exist to carry meaning,
not to add visual variety for its own sake — a diagram where every node correctly earns its shape,
including several honest Rectangles, is better than one where a third of the nodes were pushed into the
wrong non-rectangle shape just to avoid three Rectangles in a row. When retrofitting an existing diagram
specifically, see [Retrofitting Existing Diagrams](retrofitting.md) for how this plays out
at scale.

1. **Reference/read-only content (docs, `.env.example`, templates) → Rounded, not Parallelogram.**
   Confirmed in two independent real diagrams: setup documentation and a `.env.example` template were both
   given Parallelogram (implying they're data flowing into a process) and Storage-green (implying they're
   a data store), when neither is true — nobody's code reads `.env.example` and acts on it at runtime; a
   human reads it once during setup. Ask: "does something in this diagram actually consume this as
   input at runtime, or is it static material a human refers to?" The former is Parallelogram; the latter
   is Rounded, typically colored generic-infra-gray (`#D0D0D0`) rather than any service-family color.

2. **Plain action/invocation steps → Rectangle or Subroutine, not Trapezoid.** Confirmed in a real
   diagram: "manually write SQL," "execute SQL manually," "run scripts" were all given Trapezoid, which
   is reserved for a rule/config/policy that lives *inside* a parent resource (§ above) — not for a verb, a
   step, or an action someone performs. If it's a one-off step within a larger flow, it's a plain
   Rectangle (it's "doing work," same as any other compute step). If it's specifically a named, callable
   unit invoked at a point in the flow (a deploy job, a migration script run by CI), use Subroutine instead
   — see #7 below for how to tell those two apart.

3. **Insight/consequence/callout nodes → Rounded, not Cylinder.** Confirmed across four sibling diagrams
   in one real doc: a callout like "80–90% of bugs never reach integration" was given Cylinder, which
   means "holds data at rest" — a summary observation holds no data, it's a resulting insight, exactly
   like the "Live"/"Discarded" outcome states in Example 3 below. If a node's job is to state a takeaway
   or consequence rather than store or process anything, it's Rounded, not Cylinder — regardless of how
   tempting a "stats/reporting" association with Cylinder might feel.

4. **Attached policy/rule/config → Trapezoid, and don't leave it as Rectangle out of habit.** The reverse
   of #2: a Terraform `security_groups.tf` (ingress/egress rules attached to a VPC module) or a
   `backups.tf` (backup policy attached to a database module) is exactly the "rule/config living inside a
   parent, not standalone" case the vocabulary already names — but in a real retrofit these were left as
   plain Rectangle because Trapezoid wasn't already in use elsewhere in the same diagram and so didn't
   come to mind. If a node's whole job is describing a policy/rule that's attached to a sibling resource in
   the same subgraph, reach for Trapezoid even if nothing nearby is already using it.

5. **Artifact/registry vs. reference-content — the real dividing line for Storage-green + Cylinder.**
   A container image or container registry genuinely *is* Storage-green + Cylinder (it's a real stored
   artifact, pulled/fetched at runtime) — that part of a first CI/CD test diagram was a correct
   improvisation, just previously undocumented. The dividing line against #1 above: ask "is this thing
   fetched/pulled as an artifact by something else in the diagram, or is it read by a human / used as a
   static local reference?" Container image → fetched, real artifact → Storage/Cylinder. Setup doc,
   `.env.example` → read by a human, not fetched → Rounded/gray. Same visual instinct ("not a rectangle,
   not the frontend/backend color"), opposite correct answer — check which side of the fetch/read line a
   node actually falls on before picking.

6. **SaaS-hosted but runs our code (e.g. GitHub Actions, a hosted CI runner) → Rectangle/Subroutine, not
   Stadium.** Stadium means "outside the system, not something we run" — a third-party product whose
   *behavior* we don't author (GitHub the repo host, a payment provider, an email API). A CI runner
   executing *our* workflow YAML is hosted by a third party but running code we wrote and maintain — from
   a reader's perspective it's part of "the system doing work," just externally hosted. Reserve Stadium for
   things whose behavior is out of our hands; use Rectangle/Subroutine for anything executing our own
   config/code, wherever it's physically hosted.

7. **Invoked job vs. always-on service → Subroutine vs. Rectangle.** Both are "running our code," so the
   Ask-column question in the main table doesn't fully separate them. Rule of thumb: if you'd describe the
   node with a completing verb phrase ("run the deploy," "invoke the function," "execute the migration") —
   something with a discrete start, input, and return — use Subroutine. If you'd describe it as an ongoing
   noun ("the API service," "the worker daemon") that's simply *running*, not being invoked-and-returning,
   use Rectangle. A CI/CD deploy step that fires once per release and finishes is Subroutine; the
   long-running service it deploys into is Rectangle.

8. **A decision-outcome that's also the trigger for a real subsequent action (e.g. an automated rollback)
   → model it as two things, not one shape.** A rollback is simultaneously "the outcome of a failed health
   check" (a state) and "a real corrective mutation against the running system" (an action) — cramming
   both meanings into a single node/shape loses one of them. Resolved pattern: give the outcome its own
   Rounded state node (e.g. "Rolled Back"), then draw a **separate, solid, critical-weight edge** from that
   state back into whatever it's actually mutating (e.g. back into the running pods/service), distinct from
   the dashed "becomes" edges used for passive state transitions elsewhere in the same diagram. The state
   node stays Rounded; the corrective edge is what carries the "this is a real action" meaning.

### Exhaustive Shape Reference (Mermaid v11.16, Current Stable Spec)

Pulled from the official spec at `https://mermaid.js.org/syntax/flowchart.html` — use this when the curated
11 above don't have a good fit, or when a more precise semantic (e.g. "manual input" vs generic parallelogram)
is worth the extra specificity. **Compatibility caveat**: the classic shorthand shapes (first table) render
everywhere Mermaid is supported, including older renderer versions. The expanded `@{ shape: name }` syntax
(second set of tables) requires Mermaid v11.3.0+ — confirm the target renderer (GitHub, ADO wiki, Confluence,
VS Code extension, etc.) supports it before relying on anything beyond the classic set for docs that need to
render in more than one place. If in doubt, prefer the classic shorthand.

#### Classic Shorthand Shapes (universally supported)

| Shape | Syntax | Represents |
|---|---|---|
| Rectangle | `A["Text"]` | Standard process |
| Rounded | `A("Text")` | Event / rounded process |
| Stadium | `A(["Text"])` | Terminal point |
| Subroutine | `A[["Text"]]` | Subprocess |
| Cylinder | `A[("Text")]` | Database / storage |
| Circle | `A(("Text"))` | Start/end point |
| Asymmetric (flag) | `A>"Text"]` | Directional process |
| Rhombus | `A{"Text"}` | Decision / question |
| Hexagon | `A{{"Text"}}` | Preparation / condition |
| Parallelogram | `A[/"Text"/]` | Data input/output |
| Parallelogram (alt) | `A[\"Text"\]` | Data input/output (mirrored) |
| Trapezoid | `A[/"Text"\]` | Manual operation |
| Trapezoid (alt) | `A[\"Text"/]` | Priority action |
| Double circle | `A((("Text")))` | Stop/end point |

#### Expanded Shape Syntax — `nodeId@{ shape: shapeName }` (v11.3.0+)

Organized by semantic category, same spirit as the emoji categorization below — pick by what the node *is*,
not by which shape looks nice.

**Terminal / Start-End**

| Semantic Name | Shape Name | Aliases |
|---|---|---|
| Start | `circle` | `circ` |
| Start (small) | `sm-circ` | `small-circle`, `start` |
| Terminal point | `stadium` | `pill`, `terminal` |
| Stop | `dbl-circ` | `double-circle` |
| Stop (framed) | `fr-circ` | `framed-circle`, `stop` |
| Junction | `f-circ` | `filled-circle`, `junction` |
| Summary | `cross-circ` | `crossed-circle`, `summary` |

**Process**

| Semantic Name | Shape Name | Aliases |
|---|---|---|
| Process | `rect` | `proc`, `process`, `rectangle` |
| Event | `rounded` | `event` |
| Lined/shaded process | `lin-rect` | `lin-proc`, `lined-process`, `shaded-process` |
| Multi-process | `st-rect` | `processes`, `procs`, `stacked-rectangle` |
| Subprocess | `fr-rect` | `framed-rectangle`, `subproc`, `subprocess`, `subroutine` |
| Tagged process | `tag-rect` | `tag-proc`, `tagged-process`, `tagged-rectangle` |
| Divided process | `div-rect` | `div-proc`, `divided-process`, `divided-rectangle` |
| Fork/join | `fork` | `join` |

**Decision / Logic**

| Semantic Name | Shape Name | Aliases |
|---|---|---|
| Decision | `diam` | `decision`, `diamond`, `question` |
| Prepare conditional | `hex` | `hexagon`, `prepare` |
| Odd | `odd` | — |
| Bang | `bang` | — |

**Data / Storage**

| Semantic Name | Shape Name | Aliases |
|---|---|---|
| Database | `cyl` | `cylinder`, `database`, `db` |
| Direct access storage | `h-cyl` | `das`, `horizontal-cylinder` |
| Disk storage | `lin-cyl` | `disk`, `lined-cylinder` |
| Data store | `datastore` | `data-store` |
| Internal storage | `win-pane` | `internal-storage`, `window-pane` |
| Stored data | `bow-rect` | `bow-tie-rectangle`, `stored-data` |

**Document**

| Semantic Name | Shape Name | Aliases |
|---|---|---|
| Document | `doc` | `document` |
| Multi-document | `docs` | `documents`, `st-doc`, `stacked-document` |
| Lined document | `lin-doc` | `lined-document` |
| Tagged document | `tag-doc` | `tagged-document` |

**Input / Output**

| Semantic Name | Shape Name | Aliases |
|---|---|---|
| Data input/output | `lean-r` | `in-out`, `lean-right` |
| Data input/output (mirrored) | `lean-l` | `lean-left`, `out-in` |
| Manual input | `sl-rect` | `manual-input`, `sloped-rectangle` |

**Flow Control**

| Semantic Name | Shape Name | Aliases |
|---|---|---|
| Delay | `delay` | `half-rounded-rectangle` |
| Collate | `hourglass` | `collate` |
| Extract | `tri` | `extract`, `triangle` |
| Manual file | `flip-tri` | `flipped-triangle`, `manual-file` |
| Loop limit | `notch-pent` | `loop-limit`, `notched-pentagon` |

**Comment / Documentation**

| Semantic Name | Shape Name | Aliases |
|---|---|---|
| Comment | `brace` | `brace-l`, `comment` |
| Comment (right) | `brace-r` | — |
| Comment (both sides) | `braces` | — |

**Communication / Display**

| Semantic Name | Shape Name | Aliases |
|---|---|---|
| Com link | `bolt` | `com-link`, `lightning-bolt` |
| Display | `curv-trap` | `curved-trapezoid`, `display` |
| Card | `notch-rect` | `card`, `notched-rectangle` |
| Cloud | `cloud` | — |

**Manual Operations**

| Semantic Name | Shape Name | Aliases |
|---|---|---|
| Manual operation | `trap-t` | `inv-trapezoid`, `manual`, `trapezoid-top` |
| Priority action | `trap-b` | `priority`, `trapezoid`, `trapezoid-bottom` |

**Text / Content**

| Semantic Name | Shape Name | Aliases |
|---|---|---|
| Text block | `text` | — |
| Paper tape | `flag` | `paper-tape` |

**Icon / Image nodes** (v11.3.0+, no classic equivalent):
```
nodeId@{ icon: "iconName", form: "circle|square|rounded", label: "text", pos: "t|b", h: 48 }
nodeId@{ img: "url", label: "text", pos: "t|b", w: 60, h: 60, constraint: "on|off" }
```
Useful for dropping in real cloud-provider icons (AWS/Azure/GCP) instead of an emoji-labeled generic shape —
higher fidelity, but only use if the target renderer is confirmed to support it and an icon registry is
already wired up; otherwise stick to emoji + shape, which works everywhere.

### Decision Checklist

1. Does this node represent something outside the system's control? → **Stadium**
2. Is it the entry/edge layer? → **Hexagon**
3. Does it distribute traffic to multiple targets? → **Flag**
4. Is it doing compute work? → **Rectangle**
5. Is it a sub-config that lives inside a parent, not standalone? → **Trapezoid**
6. Is it data/an artifact flowing in, not a processing step? → **Parallelogram**
7. Is it a genuine branch in the logic? → **Rhombus**
8. Is it an outcome/state, not a component? → **Rounded**
9. Does it persist data? → **Cylinder**
10. Is it a named callable function? → **Subroutine**
11. Is it a dashboard/report/read-only view of already-computed data? → **Rounded** (classic-only contexts)
    or `curv-trap`/Display (if v11.3+ syntax is supported) — see the "Known gap" note above
12. None of the above fit well? → check the **exhaustive spec** above for a more precise semantic match
    before falling back to a plain rectangle.

---

