# Admin Application Working Agreement

This file applies to all work under `UI/admin/` and collapses the parent contract file `../AGENTS.md`. For each task, read both files first. The user's current open request is the highest priority; followed by this file, `../AGENTS.md`, documented API contracts and associated skill instructions. This file does not authorize changes to the API, API documentation, Accounting backend, or `../storefront/`.

## 1. Application ID and current status

This package is a standalone Admin Panel application:

- Package: `ecommerce-admin`
- Root: `UI/admin/`
- Next.js `16.2.12`, React `19.2.4`, strict TypeScript, App Router
- Tailwind CSS v4 CSS-first; There is no separate `tailwind.config.*`
- Package manager: pnpm from workspace root
- Unit runner: Vitest
- Development port: `3001` according to package script; writing the port into application code

`SERANTIS` is only a temporary working name. Use plain text wordmark with name from central application config; Creating a permanent logo, domain, palette or brand asset. Neutral finishes and sober blue accents are an approved temporary direction, not a permanent brand decision.

The existing `src/app/page.tsx`, `src/app/product/[slug]`, `sitemap.ts`, product SEO aids and create-next-app assets are starter/test remnants copied from Storefront. Do not consider these as completed Admin architecture or business contracts that need to be protected. When the application task comes up, just change it to the desired scope; This AGENTS task does not change application code.

## 2. Priority of resources

Before adding a route, field, filter, enum, action or workflow, follow this order:

1. User's current coverage decision.
2. This is `AGENTS.md` and `../AGENTS.md`.
3. `../docs/api/api-project-docs/openapi-controller-contract.json`: wire schema, required/nullable field and numeric enum contract.
4. `../docs/api/api-project-docs/08-endpoint-sozlesmeleri/`: request, response and parameter agreement of the relevant operation.
5. Related workflow document: `00-public`, auth, catalog, cart/order, returns, management/inventory and common DTO/UI documents.
6. **All nine topic documents and README** under `../docs/api/api-accounting-docs/` for the Accounting job.
7. Runtime/application behavior `../../API/AGENTS.md`, controller, DTO, validator, policy and middleware resources when necessary.

OpenAPI wire format; It defines Markdown workflow and UI behavior. If source, OpenAPI and Markdown conflict, report the conflict at the file/operation level. Do not accept ready-made frontend contracts for endpoints that appear in the source but are not documented. Don't guess the missing contract.

The Product list `summary`/`mainImage` and Orders `search`/`customerName` contracts are now published in the backend, OpenAPI and endpoint documentation. Frontend integration uses only those documented fields and does not add list-row N+1 requests.

## 3. Mandatory skill guidance

Skills are under `../.codex/skills/`. Classify the job, read completely the `SKILL.md` file of the selected skill and the mandatory references for the job:

| Job type | Mandatory skill and reference |
| --- | --- |
| Route, layout, module, state, cache, Server/Client boundary | `nextjs-ecommerce-architecture`; at least `project-api-map.md`, for concrete structure `architecture-blueprint.md`, for Accounting also `accounting-rules.md` |
| Admin shell, sidebar, table, filter, form, dashboard, drawer/dialog | `admin-dashboard-design`; all relevant admin reference files and Shopify reference analysis |
| API, generated types, auth, BFF, cookie, action/handler, error | `api-integration-auth`; `project-contract.md`, `openapi-types.md`, `bff-auth-flow.md`, `errors-and-tests.md` |
| Visual change or reference comparison | `visual-design-review`; visual language, responsive/states, screenshot workflow and reporting |
| Bundle, cache, fetch, image/font/script, CWV | `performance-core-web-vitals`; relevant measurement/runtime/cache/CWV references |
| Test, responsive, keyboard, form or accessibility | `testing-accessibility`; test architecture and accessibility/reporting; project flows |
| Admin indexability/noindex control | `ecommerce-seo-review` only to verify the admin noindex/robots distinction; Applying Storefront metadata/JSON-LD patterns to Admin |

The generic `/admin/**` route examples in the `admin-dashboard-design` skill are invalid in this project. The `/admin` URL prefix is ​​not used due to separate Admin application decision.

If MCP tools are available, Next DevTools, Chrome DevTools and Playwright are used in the order specified in the common contract. MCP output does not replace source, persistent test, lint, typecheck and build. Tokens, cookies, credentials, addresses, payment or personal data are not written to the output.

## 4. Phase 1 scope

The only Admin slices now approved for implementation are:

1. Required login basis and protected route gate.
2. Admin application shell.
3. Responsive sidebar and topbar.
4. Dashboard home page; Without verified metric.
5. E-commerce Orders list and detail basis.
6. Products list.
7. Add Product flow.

Accounting, marketplace integrations, inventory, customers, campaigns/coupons, reports, administrators and settings sidebar appear in the right place in the information architecture; Phase 1 is not considered a completed page. Planned/future groups can be drop-down, showing child items; Until the route is applied, the item remains disabled, does not navigate, and has a 'Planned'/'Coming Soon' description.

## 5. Route agreement

Route segments and dynamic parameter names are in English, lowercase and `kebab-case`; UI text may be Turkish. The route group does not appear in the URL.

Phase 1 URLs:

- `/login`
- `/dashboard`
- `/orders`
- `/orders/[orderId]`
- `/products`
- `/products/new`

Strict redirect behavior:

- Guest `/` â†’ `/login`
- Authenticated `/` â†’ `/dashboard`
- Guest protected route â†’ `/login`
- Authenticated `/login` â†’ `/dashboard`

The hostname and future admin subdomain decision does not change the route. Creating a dual admin segment like `admin.domain.com/admin/dashboard`. `returnTo` can only be a verified relative same-origin path.

Target route layout:

```text
src/app/
  (auth)/
    login/page.tsx
  (admin)/
    layout.tsx
    dashboard/page.tsx
    orders/page.tsx
    orders/[orderId]/page.tsx
    products/page.tsx
    products/new/page.tsx
  api/auth/                 # yalnÄ±z gerÃ§ek browser-facing BFF sÄ±nÄ±rlarÄ±
  page.tsx                  # session-aware redirect
  layout.tsx
```

Create only the folders necessary for the active slice. Creating an empty page/route for the Planned sidebar element.

## 6. Code ownership and folder architecture

Target direction:

```text
src/
  app/                      # routing, metadata, composition
  modules/
    admin-shell/
    auth/
    dashboard/
    orders/
    products/
    accounting/             # yalnÄ±z aÃ§Ä±k Accounting gÃ¶revi gelince
  components/ui/            # domain bilmeyen primitives
  lib/
    api/
    auth/
    formatting/
    validation/
  config/
  generated/api.ts          # OpenAPI dÃ¼zeltildikten sonra generated
  test/
```

- `page.tsx`, `layout.tsx`, `loading.tsx`, `error.tsx`, `not-found.tsx` remain thin: params/searchParams, initial fetch and feature composition.
- Business UI, status label, mapper, form schema, action and feature API operation has `src/modules/<feature>`.
- `components/ui` is for primitives that do not know domains such as Button/Input/Select/Checkbox/Badge/Dialog/Drawer/Menu.
- `modules/admin-shell` contains shell, sidebar, topbar, page header, navigation and common admin compositions.
- One module does not import another's private component/action/API file.
- If there are not two real consumers, do not remove the code to the shared area. Direct source import or establishing a symlink between Admin and Storefront; common package only consists of actual need and separate confirmation.
- Creating universal components that manage large `utils.ts`, obscure `services`, unnecessary barrel and each feature with props.

## 7. Server Component, state and form limits

- Page/layout and initial fetch Server Component are default.
- `use client`` only occurs in the smallest leaf containing event handler, browser API, drawer/dialog, form field array, interactive table selection or the required optimistic UI.
- Pass small serializable props to the client; Migrating token, private API origin or entire DTO graph.
- Search, filter, sort, pagination and shareable tab URL are kept in search params; When the filter changes, it becomes `pageNumber=1`.
- Entity/list/lifecycle state is API/server state.
- Draft, overlay, row selection and dirty state are kept feature-local.
- Getting started with Redux/Zustand/TanStack Query. If Server Components, Server Actions, URL and local state prove to be insufficient, request confirmation.
- Long Product and Accounting forms cannot be a single giant Client Component; It is divided into decision groups and narrow interactive leaves.

## 8. API integration

- Single `server-only` typed client; The `INTERNAL_API_BASE_URL` equivalent handles origin, path join, Bearer, JSON, timeout/abort, `204`, ProblemDetails, correlation/idempotency and explicit cache policy.
- The target of the authenticated stream coming from Browser is `Browser â†’ same-origin Next.js BFF â†’ ASP.NET API`.
- Server Component does not make HTTP calls to its own Route Handler; It calls the server-only function directly.
- Route Handler is used only when auth cookie, controlled browser proxy, upload/download, callback/webhook or real HTTP limit is required. Don't mirror every ASP.NET endpoint.
- There is no raw fetch or repetitive endpoint string in the visual component.
- Request/response models are generated from current OpenAPI; `src/generated/api.ts` cannot be changed manually. `openapi-typescript`, appropriate form validation and browser/a11y testing tools are user approved, but are only installed in the owning app in the relevant implementation task.
- Numeric enum values ​​come exactly from the wire contract. The string remains in the date wire; It is parsed in owned mapper.
- Parsing JSON without checking response status/content-type; Don't parse `204`.
- `400/401/403/404/409/429/500`, timeout, abort and non-JSON upstream failure center are normalized.
- Validation field error is mapped to the form; global error is preserved. Safe `traceId` can be shown. No stack, upstream secret URL or token is displayed.
- Non-idempotent mutation is not automatically retried without documentation. If the same user intent is retried, the same idempotency key is preserved.
- Price, discount, tax, shipping, stock, balance, paid/remaining, FIFO cost and profit API authority; The frontend alone can explicitly calculate the UX preview.

## 9. Authentication and BFF security

ASP.NET login/refresh response returns access + refresh token and backend expiry. The browser does not manage these tokens.

- Tokens are kept in separate `HttpOnly`, `Secure` in production, `SameSite=Lax`, `Path=/`, `Domain` unspecified host-only cookies.
- Admin and Storefront sessions are separate; There is no Phase 1 cross-subdomain SSO or shared cookies.
- Cookie set/rotate/delete is only done at the cookie-writing server border, such as Server Action/Route Handler.
- Login `/api/auth/login`; refresh `/api/auth/refresh-token`; logout `/api/auth/logout` uses the actual body/status contract.
- Refresh rotates two tokens together. After a successful refresh, the request is retried at most once; parallel refresh race and redirect loop are blocked.
- Logout tries upstream and clears local cookies `finally` even if upstream fails.
- In Next.js 16, `proxy.ts` is used for optimistic route gate; There is no authorization limit. It checks each Server Action, Route Handler and server-side data operation session/role again; ASP.NET is the final authority.
- JWT decode is not proof of authorization. `401` means session loss, `403` means lack of authorization.
- State-changing BFF request requires POST semantics, Origin and Referer/CSRF protection where appropriate.
- Creating fake auth, hard-coded admin account or credential. If the API auth contract does not support BFF, the route/login UI basis can be established; The missing contract is reported and the auth behavior is stopped without being adapted.

Global Bearer security in the auth document conflicts with runtime-public auth operations. OpenAPI also shows missing register `201`, logout/reset `204`, forgot-password `202`, auth error responses and ProblemDetails schema. Do not blindly trust generated auth behavior without fixing this drift.

## 10. Admin shell and information architecture

Shell Server Component remains; Interactions like collapse/drawer/menu become narrow Client Components. Use permanent/removable sidebar on desktop, accessible drawer on mobile. The drawer background becomes inert, focus is contained and returns to the trigger when closed.

Sidebar contract:

| Group | Item | Target | Status |
| --- | --- | --- | --- |
| Overview | Dashboard | `/dashboard` | Phase 1 |
| Commerce | Orders | `/orders` | Phase 1 |
| Commerce | Products | `/products` | Phase 1 |
| Commerce | Add Product | `/products/new` | Phase 1; Secondary under Products |
| Commerce | Collections | `/collections` | Planned, disabled |
| Commerce | Campaigns / Coupons | in the future `/coupons` capability | Planned, disabled; No generic campaign engine |
| Operations | Stock Operations | `/inventory/stock-movements` | Planned, disabled |
| Operations | Customers | `/customers` | Planned, disabled |
| Accounting | Overview, Current Accounts, Purchase Invoices, Accounting Sales Orders, Sales Invoices, Payments and Collections, Cash and Bank, Expenses, Reports | `/accounting/**` targets | Planned, disabled |
| Marketplace Integrations | Overview, Connections, Product Sync, Order Sync | no route | Future, disabled |
| System | Administrators | `/administrators` target | Planned, disabled |
| System | Settings | no route | Placeholder; generic settings no API |

Sidebar is not a controller list. Use a maximum of two visible hierarchy levels. Global search is only shown if there is a real cross-module search contract. The notification icon appears only if there is real data. Hiding the link is not authorization.

## 11. Dashboard rules

`GET /api/dashboard/overview` is an AdminOnly operational-summary endpoint. It returns `totalOrderCount`, `pendingOrderCount`, `paidOrderCount`, `paidRevenue`, `activeProductCount`, `lowStockVariantCount` and `generatedAtUtc`.

- Show only these backend-provided, scoped values and display their generation time where it helps the operator.
- Never present current-page rows as global totals or derive a metric in the browser.
- Do not produce undocumented trends, comparisons or charts.
- Loading, error and unavailable states remain explicit; endpoint scope and unit must be documented before adding any additional metric.

## 12. Products and Add Product

Product ID `P...`; variant/image/relation IDs are UUID. Product list documented filters: page, search, type, brand, collection, tag, status, active, featured, sort and descending. Paging is server-side; The detail/image N+1 call is not made for each line.

`GET /api/products` returns the backend-projected `summary` and `mainImage` fields in `ProductDto`. Use that single row projection for the thumbnail/summary and never make image/detail N+1 requests for the product table.

The Add Product form is progressively divided into:

1. Basic: title, main SKU, URL, description.
2. Organization: ProductType, brand, collections, tags, tax rate.
3. State: numeric ProductStatus, active, featured, display order.
4. Variants: at least one variant; Separate `name` and `value` in the backend model, SKU, price, stock/opening contract, compare-at, barcode, material, active.
5. SEO title/description.
6. Images: URL-based ProductImage operation after the product is created; sub, main, order.

`Category` entity fitting: main classification is `ProductType`, merchandising group is `Collection`. `hasVariants` is a persisted request/response boolean that defaults to `false`; more than one variant requires `true`. `netPrice` is response-only. Stock is not the normal product area; The opening behavior is based on the create contract, the subsequent change is based on the signed `StockMovement`. There is no upload/storage endpoint. Since product create and image operations are separate, false atomic-save promises are not made; Preserve partial success and the resulting product ID.

## 13. E-commerce Orders

`Order` is authenticated customer + Cart checkout aggregate. It is not `AccountingSalesOrder`. Route, type, DTO, label, service and navigation never confuse these concepts.

Phase 1:

- Admin list: `GET /api/orders`.
- Admin detail: `GET /api/orders/admin/{id}`; The `admin` segment in the API path is not the frontend URL prefix.
- Available list filters: page, search, status, createdFromUtc, createdToUtc.
- List/detail; status display; loading, empty, error, permission, 404 and pagination.
- `OrderSummaryDto.customerName` is nullable and the documented `search` parameter searches order number, customer name/surname and email. Render a null-safe customer fallback; do not make detail N+1 calls for list rows.
- Generic status does not set endpoint refund/return workflow statuses. Adding an unsupported transition or operation.
- Detail snapshot items, shipping addresses, payments, totals and lifecycle timestamps are shown as they come from the API.

## 14. Planned admin modules

Real backend capabilities other than Phase 1 may be implemented in the future with separate requests:

- Catalog: collections, brands, product types, tags, product relations/images/variants/status/activation/featured.
- Returns: Pending â†’ Approved/Rejected â†’ Received â†’ Completed documented lifecycle.
- Inventory: signed StockMovement, balance and atomic bulk with maximum 500 lines; workflow-owned movement type cannot be selected manually.
- Customers/Administrators: Users API documented search/role/status; last-active-admin conflict is preserved. Assuming separate generic Customer API.
- Coupons are real capabilities; Generic Campaign model, metric or campaign type fitting.
- ShippingMethod and TaxRate separate capabilities; generic Settings page is fake.
- Review approval and product engagement metrics only with documented endpoints.

Even if a planned capability appears in the sidebar, it does not authorize route/page implementation without explicit user scope.

## 15. Accounting domain contract

Accounting is Admin only and is a separate `src/modules/accounting` feature field. A page is not created in Phase 1; sidebar location is preserved. When accounting comes up, all `api-accounting-docs` are read again.

Clear distinctions:

- `CurrentAccount`: customer/supplier/both accounting master record; The address fields are directly on the record. `userId` is optional link.
- `CurrentAccountTransaction`: immutable current ledger transaction; PaymentAllocation targets its ID, not its SalesInvoice.
- `PurchaseInvoice`: does not create physical `StockMovement`. Draft lines are fully allocated to existing positive Purchase movements; post supplier debt and FIFO create CostLayer.
- `AccountingSalesOrder`: independent of e-commerce Order/Cart/User; It uses `CurrentAccountId` and `ProductVariantId` lines directly. Post creates AccountingSale stock-out, FIFO consumption and appropriate customer receivables with the existing StockMovement infrastructure.
- `SalesInvoice`: optional document linked to AccountingSalesOrder; It does not create a second stock or receivable effect. Cancel alone does not create/delete physical stock.
- `StockMovement`: single physical stock ledger.
- `InventoryCostLayer`: cost source/ledger; It is not a physical stock source. The spent past is not changed silently.
- Payment: CustomerCollection requests allocation; SupplierPayment allocations may be empty and unallocated supplier advance. Exactly a cash or bank account is selected.
- Cash/Bank balance is derived from FinancialTransactions; It is not edited directly. Reversal does not erase the past.

AccountingSalesOrder, SalesInvoice and PurchaseInvoice `Draft=1`, `Posted=2`, `Cancelled=3`; Only Draft is edited. After post/cancel/reverse, the detail is read again. Backend total, VAT, discount, paid/remaining, FIFO cost, profit and balance are the authority.

Reports use `PagedResult<AccountingReportRowDto>`, but `amount`, `secondaryAmount`, `tertiaryAmount` have different meaning in each report. Each report has its own column map; universal finance table or current-page grand total is not made. There is no free sort/export, opening balances, return invoices, notes, attachments, periods/closing, bank import/reconciliation, check/promissory note, non-admin role matrix, e-invoice/ERP/marketplace integration.

## 16. Marketplace status

There is no Marketplace backend contract. Trendyol, Hepsiburada, Amazon, Shopify, PrestaShop or PrePazar connections are not considered. Creating fake connection/sync status/log/data. Sidebar remains future/disabled. If the contract is approved in the future, the provider-adapter approach is used; products, orders, stock, price, mapping, secret and sync log are separate backend contracts. `MarketplaceCommission` in the Accounting enum is not link proof.

## 17. Cache and performance

- Admin/auth/order/accounting data does not enter the shared cache; The default is private/`no-store` and freshness.
- Initial data client cannot be pulled with `useEffect` waterfall.
- Independent server requests are started together; dependent requests are not parallelized unnecessarily.
- Use list projection and server pagination; Importing the entire dataset or detail graph.
- Huge client sort/filter/aggregate.
- Keep Client Component and provider limits small; Adding memoization/virtualization without dying.
- Virtualization only if there is a really large dataset and evidence of accessibility; pagination is the default.
- `next/image` stable dimensions/aspect ratio and correct `sizes`; Prioritizing table thumbnails collectively.
- `next/font`, the only family/weight set required; No unauthorized font/icon libraries.
- In performance baseline production build; Lighthouse lab diagnostic is not field CWV evidence. In Admin, interaction latency and data freshness take priority over public SEO score.

## 18. Admin SEO and privacy

- Gets all Admin routes and `/login` `robots: { index: false, follow: false }`; It does not enter the sitemap.
- Auth ensures privacy; robots/noindex is not security.
- Product/ProductGroup/Breadcrumb JSON-LD is not added to Storefront canonical or product sitemap Admin.
- The existing Admin `sitemap.ts` and product SEO files are scaffold residue; This is a known issue that needs to be removed/rearranged during admin implementation.
- Disallow only `/admin` in `robots.ts` does not cover the unprefixed routes of this application. Metadata/header noindex must be explicitly applied for the entire application; The conflict between crawler's ability to see noindex and crawl blocking should be checked.

## 19. Visual system

The admin character is compact, data-oriented, fast, functional, prioritizes desktop operations, but is mobile usable and less decorative.

- With Tailwind v4 `@theme` + CSS variables, page, surface, border, foreground, muted, primary, focus, success/warning/danger/info roles are defined in one place.
- Restrained blue only for primary action, link, selected nav/row and focus cue. Painting every surface blue.
- Starting density: page heading 20â€“24px, body/control 14px, desktop control 32â€“40px, mobile touch about 44px, row 48â€“56px, sidebar 240â€“256px, topbar 52â€“56px; verify with real content.
- 4px spacing scale; control radius 6â€“8px, grouping/overlay 10â€“12px. Pill only has real role like status/tag/filter.
- Border/grouping comes before shadow; shadow is saved for overlay.
- Not every section card. Do not use gradient, glass, glow, large blur/shadow, huge hero, decorative chart, colored eyebrow and unnecessary animation.
- Single primary action in a page/bounded form area. Destructive rejection occurs only at the decision point.
- Status badge is only for semantic status.
- Shell ratio, density, table toolbar and two-column form hierarchy can be adapted from the Shopify reference; No logo, name, icon, color, wording or unsupported feature is copied.

In visual changes, first evidence-based problem report, then change; Desktop + mobile before/after screenshot is visually examined with the same fixture/route/state/viewport. If there is no runtime, report the visual result as `provisional/not verified`.

## 20. Responsive, accessibility and professional situations

WCAG 2.2 AA is targeted.

- Mobile sidebar accessible drawer; background inert, focus trap/restore and Escape behavior are correct.
- Critical action cannot be found with hover alone. Touch targets are easy to use.
- Tables can use priority layout/card/controlled horizontal scroll on mobile; identifier, status and primary action are not lost.
- Complex form rail is stacked with task order on the small screen; sticky save does not cover content/focus/keyboard.
- Semantic landmark, single meaning h1, heading order, skip link, real button/link semantics.
- Each input persistent label gets the correct autocomplete/type/inputMode and associated error. Long form error summary links to invalid fields.
- Focus appears; status/error cannot be described with color alone; default/hover/focus/disabled/error contrast is verified.
- Dialog/drawer/menu/combobox/tab/sort keyboard patterns and focus restoration works.
- Loading preserves final geometry and uses `aria-busy`. Empty: no data and no filter result are different. Error safe protects input/data and provides valid retry. Disabled is readable and justified. Success is not based on temporary toast alone.
- Long Turkish names, high TRY values, many variants, missing images, out-of-stock, empty, timeout, long validation and dense data are tested.
- 200% zoom, 400% reflow and reduced motion are controlled at appropriate places. If there is no real screen reader, it is called `not verified`.

## 21. Testing and verification

At the application root:

```powershell
pnpm lint
pnpm typecheck
pnpm test
pnpm build
```

Equivalents from the Workspace root:

```powershell
pnpm lint:admin
pnpm typecheck:admin
pnpm test:admin
pnpm build:admin
```

Phase 1 risk matrix:

- Auth: valid/invalid, validation, root/protected/login redirect, expiry/refresh once, logout, 401/403, no token leakage.
- Products: loading/populated/empty/filter/sort/pagination/error, list contract, Add Product link.
- Add Product: required, variants name/value, at least one variant, duplicate submit, field/global error, partial image failure.
- Orders: status/date/search/customer only up to current contract, pagination, detail, 404, empty/error; There is no unsupported transition.
- Shell: desktop/mobile sidebar, keyboard/focus, planned disabled items.
- Runtime: console/page error, unexpected request failure/4xx/5xx, hydration, redirect/refresh loop, image/font.

Playwright/axe is not yet installed; The user has allowed the installation. Install the owning app as needed; credential and storage state become gitignored/secret. Integration/E2E isolated testing uses DB/ephemeral environment, runtime-generated credential and run-owned data. Deleting production or shared development data; If there is no secure seed/reset, report blocker.

## 22. Prohibitions

- Do not invent undocumented endpoint, DTO, enum, role, filter, sort, column, transition, metric or workflow.
- Move the Storefront route/component/SEO behavior to Admin or do not touch the `../storefront` code.
- Adding `/admin` frontend prefix.
- Writing API pricing/stock/tax/accounting/lifecycle rules as the second engine in the browser.
- Do not put the token in localStorage, sessionStorage, browser-readable cookie, client state, `NEXT_PUBLIC_*`, props, HTML, log or analytics.
- Producing fake credential, marketplace, dashboard metric, notification, search, bulk action or business data.
- Creating empty route for planned/disabled item.
- Product image upload/storage provider is fake.
- AccountingSalesOrder with Order; Don't mix StockMovement with CostLayer.
- Adding package/UI kit/state library/font/icon set without approval. Even pre-approved tools are not installed outside of the concrete task.
- Adding unnecessary `use client`, global store/provider, raw component fetch, broad cache or N+1 call.
- Don't make everything a card; Using gradient/glass/glow/blur/oversized radius/shadow/animation.
- Do not perform API/doc/migration, commit, push or deploy without the user requesting it.

## 23. Definition of done

An Admin slice is OK only if:

1. Validated relevant OpenAPI, endpoint Markdown, workflow doc and controller/DTO/validator where necessary.
2. Contract gap is clearly reported as no or blocker; The area/action was not made up.
3. URL/code is in English, route file is fine, Server Component default is preserved.
4. API typed server-only border; secret/private data does not leak to the client.
5. Auth/role is verified in each server mutation/read boundary and backend.
6. The relevant ones among loading, empty, validation, permission, not-found, conflict, timeout and unexpected error are ready.
7. Desktop + mobile, keyboard, focus, labels, contrast and stress data verified.
8. Admin noindex; No fake metric/data/marketplace.
9. The relevant lint/typecheck/test/build is passed or the exact blocker is reported.
10. If there is runtime access, console/network and browser flow is verified; Here is the visual before/after screenshot reviewed.
11. Diff only contains the requested scope `admin/`; new abstraction/dependency justification.

## 24. Known clearances and stopping conditions

- OpenAPI auth security, success statuses, error responses and ProblemDetails schema are not up to date.
- There is no product upload/storage contract.
- CurrentAccount does not support list search.
- No generic Settings, generic Campaign and marketplace contract.
- Final domain, host, deployment, BFF session storage, production cookie/CORS/CSRF details and production admin bootstrap are not final.
- `openapi-typescript`, form validation, Playwright and ax are approved but not installed.

These gaps do not completely hinder the shell/token foundation design. However, if the task requires missing contract field, production security/deployment decision or real business data, stop at that limit and report the deficiency with the exact source and expected contract; progress with prediction.

## Kod yazımı ve revizyon disiplini

- Kod değişikliğini tamamladıktan sonra yeniden inceleme yap; gereksiz tekrarları, kullanılmayan bağımlılıkları, gereksiz soyutlamaları ve okunabilirliği düşüren yapıları düzeltmeden işi tamamlanmış sayma.
- Kod kalabalığından kaçın: ihtiyacı karşılayan en küçük, açık ve yerel çözümü tercih et; tek kullanımlık yardımcı, gereksiz dosya, belirsiz genel amaçlı utility, tekrar eden koşul ve gereksiz katman ekleme.
- Eklediğin veya değiştirdiğin her anlamlı kod bloğunun üstüne Türkçe yorum satırı ekle. Yorumları birinci şahıs anlatımıyla yaz, ancak `Ben` kelimesiyle başlatma; örneğin: `// Burada form verisini doğruluyorum.`
# Coupon Membership Scope

- Show `isMemberOnly` field in coupon list, creation and editing models.
- The field is boolean and should default to `false` in the new coupon form.
- `true` means that the coupon will only be used for authenticated member checkout; `false` means that it can be used for guests and members if other eligibility conditions are met.
- Converting this setting to generic campaign type or second availability engine on the frontend; The backend is the ultimate authority.
