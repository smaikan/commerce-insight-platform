---
name: visual-design-review
description: Audit, compare, report, and improve the visual design of production web interfaces, especially Next.js e-commerce pages. Use for visual hierarchy, spacing, alignment, typography, color, contrast, radius, shadow, density, responsive consistency, loading/empty/error/disabled states, reference-screenshot comparison, realistic product-data stress tests, generic AI-design symptoms, excessive gradients/blur/glow/glassmorphism/cards/animation, design-system drift, and screenshot-based before/after review without harming accessibility or performance.
---

# Visual Design Review

Improve the interface from evidence. Report issues before editing, preserve intentional brand character, and prefer a coherent product interface over decorative novelty.

## Ground the review

1. Locate the application root and inspect the package, framework version, routes, layouts, global CSS, theme/tokens, fonts, shared UI primitives, and existing screenshots or design references.
2. Read [visual-language.md](references/visual-language.md) before judging hierarchy, effects, cards, typography, color, radius, shadow, or design-system consistency.
3. Read [responsive-and-states.md](references/responsive-and-states.md) before reviewing mobile layouts, component density, commerce edge cases, or UI states.
4. Read [screenshot-workflow.md](references/screenshot-workflow.md) when a live page or reference image is available.
5. Read [reporting.md](references/reporting.md) before presenting findings or making changes.
6. If source exists, run the heuristic inventory:

   ```powershell
   node .codex/skills/visual-design-review/scripts/visual-source-audit.mjs .
   ```

7. Treat static matches as inspection leads, never automatic defects. Confirm each finding in the rendered interface.

If no reference design exists, use the product's own established system and neighboring production screens as the baseline. Do not invent a fashionable replacement style.

## Use available MCP tools

- Use **Chrome DevTools MCP** (`chrome_devtools`) to open the live page, emulate exact viewports/color schemes, inspect layout and accessibility snapshots, capture screenshots, and check console/network/performance regressions.
- Use **Playwright MCP** (`playwright`) to reproduce deterministic UI states and capture matching before/after screenshots across desktop and mobile. Keep the same fixture, viewport, scale, theme, and interaction state.
- Use **Next DevTools MCP** (`next_devtools`) when the target is Next.js to discover the correct running route, diagnose runtime/rendering errors, and verify framework-specific behavior before attributing a visual issue to CSS.
- Inspect screenshots visually; MCP DOM/snapshot output alone does not prove visual quality. If an MCP is unavailable, use equivalent browser automation or manual capture and state the unverified portion.

## Establish comparable evidence

- Use production-like content and a stable application build.
- Capture the same route, data, UI state, viewport, scale, color scheme, and scroll position before and after changes.
- Review at least one representative desktop and one narrow mobile viewport. Add tablet or dense admin widths when the component changes behavior there.
- Capture important states, not only the ideal loaded state: loading, empty, error, disabled, long content, missing image, high price, and out of stock.
- Record browser, viewport, device scale, build identifier, data fixture, and reference source.
- Do not call an implementation matched when only one viewport or state was inspected.

## Report before changing

Group observations by root cause and rank them by user impact:

1. blocked or confusing task;
2. broken hierarchy/responsive layout;
3. inconsistent system or component behavior;
4. unnecessary decoration and density problems;
5. low-impact polish.

For each finding, provide visible evidence, affected state/viewport, the relevant design-system rule, and the smallest coherent correction. Separate objective defects from subjective preferences and reference ambiguities.

When the request is review-only, stop after the report. Do not mutate application files.

## Remove generic AI-design symptoms

- Require a functional or brand reason for every gradient, glow, blur, translucent layer, large shadow, and animation.
- Remove decorative effects that compete with content, reduce contrast, increase GPU work, or appear on every section.
- Do not wrap every content block in a card. Use grouping, headings, dividers, whitespace, and page structure where a surface adds no meaning.
- Standardize surface roles, radius levels, borders, shadows, spacing, and component heights instead of styling each section independently.
- Avoid a badge or colored eyebrow above every heading.
- Reduce oversized hero text and empty vertical space when they delay products, filters, forms, totals, or primary actions.
- Remove fake statistics, invented social proof, vague slogans, and filler copy. Use verified product information or omit the block.
- Do not turn catalog, account, cart, checkout, or admin screens into disconnected landing-page sections.
- Limit emphasis colors. Establish one primary action hierarchy and reserve semantic colors for actual states.
- Customize third-party primitives to the product's tokens, density, language, and state behavior; do not ship untouched component-library defaults.
- Add animation only when it explains change, orientation, or causality; respect reduced motion.

Do not flatten a distinctive brand into a monochrome template. Simplify repeated decoration while retaining meaningful identity.

## Correct hierarchy and density

- Make the page's purpose and primary action clear at first scan.
- Use a restrained heading scale with one page-level heading and logical section levels.
- Keep line length, weight, leading, and contrast appropriate to the content role.
- Align content to a consistent container and grid; correct near-miss edges, baselines, and control heights.
- Use a small spacing scale consistently. Remove both unexplained gaps and cramped clusters.
- Size product cards, tables, filters, forms, media, and controls for realistic content and task frequency.
- Favor information density in operational/accounting screens and calm scanability in storefront pages; do not apply one density everywhere.
- Keep critical commerce information visible: product name, price, variant, stock, quantity, total, validation, and primary action.

## Improve real states

- Make loading states structurally resemble final content and reserve dimensions to prevent layout shift.
- Use restrained skeletons; do not pulse every surface or hide fast responses behind theatrical loading.
- Make empty states specific, concise, and actionable only when a real next action exists.
- Make errors explain what failed, preserve safe input, and offer a valid recovery action.
- Make disabled states visibly distinct yet readable; communicate why an action is unavailable when it is not obvious.
- Distinguish unavailable, out-of-stock, price-changed, validation, permission, network, and unexpected-error states.
- Avoid fake placeholder products or invented metrics in production-facing empty states.

## Preserve accessibility and performance

- Preserve semantic structure, focus order, visible focus, labels, target sizes, and contrast while simplifying visuals.
- Never remove necessary text or status cues only to create a cleaner screenshot.
- Prefer CSS and existing assets over decorative JavaScript, canvas, autoplay video, or new runtime dependencies.
- Avoid large-area `backdrop-filter`, repeated layered shadows, continuous animation, and oversized off-screen effects.
- Keep image dimensions/aspect ratios stable; use the framework image pipeline and accurate responsive sizing where appropriate.
- Do not increase font families, weights, or icon sets without a demonstrated need.
- Check LCP, CLS, interaction cost, bundle impact, and reduced-motion behavior when changes affect above-the-fold media, fonts, scripts, or animation.
- Use the local `performance-core-web-vitals` and `testing-accessibility` skills for deeper verification when those concerns are materially affected.

## Iterate after changes

1. Fix one coherent visual system or component cluster at a time.
2. Render the changed page with the same fixture and viewports.
3. Capture new screenshots and compare them to both the baseline and reference.
4. Re-run the source inventory and relevant functional/accessibility checks.
5. Inspect desktop and mobile for new wrapping, overflow, clipping, density, or hierarchy regressions.
6. Re-test real-data and non-happy states.
7. Report resolved findings, intentional deviations, remaining risks, and evidence paths.

Do not claim improvement from code inspection alone. Finish only after visually re-evaluating the rendered result, unless runtime access is blocked; then state exactly what remains unverified.
