import assert from "node:assert/strict";
import { readFile, readdir } from "node:fs/promises";
import { describe, it } from "node:test";

/**
 * Every <script src> pointing at a CDN's global bundle must be pinned to a
 * real version and carry a real integrity hash -- or be the one explicit,
 * unmistakable placeholder form this repo uses instead of a real pin (see
 * below for why). Enforced rather than remembered, because both failures are
 * silent: an unpinned URL upgrades a consumer without anyone touching their
 * site, and a tag without `integrity` executes whatever the CDN returns with
 * full access to the page.
 *
 * This is a documentation test on purpose. The risk lives in what we tell
 * people to paste, not in the library's own code.
 *
 * Two deliberate exceptions, not oversights:
 *
 * - `@latest` is allowed, but only on the `/+esm` transform path. A real pin
 *   was tried here twice already (this package's own CDN examples went
 *   stale at `@0.0.2`, then again at `@0.0.5`) -- and an ESM CDN URL has no
 *   integrity hash to protect in the first place (it transforms on the fly,
 *   so there are no stable bytes to hash), meaning a pin bought nothing but
 *   the staleness risk. `@latest` on the global-bundle path is still a real
 *   failure: that path *does* need a matching integrity hash, and `@latest`
 *   makes one structurally impossible to keep correct.
 * - The literal placeholder `@X.Y.Z` plus an `integrity="sha384-<...>"`
 *   value are recognized as a deliberate placeholder pin, not a real one
 *   left unfinished -- tokens that can never be mistaken for real values,
 *   so a reader who copies them verbatim gets an immediately obvious
 *   404/integrity failure instead of a silently-stale real-looking version.
 *   The package README points at jsdelivr's own package page for the real
 *   values instead of keeping a copy here that would need updating every
 *   release.
 */
const CDN_HOSTS = ["cdn.jsdelivr.net", "unpkg.com", "cdnjs.cloudflare.com", "esm.sh"];
const PLACEHOLDER_VERSION = "X.Y.Z";
const PLACEHOLDER_HASH = /^sha(256|384|512)-<.*>$/;

async function markdownFiles() {
  const entries = await readdir(".", { withFileTypes: true });
  return entries
    .filter((e) => e.isFile() && e.name.endsWith(".md"))
    .map((e) => e.name);
}

describe("CDN snippets in our documentation", () => {
  it("never show an unpinned version, except @latest on the /+esm path", async () => {
    for (const file of await markdownFiles()) {
      const text = await readFile(file, "utf8");
      for (const host of CDN_HOSTS) {
        // Every occurrence of "<host>/npm/verdict-rules@<version>", capturing
        // the version and whether it's the ESM transform path.
        const pattern = new RegExp(
          `${host.replace(/\./g, "\\.")}/[^\\s"']*?/verdict-rules(?:@([^/\\s"']+))?([^\\s"']*)`,
          "g",
        );
        for (const match of text.matchAll(pattern)) {
          const [, version, rest] = match;
          assert.notEqual(
            version,
            undefined,
            `${file} has an unpinned ${host} URL — it would silently upgrade consumers: ${match[0]}`,
          );
          if (version === "latest") {
            assert.equal(
              rest.startsWith("/+esm"),
              true,
              `${file}: "@latest" is only allowed on the /+esm transform path, ` +
                `where there's no integrity hash to protect -- the global bundle ` +
                `path needs a real pin: ${match[0]}`,
            );
          }
        }
      }
    }
  });

  it("always pair a global-bundle <script src> CDN tag with integrity and crossorigin", async () => {
    for (const file of await markdownFiles()) {
      const text = await readFile(file, "utf8");
      // The opening tag's own `>` must end the match before the lazy body
      // starts -- requiring `></script>` directly (the original shape here)
      // only matches an empty-body tag; a tag with real content between
      // (like the type="module" import block below) has its closing
      // </script> preceded by whatever its last line of code is, not `>`,
      // so that version silently merged two separate tags into one match
      // instead of finding both. Confirmed by reproducing it standalone
      // before trusting this fix.
      const tags = text.match(/<script[^>]*>[\s\S]*?<\/script>/g) ?? [];
      for (const tag of tags) {
        if (!CDN_HOSTS.some((h) => tag.includes(h))) continue;
        if (tag.includes("type=\"module\"")) continue; // import maps carry no src

        const integrityMatch = tag.match(/integrity="([^"]+)"/);
        assert.notEqual(
          integrityMatch,
          null,
          `${file}: a CDN <script> tag without an integrity hash executes whatever the CDN returns:\n${tag}`,
        );
        assert.match(
          tag,
          /crossorigin="anonymous"/,
          `${file}: SRI on a cross-origin script requires crossorigin="anonymous":\n${tag}`,
        );

        // A real hash and the placeholder form are both acceptable; anything
        // else (an empty string, a truncated copy-paste, a typo) is not.
        const isRealHash = /^sha(256|384|512)-[A-Za-z0-9+/]+=*$/.test(integrityMatch[1]);
        const isPlaceholderHash = PLACEHOLDER_HASH.test(integrityMatch[1]);
        assert.equal(
          isRealHash || isPlaceholderHash,
          true,
          `${file}: integrity value is neither a real hash nor the recognized placeholder form:\n${tag}`,
        );

        // The placeholder version and placeholder hash must appear together
        // -- a real version paired with a placeholder hash (or vice versa)
        // is a real bug, not a deliberate illustration.
        const hasPlaceholderVersion = tag.includes(`@${PLACEHOLDER_VERSION}`);
        assert.equal(
          hasPlaceholderVersion,
          isPlaceholderHash,
          `${file}: placeholder version and placeholder hash must appear together, not one without the other:\n${tag}`,
        );
      }
    }
  });
});
