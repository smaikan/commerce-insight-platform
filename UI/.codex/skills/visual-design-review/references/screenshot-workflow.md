# Screenshot Comparison Workflow

## Prepare

1. Confirm permission to inspect the local/live environment.
2. Identify the target route, reference image, expected data, interaction state, viewport, theme, and device scale.
3. Stabilize fonts, animations, clocks, network data, and consent banners where possible without changing the design under review.
4. Capture a baseline before editing.

Use browser automation for repeatability, but inspect the actual screenshot visually. DOM and CSS inspection cannot replace rendered evidence.

## Compare with a reference

Compare in this order:

1. content and task equivalence;
2. page composition and responsive reflow;
3. hierarchy and reading order;
4. container/grid geometry and alignment;
5. typography roles and wrapping;
6. component density and states;
7. color, border, radius, shadow, and effects;
8. small polish.

Classify each difference as:

- required mismatch: violates the reference or established system;
- intentional adaptation: required by real content, accessibility, platform, or viewport;
- ambiguous: reference lacks enough evidence;
- out of scope: unrelated functionality/content.

Do not copy reference pixels blindly when fonts, data, platform rendering, or accessibility requirements differ.

## Capture states

For commerce pages, capture the states relevant to the change:

- default loaded view;
- long name/high price;
- missing image/out of stock;
- loading/empty/error/disabled;
- menu, filter, dialog, cart drawer, or validation state while open;
- mobile and desktop at minimum.

Use full-page screenshots for overall rhythm and focused crops for component details. Keep filenames stable and identify before/after, route, viewport, and state without personal data.

## Re-evaluate after editing

1. Rebuild or refresh the correct build.
2. Navigate to the identical state and scroll position.
3. Capture at identical viewport and scale.
4. Compare baseline, updated result, and reference side by side.
5. Check whether the fix moved the problem elsewhere.
6. Re-run console/network, responsive, accessibility, and performance checks proportional to the change.

Do not accept a desktop improvement that worsens mobile, or a visually closer match that introduces overflow, low contrast, layout shift, heavier assets, or hidden task information.

## When runtime is unavailable

Review source and static assets, label every visual conclusion as provisional, provide exact routes/states still needed, and do not claim the redesign is visually verified.
