# Storefront Application Working Agreement

This file applies to all runs under `UI/storefront/` and collapses the parent contract file `../AGENTS.md` into the Storefront context. Read both for each assignment. The user's current open request is the highest priority; followed by this file, `../AGENTS.md`, the API documentation and the relevant skill. This file `../admin/` does not authorize changes to the API, API documentation or Accounting backend.

## 1. Application ID and status

This package is a standalone Next.js implementation for the future public store and customer experience:

- Package: `ecommerce-storefront`
- Root: `UI/storefront/`
- Next.js `16.2.12`, React `19.2.4`, strict TypeScript, App Router
- Tailwind CSS v4 CSS-first; There is no separate `tailwind.config.*`
- Package manager: pnpm from workspace root
- Unit runner: Vitest
- Development port is `3000` according to the package script; write the port to source code

Common Phase 1 covers only the `../admin/` application. Storefront is installed as a separate application in this workspace, but public feature implementation starts when the user additionally requests it. Storefront development as a side effect of the admin role.

`SERANTIS` is the temporary working name. Text wordmark can be used with the name from the central config; Creating a permanent logo, production domain, palette or brand asset. Storefront may be brand focused, but don't embed the temporary working name into the permanent domain/metadata/asset.

Current `src/app/page.tsx` create-next-app screen; `src/app/product/[slug]`, SEO helpers, `robots.ts` and `sitemap.ts` are an earlier attempt at SEO. These are not proof of a completed store, approved route contract or production SEO.

## 2. Priority of resources

Before applying a Storefront capability, follow this order:

1. User's decision on current and open scope.
2. This file and `../AGENTS.md`.
3. `../docs/api/api-project-docs/openapi-controller-contract.json`: wire schema, required/nullable field and numeric enum.
4. `../docs/api/api-project-docs/08-endpoint-sozleslemeleri/`: exact parameter/request/response of the relevant operation.
5. Workflow documents:
   - `00-general/01-api-rules.md`
   - `01-auth-user/01-auth.md`
   - `02-katalog/01-katalog-endpointleri.md`
   - `03-sepet-siparis/01-sepet.md`
   - `03-sepet-order/02-order-order.md`
   - `04-iade/01-iade-akisi.md`
   - Public/user contracts related to customer/checkout in `05-yonetim/01-address-kargo-tax-kupon.md`
   - `07-partner/01-dto-ve-ui-sozlesmeleri.md`
6. `../../API/AGENTS.md`, controller, DTO, validator, policy and middleware resources when necessary.

Accounting documents are not a Storefront endpoint resource. Storefront displays money/tax/order data from the API but does not generate AccountingSalesOrder, PurchaseInvoice, SalesInvoice, CurrentAccount, FIFO report, cash/bank or accounting navigation.

If OpenAPI and Markdown conflict, report the difference. Using slug/SEO-index, import, marketplace or other operation that is present in the source but not documented. If the backend contract is unclear, do not fit the UI area, enum, endpoint, filter, role or workflow.

## 3. Mandatory skill guidance

Skills are under `../.codex/skills/`. Read completely `SKILL.md` and mandatory references of the skill appropriate to the task:

| Job type | Mandatory skill |
| --- | --- |
| Route/layout, module, fetch/cache/state, Server/Client limit | `nextjs-ecommerce-architecture`; `project-api-map.md` and `architecture-blueprint.md` for concrete decision |
| Public route, metadata, canonical, robots, sitemap, OG, JSON-LD, indexability | `ecommerce-seo-review`; Three SEO reference files |
| API, auth/BFF, cookie, generated types, ProblemDetails | `api-integration-auth`; project contract, OpenAPI, BFF and error/test references |
| Performance, images/fonts/scripts, bundle/cache/CWV | `performance-core-web-vitals`; measurement, runtime, cache and Lighthouse/CWV references |
| Visual design/review/reference | `visual-design-review`; visual language, responsive/states, screenshot workflow and reporting |
| Login, product, cart, checkout, responsive or accessibility test | `testing-accessibility`; project flows, test architecture and accessibility/reporting |

`admin-dashboard-design` is not used for Storefront. Transferring admin density, sidebar, operational table or accounting pattern to the store.

If MCP is available, use Next DevTools for Next.js behavior, Chrome DevTools for rendered/network/performance, Playwright for deterministic streaming/screenshot. MCP does not replace permanent testing and production build; credential, cookie, token, address, payment body or PII are not written to the report.

## 4. Storefront ownership and strict exclusions

Storefront has:

- Public home, catalog/product/collection discovery.
- Product detail, variant selection, images, availability and reviews.
- Guest/authenticated cart.
- Customer auth and account.
- Address, checkout, e-commerce payment and confirmation.
- Customer e-commerce orders and returns.
- Favorites, ratings/reviews and documented engagement behavior.
- Public SEO, structured data, sitemap, robots and Core Web Vitals.

Storefront does not have:

- Admin shell/sidebar/topbar.
- Admin product/order/return/customer/coupon management.
- Inventory/StockMovement operations.
- Accounting and AccountingSalesOrder.
- Marketplace connections/synchronization.
- Admin settings, users/roles or reports.

Storefront resources are not shared directly with Admin. Creating a workspace shared package between two real consumers and without explicit consent. Auth session, operational component and navigation do not have a common state between two apps.

## 5. Route and indexability architecture

Static segment and dynamic parameter names are in English, lowercase `kebab-case`; Turkish UI copy and API-owned slug are separate issues. Don't translate the API slug or invent a second slug.

Recommended application limits, when the relevant capability is explicitly requested:

```text
src/app/
  (store)/
    page.tsx
    products/
      page.tsx
      [productId]/[slug]/page.tsx
    collections/[slug]/page.tsx
    search/page.tsx
  (auth)/
    login/page.tsx
    register/page.tsx
    forgot-password/page.tsx
    reset-password/page.tsx
  (account)/
    account/
      page.tsx
      addresses/page.tsx
      orders/page.tsx
      orders/[orderId]/page.tsx
      returns/page.tsx
  (checkout)/
    cart/page.tsx
    checkout/page.tsx
    checkout/confirmation/[orderId]/page.tsx
  api/auth/                  # gerÃ§ek BFF HTTP sÄ±nÄ±rlarÄ±
  layout.tsx
  robots.ts
  sitemap.ts
  not-found.tsx
```

Just create the folder of the desired slice. The route group does not appear in the URL.

Default index policy:

| Route family | Index | Canonical | Sitemap |
| --- | --- | --- | --- |
| Home | index | self | yes |
| Active/public product | index | current canonical product URL | yes |
| Useful populated collection/category landing | index | self | yes |
| Curated filter landing | only if unique intent/content is approved | self | by strategy |
| Arbitrary filters/facets | `noindex,follow` | according to intent clean/self | no |
| Sort/view/tracking params | duplicate is not indexed | clean equivalent | no |
| Internal search | `noindex,follow` | self/omitted; not used instead of canonical noindex | no |
| Pagination | addressable; page 1 cannot be collectively canonical | self | by strategy |
| Login/account/cart/checkout/confirmation | noindex | self or omitted | no |
| BFF/internal API | noindex | none | no |

The current attempt at `/product/[slug]` is not consistent with the canonical `/products/{slug}`. Works with documented public read product ID; There is no slug-only lookup or SEO index endpoint in the docs. Do not promise a slug-only route until the current contract is in place. If the current documented model must be relied upon, use `/products/[productId]/[slug]`, fetch by ID and redirect the stale slug to the API's current slug; Verify this decision with SEO/contract review during implementation.

## 6. Folder and module ownership

Target direction:

```text
src/
  app/                       # route, layout, metadata, composition
  modules/
    auth/
    catalog/
    product/
    collections/
    cart/
    checkout/
    account/
    orders/
    returns/
    engagement/
  components/
    ui/                      # domain bilmeyen primitives
    storefront/              # gerÃ§ekten route-family ortak compositions
  lib/
    api/
    auth/
    seo/
    formatting/
    validation/
  config/
  generated/api.ts
  test/
```

- Route files params/searchParams, initial authoritative fetch, metadata and module composition.
- Feature business UI, mapper, action, schema, API operation and status label are in the owning module.
- Button/Input/Dialog/Drawer etc. that do not know the domain. `components/ui`; Real Storefront compositions such as product card/grid, header/footer are on the appropriate common Storefront border.
- Product-specific variant/gallery/buy box is not transferred to another module as generic.
- One module does not import the private internals of another. Shared extraction without two consumers.
- Creating obscure `utils`, `helpers`, `services`, huge hook, universal configurable card/grid.

## 7. Server/Client and state limits

- Public page content and initial data Server Component is default; crawlable content does not lag behind client effect.
- `generateMetadata` shares/deduplicates the same authoritative data source with server-side and page fetch.
- `"use client"` only in variant selection, cart controls/drawer, form state, browser API or required interaction leaf.
- Sending the entire product DTO graph to the client; Pass the small serializable model required by the selection.
- Search/filter/sort/page/selected addressable view URL in search params.
- API entity/cart/order server state; variant/cart drawer/form draft is the closest feature-local state.
- Adding cross-route browser-owned global store/context if there is no real need. Redux/Zustand/TanStack Query is not the default.
- Do not make client `useEffect` waterfall for initial data.

## 8. Typed API and error limit

- Single `server-only` typed API client; The equivalent of `INTERNAL_API_BASE_URL` handles origin, path join, headers, JSON, timeout/abort, cache, `204`, ProblemDetails and safe retry behavior.
- Secret/internal origin is not `NEXT_PUBLIC_*`. The equivalent of `STOREFRONT_APP_ORIGIN` comes from the public canonical origin server config; production hostname is not hard-coded.
- Browser-facing auth/cart/checkout requirements are resolved with a controlled BFF Route Handler or appropriate Server Action. Server Component does not do internal HTTP to its Route Handler.
- Mechanical mirroring of backend endpoints. The endpoint constant/typed operation remains in the owning API module.
- Generated types from current OpenAPI into `src/generated/api.ts`; The file is not modified manually. `openapi-typescript`, appropriate validation and Playwright/axe are user approved but are not installed outside of the relevant task.
- `400` field/global validation, `401`, `403`, `404`, `409`, `429`, `500`, timeout/abort and non-JSON failure center are normalized.
- Error safe protects `status`, `code`, `detail`, `errors`, `traceId`, `timestamp`, `Retry-After`; stack/token/secret does not show the URL.
- `409` re-reads the current state; It does not blind retry/overwrite. Non-idempotent mutation can only be retried with documented idempotency and the same key.
- The UI does not calculate price, stock, tax, discount, shipping, total, payment or refund authoritatively.

## 9. Authentication and customer session

Auth endpoints: register, login, refresh, logout, forgot/reset. User endpoints: `/me`, profile/email/password, account closure and session management. Use the relevant endpoint document for exact request/status.

- Access/refresh token is not given to browser JavaScript, localStorage, sessionStorage, browser-readable cookies, client store, props or HTML.
- Storefront tokens are kept in separate HttpOnly host-only cookies/session; It is not shared with the admin session.
- Secure in cookie production, default SameSite=Lax, Path `/`, No domain; backend expiry is used.
- Login server-to-server; refresh rotates two tokens together; retry at most one; Even if the logout upstream fails, the local cookie is cleared.
- `proxy.ts` is for optimistic route UX; Server Action/Route Handler/DAL checks the auth again, API is the final authorization authority.
- `401` and `403` are separate. Safe relative `returnTo`; There is no open redirect.
- Forgot-password does not reveal the existence of the email. Password/token is not backfilled or logged as safe input.
- Guest cart is merged once after login and guest cookie clearing is verified according to API behavior.

The OpenAPI global Bearer definition conflicts with runtime-public auth endpoints; success statuses and ProblemDetails/error schemas are missing. Without correcting this drift, the generated auth contract is not considered an authority on its own.

## 10. Catalog and product contract

- Public product ID `P...`; variant/image UUID.
- Public reads: product list/detail, variants, images, brands, collections, tags, product types and approved reviews.
- Product list only uses documented search/type/brand/status/active/featured/sort/page queries. The fact that the Public UI only displays truly public/active products is verified by the backend scope; indexing admin-only statuses.
- The product price catalog may include VAT; `netPrice` server-derived. UI does not calculate tax/discount/stock.
- `hasVariants` is a persisted request/response boolean and defaults to `false`; a product with more than one variant requires `true`. Variant `name`/option model is based on backend contract; color/size fixed column or free-text inference fitting.
- Product images can be a separate paged API. Grid/detail N+1 waterfall; If there is no list projection image, request a backend contract. Use stable quiet placeholder for missing image.
- Product detail title, description, price, selected variant, availability, gallery and purchase action are based on semantic/server-rendered basis.
- Inactive/missing product gets `notFound()` or correct non-indexable behavior. Out-of-stock closes the invalid purchase action and explains the reason.
- Rating/review only approved visible API data. Review/rating/GTIN/brand/stock is fake.

The current product list summary/main-image contract is missing; The user has approved the backend change. Assuming Storefront grid thumbnail and optimized list model without updating documentation.

## 11. Cart contract

Cart anonymous can be used; authenticated JWT takes precedence over user guest identity. The API generates the `ecommerce_guest_cart` Secure/HttpOnly/SameSite=Lax cookie for the guest.

Endpoints:

- `GET /api/cart`
- `POST /api/cart/items`
- `PUT /api/cart/items/{cartItemId}`
- `DELETE /api/cart/items/{cartItemId}`
- `DELETE /api/cart`
- authenticated `POST /api/cart/merge-guest`

Rules:

- In the first cart create add, the token can be null; Each mutation of the current cart sends the last `concurrencyToken`.
- Authoritative is not sent in Product ID, price, stock and totals request.
- Current cart is read again in `409` stale token; The conflict is explained to the user, it is not overwritten/retryed silently.
- `isAvailable=false` and `priceChanged=true` are explicitly displayed/confirmed before checkout.
- Rapid double activation should not produce duplicate mutation.
- Cart totals and quantity are displayed from the last API response.

Guest cookie topology is now final: browser uses same-origin Route Handler; BFF only moves allowlisted guest cookies/headers upstream and rewrites upstream `Set-Cookie` values ​​under storefront origin with Secure/HttpOnly/SameSite=Lax options. The cookie value is not opened to browser JS.

## Guest checkout and self-service

- Anonymous checkout does not require login. Sending a public/default User or creating a secret account with order information.
- Guest checkout `POST /api/cart/checkout/guest`; It sends mandatory customer name/surname/email/phone, shipping address, active `shippingMethodId`, last cart concurrency token and `Idempotency-Key`. Billing address is optional and if not available, shipping fallback will be applied.
- Checkout to member `POST /api/orders`; Active shipping method with registered shipping address is mandatory.
- Price/tax/discount/shipping fee/stock/total/UserId is the server authority. UI does not put these fields in the request.
- Apply `428 guest_checkout_challenge_required`, `429 guest_checkout_rate_limited`, `503 guest_checkout_protection_unavailable` agreements in guest checkout challenge/limit responses.
- `409 coupon_members_only` message will be 'This coupon is for members only' and there will be no automatic retry.
- Guest order session 7 days; magic-link is for 30 minutes/single use. Works with list/detail/payment/cancellation/refund/exchange grant; different order access is shown as 404.
- In cookie mutations, BFF carries a trusted origin and server-side CSRF header. The magic-link token is exchanged from the fragment to the body; The token URL does not enter query, log, analytics or persistent client state.
- Claim requires both JWT and a verified guest session with the same normalized email. Guest cannot review/rating before the claim.
- Guest cart/order/detail fetches are `no-store`; Server Component does not self-fetch to Route Handler.

## 12. Checkout and e-commerce payment

`POST /api/orders` creates checkout from authenticated current Cart; It is not `AccountingSalesOrder`. Body only uses documented current cart concurrency tokens and optional owned address/coupon/shipping method IDs.

- The cart is read again before checkout.
- Address must belong to the user and be of Shipping type; Request manipulation cannot bypass authorization.
- Cart snapshot, stock-out, order item snapshot, shipping, coupon and metrics are backend transactions.
- Double submit is blocked; The same checkout/payment intent preserves the same idempotency key, the new intent gets a new key.
- Payment create sends only the provider selection and the required `Idempotency-Key`; amount/transaction/status is the API/provider authority.
- `Fake` is not used in provider production. Appearing in the Iyzico/Stripe/PayTR enum does not prove that the production integration is ready; Verify provider enablement contract.
- Pending/Paid/Failed/Cancelled/unknown-timeout situations are honest and recoverable. Back/refresh does not generate second order/payment.
- Success confirmation shows authoritative order number/ID and remains noindex.

## 13. Customer account, orders and returns

Account routes are authenticated and noindex/no-store.

- Profile, email/password, account closure, addresses and sessions only with owner-scoped `/me`/address contracts.
- Customer orders: `/api/orders/mine`, owner detail `/api/orders/{id}`, allowed cancellation and payment operations.
- Generic order status is not a customer action.
- Order immutable item/address snapshots, totals, payments and timestamps appear as coming from the API.
- Customer returns: create, mine, owner detail. Admin approve/reject/receive/complete is not implemented in Storefront.
- Return/refund and Accounting return invoice are not the same thing.
- Order and AccountingSalesOrder are not mixed in any type/route/label/API file.

## 14. Favorites, rating, reviews and activity

- Favorites list/add/remove is the User endpoint; guest behavior is fake.
- Reviews public approved list; create/rating authenticated.
- Selected variant, availability and review form carry accessible names/states.
- `Click`, `AddToCart`, `Purchase` activities are sent only to the documented flow. If Cart/Order already updates the trusted counter, do not send the same event twice.
- Admin-only approval and metrics are not placed in the Storefront UI.

## 15. Metadata, canonical and Open Graph

- Root `metadataBase`, title template, default description, site name and OG defaults come from central `STOREFRONT_APP_ORIGIN` + configurable app name.
- Do not hard-code `serantis.com` or localhost canonically before the production domain is finalized.
- Static route `metadata` uses data-dependent route `generateMetadata`.
- Title, description, visible h1, canonical and OG describe the same page intent; dynamic pages duplicate default does not receive metadata.
- Canonical absolute, normalized, production HTTPS and tracking/sort/view are parameterless.
- OG title/description/url/type/locale/site name/image dimensions/alt becomes true and absolute.
- Missing/inactive/private product is not indexed; Producing fake metadata/OG image.
- Metadata and page request-scoped deduplicate the same authoritative fetch.

Character-count alone is not a pass/fail; uniqueness, intent, truncation, stuffing and truthfulness are examined.

## 16. Sitemap, robots and structured data

`src/app/sitemap.ts` contains only canonical, indexable, `200` expected URLs and actual `lastModified`. There is no search, filter duplicates, auth, account, cart, checkout, admin/internal routes, redirects or inactive products. There are no hard-coded demo products. Large sitemap can be split according to limits if necessary.

`robots.ts` is the crawl control; It is the route metadata/index headers index decision. `noindex` blindly blocking a must-see public page. Breaking CSS/JS/image crawling. Auth protects private data, not robots.

JSON-LD initial server is serialized in HTML with visible authoritative data and `<` escaped:

- `Product`: real single purchasable product/selected variant.
- `ProductGroup`: only if the API can actually express the relationship between variant family and `variesBy`; color/size is not subtracted from free text.
- `BreadcrumbList`: visible hierarchy and canonical URLs; A filesystem or non-existent category relationship is not created.
- Offer price/currency/availability/condition matches the actual selected variant.
- Rating, review, GTIN, MPN, discount, shipping, returns, stock or price-valid-until are not fabricated.

Schema.org syntax and Google Rich Results eligibility are verified separately. The presence of JSON-LD does not guarantee a ranking/rich result.

## 17. Cache, fetching and Core Web Vitals

Freshness matrix:

| Data | Default |
| --- | --- |
| Public catalog/content | if business freshness allows explicit revalidation + stable domain tags |
| Price/availability | short revalidation or dynamic according to contract |
| Static navigation/config | long cache + narrow invalidation |
| Session/profile | private/no-store |
| Cart/checkout/orders/returns | dynamic/no-store |

- Contains cache key identity/locale/currency dimensions. Authenticated data is not cached under the public key.
- Same product fetch memoize/deduplicate between metadata/page/child; There are no duplicate API calls.
- Independent server requests together/`Promise.all`; dependent request order is preserved. Slow independent section can be streamed with Suspense.
- After the mutation, only the relevant product/collection/list tag/path is invalidated; broad `/` invalidation is not the default.
- Initial crawlable content does not fall behind client effect/dynamic import.
- Page/layout Server Component, client leaf small; There is no global provider and broad renderer.
- `next/image`, stable dimensions/aspect ratio, correct `sizes`, modern source. Only real above-fold LCP candidate eager/high priority; not grid images.
- `next/font`, required family/weights/subsets and stable fallback. Third-party script route-scoped and least-blocking strategy; chat/review/analytics extras deferred.
- Dynamic import only measured non-critical heavy widget; product core/LCP is not hidden.

Field CWV 75th percentile mobile/desktop separate: LCP â‰¤2.5s, INP â‰¤200ms, CLS â‰¤0.1 good. CrUX/Search Console/RUM field evidence. Lighthouse production lab diagnostic; It is not a single run or dev server pass/fail. Baseline is saved with build, route, viewport, network/CPU, auth/data/cache state and at least a few samples.

## 18. Storefront visual language

The storefront should be spacious, visually focused, brand-focused, sales-driven and mobile-first; It should not turn into landing-page decoration.

- Product photo, product name, price, variant/availability and primary purchase action are the first hierarchy.
- Use calm surfaces, controlled accent, consistent spacing/type/radius/shadow. Inventing a new system for each section/card.
- Only when the card is a selectable/repeatable entity or meaningful interaction surface.
- Do not use gradient, glass, glow, repeated blur/large shadow, badge-above-every-heading, giant hero, fake slogan/stat/testimonial/brand logos and purposeless animation.
- Do not make each page a landing page or a disconnected demo section.
- Primary highlight color controlled; semantic colors only real state. There are no competing accents on a page.
- Leave the ready-made component default without adapting it to product token, language, density and states.
- Mobile composition is not a reduced version of desktop; It flows again with product/action priority.
- Long product/brand/variant name, high/discounted price, missing image, many variants, out-of-stock, empty/error/loading/disabled and dense grids are tested.

In the visual task, first the findings report, then scoped change. Take a desktop + mobile before/after screenshot and examine the visual with the identical fixture/viewport/scale/theme/state. CSS/source inspection alone is not visual verification.

## 19. Mobile-first and accessibility

WCAG 2.2 AA is targeted.

- Semantic landmarks, skip link, single meaning h1, logical headings/read order.
- Button action, link navigation; There is no clickable div.
- Can be used with header/nav/mobile menu, filters, variant selector, quantity, cart drawer, dialog and checkout keyboard.
- Focus visible, logical, trapped/restored only in modal UI; hidden/inert content is not focusable.
- Inputs persistent label, required/optional text+semantics, correct autocomplete/type/inputMode, associated errors and long form error summary.
- Selected variant, availability, price change, cart total, loading/save/payment status are clearly announced to the screen reader.
- Status/error/selection cannot be described with color alone; all states contrast is verified.
- Touch target/spacing is comfortable; It does not cover the sticky cart/checkout action content/focus/virtual keyboard.
- 200% zoom, 400% reflow where appropriate, narrow/wide mobile, tablet-risk and desktop are controlled.
- Reduced motion is preserved; animation is not mandatory for task completion.
- `not verified` if there is no real screen reader; axe/accessibility is not a tree pass.

## 20. Loading, empty, error and unavailable situations

- Reserves loading final geometry and image/product grid aspect ratios; No theatrical skeleton/shimmer.
- Empty state tells what is not there and only offers the real next action; No fake products/illustration/metrics.
- API error gives what failed, safe retry/back path and trace reference if necessary; Maintains valid form/cart state.
- Disabled readable and semantic; If the reason is not obvious, it explains. Busy carries not only opacity but also announcement/state.
- Missing image quiet stable placeholder; decorative alt empty, content image alt meaningful.
- Unavailable/out-of-stock/price-changed are separate cases; purchase incorrectly does not remain active.
- Success is not based on ephemeral toast alone; updated cart/order state appears and is announced with the appropriate live region.

## 21. Testing and verification

At the application root:

```powershell
pnpm lint
pnpm typecheck
pnpm test
pnpm build
```

From Workspace root:

```powershell
pnpm lint:storefront
pnpm typecheck:storefront
pnpm test:storefront
pnpm build:storefront
```

Minimum risk matrix when public implementation starts:

- Auth: register/login invalid/valid, validation, refresh once, logout, safe returnTo, token leakage.
- Catalog: populated/empty/error/pagination/filter; product 200/404/inactive; variant, images, availability, canonical.
- Cart: guest/auth add/update/remove/clear, latest concurrency token, 409 refresh, unavailable/price change, merge once.
- Checkout/payment: auth redirect, address ownership, shipping/coupon, double submit/idempotency, stock/cart conflict, pending/paid/failed/unknown and confirmation.
- Account/orders/returns: owner scope, noindex/no-store, allowed cancel/return only.
- SEO: indexability matrix, rendered metadata/canonical/OG, robots/sitemap, JSON-LD truth/escaping, missing product.
- Responsive/a11y: desktop + mobile projects, keyboard/focus, form errors, open menu/drawer/dialog, ax stable states, screen reader status.
- Runtime: page/console errors, unexpected request failures/4xx/5xx, redirect/refresh loops, hydration, broken images/fonts.

Playwright/axe is not yet installed; The user has allowed the installation. From Credentials secret store/gitignored env; reusable storage state gitignored; test data unique/run-owned; production payment/credential is not used. If there is no safe seed/reset, delete shared data and report to blocker.

Performance/SEO measurement with representative product/category/cart/checkout templates on the production build/server; If there is no field data `not verified`.

## 22. Prohibitions

- Adding Admin/Accounting/marketplace route, navigation, DTO or workflow to Storefront.
- Undocumented slug lookup, SEO index, filter, enum, field, payment behavior or provider capability fabrication.
- Rewriting API business rule/totals/stock/tax/discount/payment/refund logic in browser.
- Do not put token/secret/private origin in client, public env, storage, HTML, log or analytics.
- Do not put authenticated/account/cart/order data in shared cache.
- Copying product/cart/order DTOs to the global client store; Fetching initial data with client effect.
- Generating fake rating/review/stock/discount/shipping/return policy/GTIN/metric/testimonial/product or marketplace data.
- Indexing search/filter pages in bulk or canonicalizing the entire pagination to page 1.
- Preventing noindex from being seen with `robots.ts`; Don't trust robots instead of auth.
- Adding Product JSON-LD to the list/search page or creating different markups with visible data.
- Adding package/UI kit/state library/font/icon/analytics/script without approval. Do not install even pre-approved test/type/validation tools off-task.
- Every-section card, excessive gradient/glass/glow/blur/shadow/radius, huge hero or purposeless animation.
- modify `../admin`, API/docs/migration; Do not commit/push/deploy without user request.

## 23. Definition of done

A Storefront slice is OK only if:

1. Verified relevant OpenAPI, endpoint Markdown, workflow docs and controller/DTO where necessary; The contract gap was not made up.
2. URL/code is English, API-owned slug is protected, route file is slim and Server Component is default.
3. Public initial content server-rendered; client boundary/props minimum.
4. API typed server-only border; auth/cart topology secure; There is no token/private data leak.
5. Indexability, canonical, sitemap, robots, metadata and structured-data decisions are consistent on a route family basis.
6. Backend price/stock/tax/discount/total/payment/order authority is protected.
7. Loading/empty/error/disabled/missing/unavailable/conflict states available and accessible.
8. Mobile + desktop, keyboard/focus/forms/contrast and real-data stress states are verified.
9. The relevant lint/typecheck/test/build is passed or the exact blocker is reported.
10. If there is runtime access, rendered HTML, console/network and critical journey were checked; Here is the visual before/after screenshot reviewed.
11. There is only evidence level claim for SEO/CWV/rich-result/ranking; If there is no field/deployment evidence, `not verified`.
12. Diff only in `storefront/` and within the desired scope; admin/shared/API no side effects.

## 24. Known clearances and stopping conditions

- Storefront implementation is not yet within the scope of active Phase 1; Explicit user request required.
- There is OpenAPI public auth security, success status, errors and ProblemDetails schema drift.
- Product slug-only lookup and authoritative SEO-index enumeration are not documented.
- The current product list summary/main image contract is missing; The user has approved adding the backend, but the docs must be updated.
- Storefront public/active-only catalog scope and price/availability freshness must be verified with the exact backend contract.
- The exact propagation/session model of the same-origin Next.js BFF topology with the guest cart API cookie is not documented.
- Production payment providers, webhook/reconciliation and safe sandbox behavior cannot be extracted from the enum alone.
- Final commercial name, domain, hosting, deployment, session storage, cookie/CORS/CSRF and cross-subdomain SSO decisions are not final.
- There are no product upload/CDN/storage, marketplace, external e-invoice and return-accounting automation contracts.
- `openapi-typescript`, validation, Playwright and ax are approved but not installed.

The missing contract only stops the relevant feature. Do not guess without exact operation/field/topology; Report which doc/API changes are required and stop at that limit. If the work is unrelated, safe and clearly requested, you can continue in scope.

## Kod yazımı ve revizyon disiplini

- Kod değişikliğini tamamladıktan sonra yeniden inceleme yap; gereksiz tekrarları, kullanılmayan bağımlılıkları, gereksiz soyutlamaları ve okunabilirliği düşüren yapıları düzeltmeden işi tamamlanmış sayma.
- Kod kalabalığından kaçın: ihtiyacı karşılayan en küçük, açık ve yerel çözümü tercih et; tek kullanımlık yardımcı, gereksiz dosya, belirsiz genel amaçlı utility, tekrar eden koşul ve gereksiz katman ekleme.
- Eklediğin veya değiştirdiğin her anlamlı kod bloğunun üstüne Türkçe yorum satırı ekle. Yorumları birinci şahıs anlatımıyla yaz, ancak `Ben` kelimesiyle başlatma; örneğin: `// Burada form verisini doğruluyorum.`
