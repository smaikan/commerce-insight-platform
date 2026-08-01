---
name: admin-dashboard-design
description: Design, implement, refactor, or review a compact, data-focused, desktop-first e-commerce administration interface for this Next.js project. Use for the admin shell, sidebar, topbar, dashboard metrics, product/order/inventory/customer/accounting tables, filtering/search/pagination/bulk actions, product and variant forms, order lifecycle screens, dialogs/drawers/confirmations, responsive admin behavior, professional UI states, restrained blue theming, and adapting the repository's local Shopify admin references without copying unsupported features or proprietary branding.
---

# Admin Dashboard Design

Build a fast operational workspace: compact, legible, predictable, and low-decoration. Use Shopify's information density and workflow clarity as reference, not as a template to copy.

## Ground the work

1. Locate the frontend root and inspect the installed framework, route tree, admin modules, global styles, shared primitives, tokens, icon set, and existing screenshots.
2. Read [shopify-reference-analysis.md](references/shopify-reference-analysis.md) before using the local files under `docs/references/admin-ui/shopify`.
3. Read [project-admin-contracts.md](references/project-admin-contracts.md) and the relevant OpenAPI/Markdown contracts before adding navigation, columns, filters, metrics, forms, or actions.
4. Read [admin-visual-system.md](references/admin-visual-system.md) before choosing color, density, type, spacing, radius, shadow, or component sizes.
5. Read [shell-and-navigation.md](references/shell-and-navigation.md) for layout, sidebar, topbar, route groups, and responsive behavior.
6. Read [tables-and-operations.md](references/tables-and-operations.md) for lists, filtering, pagination, selection, and bulk actions.
7. Read [forms-details-and-overlays.md](references/forms-details-and-overlays.md) for product/variant forms, order details, dialogs, drawers, and confirmations.
8. Use the local `visual-design-review` skill to report existing issues, capture a baseline, and compare screenshots after implementation.
9. Use the shared `ecommerce-design-system` when it exists. Until then, define provisional semantic tokens in one place and avoid spreading literal values.

Do not invent dashboard statistics, workflow states, filters, columns, permissions, bulk operations, or settings absent from the API. Report reference/API conflicts explicitly.

## Use available MCP tools

- Use **Next DevTools MCP** (`next_devtools`) to find the running admin routes, inspect App Router/runtime errors, and verify Next.js-specific layout and rendering behavior.
- Use **Chrome DevTools MCP** (`chrome_devtools`) to inspect the rendered shell, table/form density, console/network failures, responsive breakpoints, accessibility tree, screenshots, and performance impact.
- Use **Playwright MCP** (`playwright`) to reproduce sidebar, filters, selection, forms, dialogs/drawers, lifecycle confirmations, loading/error states, and identical before/after screenshots across desktop and narrow layouts.
- Use production-like fixtures and redact customer, order, payment, cookie, and token data from MCP output. Convert critical behavior into committed tests and fall back to equivalent local tooling if an MCP is unavailable.

## Follow the design workflow

1. Classify the screen as shell, dashboard, list, detail, create/edit form, report, or short overlay task.
2. Identify the operator's primary task, decision frequency, data density, authorization, lifecycle, error, concurrency, and idempotency risks.
3. Map every visible action and filter to a documented API capability.
4. Choose the smallest reference pattern that supports the task.
5. Define the hierarchy, responsive transitions, keyboard path, loading/empty/error/disabled states, and realistic stress data before polishing.
6. Implement shared primitives or admin compositions instead of duplicating page-specific variants.
7. Render representative desktop and mobile/tablet states, capture screenshots, and review them against the baseline and references.
8. Run accessibility, functional, and performance checks proportional to the change.

When asked only to review or plan, report findings and decisions without editing application code.

## Preserve the admin character

- Prioritize compact density, fast scanning, and predictable placement.
- Use neutral page/surface colors with restrained blue for primary actions, active navigation, links, selected rows, and focus.
- Do not use blue gradients, glowing controls, glassmorphism, tinted cards everywhere, or a fully saturated blue shell.
- Use borders and grouping before shadows. Reserve elevation for menus, popovers, drawers, and dialogs.
- Keep headings restrained; admin pages are work surfaces, not landing pages.
- Avoid large hero areas, decorative charts, fake trends, motivational copy, and animation without an operational purpose.
- Use semantic status colors only for real statuses; do not make every label a badge.
- Show verified values with their time period, scope, unit, and loading/error state.
- Keep the storefront brand recognizable through typography, icon language, and accent color while giving admin screens higher density.

## Build a disciplined shell

- Use `/admin` with English, lowercase `kebab-case` route segments.
- Keep a stable desktop sidebar and compact topbar; let content use the remaining width rather than forcing a marketing-style max-width.
- Group navigation by operator task and expose at most two visible hierarchy levels.
- Highlight the current item with a restrained blue cue and sufficient text contrast.
- Put account/session and rare settings away from primary operational navigation.
- Show a global search box only when a real cross-module search exists. Otherwise implement a clearly labeled navigation command or route-local search.
- Make the sidebar a drawer below the desktop breakpoint and restore focus when it closes.
- Keep the layout a Server Component; isolate collapse, drawer, command, selection, and overlay behavior in narrow Client Components.

## Design dashboards honestly

- Prefer three to five compact metrics only when backed by authoritative endpoints or report totals.
- Label the period and comparison basis; omit percentage deltas when no prior-period contract exists.
- Never sum the current page and present it as a global total.
- Use operational queues, recent records, or quick actions when they are more useful than metrics.
- Keep charts secondary to actionable lists. Add a chart only when the underlying series and decision it supports are documented.
- Provide unavailable and partial-data states instead of zeroing failed metrics.

## Design tables for operations

- Combine view/tab controls, search, supported filters, and column controls in one compact toolbar associated with the table.
- Put filters, sorting, and pagination in URL search parameters.
- Use one primary table surface, consistent row heights, aligned numeric columns, clear status, and restrained row actions.
- Show checkboxes only when at least one real multi-record action exists.
- Reveal the bulk-action bar after selection and state the exact selected scope.
- Do not simulate an atomic bulk action with undocumented client loops.
- Preserve filter state after row detail/edit navigation and reset pagination when filters change.
- Use sticky headers or pinned identifiers only when they materially improve long-table work.
- At narrow widths, preserve critical data through horizontal scrolling or an explicit priority layout; do not silently drop values/actions.

## Design forms and details around workflows

- Use a two-column desktop layout for complex entities: editable primary content plus a narrower context/status rail. Stack the rail in task order at smaller widths.
- Group fields by operator decision, not DTO shape. Use progressive disclosure for infrequent advanced fields.
- Keep labels persistent, help text concise, validation adjacent, and save state explicit.
- Protect unsaved changes and preserve valid input after server errors.
- Use page-level forms for products, variants, orders, accounting documents, and other long workflows.
- Use drawers for contextual preview or a short secondary edit; use dialogs for bounded decisions; use confirmations for consequential actions.
- Name the resource and consequence in destructive/lifecycle confirmations. Never use a generic “Are you sure?” alone.
- Re-read authoritative data after status, post, cancel, reverse, stock, role, or payment mutations.
- On `409`, show changed data and require a fresh user decision; never silently overwrite.

## Keep components coherent

- Build primitives such as Button, Input, Select, Checkbox, Badge, Dialog, Drawer, Tooltip, and Menu in the shared design-system layer.
- Keep admin compositions such as `AdminShell`, `AdminSidebar`, `PageHeader`, `FilterBar`, `DataTable`, `BulkActionBar`, `MetricCard`, `StatusBadge`, `DetailRail`, and `SaveBar` in the admin shell/shared admin boundary.
- Keep product, order, stock, customer, return, coupon, and accounting-specific components in their owning modules.
- Do not ship raw component-library defaults; map them to shared tokens, density, states, and Turkish UI copy.
- Avoid a universal configurable component whose props encode every page. Extract only behavior that has multiple real consumers.

## Cover professional states

- Loading: reserve table/form geometry; use restrained row/section skeletons and prevent layout shift.
- Empty: distinguish no data from no filter result and offer one valid next action.
- Error: preserve usable data/input, show a precise recovery action, and expose a safe `traceId` when available.
- Disabled: keep text readable and explain non-obvious lifecycle/permission reasons.
- Saving: disable duplicate intent, show progress without changing layout, and keep the same idempotency key for the same retryable operation.
- Success: update the authoritative state; use a concise toast only for non-blocking confirmation.
- Permission: hide impossible navigation when appropriate but still enforce authorization server-side.

## Validate the result

- Test at desktop operational widths and at least one tablet/narrow-mobile layout.
- Stress long names, large TRY values, many variants, missing images, zero/low stock, long validation messages, dense rows, and empty pages.
- Test keyboard navigation, focus visibility/restoration, dialogs/drawers, labels, table semantics, and contrast.
- Verify no unsupported Shopify feature or copied brand asset/text/icon remains.
- Verify no derived stock, balance, tax, profit, paid/remaining, or lifecycle state is calculated as frontend authority.
- Capture before/after screenshots with identical data and viewports and re-run the visual review.
- Run type checking, lint, focused tests, and a production build for implementation work.

Finish with the implemented routes/states, reference adaptations, intentional deviations, screenshots, checks run, and remaining risk. Never claim visual completion without inspecting the rendered result.
