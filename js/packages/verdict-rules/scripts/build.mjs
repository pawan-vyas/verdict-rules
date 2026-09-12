// Produces every distribution shape from one source tree.
//
// Why all four rather than ESM alone: the `exports` map and the file layout it
// points at are effectively permanent once published. A consumer's `require()`
// or `<script src>` that works at 0.0.1 must keep working, and adding a format
// later means a consumer's resolution silently changes. Cheaper to be complete
// now than to be compatible later.
import { build } from "esbuild";

const shared = {
  entryPoints: ["src/index.ts"],
  bundle: true,
  sourcemap: true,
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

  // Minified global — what a CDN URL should actually serve.
  build({
    ...shared,
    format: "iife",
    globalName: "VerdictRules",
    target: ["es2019"],
    minify: true,
    outfile: "dist/verdict-rules.global.min.js",
  }),
]);
