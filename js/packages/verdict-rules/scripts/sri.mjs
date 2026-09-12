#!/usr/bin/env node
// Computes Subresource Integrity hashes for the browser bundles, and can
// verify them against what a CDN actually serves.
//
// This can run *before* publishing, which is the whole reason it is automation
// rather than a manual follow-up: unpkg and jsDelivr serve the npm tarball's
// files verbatim, and the hashed artifact does not contain its own hash — so
// the bytes we are about to publish are the bytes a browser will fetch. There
// is no chicken-and-egg.
//
//   node scripts/sri.mjs            ready-to-paste <script> tags
//   node scripts/sri.mjs --json     machine-readable, for release notes
//   node scripts/sri.mjs --verify   fetch from every CDN and compare
import { createHash } from "node:crypto";
import { readFile } from "node:fs/promises";
import { createRequire } from "node:module";

const require = createRequire(import.meta.url);
const { name, version } = require("../package.json");

/** Every file a consumer could load with a <script src> tag. */
const BROWSER_BUNDLES = [
  "dist/verdict-rules.global.min.js",
  "dist/verdict-rules.global.js",
];

/**
 * CDNs that mirror npm automatically. Adding one is a row here — they all
 * serve the tarball's bytes unchanged, so the same hash holds for all of them.
 * That is the point of verifying against every one rather than a favourite.
 */
const CDNS = {
  jsdelivr: (pkg, ver, file) => `https://cdn.jsdelivr.net/npm/${pkg}@${ver}/${file}`,
  unpkg: (pkg, ver, file) => `https://unpkg.com/${pkg}@${ver}/${file}`,
};

const sri = (bytes) => `sha384-${createHash("sha384").update(bytes).digest("base64")}`;

async function hashes() {
  const out = {};
  for (const file of BROWSER_BUNDLES) {
    out[file] = sri(await readFile(file));
  }
  return out;
}

async function verify() {
  const local = await hashes();
  let failures = 0;
  for (const [file, expected] of Object.entries(local)) {
    for (const [cdn, url] of Object.entries(CDNS)) {
      const href = url(name, version, file);
      let actual;
      try {
        const res = await fetch(href);
        if (!res.ok) throw new Error(`HTTP ${res.status}`);
        actual = sri(Buffer.from(await res.arrayBuffer()));
      } catch (err) {
        console.error(`  ✗ ${cdn} ${file}: ${err.message}`);
        failures++;
        continue;
      }
      const ok = actual === expected;
      console.log(`  ${ok ? "✓" : "✗"} ${cdn} ${file}`);
      if (!ok) {
        console.error(`      expected ${expected}`);
        console.error(`      served   ${actual}`);
        failures++;
      }
    }
  }
  if (failures > 0) {
    console.error(
      `\n${failures} mismatch(es). A CDN is serving bytes that are not what was published.`,
    );
    process.exit(1);
  }
  console.log("\nEvery CDN serves exactly the published bytes.");
}

const mode = process.argv[2];

if (mode === "--verify") {
  await verify();
} else if (mode === "--json") {
  console.log(JSON.stringify({ name, version, integrity: await hashes() }, null, 2));
} else {
  const all = await hashes();
  console.log(`<!-- ${name}@${version} — pinned and integrity-checked -->`);
  for (const [cdn, url] of Object.entries(CDNS)) {
    const file = BROWSER_BUNDLES[0];
    console.log(
      `\n<!-- ${cdn} -->\n` +
        `<script\n` +
        `  src="${url(name, version, file)}"\n` +
        `  integrity="${all[file]}"\n` +
        `  crossorigin="anonymous"></script>`,
    );
  }
}
