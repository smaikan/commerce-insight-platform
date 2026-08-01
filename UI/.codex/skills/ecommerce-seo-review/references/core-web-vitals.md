# Core Web Vitals

## Passing field thresholds

Evaluate the 75th percentile separately for mobile and desktop:

- LCP: good at or below 2.5 seconds.
- INP: good at or below 200 milliseconds.
- CLS: good at or below 0.1.

Prefer CrUX, Search Console, or project RUM. Lighthouse and local traces diagnose problems but do not prove real-user pass/fail.

## LCP review

- Identify the actual LCP element per representative template.
- Avoid lazy-loading the above-the-fold LCP image.
- Use `next/image`, correct `sizes`, stable intrinsic dimensions, and responsive source selection.
- Preload/eager-load only the true critical candidate.
- Reduce server delay, client waterfalls, render-blocking CSS/fonts, and oversized assets.
- Cache public catalog data/assets according to freshness needs.

## INP review

- Minimize Client Component scope and shipped JavaScript.
- Break long tasks and avoid expensive synchronous handlers.
- Avoid broad state updates and contexts that rerender large product grids.
- Defer analytics, chat, review widgets, and personalization.
- Use Server Actions/Server Components where interaction does not require client ownership.
- Test filters, variant selection, add-to-cart, menus, and checkout interactions.

## CLS review

- Reserve dimensions/aspect ratio for product images and banners.
- Reserve space for async recommendations, stock messages, reviews, and consent UI.
- Avoid inserting content above existing content after render.
- Use stable font loading and fallback metrics.
- Prefer transform/opacity animations over layout-changing properties.

## Measurement

- Add a tiny client boundary using `useReportWebVitals` only when a real analytics destination exists.
- Segment by route template, device class, geography, and release.
- Compare before/after field data; do not optimize against one Lighthouse run.
- Record evidence source, time range, sample size when available, and whether the value is lab or field data.
