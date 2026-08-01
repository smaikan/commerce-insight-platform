# Measurement Workflow

## Evidence hierarchy

1. Field data: CrUX, Search Console, or project RUM.
2. Production trace/network/bundle evidence on a representative route.
3. Repeated Lighthouse/mobile lab measurements.
4. Production build output and import graphs.
5. Static source heuristics.

Do not present a lower tier as proof of a higher-tier outcome.

## Baseline record

Capture:

- URL and route template.
- Build SHA/version.
- Mobile/desktop profile, CPU slowdown, and network profile.
- Auth/data/stock/cart state.
- Cold or warm cache.
- Browser/Lighthouse version.
- Three or more samples when feasible; use median and retain spread.
- Field source, date range, percentile, and sample adequacy.

## Investigation order

1. Confirm the regression and affected route/device.
2. Separate server wait, resource loading, main-thread execution, and layout instability.
3. Identify the element/task/request responsible.
4. Trace it to code, import, API, asset, or third party.
5. Make the smallest causal change.
6. Re-run the same measurement.
7. Check correctness, accessibility, SEO, caching, and other route families.

## Reporting language

- “Measured”: raw evidence exists.
- “Likely”: code/trace supports a hypothesis but field confirmation is absent.
- “Not verified”: required production/runtime/field evidence is unavailable.
- Never guarantee ranking, conversion, or CWV improvement before measurement.
