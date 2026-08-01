#!/usr/bin/env node

import fs from "node:fs";
import path from "node:path";

const reportArg = process.argv.slice(2).find((arg) => !arg.startsWith("--"));
const jsonOutput = process.argv.includes("--json");

if (!reportArg) {
  console.error("Usage: node summarize-lighthouse.mjs <lighthouse-report.json> [--json]");
  process.exit(2);
}

const reportPath = path.resolve(reportArg);
const lhr = JSON.parse(fs.readFileSync(reportPath, "utf8"));
const audits = lhr.audits ?? {};
const performanceScore =
  typeof lhr.categories?.performance?.score === "number"
    ? Math.round(lhr.categories.performance.score * 100)
    : null;

const metricIds = [
  "first-contentful-paint",
  "largest-contentful-paint",
  "interaction-to-next-paint",
  "cumulative-layout-shift",
  "total-blocking-time",
  "speed-index",
  "server-response-time",
];

const metrics = metricIds
  .filter((id) => audits[id])
  .map((id) => ({
    id,
    title: audits[id].title,
    displayValue: audits[id].displayValue ?? null,
    numericValue: audits[id].numericValue ?? null,
    score: audits[id].score ?? null,
  }));

const opportunities = Object.values(audits)
  .filter((audit) => {
    const savingsMs = audit.details?.overallSavingsMs ?? 0;
    const savingsBytes = audit.details?.overallSavingsBytes ?? 0;
    return audit.scoreDisplayMode !== "notApplicable" && (savingsMs > 0 || savingsBytes > 0);
  })
  .map((audit) => ({
    id: audit.id,
    title: audit.title,
    displayValue: audit.displayValue ?? null,
    savingsMs: Math.round(audit.details?.overallSavingsMs ?? 0),
    savingsBytes: Math.round(audit.details?.overallSavingsBytes ?? 0),
  }))
  .sort((a, b) => b.savingsMs - a.savingsMs || b.savingsBytes - a.savingsBytes)
  .slice(0, 10);

const summary = {
  source: reportPath,
  requestedUrl: lhr.requestedUrl ?? null,
  finalUrl: lhr.finalDisplayedUrl ?? lhr.finalUrl ?? null,
  fetchTime: lhr.fetchTime ?? null,
  lighthouseVersion: lhr.lighthouseVersion ?? null,
  performanceScore,
  metrics,
  opportunities,
  caveat: "Lighthouse is lab evidence and does not establish field Core Web Vitals pass/fail.",
};

if (jsonOutput) {
  console.log(JSON.stringify(summary, null, 2));
} else {
  console.log(`Performance score: ${performanceScore ?? "n/a"}`);
  for (const metric of metrics) {
    console.log(`- ${metric.title}: ${metric.displayValue ?? metric.numericValue ?? "n/a"} (score ${formatScore(metric.score)})`);
  }
  if (opportunities.length) {
    console.log("\nTop opportunities:");
    for (const item of opportunities) {
      const savings = [
        item.savingsMs ? `${item.savingsMs} ms` : null,
        item.savingsBytes ? `${Math.round(item.savingsBytes / 1024)} KiB` : null,
      ].filter(Boolean).join(", ");
      console.log(`- ${item.title}: ${item.displayValue ?? (savings || "review audit")}`);
    }
  }
  console.log("\nLab evidence only; verify field CWV separately.");
}

function formatScore(score) {
  return typeof score === "number" ? Math.round(score * 100) : "n/a";
}
