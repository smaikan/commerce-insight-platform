#!/usr/bin/env node

import fs from "node:fs";
import path from "node:path";

const args = process.argv.slice(2);
const jsonOutput = args.includes("--json");
const rootArg = args.find((arg) => !arg.startsWith("--")) ?? ".";
const root = path.resolve(rootArg);
const srcRoot = fs.existsSync(path.join(root, "src")) ? path.join(root, "src") : root;
const appDir = [path.join(root, "src", "app"), path.join(root, "app")].find(fs.existsSync);
const findings = [];

const add = (severity, rule, file, message) =>
  findings.push({ severity, rule, file: file ? path.relative(root, file) : null, message });

if (!appDir) {
  add("critical", "app-router", null, "No src/app or app directory was found.");
  finish();
  process.exit(process.exitCode ?? 0);
}

const extensions = new Set([".ts", ".tsx", ".js", ".jsx", ".mjs"]);
const files = walk(srcRoot).filter((file) => extensions.has(path.extname(file)));
const rootLayout = [path.join(appDir, "layout.tsx"), path.join(appDir, "layout.js")].find(fs.existsSync);

if (rootLayout && !/next\/font/.test(read(rootLayout))) {
  add("medium", "font-loading", rootLayout, "Root layout has no next/font import; verify font loading and layout stability.");
}

for (const file of files) {
  const content = read(file);
  const relative = path.relative(root, file);
  const base = path.basename(file);
  const isClient = /^\s*["']use client["'];?/m.test(content);
  const isPageOrLayout = /^(page|layout)\.(t|j)sx?$/.test(base);

  if (isClient && isPageOrLayout) {
    add("high", "broad-client-boundary", file, `${relative} is a client page/layout; verify the entire subtree requires browser JavaScript.`);
  } else if (isClient) {
    add("info", "client-component", file, "Client Component found; confirm its boundary is the smallest interactive leaf.");
  }

  if (isClient && /\bfetch\s*\(/.test(content) && /useEffect\s*\(/.test(content)) {
    add("high", "client-fetch-waterfall", file, "Client fetch inside an effect may delay initial content and duplicate server work.");
  }

  if (/<img(?:\s|>)/i.test(content)) {
    add("medium", "raw-image", file, "Raw <img> found; verify optimization, responsive sizing, dimensions, and LCP behavior.");
  }

  if (/<Image\b/.test(content) && /\bfill\b/.test(content) && !/\bsizes\s*=/.test(content)) {
    add("medium", "image-sizes", file, "A fill Image has no visible sizes prop and may download an oversized resource.");
  }

  if (/<script\b[^>]*\bsrc=/i.test(content)) {
    add("high", "raw-third-party-script", file, "Raw external script found; verify next/script strategy, route scope, consent, and main-thread cost.");
  }

  if (/<Script\b/.test(content) && /strategy\s*=\s*["']beforeInteractive["']/.test(content)) {
    add("medium", "blocking-script", file, "beforeInteractive script found; confirm it is required before hydration.");
  }

  if (/dynamic\s*\([\s\S]{0,300}ssr\s*:\s*false/.test(content)) {
    add("medium", "client-only-dynamic", file, "Dynamic import with ssr:false found; verify content is non-critical and browser-only.");
  }

  if (/revalidatePath\s*\(\s*["']\/["']\s*\)/.test(content)) {
    add("medium", "broad-revalidation", file, "Root-wide revalidation found; prefer the narrowest safe tag/path.");
  }

  if (!isClient && /\bfetch\s*\(/.test(content) && !/cache\s*:|revalidate\s*:|next\s*:/.test(content)) {
    add("info", "implicit-fetch-policy", file, "Server fetch found without a visible cache/revalidation option; confirm the default matches data freshness.");
  }

  const stat = fs.statSync(file);
  if (stat.size > 100_000) {
    add("medium", "large-source-file", file, `Source file is ${Math.round(stat.size / 1024)} KiB; inspect generated data or oversized modules.`);
  }
}

const publicDir = path.join(root, "public");
if (fs.existsSync(publicDir)) {
  for (const file of walk(publicDir)) {
    const stat = fs.statSync(file);
    if (stat.size > 500_000) {
      add("medium", "large-public-asset", file, `Public asset is ${Math.round(stat.size / 1024)} KiB; verify compression, dimensions, and route usage.`);
    }
  }
}

finish();

function walk(directory) {
  const result = [];
  for (const entry of fs.readdirSync(directory, { withFileTypes: true })) {
    if (entry.name === "node_modules" || entry.name === ".next" || entry.name.startsWith(".git")) continue;
    const target = path.join(directory, entry.name);
    if (entry.isDirectory()) result.push(...walk(target));
    else result.push(target);
  }
  return result;
}

function read(file) {
  return fs.readFileSync(file, "utf8");
}

function finish() {
  const order = { critical: 0, high: 1, medium: 2, low: 3, info: 4 };
  findings.sort((a, b) => order[a.severity] - order[b.severity] || (a.file ?? "").localeCompare(b.file ?? ""));

  if (jsonOutput) {
    console.log(JSON.stringify({ root, appDir, findings }, null, 2));
  } else if (findings.length === 0) {
    console.log("No static performance leads found. Production runtime and field verification are still required.");
  } else {
    for (const finding of findings) {
      console.log(`[${finding.severity.toUpperCase()}] ${finding.rule}${finding.file ? ` — ${finding.file}` : ""}`);
      console.log(`  ${finding.message}`);
    }
    console.log(`\n${findings.length} static lead(s). Confirm with production bundle/runtime evidence.`);
  }

  process.exitCode = findings.some((finding) => finding.severity === "critical") ? 2 : 0;
}
