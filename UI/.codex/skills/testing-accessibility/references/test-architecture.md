# Test Architecture

## Tooling choice

Prefer:

- Playwright Test for full browser journeys and responsive projects;
- `@axe-core/playwright` for automated accessibility rules;
- the repository's existing unit/component runner for pure functions and isolated UI behavior.

Do not introduce a second tool that duplicates an established capability. Check the installed versions and current official documentation before writing configuration.

## Suggested placement

```text
front-end/
├── playwright.config.ts
├── tests/
│   ├── e2e/
│   │   ├── auth/
│   │   ├── products/
│   │   ├── cart/
│   │   └── checkout/
│   ├── accessibility/
│   ├── fixtures/
│   └── support/
├── playwright/
│   └── .auth/          # generated and gitignored
└── test-results/       # generated and gitignored
```

Keep application unit/component tests next to owned code when repository convention allows it. Keep page objects small and task-oriented; do not hide assertions or entire journeys in generic page-object methods.

## Project matrix

Use a fast pull-request tier and a broader scheduled/release tier.

- Pull request: primary desktop Chromium, representative mobile Chromium, critical accessibility states.
- Scheduled/release: Chromium, Firefox, WebKit, representative mobile Chrome and Mobile Safari profiles.
- Add explicit narrow-width/reflow checks where mobile device presets do not cover the risk.

Use real browser/device emulation labels accurately. A Playwright mobile preset is emulation, not a physical-device test.

## Configuration principles

- Start the production-like local application through `webServer` when practical.
- Keep base URLs and non-secret test identifiers in environment configuration.
- Load credentials from secret storage; never hard-code them.
- Enable trace on first retry and screenshots/videos on failure according to CI storage limits.
- Keep default retries at zero locally. Use limited CI retries only to expose and classify flakes, never to make instability look green.
- Avoid global mutable data and tests that depend on execution order.
- Disable animations in deterministic visual assertions without changing semantic state.

## Fixture strategy

Build deterministic fixtures for:

- active product with one and multiple variants;
- unavailable/out-of-stock product;
- product with a server-side price change;
- guest cart and authenticated cart;
- valid customer with address;
- expiring session;
- valid, invalid, and exhausted coupon;
- safe checkout and payment outcomes.

Prefer API or database-supported test setup that belongs to a non-production environment. If no safe reset/seed mechanism exists, report the blocker rather than deleting shared data.

Create unique emails and idempotency keys. Clean up only resources owned by the current run. Validate exact targets before cleanup.

## Selector strategy

Use selectors in this order:

1. role and accessible name;
2. associated label;
3. stable visible text;
4. placeholder only when it is the actual user cue;
5. test ID as a narrow escape hatch.

A failing role/name locator can reveal a real accessibility regression. Do not immediately replace it with a CSS selector.

## Waiting and assertions

- Wait for visible user state or the exact relevant response, not `networkidle` as a universal readiness signal.
- Assert a mutation's final rendered outcome and important request invariant.
- Avoid duplicating every backend field assertion in E2E tests.
- Freeze or inject time only for scenarios that require deterministic expiry.
- Match expected request failures exactly; never blanket-ignore all `4xx` responses.

## Runtime evidence fixture

Attach listeners before navigation:

- `page.on("pageerror")` for uncaught exceptions;
- `page.on("console")` for unexpected error logs and selected warnings;
- `page.on("requestfailed")` for transport failures;
- `page.on("response")` for unexpected HTTP failures.

Collect findings during the test and attach redacted evidence before failing, so the report preserves context. Ignore only exact known cases such as the intentionally invalid login response in that scenario.

Never attach raw authorization/cookie headers, full request bodies, access/refresh tokens, passwords, addresses, or payment data.

## Accessibility scan placement

Scan meaningful settled states rather than only route load:

- product list and detail after content renders;
- variant selected and cart confirmation visible;
- cart empty, populated, conflict, and unavailable-item states;
- login validation and authentication failure;
- every checkout step and validation state;
- modal, drawer, toast, menu, and error boundary while open.

Use explicit WCAG tags supported by the installed axe version. Review disabled rules and `incomplete` nodes. Scope exclusions to the smallest known third-party region, record the owner/reason/expiry, and retain manual coverage.
