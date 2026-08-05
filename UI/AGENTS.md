# SERANTIS Frontend Working Agreement

This file is binding on all future Codex work under the `UI/` workspace. The rules here belong to frontend applications; It does not give you the authority to change the API code, API documentation or Accounting application.

## 1. Project overview

SERANTIS; It is an e-commerce platform that aims to combine store, product and stock operations, orders, customers, campaigns, accounting, future marketplace connections, reporting and analytics in a single professional system.

Current actual warehouse structure:

- Frontend workspace root: `UI/`
- Admin application: `admin/`
- Storefront application: `storefront/`
- API root: `../API/`
- API working agreement: `../API/AGENTS.md`
- API documentation for Frontend: `docs/api/`
- Detailed Accounting frontend documentation: `docs/api/api-accounting-docs/`
- OpenAPI document: `docs/api/api-project-docs/openapi-controller-contract.json`

Frontend today uses Next.js `16.2.12`, React `19.2.4`, TypeScript strict mode, App Router, pnpm, Tailwind CSS v4, ESLint and Vitest. Tailwind v4 is CSS-first; There is no separate `tailwind.config.*` file. A new Tailwind config file cannot be created just out of habit.

The existing `admin/src/app/page.tsx` create-next-app starter and `admin/src/app/product/[slug]` SEO experiment are not the completed Admin Panel interface. Phase 1 covers only the `admin/` application. `storefront/` is installed as a separate application, but the public Storefront application is outside the scope of this Phase 1; admin layout, auth state, operation components and navigation are not shared with Storefront.

## 2. Product identity and brand

**SERANTIS is a temporary working name; It is not an exact trademark or domain name.** The application name is read from a central config/environment value. The default enhancement value can be `SERANTIS`; Feature components, metadata, hostname, colors or assets do not hard-code this name assuming a permanent brand.

- There is no permanent logo yet. Until approval is given, the plain text-based 'SERANTIS' wordmark produced under the name of the central application is used.
- No logos, monograms, mascots or permanent brand signs are produced.
- There is no permanent brand palette available. The user has confirmed the small provisional token base consisting of neutral surfaces and moderate blue accents; This base is not presented like a permanent brand palette and is kept easily changeable.
- `serantis.com`, `www.serantis.com`, `admin.serantis.com`, `api.serantis.com` or any other production hostname is not assumed.
- The management interface should look serious, reliable and operation-oriented. It should not turn into a landing page or generic AI dashboard view.
- Fake statistics, number of customers, total sales, growth rate, social proof, slogan or demo business data are not presented as real.

## 3. Current frontend phase

The current Next.js workspace includes standalone `admin/` and `storefront/` applications. In Phase 1, only the Admin Panel is developed; The public Storefront application will be developed in the next separate scope. Two applications can be deployed independently.

Status terms strictly mean:

- **Phase 1:** First user-approved implementation phase; Now there is room for improvement.
- **Planned next:** It may have backend support, but it is an area whose page will not be completed in Phase 1.
- **Future module:** Long-term domain that does not have a backend/frontend agreement yet.
- **Placeholder/disabled:** An element that has a reserved place in the information architecture but does not have a clickable route or real data.

Repository reality: Currently no admin shell, BFF auth, admin routes or admin design system implemented. Therefore, the phrase “Phase 1” in this file is not the current completed page, but the next approved application scope.

Phase 1 only covers:

1. Required login and admin route protection.
2. Admin application shell.
3. Responsive sidebar and topbar/header.
4. Dashboard home page.
5. E-commerce Orders list and detail basis.
6. Products list.
7. Add Product flow.

Accounting, marketplace integrations, customers, campaigns/coupons, reports, inventory/stock operations, administrators and settings are not automatically counted as completed pages in Phase 1.

## 4. Sources of truth

Before working on a feature, find not only the public document, but also the relevant endpoint file and the actual DTO.

Resource priority:

1. By the user's current and open scope decision, this `AGENTS.md`.
2. `docs/api/api-project-docs/openapi-controller-contract.json`: documented wire schema, nullable/required field and numeric enum contract.
3. `docs/api/api-project-docs/08-endpoint-contracts/` and Markdown documentation on the subject: approved route, workflow and frontend behavior.
4. `docs/api/api-accounting-docs/`: Up-to-date, detailed business and UI agreement for the accounting frontend.
5. When necessary `../API/src/`: Used to verify controller attributes, DTO/validator and actual runtime behavior.
6. `../API/docs/accounting-module-spec.md`: Historical design source of Accounting; When in conflict with the current controller and `api-accounting-docs` the currently implemented agreement takes precedence.

API documentation is the source of the frontend contract. An endpoint that is present in the source code but not in the OpenAPI and endpoint documentation cannot be accepted as a "canned contract" by the frontend. In such a case, stop, report the difference at the file/route level and establish integration without updating the document.

The following are definitely not predictable:

- Endpoint, query parameter, filter or sort field.
- Request/response field, nullable behavior or numeric enum value.
- Auth, refresh, role or authority behavior.
- Idempotency, concurrency or retry behavior.
- Stock, price, tax, discount, campaign, invoice or accounting account.
- Unsupported lifecycle migration.

If missing information affects user experience or data accuracy, stop the work at that limit and report it as a "missing contract".

## 5. Mandatory skills

Project skills are under `UI/.codex/skills/`. Before starting each frontend task, identify the target application (`admin/` or `storefront/`) with the closest `package.json`; classify the task according to the following matrix; Read the `SKILL.md` file of each selected skill and the references it requires for the job type. Mentioning the name of the skill or just reading the 'SKILL.md' title is not enough.

This file and the user's current scope decision takes precedence over project skills. If the general example in a skill conflicts with the project rule, this file is applied. For example, the generic `/admin` URL example in `admin-dashboard-design` is not valid for this project; `/admin` prefix is ​​not used in the admin application.

| Skill | When mandatory | Application instruction |
| --- | --- | --- |
| `nextjs-ecommerce-architecture` | In every route, layout, data access, cache, state, module ownership or frontend refactor work. `project-api-map.md` and `architecture-blueprint.md` are read when necessary. Route is kept thin; Server Component default, `src/modules` ownership, server-only API limit, open cache decision and admin/storefront distinction are preserved. |
| `api-integration-auth` | API integration, OpenAPI type generation, BFF, auth, cookie, Server Action/Route Handler or ProblemDetails | The relevant OpenAPI + endpoint contract, controller/DTO and skill's contract/BFF/error references are read. Contract audit is run. Tokens are not leaked to the browser; refresh is attempted at most once; CSRF, idempotency, `401`/`403`/`409` behaviors are validated. |
| `admin-dashboard-design` | Only in `admin/` shell, dashboard, operation table, filter, form, drawer/dialog or responsive admin UI | Relevant admin reference files and API contract are read. Every visible metric, column, filter, and action is based on documented capability; URL state, compact density, real loading/empty/error/permission states and keyboard/focus flows are implemented. |
| `ecommerce-seo-review` | Only in `storefront/` public route, metadata, canonical, sitemap, robots, Open Graph, JSON-LD or indexability | Static SEO inventory is run; Route indexability matrix is ​​prepared. Admin/auth/account/cart/checkout remains noindex. Product structured data and sitemap are generated with authoritative API data only; Search engine result or CWV pass claim is not made without evidence. |
| `visual-design-review` | Whether in the business of visual design change, design audit, or working with a reference screen | First, a proven problem report is prepared with references to `visual-language`, responsive/state and screenshot. The desktop + mobile before/after screenshot with the same fixture, viewport, scale and state is visually examined; A DOM snapshot alone is not considered visual evidence. |
| `performance-core-web-vitals` | Performance, bundle, Client Component limit, render waterfall, cache, image/font/script or CWV work | Baseline is taken from the production build; static inventory and import/network/trace evidence are examined. LCP, INP and CLS are evaluated separately; Lighthouse is laboratory diagnostics only. Admin/auth data remains private/no-store; Freshness, security or accessibility are not sacrificed for performance. |
| `testing-accessibility` | In critical flow, regression, E2E, responsive, keyboard/focus, form, WCAG or error verification work | Risk-based test matrix is ​​defined. Playwright Test is used for persistent browser regressions, ax + manual checks for accessibility when the tools are installed in the relevant application. If the real screen-reader test cannot be performed, the result is written as `not verified`; credential, token, payment and personal data are not added to the test output. |

If a job falls into more than one scope, the order of execution is as follows: first `nextjs-ecommerce-architecture` and contract review; then the relevant feature skill; `api-integration-auth` if there is auth/API limit; If there is a visual or performance effect, use the relevant review/measurement skill; finally `testing-accessibility`. The application code is not changed when requesting a review or plan; Only the evidence and the decision are reported.

### MCP usage instructions

MCP is used only if vehicle is available; The tool name or tool output is not invented. When the equivalent local tool is continued and runtime evidence is missing, this is written in the report.

- **Next DevTools MCP (`next_devtools`):** Next.js runtime, route, App Router rendering, Route Handler/Server Action, framework is the first tool for examining error and version-appropriate behavior. Used before Next.js behavior is assumed.
- **Chrome DevTools MCP (`chrome_devtools`):** Used for console/page exception, network and cache header, redirect/cookie attribute, hydration, accessibility snapshot, responsive render, screenshot, Lighthouse and performance trace analysis. In performance work, trace takes precedence over Lighthouse score.
- **Playwright MCP (`playwright`):** Used for login/logout, protected navigation, lifecycle, form/dialog/drawer, responsive/keyboard flows and repeatable screenshot states. The Discovery MCP session is not a substitute for permanent regression testing; Valuable scenarios are converted to testing in the repo.

MCP observation; It is not a substitute for source review, OpenAPI/controller validation, persistent tests, `pnpm lint`, `pnpm typecheck`, `pnpm test` and production build. In each runtime record, the route, application, build, viewport/device, auth/data state and relevant tool are specified. Cookie value, Authorization header, token, password, credential, payment data or personal data are not written to the MCP output, screenshot name or report.

## 6. Warehouse and architectural rules

The existing App Router and `src/` layout are preserved. The target structure is in the following direction; Only the folders required for the active slice are created:

```text
src/
  app/
    (auth)/
      login/
    (admin)/
      layout.tsx
      dashboard/
        page.tsx
      orders/
        page.tsx
      products/
        page.tsx
        new/
          page.tsx
    api/auth/              # yalnız gerçek BFF HTTP sınırları
    page.tsx               # session durumuna göre /login veya /dashboard redirect
    layout.tsx
    robots.ts
    sitemap.ts
  modules/
    admin-shell/
    auth/
    orders/
    products/
    accounting/            # yalnız aÃ§Ä±kÃ§a istendiÄŸinde
  components/
    ui/
  lib/
    api/
    auth/
    formatting/
    validation/
  generated/
    api.ts                 # onaylÄ± OpenAPI generation sonrasÄ±
```

Rules:

- Static URL segments, route group names and dynamic parameter names are English, lowercase and `kebab-case`. UI text may be Turkish.
- The `/admin` URL prefix is ​​not used in this separate Admin Panel application. Phase 1 URLs are `/login`, `/dashboard`, `/orders`, `/products` and `/products/new`. Even if the hostname later becomes an admin subdomain, the route structure does not change.
- Route group is not used to change the URL; Provides layout, auth and rendering limits.
- `page.tsx`, `layout.tsx`, `loading.tsx`, `error.tsx` and `not-found.tsx` only perform routing, params/searchParams analysis, initial fetch and feature composition.
- Business rules and endpoint calls are not kept in presentation components.
- Business UI, action, form schema, mapper, status label and feature API operation remains under `src/modules/<feature>`.
- `src/components/ui` is only for truly common primitives that do not know domains. `src/modules/admin-shell` owns the sidebar, topbar and page frame.
- A module does not import another module's private component/action/API file. The code is not moved to the shared area until there are actually two consumers.
- No large `utils.ts`, `helpers.ts`, vague `services/`, unnecessary barrel exports or â€œgenericâ€ components that combine different job responsibilities are created.
- Shared pagination, ProblemDetails and generated wire type are not rewritten.
- New dependency is added only with concrete need, bundle/maintenance impact and explicit user approval.
- The current Tailwind v4 `@theme`/CSS variable approach is used; The second styling system is not added.

### Host-agnostic configuration

- Final commercial name, domain, hosting provider, API hostname and deployment topology are not yet final. Routing remains independent of hostname; Next.js does not create DNS records.
- Origin and API addresses from the central environment/config layer, `ADMIN_APP_ORIGIN`, `STOREFRONT_APP_ORIGIN`, `INTERNAL_API_BASE_URL` and `BROWSER_API_BASE_URL` only if direct browser access is explicitly required It is read from the equivalent values.
- Production hostname or localhost port is not hard-coded in the source code. Local addresses come from environment values ​​defined by repository/config.
- Secret, token and internal API address are not put in the `NEXT_PUBLIC_*` variable. The non-secret runtime config that should be opened to the browser is a separate and explicit decision.
- Possible future topology is root/www Storefront, separate admin subdomain and separate API subdomain; This is only the provisional deployment aspect, it is not considered the exact architecture.

### Server and Client Component limit

- Page/layout and first data read becomes Server Component.
- `use client`` is placed only in the smallest leaf that contains event handler, browser API, drawer/dialog, complex form state, interactive table or actually necessary optimistic UI.
- Client Component is not used just for `next/image`, static markup, server-known formatting or initial fetch.
- Only small and serializable props are passed to the client boundary; The entire API graph is not transferred.
- For an interactive control, the layout or the entire page tree client is not made.

### State ownership

1. Filter, search, sort, page and shareable tab: URL search params.
2. Entity, list, balance and lifecycle state: API/server state.
3. Form draft, drawer, dialog, row selection: closest feature-local state.
4. Global client store: only if the browser-owned state is actually shared across multiple unrelated routes.

Redux does not start with Zustand or TanStack Query. If Server Components, Server Actions, URL state and local state are not enough, prove it and get approval first.

## 7. Admin and storefront distinction

### Admin Panel

Admin is authenticated operating software:

- Its priorities are clarity, speed, accessible data density, keyboard usage, reliable API state and data freshness.
- Admin data is not placed in the Next.js shared cache; The default is `no-store`/private behavior.
- Admin routes are protected with auth and receive `robots: { index: false, follow: false }` via route/layout metadata.
- `robots.ts` is not a security mechanism. Confidential data is protected only by auth/authorization.
- Storefront canonical, Product JSON-LD or sitemap behavior is not applied to admin pages.

###Public storefront

Storefront is installed as a separate Next.js application under `storefront/`; The public features will be developed in the next separate scope:

- Server-rendered, crawlable product and collection pages are targeted.
- Metadata, canonical, Open Graph, structured data, sitemap, image optimization and Core Web Vitals are storefront responsibility.
- Admin design density is not transferred to storefront, storefront SEO/cache rules are not transferred to admin.
- Admin-only layout, navigation, authentication/session state or operational components are not connected to Storefront. Shared package is issued only when two real consumers and a clear common need are created.
- The current trial `/product/[slug]` and the canonical `/products/{slug}` are not the same route. Additionally, slug endpoints are found in the source code but not in the current OpenAPI. The public product route strategy will not be extended without correcting this agreement.

## 8. Sidebar and information architecture

In Phase 1, the sidebar represents the following information architecture. Group titles can be opened and closed; When the user presses the group title, the sub-items appear. The `Planned`, `Future` and `Placeholder` sub-items also appear, but remain disabled until the actual route/contract is applied, do not navigate and are described with the text `Planned/Coming Soon`.

| Group | Item | URL/location | Status | Contract note |
| --- | --- | --- | --- | --- |
| Overview | Dashboard | `/dashboard` | Phase 1 | There is no general dashboard metric endpoint; Showing a fake card. |
| Commerce | Orders | `/orders` | Phase 1 | E-commerce `Order`; Accounting is not sales. |
| Commerce | Products | `/products` | Phase 1 | API product list. |
| Commerce | Add Product | `/products/new` | Phase 1 | There may be a secondary item/quick action under Products in the sidebar. |
| Commerce | Collections | target `/collections`; Currently disabled | Planned next | Supports API. There is no classic `Category` entity; The main category concept is `ProductType`. |
| Commerce | Campaigns | group/placeholder | Planned next | The current backend only support…6248 tokens truncated…pe/inputMode and the associated error message.
- Long form error summary links to invalid fields; safe input is preserved, password/token is not backfilled.
- Focus appears in every interactive element and is not interrupted by overflow.
- Works with menu, dialog, drawer, combobox, tab and table sort state keyboard; focus trap/restore is correct.
- Status, selected, error or success cannot be described with color alone.
- Default, hover, focus, selected, disabled and error contrast is verified.
- Disabled state becomes readable and explained if the reason is not clear. Busy state is not indicated by opacity alone; `aria-busy`/suitable announcement is used.
- Async loading, save result and fatal error are reported with appropriate live region behavior; focus is not moved randomly.
- Reduced motion, 200% zoom and 400% reflow are controlled where appropriate.
- If there is no real screen reader test, it is called “not verified”; The accessibility tree/axe result is not considered a screen-reader pass.

## 9. API integration rules

- A single `server-only` typed API client; The `INTERNAL_API_BASE_URL` equivalent handles base URL, Bearer injection, JSON, timeout/abort, empty `204`, ProblemDetails and safe retry behavior.
- The preferred authenticated browser flow is `Browser → same-origin Next.js BFF → ASP.NET Core API`. The browser does not directly manage ASP.NET access/refresh tokens.
- Internal API base URL is only a server environment variable; Cannot be `NEXT_PUBLIC_`. The secret/token is not put into any public env variable.
- Endpoint strings are not distributed across visual components or different features.
- Server Component directly calls the public server-only function for the first GET. It does not send internal HTTP requests to its Next Route Handler.
- Browser-facing authenticated operations pass through the controlled BFF Route Handler or appropriate Server Action boundary. Route Handler is used when auth cookie, browser proxy, upload/download, callback, webhook or actual browser-facing HTTP limit is required.
- Not all ASP.NET endpoints are mechanically mirrored with Next Route Handler.
- Request cancellation/timeout supported. There is a difference between user cancellation and timeout.
- JSON cannot be parsed without checking the response content-type and status; `204` body is not parsed.
- `400/401/403/404/409/429/500`, network, timeout and non-JSON upstream failure center are normalized.
- Safe error model preserves `status`, `code`, `detail`, `errors`, `traceId`, `timestamp` and `Retry-After` if applicable; stack trace does not show token and secret URL.
- `400` field errors are mapped to the form; The global error is also preserved. After '409', the data is read again, no automatic overwrite/retry is performed.
- Non-idempotent mutation is not automatically retried. The same key is preserved in documented idempotent intent repetition.
- Duplicate submit is blocked; Loading, success, empty and error states are turned on. Mistakes cannot be swallowed silently.
- Can calculate frontend preview; API is the final authority for price, discount, tax, shipping, stock, invoice, balance, paid/remaining, FIFO cost and profit.

Wire types are generated from OpenAPI into `src/generated/api.ts` in the long run and the file is not changed manually. The user has allowed the addition of `openapi-typescript`; but installation is only done in the relevant application task after the API/OpenAPI documentation has been updated first. Broad generated type layer from outdated OpenAPI is not installed. During this period, the necessary narrow types are extracted only from the documented contract, not all API DTOs are replicated manually.

## 10. Authentication rules

ASP.NET API uses JWT Bearer and returns access + refresh token and backend expiry times in the login/refresh response. Frontend BFF model implements:

- Access and refresh tokens are not exported to browser JavaScript, localStorage/sessionStorage, serialized props, or HTML.
- Tokens are kept in separate `HttpOnly`, `Secure` in production, `SameSite=Lax`, `path=/`, cookies with no domain specified.
- Cookies become host-only; In the first stage, cross-subdomain cookie sharing or SSO is not applied. Admin and future Storefront sessions remain separate.
- It is the exact expiry value in the cookie expiry backend response.
- Cookie set/rotate/delete is done only in Server Action or Route Handler; Server Component cannot write cookies during rendering.
- Login server-to-server calls `/api/auth/login`, writes two cookies and returns only secure session/user information to the client.
- Refresh sends a refresh body with `/api/auth/refresh-token` and rotates the two returned cookies together.
- After refresh, the original request is repeated at most once; loop and parallel refresh race are blocked.
- Logout attempts the upstream `/api/auth/logout` call and clears local cookies `finally` even if upstream fails.
- In Next.js 16, `proxy.ts` is used, not `middleware.ts`. Proxy is only for fast/optimistic route gate and redirect; It is not full session management or authorization limit.
- Re-checks each Server Action, Route Handler and server-side data operation session/role; ASP.NET is the final authorization authority.
- Decoding JWT payload is not proof of authorization. For Expiry alone it can help.
- `403` behavior is preserved for non-admin user and `401` behavior is preserved for session loss.
- `returnTo` is only validated as relative same-origin path.
- Cookie-authenticated mutation boundaries are protected against CSRF by checking POST semantics and origin/same-origin.
- In state-changing BFF requests, SameSite protection, Origin verification, Referer verification when appropriate, and CSRF protection required by the selected session model are evaluated together.
- Authenticated response does not enter the shared cache; There should be no private content left after logging out with browser history/back.

### Route and redirect agreement

- Unauthenticated user is redirected to `/login` when visiting `/`.
- When an authenticated user visits `/`, they are redirected to `/dashboard`.
- Unauthenticated user is redirected to `/login` in protected Admin routes.
- Authenticated user is redirected to `/dashboard` when visiting `/login`.
- Next.js route groups are for the layout/auth boundary and do not appear in the URL. `/admin` prefix is ​​not added.
- Fake auth, hard-coded admin credential or undocumented token behavior is not generated. Before redirect and protection implementation, auth documents and API-side `AGENTS.md` are read again.
- If the API auth agreement does not meet the BFF flow, the route structure and login UI basis can be established; The missing contract is documented and work stops at that limit without adjusting the auth behavior.

### Credential and test security

- Real or reusable credential; Source code, `AGENTS.md`, README, committed env, frontend config, `appsettings.json` or test source are not written to.
- Development seeds can only run in a development environment, when explicitly enabled, and in a non-production database. Uses secret/config equivalent to `ENABLE_DEVELOPMENT_SEED`, `SEED_ADMIN_EMAIL`, `SEED_ADMIN_PASSWORD`; It works idempotent with the real password hasher and is skipped if values ​​are missing.
- Integration tests use an isolated/ephemeral test database, create the user with a runtime-generated test credential during setup, and reset the state between suites. It does not connect to the development or production database.
- Local E2E credential uncommitted comes from local env, CI credential comes from encrypted secret store. Dedicated least-privilege test account is used; The production admin account is not used.
- Production does not automatically seed admins with known passwords. It is created with the first admin explicit secure bootstrap/deployment secret or approved manual operation. Secret, token, cookie, connection string and password are not logged.

## 11. Product feature rules

### Product list

Phase 1 product list uses `GET /api/products`. Documented query fields:

- `pageNumber`, `pageSize`
- `search`
- `typeId`, `brandId`
- `status`, `isActive`, `isFeatured`
- `sortBy`, `descending`

`ProductSortBy`: `0 DisplayOrder`, `1 Title`, `2 CreatedAt`, `3 PopularityScore`. `ProductStatus`: `0 Draft`, `1 Active`, `2 Passive`, `3 Archived`. Numeric wire values ​​come from the generated/verified contract; UI labels are kept in a separate map.

- Filter and pagination are kept in the URL; When the filter changes, it becomes `pageNumber=1`.
- Loading, empty dataset, no filtered result, API error and retry situations are separate.
- Server pagination is used; Not all products are transferred to the client.
- No separate detail calls are made for the list line.
- Returns the current endpoint `PagedResult<ProductDto>` and carries the variants/tags graph; There is no separate `ProductSummaryDto`. The user approved adding a small product list summary and main-image area to the backend before Phase 1. First the API/OpenAPI/endpoint documentation is updated; The frontend uses these fields only after the updated contract is published. The missing contract is not hidden with N+1 image/detail fetch.
- Current Product DTO does not contain images in the list. Thumbnail is not created until the newly documented main-image agreement arrives.

### Add Product

The route becomes `/products/new`. The form cannot be a single giant component; is divided into the following responsibilities:

1. Basic information: title, main SKU, URL, description.
2. Organization: ProductType, brand, collections, tag names and tax rate.
3. State: numeric ProductStatus, active, featured, display order.
4. Variants: at least one variant for each product; name, value, SKU, price, stock, optional compare-at price, barcode, material, active and documented opening cost fields.
5. SEO: SEO title and description.
6. Images: URL, alt text, main and display order with separate ProductImage endpoints after receiving the public product ID in the product create response.

Strict rules:

- Classic `Category` entity is not created. The main category concept is `ProductType` and the merchandising group is `Collection`.
- Variant `name` and `value` are sent separately according to the actual backend model. Backend does not produce combinations; The combined option can be in up to three pieces.
- `hasVariants` is a persisted request/response boolean. It defaults to `false`; a product with more than one variant must send `true`. `netPrice` remains response-only.
- Product create is atomic with at least one variant. The image upload endpoint is not documented; There is only a URL-based ProductImage contract.
- Since product create and image transactions are separate endpoints, the UI does not falsely promise "all one transaction". Clear reports of partial success and preserves the generated product ID.
- Stock is not updated like the normal product field. According to the opening stock create contract; The next stock change is made with signed `StockMovement`.
- Collection, ProductType, Brand and TaxRate selectors only use the existing list/pagination contract; undocumented search does not make up.
- There will be no second business engine that imitates form validation server rules. Client validation is for user input and explicit length/required limits; API verifies again.
- Long product name, high price, multiple variants, zero stock, missing image and long validation message are tested.

## 12. Order feature rules

`Order` is the authenticated customer/cart checkout aggregate. It is not `AccountingSalesOrder`. These concepts are never mixed in route, type, DTO, label and service files.

Phase 1 admin Orders:

- List: `GET /api/orders`.
- Detail: `GET /api/orders/admin/{id}`.
- List query only supports `pageNumber`, `pageSize`, `status`, `createdFromUtc`, `createdToUtc`.
- Available `OrderSummaryDto`: `id`, `orderNumber`, `status`, `grandTotal`, `itemCount`, `createdAt`, `paidAt`.
- There is no customer name/ID and free-text search agreement in the current list response. The user has approved the addition of documented search/customer fields to the Orders list by the backend before Phase 1. These columns and filters will not be applied until the API/OpenAPI and endpoint documentation is updated; The detail N+1 call is not made.
- Sort or payment filter is still not documented; Additionally, it will not be displayed on the UI until an approved API contract is created.
- Shows detail, immutable item and shipping address snapshots, payments, totals and lifecycle timestamps as they come from the API.
- Phase 1 default is list, filter, pagination, status display, detail, loading/empty/error state. When status mutation is also requested, the exact transition rules are validated again.
- Generic status endpoint does not set refund or return statuses; These are dedicated workflows. Unsupported action is not added.
- `/api/orders/import` and `/api/orders/import/bulk`, which are in the source code but not in docs/OpenAPI, are not considered frontend or marketplace contracts and are not used.

## 13. Accounting feature rules

Accounting is a separate and advanced module in the backend; The frontend is outside Phase 1. The sidebar area is reserved, but the page/route/API integration is not created without an explicit request.

Domain distinctions:

- `CurrentAccount`: Accounting customer/supplier master record. Its type can be Customer, Supplier or CustomerAndSupplier. Accounting address fields are directly on CurrentAccount in the current model; No separate Supplier or CurrentAccountAddress is created.
- `PurchaseInvoice`: allocates appropriate positive Purchase `StockMovement` quantities previously created. Posting never creates a new physical StockMovement; Supplier debt and FIFO can create cost layers.
- `AccountingSalesOrder`: Does not require `UserId`, Cart or e-commerce Order. It uses the given `ProductVariant` lines directly. Posting creates a single customer receivable if there is `AccountingSale` stock-out, FIFO consumption and positive total in the existing StockMovement infrastructure.
- `SalesInvoice`: Optional document linked to AccountingSalesOrder. It does not create a second stock movement or second receivable.
- `StockMovement`: single physical stock ledger. ProductVariant stock is just its transactionally updated read cache.
- `InventoryCostLayer`: cost source; It is not a physical stock source. FIFO consumption determines the cost of sales.
- `CurrentAccountTransaction`: It is the constant current ledger transaction for supplier debt, customer receivable, payment and reversal.
- `Payment`: makes the allocation to `CurrentAccountTransactionId`, not SalesInvoice. CustomerCollection requires at least one receivable allocation; If SupplierPayment allocations is empty, it may be unallocated supplier advance.
- Cash/Bank balance is not written directly; Derived from `FinancialTransaction` transactions. In Payment, exactly one cash or bank account is selected.

Lifecycle and UI:

- AccountingSalesOrder, SalesInvoice and PurchaseInvoice: Draft, Posted, Cancelled.
- Only Draft is arranged. After post/cancel/reversal, the detail is read again.
- Canceled/reversed record is not deleted; The original and reversal history is shown.
- `409 conflict` and `concurrency_conflict` preserve the draft, refresh the current state and request user decision.
- If the retry is the same user intent, the same Idempotency-Key is preserved.
- TRY and exchange rate `1` is the current Accounting agreement.
- API-calculated VAT, discount, totals, paid/remaining, FIFO cost, valuation, balance and profit is the definitive authority.
- Although accounting reports use the common `AccountingReportRowDto`, the meaning of `amount/secondaryAmount/tertiaryAmount` varies depending on the report. Each report has its own column map; A single generic finance table is not made.
- There is no grand total/transferring balance agreement in the report response. Current page rows are not presented as a grand total.

Undocumented accounting properties are not created: opening current/cash/bank balance, sales/purchase return invoice, debit-credit/FX difference note, general expense update/post/cancel/delete, attachment/archive, financial period/closing, report export/print, bank reconciliation/import, check/promissory note, granular non-admin accounting role or external e-invoice/ERP/marketplace integration.

## 14. Marketplace integration status

Marketplace integrations have not been implemented yet. A system similar to PreMarket is a long-term plan.

- Trendyol, Hepsiburada, Amazon, Shopify, PrestaShop or any other provider is not considered affiliated.
- No fake connection, sync status, product/order mapping, webhook, error log or marketplace data is created.
- The presence of `MarketplaceCommission` in the accounting enum does not mean that there is a marketplace connection.
- Generic order import/performance metric operations in the source code are not documented in the provider adapter or synchronization module.
- Sidebar group remains future/disabled; Route and page are not created.
- The provider-adapter structure will be used when the backend contract is approved in the future. Connection secrets, marketplace products/orders/stocks/prices and sync logs are based on separate backend contracts.

## 15. Performance rules

- Measure first, optimize later. A Development server or a single Lighthouse score is not proof of Core Web Vitals.
- Page/layout Server Component, client leaf is kept small. Global provider is not added for route-local state.
- Initial data client is not captured with `useEffect` and waterfall is not created.
- Independent server requests are started together and `Promise.all`/Suspense is used where appropriate.
- List endpoint and server pagination are used; The detail graph or the entire dataset is not retrieved.
- Large client-side sort/filter/aggregate is not done; Supported server query is used.
- Admin/accounting/auth/order data does not enter the shared cache. In admin, freshness comes before public synthetic score improvement.
- The current Next config does not open `cacheComponents`. This feature is not enabled without measurement and explicit consent.
- Storefront catalog cache is designed with explicit freshness, tags and narrow invalidation in the future.
- `next/image`, fixed dimension/aspect ratio and correct `sizes` are used in product/content images. Not every grid image is made eager/priority except the actual LCP image.
- `next/font` and single font family are preserved; No unnecessary weight/font/icon set is added.
- Dynamic import is only used if it provides measurable benefit in a truly non-critical heavy interactive module; main content/LCP is not hidden.
- Pagination is the default. Virtualization is only used when a truly large interactive dataset is measured and accessibility can be maintained.
- Loading skeleton preserves final geometry; It does not produce excessive shimmer and layout shift.
- For Storefront, LCP, INP and CLS field data are evaluated with the 75th percentile mobile/desktop distinction. Lighthouse lab is diagnostic; field is not pass/fail.

## 16. SEO rules

### Admin

- All authenticated Admin routes (`/dashboard`, `/orders/**`, `/products/**` and future operation routes), `/login` and internal operation routes in this application become `noindex`; It does not enter the sitemap.
- Auth provides privacy; robots/noindex alone does not provide privacy.
- Product/ProductGroup/Breadcrumb rich result markup is not added to admin pages.

### Future storefront

- In the separate Storefront application, the root `metadataBase`, title template, default description and Open Graph defaults are defined with the centrally configured application name; The temporary 'SERANTIS' name or the unconfirmed domain is not hard-coded like a permanent value.
- Dynamic product/collection route generates/deduplicates `generateMetadata` and page data with the same authoritative fetch.
- Title, description, visible `h1`, canonical and Open Graph have the same page intent.
- Canonical absolute HTTPS and tracking/sort/view without parameters.
- Home, useful populated collection/category landing and active public product can be indexed.
- Internal search and arbitrary low-value filters default `noindex, follow`; tracking/sort duplicates clean The URL becomes canonical.
- Pagination has its own address; Not every page is canonical to page 1.
- `sitemap.ts` only contains canonical, indexable, `200` URLs and actual `lastModified` values.
- `robots.ts` crawl control is the metadata index decision. The public URL that `noindex` wants to be seen is not blindly blocked by robots.
- Product JSON-LD only visible with actual product data; If variant family is really suitable ProductGroup; Breadcrumb is created with a visible real hierarchy.
- Rating, review, GTIN, discount, shipping, returns or stock are not fabricated. JSON-LD escapes the `<` character and is included in the initial server HTML.
- SEO success cannot be claimed without verifying Core Web Vitals and rendered HTML in a production-like environment.

## 17. Mobile-first rules

- The layout is first designed with narrow mobile width, then desktop density is added.
- Sidebar becomes an accessible drawer on mobile; When the background is inert, focus is contained and closed, it returns to the trigger.
- Touch targets can be used easily, critical action is not visible with hover alone.
- Form rail/side columns are stacked in order of decision on mobile; submit and validation remain accessible.
- On mobile, a large table uses condensed row/card, controlled horizontal scroll or column reduction, in order of importance. ID, status and primary action are not lost.
- Filter panel can be opened and closed on mobile; active filters and clear action remain visible.
- Sticky action is used only if it does not cover the content, focused control and virtual keyboard.
- Narrow mobile, wide mobile, necessary tablet and desktop viewports are tested. A desktop screenshot alone is not proof of acceptance.

## 18. Accessibility rules

The target is WCAG 2.2 AA.

- Semantic landmarks, single meaning `h1`, regular heading order and skip link are used.
- Button is for action, link is for navigation. Clickable `div` is not used.
- Each input persistent programmatic label gets the required autocomplete/input type/inputMode and associated error message.
- Long form error summary links to invalid fields; safe input is preserved, password/token is not backfilled.
- Focus appears in every interactive element and is not interrupted by overflow.
- Works with menu, dialog, drawer, combobox, tab and table sort state keyboard; focus trap/restore is correct.
- Status, selected, error or success cannot be described with color alone.
- Default, hover, focus, selected, disabled and error contrast are verified.
- Disabled state becomes readable and the reason is explained if it is not clear. Busy state is not indicated by opacity alone; `aria-busy`/suitable announcement is used.
- Async loading, save result and fatal errors are reported with appropriate live region behavior; focus is not moved randomly.
- Reduced motion, 200% zoom and 400% reflow are controlled where appropriate.
- If there is no real screen reader test, it is called “not verified”; The accessibility tree/axe result does not count as a screen-reader pass.

## 19. Visual design rules

SERANTIS Admin character is compact, data-oriented, fast, functional, suitable for desktop operations but mobile usable and less decorative.

- Tailwind v4 You cannot create many pages without creating a small token base with `@theme` and CSS variables.
- Token roles: page, surface, border, foreground, muted, primary action, focus and semantic success/warning/danger/info.
- Blue; Used sparingly for primary action, link, selected nav and focus cue. Not every surface is painted blue.
- Starting dimensions are a convention, not a dogma: page heading 20â€“24px, body/control 14px, desktop control 32â€“40px, mobile target around 44px, table row 48â€“56px, sidebar about 240â€“256px, topbar about 52â€“56px. Verified by real content.
- small spacing scale based on 4px; 6â€“8px radius is used for control, 10â€“12px radius is used for main grouping/overlay. Not every component invents its own radius/shadow value.
- Borders separate calm surfaces; shadow is reserved for menu/popover/drawer/dialog.
- Not every section is included in the card. Heading, divider, whitespace and layout grouping are preferred.
- There is only one primary action in a page or bounded form region. Destructive rejection is used only at the actual decision point.
- Status badge is for real semantic status only; Not every label is badge.
- Gradient, glow, glassmorphism, large blur/shadow, giant hero, colorful eyebrow, unnecessary animation and decorative chart are prohibited by default.
- The ready component library is not left with its default appearance; Additionally, new UI libraries cannot be added without approval.
- Dashboard metric is shown only if endpoint, period, filter and scope are verified. Current page total is not a global metric.
- Loading is similar to the final structure; empty state contains short and real next action; error tells what failed and the current recovery; Fake illustration/metric is not used.
- It is tested with long name, missing image, high price, out-of-stock, empty, timeout, long error and dense data situations.
- In case of working with reference, first the baseline and problem report, then the change; Before/after screenshot and re-evaluation are done with the same fixture/viewport/state.
- Visual optimization cannot worsen accessibility, LCP, CLS, INP or bundle cost.

## 20. Test and verification commands

The package manager is pnpm. Workspace-wide validation commands are run from the `UI/` root; If only one application is targeted, the relevant `*:admin` or `*:storefront` script is used:

```powershell
pnpm lint
pnpm typecheck
pnpm test
pnpm build
```

Workspace and the `admin/` and `storefront/` applications each have a `typecheck` script. The current unit runner is Vitest. Playwright Test and ax dependencies are not installed; No new testing framework/library is added without explicit approval.

User `openapi-typescript` has allowed the addition of a suitable form-validation dependency and browser/accessibility testing dependencies. This confirmation is not an instruction to install packages in this AGENTS update. In the relevant implementation task, existing package versions are checked, the smallest required set is selected, installed with pnpm and the lockfile/bundle/maintenance effect is reported. Other new dependencies require separate approval.

Minimum test matrix for Phase 1:

- Login: valid/invalid, validation, protected route, refresh expiry/failure, logout and 401/403 distinction.
- Products: loading, populated, empty, filters, sort, pagination, API failure and Add Product link.
- Add Product: required fields, variant name/value, minimum one variant, duplicate submit, API field/global errors and partial image failure.
- Orders: status/date filters, pagination, detail, 404, empty, API failure; undocumented search is not expected.
- Responsive: sidebar drawer, filters, tables, complex form and overlays at least desktop + mobile.
- Accessibility: keyboard/focus, labels/errors, contrast, open drawer/dialog, reduced motion and screen-reader status.
- Runtime: unexpected console error, page exception, failed request, unexpected 4xx/5xx, redirect/refresh loop, hydration warning and broken image/font.

Test fixture and credential are kept secret; No cookies, tokens, storage state, password, address or payment data are written to the repo. If there is no secure seed/reset agreement in the test environment, shared data will not be deleted; blocker is reported.

## 21. Prohibited actions

- Inventing an undocumented endpoint, DTO, enum, filter, sort, role, transition or workflow.
- Re-applying the backend pricing, stock, discount, tax, accounting or lifecycle rule in the frontend component.
- Modifying API code, migrations, API documentation or Accounting code as a side effect of the frontend task.
- Generating page/data as if there was a Marketplace connection or sync state.
- Disguising fake business metric or placeholder data as real data.
- Adding unnecessary `use client`` to the entire page/layout tree.
- Distributing raw `fetch` calls to visual components or copying endpoint strings.
- Putting the token/secret in `NEXT_PUBLIC_`, browser storage, log, HTML or analytics.
- Adding packages, test frameworks, state libraries, UI kits, fonts or icon sets without approval, other than OpenAPI generation, form validation and browser/accessibility testing tools that the user has explicitly approved.
- Creating a permanent logo or brand palette without approval.
- Make each content card; Using excessive gradient, glass, blur, glow, shadow, radius and animation.
- Considering Mobile as secondary scope to be fixed later.
- Swallowing the error, reporting the critical result only with toast, or losing form input unnecessarily.
- Creating an empty or fake page route to the planned/disabled sidebar element.
- Blindly moving the Storefront SEO/cache rules to admin or the admin no-store rule to the entire storefront.
- Commit, push or deploy without the user explicitly requesting it.

## 22. Definition of done

A frontend slice is OK only if:

1. The relevant Markdown endpoint contract, OpenAPI operation and controller/DTO where necessary have been verified.
2. There is no contract gap or it is reported as a separate blocker and user decision is made.
3. URL/code is in English, UI copy is in agreed language and route file is thin.
4. Server Component default is preserved, client leaf and props are minimum.
5. The API is called from the server-only typed boundary; token and private data do not leak to the client.
6. Relevant loading, empty, validation, permission, not-found, conflict, timeout and unexpected error situations have been implemented.
7. Mobile and desktop layout; keyboard, focus, labels and contrast are verified.
8. No fake metrics/data; backend-calculated values ​​are the authority.
9. Admin route noindex and auth/authorization are controlled.
10. From the root `UI/`, `pnpm lint`, `pnpm typecheck`, `pnpm test`, `pnpm build` is passed or the exact blocker output is reported.
11. If there is runtime access, console/network and related browser flows have been checked; Otherwise, it is written what has not been verified.
12. Diff contains only the desired scope; The new dependency/route/abstraction was not added without justification.

## 23. Phase 1 scope of application

Application order:

1. Update API/OpenAPI/endpoint documentation first: known drift, product list summary + main image and Orders search/customer contracts are completed before frontend integration.
2. Verify BFF qualification via current auth documentation and API-side `AGENTS.md`; If there is a missing contract, report it without making up the auth behavior and stop.
3. Install the required approved OpenAPI generation, form validation and browser/accessibility testing tools within the scope of the relevant implementation.
4. Small provisional Tailwind v4 neutral + moderate blue token foundation, central application config and temporary text wordmark.
5. Protected route gate with `/login`, root redirect agreement and BFF login/refresh/logout.
6. Admin shell: responsive sidebar, topbar, page frame and noindex layout.
7. Dashboard entry: real quick links/operational entry without verified metric.
8. Products list: filter/sort/pagination/summary/main-image and statuses in the updated list contract.
9. Add Product: progressive groups, real variant model, server validation and partial image workflow.
10. Orders list/detail: documented filter/pagination and real summary/detail fields with updated search/customer support.
11. Lint, type-check, unit tests, production build, mobile/accessibility/runtime verification.

Phase 1; It does not include accounting pages, marketplace routes, customers, coupons/campaigns, stock operations, administrators, settings or storefront implementation.

## 24. Future stages

Recommended sequence after the user accepts Phase 1 and the contracts are clarified:

1. Collections/ProductType/Brand/TaxRate management and Product detail/edit/image/variant operations.
2. StockMovement operations, returns and Coupons.
3. Customers and Administrators.
4. Accounting frontend: Current Accounts â†’ purchase/accounting sales documents â†’ invoices â†’ payments/treasury â†’ expenses/costing â†’ report-specific screens.
5. Public storefront: home, collections, product routes, cart and checkout; With the SEO/CWV matrix.
6. Marketplace integrations: only with provider-adapter architecture after provider connection, mapping, sync and log backend contracts are approved.

This order is not automatic authorization. Each new phase requires separate user coverage and contract review.

## 25. Known unknowns and stop conditions

| Topic | Available evidence/contradiction | Compulsive behavior |
| --- | --- | --- |
| OpenAPI update | OpenAPI 159 path/208 schema; There are product SEO/performance and order import operations that are not documented in the source code. | The user chose to have the documentation updated first. Starting frontend contract integration and using source-only endpoint before the update is completed. |
| OpenAPI auth security | Global Bearer security is also applied to public AuthController operations; `security: []` no override. | Keep runtime `[AllowAnonymous]` behavior; Report contract gap. |
| Auth success/error schema | OpenAPI shows missing register/logout/forgot/reset exact success statuses and ProblemDetails error schemas. | Authenticate with Controller + auth docs; Generated error type is fake. |
| Generated endpoint docs | Some files show create/logout status as `200`, array as object and some list responses as empty. | Code generation without cross-checking Controller/OpenAPI/functional docs. |
| Product list DTO | `GET /api/products` uses optimized projection but carries variants/tags with `PagedResult<ProductDto>`; There is no summary/main image contract. | User approved the addition of small admin summary + main image. Do not use until backend and docs are updated; Don't do N+1. |
| Public product cache/scope | It uses the same product list/detail controller public and 30-second output cache; There is no separate endpoint for admin. | Adding aggressive frontend cache without clearing admin freshness and unpublished-data scope with the backend. |
| Product route/SEO | Current frontend `/product/[slug]`, canonical `/products/{slug}`; by-url/seo-index is only in the new source, not in OpenAPI. | Freeze storefront route strategy; Wait for docs update and user decision. |
| Phase 1 root route | Decided: current app is Admin Panel; There is no `/admin` prefix. | `/` redirects to `/login` or `/dashboard` depending on the session status; protected route and login reverse redirect rules are applied. |
| Product images | There is only URL-based ProductImage CRUD; There is no upload/storage contract. | Inventing upload UI/provider; Use URL workflow or request a new contract. |
| Product list thumbnail | Product list DTO image is not returned. | Placeholder does not present as the real product image; Request backend projection decision instead of N+1. |
| Variant docs | The general catalog create example omits the `value` field; The verbose endpoint and resource says `name + value` is mandatory. | Use detailed endpoint/source model; docs report drift. |
| Orders search/customer | Current Admin list only status/date/page filter; There is no customer in summary. | User approved adding search/customer fields to the backend. Don't use it in UI until updated API/docs are released. |
| Dashboard metrics | There is no general dashboard aggregate endpoint. | Showing fake metric/chart; Limit it to entry/quick links until the dashboard contract arrives. |
| Campaigns | Only Coupon API certified; There is no general campaign model. | Campaigns parent planned; Only Coupon capability can be a real page. |
| Accounting Overview | There is no aggregate overview endpoint. | Disabled placeholder; Not turning report lines into fake total cards. |
| Settings | There is no General Settings endpoint; ShippingMethod and TaxRate are separate. | Creating a generic settings page; wait for scope decision. |
| Marketplace | There is no provider connection/sync/log contract and controller. | All marketplace navigation future/disabled; There is no fake data. |
| Accounting historical spec | The old spec allocates the payment to the invoice and excludes some cancellations from the milestone; The current API CurrentAccountTransaction implements allocation and cancel endpoints. | Use current `api-accounting-docs` + controller; Do not consider the historical proposal as a frontend contract. |
| CurrentAccount search | List supports page/pageSize only. | Searchable selector fitting; Request a backend search contract for big data UX. |
| Environment/deployment | There is no final domain/hosting/API hostname and independent deployment details. Same-origin BFF, host-only HttpOnly cookie and separate Admin/Storefront session are provisional approved. | Environment-based, stay host-agnostic; Don't assume final origin/CORS/CSRF/session store decisions. |
| Generated types dependency | `openapi-typescript` is not installed. | User allowed installation; Only add it in the relevant implementation task after OpenAPI is updated. |
| Form validation library | Zod or any other form/validation library is not installed. | User allowed appropriate dependency; Make the smallest choice during implementation according to the actual form need and report the effect. |
| Browser/a11y tests | There is vitest; Playwright Test and axe are not installed. | Allowed user browser/a11y testing dependencies. Destructive/shared-state E2E operation without isolated test data and credential rules. |
| Design foundation | There is no token set, icon standard and logo in the permanent palette. | Provisional neutral + moderate blue token foundation and plain icon approach approved; Don't go beyond the text wordmark and claim a permanent brand. |

### Recorded user decisions

1. Backend OpenAPI and endpoint documentation will be updated before frontend integration.
2. Summary + main image will be added to the Product admin list, search + customer agreements will be added to the Orders list on the backend; The frontend will only use the new documented contract.
3. Allowed `openapi-typescript`, appropriate form validation and browser/accessibility testing dependencies.
4. Planned/future sidebar groups will be pop-up; child elements will be visible but will remain disabled and labeled “Scheduled/Coming Soon” until applied.
5. Provisional neutral + moderate blue token foundation and plain icon approach approved.
6. Same-origin BFF, host-only HttpOnly cookie, separate Admin/Storefront sessions and secure seed/test credential principles were accepted as provisional architecture.
7. `/admin` URL prefix will not be used; `/`, `/login`, `/dashboard`, `/products`, `/products/new` and `/orders` redirect/route agreement approved.

### Still unresolved deployment decisions

- Final commercial product name and permanent brand identity.
- Final domain, root/admin subdomain selection, API hostname and hosting provider.
- Exact deployment format of Admin and Storefront.
- Final BFF session storage mechanism.
- Final cookie scope/SameSite and cross-origin CORS/CSRF details for Production.
- Whether cross-subdomain SSO will be required in the future.
- Production is the first definitive implementation of the administrator bootstrap operation.

These unknowns do not prevent Phase 1 route and UI foundation work. However, if a task requires production security, session persistence, domain/cookie coverage, deployment or permanent brand value, proceed with guesswork; stop at the relevant border and ask for user approval. Applying the relevant frontend areas without the API contracts approved to be updated appearing in the documents.

## Kod yazımı ve revizyon disiplini

- Kod değişikliğini tamamladıktan sonra yeniden inceleme yap; gereksiz tekrarları, kullanılmayan bağımlılıkları, gereksiz soyutlamaları ve okunabilirliği düşüren yapıları düzeltmeden işi tamamlanmış sayma.
- Kod kalabalığından kaçın: ihtiyacı karşılayan en küçük, açık ve yerel çözümü tercih et; tek kullanımlık yardımcı, gereksiz dosya, belirsiz genel amaçlı utility, tekrar eden koşul ve gereksiz katman ekleme.
- Eklediğin veya değiştirdiğin her anlamlı kod bloğunun üstüne Türkçe yorum satırı ekle. Yorumları birinci şahıs anlatımıyla yaz, ancak `Ben` kelimesiyle başlatma; örneğin: `// Burada form verisini doğruluyorum.`
# Storefront Guest Checkout and BFF Agreement

- Storefront supports anonymous checkout; public/default User is not sent and no account is created. Guest checkout uses `POST /api/cart/checkout/guest`, member checkout uses `POST /api/orders`.
- Active cargo method is mandatory for both checkouts. The UI does not send shipping, price, tax, discount, stock, total or UserId to the API.
- Browser performs guest cart/order operations via the same-origin Next.js Route Handler. BFF only moves the allowlisted guest cookie/header values ​​upstream and rewrites the 'Set-Cookie' values ​​with safe options under storefront origin.
- Exposing `ecommerce_guest_cart`, `ecommerce_guest_orders` and `ecommerce_guest_csrf` values ​​to Client Component, localStorage, DOM, props, log or analytics.
- BFF verifies origin before mutation; It reads the CSRF value from the server-side cookie and adds it to the API with `X-Guest-CSRF`. Move the magic-link token from the URL fragment to the BFF exchange body and clear the URL.
- Server Component does not make HTTP calls to its own Route Handler; Directly calls the public server-only API function. Guest cart/order/detail responses are `no-store`.
- The same idempotency key is preserved in the same checkout/payment intent. After `409`, the cart is read again and the new concurrency token is used.
- Show message “This coupon is for members only” for `coupon_members_only`; automatic retry.
- Guest order includes self-service magic-link/session grant and list, detail, payment, cancellation, return/exchange and secure claim flows. Order number and e-mail alone are not authorization.
