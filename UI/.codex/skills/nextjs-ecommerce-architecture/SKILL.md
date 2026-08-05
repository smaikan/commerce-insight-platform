---
name: nextjs-ecommerce-architecture
description: Design, implement, refactor, or review the clean Next.js App Router architecture for this e-commerce frontend. Use for project structure, route groups, Server and Client Component boundaries, API/BFF access, data fetching and caching, JWT authentication, state management, component placement, loading/error/not-found handling, SEO, feature separation, and integrations with the repository's docs/api contracts—especially the admin-only accounting module.
---

# Next.js E-Commerce Architecture

Build the smallest architecture that preserves clear feature boundaries, server-first rendering, SEO, and the backend contracts. Prefer explicit code over generic abstractions.

## Ground the work

1. Locate the frontend root from the nearest `package.json`.
2. Inspect `package.json`, Next config, TypeScript config, `src/app`, environment examples, and `.openai/hosting.json` when present.
3. Locate API documentation at `<workspace-root>/docs/api`.
4. Read [project-api-map.md](references/project-api-map.md) before choosing modules or routes.
5. For accounting work, also read [accounting-rules.md](references/accounting-rules.md), every file in `docs/api/api-accounting-docs`, and only the relevant endpoint contracts under `docs/api/api-project-docs/08-endpoint-sozlesmeleri`.
6. For concrete folder, route, cache, and API-layer decisions, read [architecture-blueprint.md](references/architecture-blueprint.md).
7. Treat the OpenAPI contract as the wire-format source of truth and the Markdown documents as the workflow/UI source of truth. Report contradictions instead of silently choosing one.

Do not invent endpoints, filters, sort fields, roles, exports, workflows, or accounting capabilities absent from the documentation.

## Use available MCP tools

- Use **Next DevTools MCP** (`next_devtools`) first for the running Next.js app: discover routes/runtime state, inspect framework diagnostics, and consult version-matched Next.js documentation before assuming behavior.
- Use **Chrome DevTools MCP** (`chrome_devtools`) to verify rendered output, console errors, network requests/cache headers, hydration behavior, and client/server boundary effects.
- Use **Playwright MCP** (`playwright`) to exercise critical navigation, authentication, lifecycle, and responsive flows when architecture changes need browser evidence.
- Treat MCP observations as runtime evidence, not a replacement for source review, type checks, tests, or a production build. If an MCP is unavailable, continue with the equivalent local CLI/browser workflow and state the limitation.

## Follow the architecture workflow

1. Classify the requested work as public storefront, customer account, general admin, or accounting.
2. Identify its authorization, SEO, freshness, mutation, idempotency, and concurrency requirements.
3. Place the route in the appropriate route group without changing its public URL.
4. Keep the page a Server Component. Introduce the narrowest possible Client Component only where interaction requires it.
5. Add API calls to the owning module through the shared server-only HTTP client.
6. Choose cache behavior explicitly from the cache matrix in the blueprint.
7. Add route-level `loading.tsx`, `error.tsx`, or `not-found.tsx` only at the nearest useful recovery boundary.
8. Add or update metadata for public indexable pages; mark private routes as non-indexable.
9. Test the boundary that carries risk: auth, caching, idempotency, concurrency, lifecycle, or SEO.
10. Re-check that no backend DTO calculation or business rule has been duplicated in the browser.

## Enforce core boundaries

### Keep routing thin

- Use `app` for routes, layouts, metadata, and composition.
- Put business UI and API operations under `src/modules/<feature>`.
- Use route groups for layout/auth boundaries, not as business-layer folders.
- Use English, lowercase `kebab-case` static URL segments and stable English code identifiers.
- Keep route-group names and dynamic parameter names in English. Route groups must not affect the public URL.
- Keep Turkish UI copy and `lang="tr"` independent from URL naming.
- Preserve content-owned slugs returned by the API; do not translate or invent a second product slug.
- Keep e-commerce `orders` separate from accounting `sales-orders`; they are different domains.

### Prefer Server Components

- Fetch initial data, render tables/details, and generate metadata in Server Components.
- Use Client Components only for browser APIs, event handlers, dialogs, rich form state, optimistic UI, and interactive editors.
- Place `"use client"` at the smallest leaf boundary.
- Pass minimal serializable props to Client Components.
- Never turn a layout or entire page client-side merely to support one interactive control.

### Keep API access server-side

- Use one `server-only` typed API client for base URL, Bearer headers, JSON, timeouts, and ProblemDetails parsing.
- Call backend GET endpoints directly from Server Components or server-side data functions.
- Perform browser-originated mutations through Server Actions by default.
- Use Route Handlers only when an HTTP boundary is genuinely required: login/logout cookies, uploads/downloads, callbacks, webhooks, or client-side streaming/live requests.
- Never expose access tokens, refresh tokens, or the private API base URL to Client Components.
- Never create a mechanical Route Handler mirror for every backend endpoint.

### Treat the API as server state

- Keep filters, sorting, and pagination in URL search parameters.
- Keep form drafts and transient interaction state local to the feature.
- Add global client state only for state that is both cross-route and browser-owned.
- Do not copy API entities into a global store by default.
- Do not calculate stock, invoice totals, balances, paid/remaining values, FIFO cost, or profit on the client when the API returns them.

### Preserve feature ownership

- A module may import shared UI, shared infrastructure, and generated API types.
- A module must not import another module's private components, actions, or internal API files.
- Promote code to shared only after at least two real consumers need the same behavior.
- Keep feature-specific tables, schemas, forms, mappers, and status labels inside the owning module.
- Avoid `utils.ts`, `helpers.ts`, barrel files, repositories, services, and hooks that have no precise responsibility.

## Apply authentication rules

- Store access and refresh tokens in `HttpOnly`, `Secure` in production, `SameSite=Lax`, path-scoped cookies.
- Set, rotate, and delete auth cookies only in Server Actions or Route Handlers.
- Use `proxy.ts` for optimistic redirects only; re-check authentication and authorization in every protected Server Action and data-access operation.
- When an access cookie is expired but a refresh cookie exists during navigation, redirect through a dedicated refresh Route Handler and then return only to a validated relative URL.
- Retry one request after a valid refresh. On refresh failure, clear the session and redirect to `/login`.
- Distinguish `401` from `403`.
- Require the backend's Admin role for every accounting operation; hiding a button is not authorization.

## Apply error and lifecycle rules

- Normalize backend ProblemDetails into one typed error model.
- Map validation errors to fields; preserve form input for business-rule and conflict errors.
- Show `traceId` for unexpected failures.
- On `409`, re-read current state and never overwrite automatically.
- Preserve the same idempotency key while retrying the same user intent.
- After cancel, post, reverse, or other lifecycle mutations, re-read the authoritative record.
- Never remove reversed accounting history from the UI.

## Apply SEO rules

- Set `lang="tr"` and a root metadata title template.
- Use `generateMetadata` for product and other dynamic public detail pages.
- Define canonical URLs, Open Graph data, meaningful image alt text, and JSON-LD where applicable.
- Keep `robots.ts` and `sitemap.ts` under `src/app`.
- Exclude admin, account, cart, checkout, auth, and internal search/filter URLs from indexing.
- Use semantic HTML and server-render indexable content.
- Do not promise clean slug-only product lookup while the API only supports public product IDs; use the canonical ID-plus-slug strategy from the blueprint.

## Validate the result

- Run generated-type drift checks when the OpenAPI contract is used for type generation.
- Run TypeScript, lint, focused unit tests, and a production build.
- Add end-to-end coverage for critical auth and lifecycle flows when those flows change.
- Review the final diff for unnecessary Client Components, duplicated DTOs, accidental public environment variables, broad shared abstractions, and undocumented API assumptions.

When asked only for an architecture plan or review, do not mutate the project. Return concrete decisions, affected boundaries, risks, and acceptance tests.
# Guest checkout extension

Guest commerce mimarisinde anonymous checkout'u login'e yönlendirme. Same-origin BFF Route Handler cookie sınırını, zorunlu aktif shipping yöntemini, guest self-service/claim akışını ve Server Component self-fetch yasağını `project-api-map.md` ile `architecture-blueprint.md` kaynaklarından uygula.
