#!/usr/bin/env node
/**
 * Self-contained Mermaid diagram validator + exporter.
 *
 * Ground-truth validation: shells out to @mermaid-js/mermaid-cli (mmdc), which runs
 * the real Mermaid.js parser inside headless Chromium via Puppeteer. This catches
 * genuine parse errors and rendering crashes, not just heuristic checks like bracket
 * counting — those miss real failures (e.g. malformed shape syntax, bad subgraph
 * nesting, invalid linkStyle indexes) and flag false positives on valid-but-unusual
 * syntax (e.g. the asymmetric/flag shape `id>"text"]`, which intentionally has
 * unbalanced brackets).
 *
 * No project setup required: run via `node validate_diagrams.js ...` or
 * `npx -y @mermaid-js/mermaid-cli` is invoked on demand per diagram (npx caches it
 * after the first run). Requires Node.js and network access on first use.
 *
 * Usage:
 *   node validate_diagrams.js --markdown path/to/doc.md [--format svg|png|pdf] [--keep-output DIR]
 *   node validate_diagrams.js --file path/to/diagram.mmd [--format svg|png|pdf] [--keep-output DIR]
 *
 * Exit code: 0 if every diagram found renders cleanly, 1 if any diagram fails or no
 * diagrams were found where at least one was expected. Designed to be checked by an
 * agent (`$?`) rather than parsed from stdout, though stdout also prints a summary.
 */

'use strict';

const fs = require('fs');
const os = require('os');
const path = require('path');
const { spawnSync } = require('child_process');

const RENDER_TIMEOUT_MS = 60_000;

function parseArgs(argv) {
  const args = { format: 'svg', keepOutput: null, markdown: null, file: null };
  for (let i = 0; i < argv.length; i++) {
    const a = argv[i];
    if (a === '--markdown') args.markdown = argv[++i];
    else if (a === '--file') args.file = argv[++i];
    else if (a === '--format') args.format = argv[++i];
    else if (a === '--keep-output') args.keepOutput = argv[++i];
    else if (a === '--help' || a === '-h') args.help = true;
    else {
      console.error(`Unknown argument: ${a}`);
      process.exit(2);
    }
  }
  return args;
}

function printHelp() {
  console.log(`
Mermaid diagram validator + exporter (wraps @mermaid-js/mermaid-cli)

Usage:
  node validate_diagrams.js --markdown <doc.md> [--format svg|png|pdf] [--keep-output <dir>]
  node validate_diagrams.js --file <diagram.mmd> [--format svg|png|pdf] [--keep-output <dir>]

Options:
  --markdown <path>    Extract and validate every \`\`\`mermaid code block in a markdown file
  --file <path>        Validate a single standalone .mmd file
  --format <fmt>       Output format: svg (default, fastest), png, or pdf
  --keep-output <dir>  Persist rendered files here instead of discarding them (created if missing)
  --help               Show this message

Exit code 0 = every diagram rendered cleanly. Exit code 1 = at least one diagram failed,
or no diagrams were found in the input.
`);
}

// Extracts fenced \`\`\`mermaid blocks from markdown, tracking source line numbers
// (1-indexed, pointing at the line the fence opens on) so failures can be traced
// back to the original document.
function extractMermaidBlocks(markdownText) {
  const lines = markdownText.split('\n');
  const blocks = [];
  let inBlock = false;
  let current = [];
  let startLine = -1;

  for (let i = 0; i < lines.length; i++) {
    const line = lines[i];
    if (!inBlock && /^\s*```mermaid\s*$/.test(line)) {
      inBlock = true;
      current = [];
      startLine = i + 1; // 1-indexed line of the fence itself
      continue;
    }
    if (inBlock && /^\s*```\s*$/.test(line)) {
      inBlock = false;
      blocks.push({ code: current.join('\n'), startLine });
      continue;
    }
    if (inBlock) current.push(line);
  }

  if (inBlock) {
    console.error(
      `WARNING: markdown file has an unterminated \`\`\`mermaid fence starting at line ${startLine} — ` +
        `treating rest of file as part of that block.`
    );
    blocks.push({ code: current.join('\n'), startLine });
  }

  return blocks;
}

function renderOne(code, outFile) {
  const tmpDir = fs.mkdtempSync(path.join(os.tmpdir(), 'mmd-validate-'));
  const inFile = path.join(tmpDir, 'diagram.mmd');
  fs.writeFileSync(inFile, code, 'utf8');

  // --no-sandbox is required for headless Chromium in many CI/container environments
  // where the default sandbox can't initialize (no setuid helper, restricted seccomp,
  // running as root, etc). Harmless to include locally.
  const puppeteerConfigPath = path.join(tmpDir, 'puppeteer-config.json');
  fs.writeFileSync(
    puppeteerConfigPath,
    JSON.stringify({ args: ['--no-sandbox', '--disable-setuid-sandbox'] }),
    'utf8'
  );

  const result = spawnSync(
    'npx',
    ['-y', '@mermaid-js/mermaid-cli', '-i', inFile, '-o', outFile, '-p', puppeteerConfigPath],
    { encoding: 'utf8', timeout: RENDER_TIMEOUT_MS }
  );

  const ok = result.status === 0 && fs.existsSync(outFile);
  const combinedOutput = `${result.stdout || ''}${result.stderr || ''}`;

  // Best-effort tmp cleanup; never let cleanup failure mask the real result.
  try {
    fs.rmSync(tmpDir, { recursive: true, force: true });
  } catch (_) {
    /* ignore */
  }

  if (result.error && result.error.code === 'ETIMEDOUT') {
    return { ok: false, error: `Render timed out after ${RENDER_TIMEOUT_MS}ms (likely a hung/crashed headless Chromium instance, not a syntax error).` };
  }

  return { ok, error: ok ? null : stripAnsi(combinedOutput).trim() || 'mermaid-cli exited non-zero with no output.' };
}

function stripAnsi(str) {
  // eslint-disable-next-line no-control-regex
  return str.replace(/\x1b\[[0-9;]*m/g, '');
}

function main() {
  const args = parseArgs(process.argv.slice(2));
  if (args.help || (!args.markdown && !args.file)) {
    printHelp();
    process.exit(args.help ? 0 : 2);
  }

  let items; // [{ code, label }]
  if (args.file) {
    if (!fs.existsSync(args.file)) {
      console.error(`File not found: ${args.file}`);
      process.exit(1);
    }
    items = [{ code: fs.readFileSync(args.file, 'utf8'), label: args.file }];
  } else {
    if (!fs.existsSync(args.markdown)) {
      console.error(`File not found: ${args.markdown}`);
      process.exit(1);
    }
    const md = fs.readFileSync(args.markdown, 'utf8');
    const blocks = extractMermaidBlocks(md);
    if (blocks.length === 0) {
      console.error(`No \`\`\`mermaid code blocks found in ${args.markdown}`);
      process.exit(1);
    }
    items = blocks.map((b, idx) => ({
      code: b.code,
      label: `${args.markdown}:${b.startLine} (block ${idx + 1}/${blocks.length})`,
    }));
  }

  if (args.keepOutput) {
    fs.mkdirSync(args.keepOutput, { recursive: true });
  }

  console.log(`Validating ${items.length} diagram(s) via @mermaid-js/mermaid-cli (format: ${args.format})...\n`);

  let failures = 0;
  const results = [];

  items.forEach((item, idx) => {
    const outName = `diagram-${idx + 1}.${args.format}`;
    const outFile = args.keepOutput
      ? path.join(args.keepOutput, outName)
      : path.join(fs.mkdtempSync(path.join(os.tmpdir(), 'mmd-out-')), outName);

    process.stdout.write(`[${idx + 1}/${items.length}] ${item.label} ... `);
    const { ok, error } = renderOne(item.code, outFile);

    if (ok) {
      console.log('PASS');
      results.push({ label: item.label, ok: true, outFile: args.keepOutput ? outFile : null });
    } else {
      console.log('FAIL');
      failures++;
      results.push({ label: item.label, ok: false, error });
    }
  });

  console.log('\n--- Summary ---');
  for (const r of results) {
    if (r.ok) {
      console.log(`  PASS  ${r.label}${r.outFile ? `  -> ${r.outFile}` : ''}`);
    } else {
      console.log(`  FAIL  ${r.label}`);
      const indented = r.error
        .split('\n')
        .slice(0, 15) // keep it readable — full trace rarely needed to fix a syntax error
        .map((l) => `          ${l}`)
        .join('\n');
      console.log(indented);
    }
  }

  console.log(`\n${items.length - failures}/${items.length} diagram(s) passed.`);
  process.exit(failures > 0 ? 1 : 0);
}

main();
