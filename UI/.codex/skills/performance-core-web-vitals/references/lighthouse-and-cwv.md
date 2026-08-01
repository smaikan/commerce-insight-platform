# Lighthouse and Core Web Vitals

## Field CWV

At the 75th percentile, segmented by mobile and desktop:

- LCP good: <= 2.5 s.
- INP good: <= 200 ms.
- CLS good: <= 0.1.

Use field data for pass/fail. CrUX is aggregated real-user data; project RUM can provide route/release detail.

## Lighthouse

- Lighthouse is a lab test with simulated conditions.
- Its performance score is weighted and can vary between runs.
- LCP and CLS are lab observations; Total Blocking Time is a diagnostic proxy, not field INP.
- Run production code, repeat samples, and compare medians under identical conditions.
- Keep JSON reports for auditability.
- Treat opportunities as hypotheses; validate the import/request/element before editing.

## Metric diagnosis

### LCP

- Determine whether delay comes from TTFB, resource discovery, resource load, or render delay.
- Inspect the actual LCP element and its request priority/size.
- Check server waterfalls, fonts, CSS, hydration, and client-only content.

### INP

- Use field attribution or interaction traces.
- Find long event handlers, render work, layout, and third-party tasks.
- Test filters, variant selection, add-to-cart, menus, forms, and checkout.

### CLS

- Identify individual layout-shift entries.
- Check missing dimensions, late banners, fonts, async recommendations, validation messages, and consent UI.
- Exclude expected shifts during active user input only when the measurement definition does.

## Result review

Report raw value, score/category, route, environment, and evidence type. Never convert one Lighthouse score into a Core Web Vitals guarantee.
