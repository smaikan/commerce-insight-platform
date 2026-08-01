---
name: performance-core-web-vitals
description: Diagnose, review, plan, measure, or improve performance in Next.js App Router applications. Use for unnecessary JavaScript and Client Components, image/font/script optimization, route bundles and dynamic imports, cache/data-fetching performance, rendering waterfalls, Lighthouse reports, and field Core Web Vitals including LCP, INP, and CLS.
---

# Performance and Core Web Vitals

Optimize from evidence. Do not trade correctness, accessibility, SEO, or freshness for a synthetic score.

## Establish the baseline

1. Locate the Next.js project root and inspect `package.json`, lockfile, Next config, route structure, providers, API layer, and environment configuration.
2. Read [measurement-workflow.md](references/measurement-workflow.md).
3. Run the source inventory:

   ```powershell
   node .codex/skills/performance-core-web-vitals/scripts/performance-static-audit.mjs .
   ```

4. Inspect representative route families separately: home, catalog/category, product detail, cart/checkout, account, and admin.
5. Use a production build/server for runtime measurements. Never use `next dev` as the performance baseline.
6. Record device/network profile, route, authentication state, data state, cache state, build SHA, and whether each number is lab or field data.
7. Verify version-specific behavior against current official Next.js documentation before recommending an API or configuration.

Treat static findings as leads. Confirm them through import chains, production bundles, performance traces, rendered output, or network timing.

## Use available MCP tools

- Use **Chrome DevTools MCP** (`chrome_devtools`) as the primary lab diagnostic surface for performance traces, LCP/CLS/interaction insights, network waterfalls, CPU work, console issues, screenshots, and device/network emulation. Use its performance trace tools for performance; do not substitute a non-performance Lighthouse category audit.
- Use **Next DevTools MCP** (`next_devtools`) to inspect the running Next.js application, route/runtime errors, framework state, and version-matched optimization guidance before changing Next.js behavior.
- Use **Playwright MCP** (`playwright`) to reproduce authenticated/stateful journeys and stabilize navigation/data states before measurement. Do not use Playwright timing as Core Web Vitals field evidence.
- Record tool, viewport, throttling, build, route, state, and sample count. If an MCP is unavailable, use the equivalent local Chrome/Lighthouse/Next.js tooling and report the limitation.

## Reduce JavaScript and Client Components

Read [nextjs-runtime-optimization.md](references/nextjs-runtime-optimization.md).

- Keep pages and layouts as Server Components by default.
- Move `"use client"` to the smallest interactive leaf.
- Do not make a page client-side to use `next/image`, render static markup, format server-known values, or fetch initial data.
- Pass minimal serializable props across the server/client boundary.
- Replace client fetch-on-mount waterfalls with server fetching where personalization and freshness allow.
- Avoid global providers for route-local state.
- Lazy-load genuinely non-critical interactive widgets; do not hide critical above-the-fold content behind client-only dynamic imports.
- Inspect the actual import chain before blaming a package.
- Remove dependencies only after confirming no consumer and measuring the bundle change.

Use built-in route/client bundle analysis supported by the installed Next.js version. Inspect client and server graphs separately and identify which boundary pulled a dependency into the browser.

## Optimize images, fonts, and scripts

- Use `next/image` for product/content images unless a documented exception applies.
- Set intrinsic dimensions or a stable aspect ratio and a correct responsive `sizes` value.
- Identify the real LCP element from a trace. Eager/high-priority load only the actual above-the-fold candidate.
- Avoid eager-loading every product-grid image.
- Serve appropriately sized modern formats through the image pipeline/CDN.
- Use `next/font` or an equivalent self-hosted strategy with stable fallbacks and only required weights/subsets.
- Scope third-party scripts to routes that need them.
- Use `next/script` with the least blocking valid strategy; reserve `beforeInteractive` for truly critical scripts.
- Defer chat, reviews, analytics extras, heatmaps, and personalization when they are not required for first interaction.
- Measure third-party CPU, transfer, and main-thread cost before and after changes.

## Improve cache and data fetching

Read [cache-and-data-fetching.md](references/cache-and-data-fetching.md).

- Choose freshness per data class; never add caching only to improve a score.
- Cache public catalog data with explicit revalidation/tags when business freshness permits.
- Keep session, cart, checkout, account, admin, and accounting data private and non-shared.
- Start independent requests together and await them with `Promise.all`.
- Use nested Server Components and Suspense to stream independent slow sections.
- Deduplicate identical work within a render/request.
- Avoid fetching the same API entity separately in metadata, page, and child components without memoization.
- Invalidate the narrowest tag/path after mutations.
- Inspect backend/API TTFB before attempting browser-side workarounds.

## Measure Lighthouse correctly

Read [lighthouse-and-cwv.md](references/lighthouse-and-cwv.md).

1. Build and run the production application.
2. Measure a stable, representative URL with a documented mobile profile.
3. Run multiple samples and compare medians; retain raw JSON.
4. Summarize a Lighthouse JSON report:

   ```powershell
   node .codex/skills/performance-core-web-vitals/scripts/summarize-lighthouse.mjs path/to/report.json
   ```

5. Use Lighthouse opportunities and diagnostics to locate work, not as automatic change instructions.
6. Compare before/after using the same environment.
7. Do not claim Core Web Vitals pass/fail from Lighthouse alone.

Authenticated or stateful routes require a repeatable test account/session and deterministic fixtures. Do not expose credentials in reports or scripts.

## Evaluate field Core Web Vitals

- Prefer CrUX, Search Console, or RUM at the 75th percentile, segmented by route template and device class.
- Evaluate LCP at or below 2.5 seconds, INP at or below 200 milliseconds, and CLS at or below 0.1 as “good”.
- Use a tiny dedicated Client Component for `useReportWebVitals` only when a real telemetry destination exists.
- Record sample size and date range. Mark insufficient or unavailable field data as “not verified”.
- Diagnose LCP, INP, and CLS independently; a single aggregate score must not hide a failing metric.

## Prioritize and fix

Order work by:

1. User-visible field regression on high-traffic/revenue routes.
2. Large LCP/INP/CLS issue with trace evidence.
3. Server/API latency or data waterfall.
4. Route-level JavaScript/bundle cost.
5. Images, fonts, and third-party scripts.
6. Low-impact polish.

For each finding provide:

- severity and affected route/device;
- field or lab evidence;
- source/import/network evidence;
- likely root cause;
- smallest safe change;
- expected metric affected;
- validation plan and rollback risk.

Change one causal cluster at a time when practical. Rebuild and remeasure after each meaningful change. Reject optimizations that make data stale, break authentication, remove accessible content, or shift work from the browser to an overloaded server without improving the user experience.

When asked only to diagnose or review, do not edit files. When asked to optimize, preserve project architecture, add no dependency without evidence, run focused tests and a production build, and report before/after measurements separately.
