#!/usr/bin/env node

import { readFile, readdir, stat } from "node:fs/promises";
import { extname, relative, resolve } from "node:path";

const args = process.argv.slice(2);
const json = args.includes("--json");
const rootArg = args.find((arg) => !arg.startsWith("--")) ?? ".";
const root = resolve(rootArg);
const sourceRoot = resolve(root, "src");

const extensions = new Set([".css", ".scss", ".sass", ".less", ".js", ".jsx", ".ts", ".tsx"]);
const ignoredDirectories = new Set([".git", ".next", "node_modules", "coverage", "dist", "build", "generated"]);

const checks = [
  { id: "gradient", label: "gradient usage", pattern: /(?:bg-gradient-|linear-gradient\(|radial-gradient\(|conic-gradient\()/i },
  { id: "blur", label: "blur/backdrop filter", pattern: /(?:backdrop-blur(?:-|\b)|\bblur-(?:sm|md|lg|xl|2xl|3xl)|backdrop-filter\s*:|filter\s*:\s*blur\()/i },
  { id: "large-shadow", label: "large shadow/glow", pattern: /(?:shadow-(?:lg|xl|2xl)|drop-shadow-|box-shadow\s*:)/i },
  { id: "large-radius", label: "large/full radius", pattern: /(?:rounded-(?:2xl|3xl|full)|border-radius\s*:\s*(?:999|[2-9]\d)px)/i },
  { id: "large-type", label: "very large type", pattern: /(?:text-(?:5xl|6xl|7xl|8xl|9xl)|font-size\s*:\s*(?:clamp\([^;]*(?:4|5|6|7|8|9)(?:rem|vw)|(?:4|5|6|7|8|9)(?:rem|vw)))/i },
  { id: "large-spacing", label: "large spacing/hero lead", pattern: /(?:\b(?:py|pt|pb|my|mt|mb|gap)-(?:24|28|32|36|40|44|48|52|56|60|64)\b|min-h-screen)/i },
  { id: "animation", label: "animation or broad transition", pattern: /(?:\banimate-(?!none)|\btransition-all\b|animation\s*:)/i },
  { id: "arbitrary-color", label: "arbitrary color value", pattern: /(?:\[(?:#(?:[0-9a-f]{3,8})|(?:rgb|hsl)a?\()|#[0-9a-f]{3,8}\b)/i },
];

async function collect(directory) {
  const entries = await readdir(directory, { withFileTypes: true });
  const files = [];
  for (const entry of entries) {
    if (entry.isDirectory() && ignoredDirectories.has(entry.name)) continue;
    const path = resolve(directory, entry.name);
    if (entry.isDirectory()) files.push(...(await collect(path)));
    else if (entry.isFile() && extensions.has(extname(entry.name))) files.push(path);
  }
  return files;
}

let files;
try {
  if (!(await stat(sourceRoot)).isDirectory()) throw new Error("src is not a directory");
  files = await collect(sourceRoot);
} catch {
  process.stderr.write(`Could not inspect ${sourceRoot}. Pass the application root containing src.\n`);
  process.exitCode = 1;
  process.exit();
}

const findings = [];
for (const file of files) {
  const lines = (await readFile(file, "utf8")).split(/\r?\n/);
  for (let index = 0; index < lines.length; index += 1) {
    const line = lines[index];
    for (const check of checks) {
      if (check.pattern.test(line)) {
        findings.push({
          check: check.id,
          label: check.label,
          file: relative(root, file).replaceAll("\\", "/"),
          line: index + 1,
          excerpt: line.trim().slice(0, 220),
        });
      }
    }
  }
}

const counts = Object.fromEntries(checks.map((check) => [check.id, findings.filter((item) => item.check === check.id).length]));
const result = {
  root,
  filesInspected: files.length,
  leadCount: findings.length,
  counts,
  findings,
  note: "Heuristic leads only. Confirm every item in the rendered interface before reporting a defect.",
};

if (json) {
  process.stdout.write(`${JSON.stringify(result, null, 2)}\n`);
} else {
  process.stdout.write(`Visual source audit: ${files.length} files, ${findings.length} heuristic leads.\n`);
  for (const check of checks) {
    const count = counts[check.id];
    if (count > 0) process.stdout.write(`- ${check.label}: ${count}\n`);
  }
  for (const item of findings) {
    process.stdout.write(`  ${item.file}:${item.line} [${item.check}] ${item.excerpt}\n`);
  }
  process.stdout.write("Treat these as inspection leads; verify them visually.\n");
}
