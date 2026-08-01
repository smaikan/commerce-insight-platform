#!/usr/bin/env node

import fs from "node:fs";
import path from "node:path";

const args = process.argv.slice(2);
const jsonOutput = args.includes("--json");
const rootArg = args.find((arg) => !arg.startsWith("--")) ?? ".";
const root = path.resolve(rootArg);
const appDir = [path.join(root, "src", "app"), path.join(root, "app")].find(
  (candidate) => fs.existsSync(candidate),
);

const findings = [];
const add = (severity, rule, file, message) =>
  findings.push({ severity, rule, file: file ? path.relative(root, file) : null, message });

if (!appDir) {
  add("critical", "app-router", null, "No src/app or app directory was found.");
  finish();
  process.exit(process.exitCode ?? 0);
}

const sourceExtensions = new Set([".ts", ".tsx", ".js", ".jsx", ".mjs"]);
const files = walk(appDir).filter((file) => sourceExtensions.has(path.extname(file)));
const read = (file) => fs.readFileSync(file, "utf8");
const rootLayout = [path.join(appDir, "layout.tsx"), path.join(appDir, "layout.js")].find(
  fs.existsSync,
);

if (!rootLayout) {
  add("critical", "root-metadata", null, "Root layout was not found.");
} else {
  const content = read(rootLayout);
  checkContains(content, /metadata|generateMetadata/, "high", "root-metadata", rootLayout, "Root layout has no metadata export.");
  checkContains(content, /title\s*:/, "high", "title", rootLayout, "Root metadata has no title.");
  checkContains(content, /description\s*:/, "medium", "description", rootLayout, "Root metadata has no description.");
  checkContains(content, /metadataBase\s*:/, "high", "metadata-base", rootLayout, "Root metadata has no metadataBase.");
  checkContains(content, /openGraph\s*:/, "medium", "open-graph", rootLayout, "Root metadata has no Open Graph defaults.");
  checkContains(content, /lang\s*=\s*["']tr["']/, "low", "document-language", rootLayout, 'Root html is not explicitly lang="tr".');
}

const sitemap = [path.join(appDir, "sitemap.ts"), path.join(appDir, "sitemap.js")].find(fs.existsSync);
const robots = [path.join(appDir, "robots.ts"), path.join(appDir, "robots.js")].find(fs.existsSync);
if (!sitemap) add("high", "sitemap", null, "No sitemap.ts or sitemap.js was found.");
if (!robots) add("high", "robots", null, "No robots.ts or robots.js was found.");

for (const file of files) {
  const base = path.basename(file);
  if (!/^(page|layout)\.(t|j)sx?$/.test(base)) continue;
  const content = read(file);
  const route = routeFromFile(file);
  const privateRoute = /(^|\/)(yonetim|hesabim|sepet|odeme|giris)(\/|$)/i.test(route);
  const dynamicRoute = route.includes("[");
  const productRoute = /(^|\/)(urun|product)(\/|$)/i.test(route);
  const searchRoute = /(^|\/)(arama|search)(\/|$)/i.test(route);

  if (/^\s*["']use client["'];?/m.test(content)) {
    add("medium", "client-boundary", file, `${base} is a Client Component; confirm the whole route boundary requires hydration.`);
  }
  if (/<img(?:\s|>)/i.test(content)) {
    add("medium", "image-optimization", file, "Raw <img> found; verify dimensions, responsive sources, and LCP behavior.");
  }
  if (privateRoute && !hasInheritedNoIndex(file)) {
    add("high", "private-noindex", file, `${route || "/"} appears private but no inherited robots noindex was found.`);
  }
  if (searchRoute && !hasInheritedNoIndex(file)) {
    add("high", "search-noindex", file, `${route} appears to be internal search but no robots noindex was found.`);
  }
  if (base.startsWith("page.") && !privateRoute && !hasInheritedPattern(file, /canonical\s*:/)) {
    add("medium", "canonical", file, `${route || "/"} has no canonical marker in its page/layout chain.`);
  }
  if (base.startsWith("page.") && !privateRoute && /searchParams/.test(content)) {
    add("medium", "query-indexability", file, `${route || "/"} reads searchParams; review canonical and index/noindex behavior for every supported parameter family.`);
  }
  if (dynamicRoute && !privateRoute && base.startsWith("page.") && !/generateMetadata/.test(content)) {
    add("high", "dynamic-metadata", file, `${route} is dynamic and has no local generateMetadata; verify inherited metadata is not duplicated.`);
  }
  if (productRoute && base.startsWith("page.")) {
    if (!/openGraph\s*:/.test(content)) {
      add("medium", "product-open-graph", file, `${route} has no route-specific Open Graph marker.`);
    }
    if (!/ProductGroup|["']Product["']/.test(content)) {
      add("medium", "product-jsonld", file, `${route} has no visible Product/ProductGroup JSON-LD marker.`);
    }
    if (!/BreadcrumbList/.test(content)) {
      add("medium", "breadcrumb-jsonld", file, `${route} has no visible BreadcrumbList JSON-LD marker.`);
    }
  }
}

finish();

function walk(directory) {
  const result = [];
  for (const entry of fs.readdirSync(directory, { withFileTypes: true })) {
    if (entry.name.startsWith(".") || entry.name === "node_modules") continue;
    const target = path.join(directory, entry.name);
    if (entry.isDirectory()) result.push(...walk(target));
    else result.push(target);
  }
  return result;
}

function routeFromFile(file) {
  const directory = path.relative(appDir, path.dirname(file));
  return directory
    .split(path.sep)
    .filter((segment) => segment && !(segment.startsWith("(") && segment.endsWith(")")))
    .join("/");
}

function hasInheritedNoIndex(file) {
  return hasInheritedPattern(
    file,
    /robots\s*:\s*\{[\s\S]*?index\s*:\s*false|noindex/i,
  );
}

function hasInheritedPattern(file, pattern) {
  let directory = path.dirname(file);
  while (directory.startsWith(appDir)) {
    for (const name of ["layout.tsx", "layout.js", "page.tsx", "page.js"]) {
      const candidate = path.join(directory, name);
      if (!fs.existsSync(candidate)) continue;
      const content = read(candidate);
      pattern.lastIndex = 0;
      if (pattern.test(content)) return true;
    }
    if (directory === appDir) break;
    directory = path.dirname(directory);
  }
  return false;
}

function checkContains(content, pattern, severity, rule, file, message) {
  if (!pattern.test(content)) add(severity, rule, file, message);
}

function finish() {
  const order = { critical: 0, high: 1, medium: 2, low: 3 };
  findings.sort((a, b) => order[a.severity] - order[b.severity] || (a.file ?? "").localeCompare(b.file ?? ""));

  if (jsonOutput) {
    console.log(JSON.stringify({ root, appDir, findings }, null, 2));
  } else if (findings.length === 0) {
    console.log("No static SEO leads found. Runtime and field-data verification is still required.");
  } else {
    for (const finding of findings) {
      console.log(`[${finding.severity.toUpperCase()}] ${finding.rule}${finding.file ? ` — ${finding.file}` : ""}`);
      console.log(`  ${finding.message}`);
    }
    console.log(`\n${findings.length} static lead(s). Confirm before reporting as defects.`);
  }

  process.exitCode = findings.some((finding) => finding.severity === "critical") ? 2 : 0;
}
