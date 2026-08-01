---
name: testing-accessibility
description: Plan, implement, run, or review tests and accessibility for this Next.js e-commerce frontend. Use for critical login, product, cart, checkout, and payment journeys; Playwright end-to-end coverage; desktop/mobile responsive checks; keyboard, screen-reader, form, WCAG, and axe audits; and evidence-based reporting of console, page, network, visual, and user-interface failures.
---

# Testing and Accessibility

Test observable user outcomes against the real frontend and documented ASP.NET API behavior. Treat automated accessibility checks as one layer, not as proof of accessibility.

## Ground the work

1. Locate the frontend root from the nearest `package.json`.
2. Inspect the package manager, installed test tools, Next.js version, route tree, auth/BFF layer, error boundaries, form library, and existing test conventions.
3. Locate the API documentation at `<workspace-root>/docs/api`.
4. Read [project-flows.md](references/project-flows.md) before testing login, product, cart, or checkout.
5. Read [test-architecture.md](references/test-architecture.md) before adding test dependencies, configuration, fixtures, or selectors.
6. Read [accessibility-and-reporting.md](references/accessibility-and-reporting.md) before an accessibility audit or defect report.
7. Inspect the relevant OpenAPI operation and Markdown endpoint contract. Report conflicts instead of inventing expected behavior.
8. Verify version-specific test-library behavior against current official documentation.

Do not run destructive scenarios against production, submit real payments, expose credentials, or assume a seeded product/account exists without verifying the test environment.

## Use available MCP tools

- Use **Playwright MCP** (`playwright`) for exploratory browser execution, responsive emulation, keyboard interaction, screenshots, console capture, and quick reproduction of login/product/cart/checkout states. Convert valuable coverage into committed Playwright Test cases; an MCP session is not the regression suite.
- Use **Chrome DevTools MCP** (`chrome_devtools`) for independent console, page, network, accessibility/Lighthouse, rendering, and performance evidence when a browser failure needs deeper diagnosis.
- Use **Next DevTools MCP** (`next_devtools`) for Next.js-specific route/runtime errors, rendering behavior, and version-matched framework guidance.
- Use the smallest sufficient tool set and correlate evidence by route/state. If an MCP is unavailable, continue with repository tests or local browser tooling and mark what was not verified.

## Define scope before execution

Record:

- base URL and environment;
- build or commit identifier;
- browser, viewport/device profile, operating system, locale, and color scheme;
- anonymous, customer, or admin state;
- fixture/account identifiers without secrets;
- mocked, sandboxed, or real test API boundaries;
- flows and states included or explicitly not verified.

Prefer a small risk-based matrix over duplicating every assertion at every viewport. Run the canonical happy paths plus high-risk error and recovery states.

## Build stable end-to-end coverage

- Use Playwright Test for browser-level flows unless the repository already standardizes on another capable runner.
- Prefer `getByRole`, `getByLabel`, `getByText`, and visible user outcomes.
- Use `data-testid` only when no stable accessible or user-facing locator exists.
- Never select by generated CSS class, DOM depth, arbitrary timeout, or implementation-only text.
- Use web-first assertions and event-driven waits. Do not use fixed sleeps.
- Keep each test isolated and repeatable. Create unique users, carts, idempotency keys, and orders per test or worker.
- Use API/fixture setup for prerequisites, but keep at least one full browser journey through every critical boundary.
- Store reusable login state only in a gitignored location. Never commit cookies, tokens, passwords, or storage-state files.
- Preserve traces, screenshots, and videos on failure according to the repository's CI retention policy.

## Cover the critical commerce journeys

Implement the detailed matrix in [project-flows.md](references/project-flows.md). At minimum cover:

1. Login success, invalid credentials, validation, protected-route behavior, expiry/refresh, and logout.
2. Product listing/detail, variant selection, unavailable states, image alternatives, and an add-to-cart entry point.
3. Guest and authenticated cart add/update/remove, authoritative totals, stale concurrency, price/availability changes, and guest merge.
4. Checkout authentication, address/shipping/coupon choices, double submission, idempotency, stock/cart conflict, payment result, and confirmation.

Assert both visible state and the important boundary behavior. Never derive expected price, stock, tax, discount, or payment status with duplicated frontend business rules; compare to controlled fixture facts or authoritative API responses.

## Test responsive behavior

- Configure representative desktop Chromium and mobile Chrome/Safari-compatible projects.
- Add targeted narrow, tablet, desktop, and zoom/reflow checks where the layout risk exists.
- Check for horizontal overflow, clipped content, overlapping controls, unreachable actions, off-screen dialogs, sticky-element collisions, and virtual keyboard obstruction.
- Exercise navigation, filters, product variants, cart controls, form errors, checkout steps, dialogs, drawers, and orientation changes.
- Verify touch target spacing manually when automation cannot determine it.
- Assert behavior and usable layout, not pixel-perfect screenshots across every browser.
- Use visual snapshots only for stable, high-value regions with deterministic data, fonts, animations, and clocks.

## Audit accessibility

Target WCAG 2.2 AA unless the project declares a stricter standard.

- Run axe on representative stable states, including open dialogs, validation errors, populated cart, and each checkout step.
- Treat every excluded selector or disabled rule as a documented, time-bounded exception.
- Review axe `incomplete` results; do not discard them.
- Manually complete keyboard, focus, screen-reader, form, zoom, reflow, contrast, and motion checks from [accessibility-and-reporting.md](references/accessibility-and-reporting.md).
- Test semantic names and states, not merely the presence of ARIA attributes.
- Test with a real screen reader when the environment permits. Otherwise report screen-reader behavior as not verified, never as passed.
- Re-test the exact affected state after a fix and run a focused regression around it.

## Capture runtime failures

Collect evidence from the beginning of each test:

- uncaught page exceptions;
- unexpected `console.error` messages;
- failed requests;
- unexpected `4xx` and `5xx` responses;
- redirect loops and repeated refresh calls;
- hydration warnings;
- broken images and fonts;
- visible error, empty, loading, and retry states.

Allowlist only errors intentionally produced by the scenario, and scope the allowlist to the exact request and status. Redact authorization headers, cookies, tokens, credentials, personal data, payment data, and sensitive request bodies from all attachments and reports.

For UI failures, include the last visible state and accessible locator/action. For network failures, include method, safe path, status, response code/trace ID when available, and request correlation without secrets.

## Classify findings accurately

Separate:

- product defect;
- accessibility defect;
- frontend/API contract mismatch;
- environment or fixture failure;
- test defect or flake;
- observation requiring manual verification.

Report one root cause per issue when possible. Include severity, affected flow/state/device, reproducible steps, expected and actual results, evidence, likely owner, and retest criteria. Never call a flow passed if a required assertion was skipped or the environment masked the behavior.

## Validate changes

1. Run the smallest focused test set during iteration.
2. Run type checking, lint, and relevant unit/component tests.
3. Run critical E2E flows on the primary desktop and mobile projects.
4. Run the accessibility scan plus required manual checks.
5. Inspect console, page-error, and network evidence.
6. Repeat failed tests to distinguish deterministic failures from flakes; fix the cause rather than adding retries.
7. Run the production build when test/configuration changes affect the application boundary.
8. Summarize passes, failures, skipped/not-verified checks, artifacts, and residual risk.

When asked only to plan, review, audit, or report, do not modify application code. When asked to implement or fix, make the smallest cohesive change, preserve the repository architecture, and provide evidence from focused retesting.
