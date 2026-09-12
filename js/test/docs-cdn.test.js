import assert from "node:assert/strict";
import { readFile, readdir } from "node:fs/promises";
import { describe, it } from "node:test";

/**
 * Every <script src> pointing at a CDN must be pinned to a version and carry
 * an integrity hash. Enforced rather than remembered, because both failures
 * are silent: an unpinned URL upgrades a consumer without anyone touching
 * their site, and a tag without `integrity` executes whatever the CDN returns
 * with full access to the page.
 *
 * This is a documentation test on purpose. The risk lives in what we tell
 * people to paste, not in the library's own code.
 */
const CDN_HOSTS = ["cdn.jsdelivr.net", "unpkg.com", "cdnjs.cloudflare.com", "esm.sh"];

async function markdownFiles() {
  const entries = await readdir(".", { withFileTypes: true });
  return entries
    .filter((e) => e.isFile() && e.name.endsWith(".md"))
    .map((e) => e.name);
}

describe("CDN snippets in our documentation", () => {
  it("never show an unpinned version", async () => {
    for (const file of await markdownFiles()) {
      const text = await readFile(file, "utf8");
      for (const host of CDN_HOSTS) {
        // `…/npm/verdict-rules/…` with no @version is the unpinned form.
        const unpinned = new RegExp(`${host.replace(/\./g, "\\.")}/[^\\s"']*?/verdict-rules(?!@)[/\\s"']`);
        assert.equal(
          unpinned.test(text),
          false,
          `${file} has an unpinned ${host} URL — it would silently upgrade consumers`,
        );
      }
    }
  });

  it("always pair a <script src> CDN tag with integrity and crossorigin", async () => {
    for (const file of await markdownFiles()) {
      const text = await readFile(file, "utf8");
      const tags = text.match(/<script[\s\S]*?><\/script>/g) ?? [];
      for (const tag of tags) {
        if (!CDN_HOSTS.some((h) => tag.includes(h))) continue;
        if (tag.includes("type=\"module\"")) continue; // import maps carry no src
        assert.match(
          tag,
          /integrity="sha(256|384|512)-/,
          `${file}: a CDN <script> tag without an integrity hash executes whatever the CDN returns:\n${tag}`,
        );
        assert.match(
          tag,
          /crossorigin="anonymous"/,
          `${file}: SRI on a cross-origin script requires crossorigin="anonymous":\n${tag}`,
        );
      }
    }
  });
});
