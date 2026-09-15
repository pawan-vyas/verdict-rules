// Produces every distribution shape from one source tree.
//
// Why all three rather than ESM alone: the `exports` map and the file layout
// it points at are effectively permanent once published. A consumer's
// `require()` or `<script src>` that works at 0.0.1 must keep working, and
// adding a format later means a consumer's resolution silently changes.
// Cheaper to be complete now than to be compatible later.
//
// No minified build: for a library this size, minification's transfer-size
// saving is mostly erased by gzip/brotli, and the plain IIFE global is the
// one artifact meant for someone to actually read via a CDN URL or a saved
// copy -- not worth trading that for a saving too small to matter.
import { build } from "esbuild";

const shared = {
  entryPoints: ["src/index.ts"],
  bundle: true,
  sourcemap: true,
  // Annotated, not left to plain-`string` inference: esbuild's own
  // BuildOptions expects the LogLevel union, and a widened string type
  // would let a typo here (`"inf0"`) through unnoticed by anything that
  // ever typechecks this file.
  /** @type {"info"} */
  logLevel: "info",
};

await Promise.all([
  // ESM — modern bundlers, Node `import`, and `https://cdn.jsdelivr.net/npm/verdict-rules/+esm`.
  build({
    ...shared,
    format: "esm",
    target: "es2022",
    outfile: "dist/index.js",
  }),

  // CJS — Node `require()`, and anything predating ESM.
  build({
    ...shared,
    format: "cjs",
    target: "node18",
    outfile: "dist/index.cjs",
  }),

  // IIFE global — a plain <script src> tag in vanilla JS, no module system at
  // all. Targets es2019 so it runs in browsers that never learned private
  // class fields; esbuild downlevels them.
  build({
    ...shared,
    format: "iife",
    globalName: "VerdictRules",
    target: ["es2019"],
    outfile: "dist/verdict-rules.global.js",
  }),
]);
