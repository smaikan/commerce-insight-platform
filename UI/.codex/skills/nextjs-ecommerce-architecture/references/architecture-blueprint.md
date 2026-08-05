# Architecture Blueprint

## Contents

- Target structure
- Route groups
- Module shape
- Data and cache matrix
- API layer
- Authentication flow
- State ownership
- Error boundaries
- SEO placement

## Target structure

```text
src/
├── app/
│   ├── (auth)/login/
│   ├── (store)/
│   ├── (account)/account/
│   ├── (admin)/admin/
│   ├── api/auth/
│   ├── layout.tsx
│   ├── error.tsx
│   ├── not-found.tsx
│   ├── robots.ts
│   └── sitemap.ts
├── modules/
│   ├── auth/
│   ├── catalog/
│   ├── cart/
│   ├── orders/
│   ├── returns/
│   ├── admin/
│   └── accounting/
├── components/
│   ├── ui/
│   └── layout/
├── lib/
│   ├── api/
│   ├── auth/
│   ├── formatting/
│   └── validation/
├── generated/
├── config/
└── test/
```

Create only directories needed by the current slice.

## Route groups

Use English, lowercase `kebab-case` for every static URL segment. Keep route-group folders and dynamic parameter names in English. UI copy may remain Turkish because presentation language and URL vocabulary are separate concerns.

| Group | URLs | Layout/policy |
| --- | --- | --- |
| `(store)` | `/`, `/products`, `/products/[productId]/[slug]`, `/collections/[slug]` | Public, indexable |
| `(auth)` | `/login`, `/register`, `/forgot-password`, `/reset-password` | Public, noindex |
| `(account)` | `/account`, `/account/addresses`, `/account/orders`, `/account/returns` | Authenticated, noindex |
| `(admin)` | `/admin/**` | Admin where required, noindex |

Recommended accounting routes:

- `/admin/accounting/current-accounts`
- `/admin/accounting/sales-orders`
- `/admin/accounting/sales-invoices`
- `/admin/accounting/purchase-invoices`
- `/admin/accounting/expenses`
- `/admin/accounting/payments`
- `/admin/accounting/treasury`
- `/admin/accounting/costing`
- `/admin/accounting/reports/[report]`

## Module shape

```text
modules/accounting/payments/
├── api.ts
├── actions.ts
├── schemas.ts
├── types.ts
└── components/
```

Omit files that the feature does not need. Keep route `page.tsx` responsible for parsing params, fetching initial data, and composing module components.

## Data and cache matrix

| Data | Fetch location | Cache |
| --- | --- | --- |
| Public catalog/list/detail | Server Component/data function | Revalidate with feature tags |
| SEO metadata | Server-side `generateMetadata` | Share/deduplicate the page fetch |
| Session/user/account | Server-only DAL | `no-store` |
| Cart/checkout/orders/returns | Server-side API client | `no-store` |
| Admin/accounting | Server-side API client | `no-store` |
| Mutations | Server Action | Invalidate precise tag/path after success |

Use request memoization for duplicate work in one render. Do not cache authenticated data across users.

## API layer

```text
lib/api/
├── client.ts       # server-only fetch wrapper
├── problem.ts      # ProblemDetails parsing
└── pagination.ts   # shared query/result helpers
```

- Keep `API_BASE_URL` server-only.
- Generate wire types from `docs/api/api-project-docs/openapi-controller-contract.json`.
- Wrap generated types with local aliases only when this improves readability.
- Use Zod for form/input validation, not as a hand-written duplicate of every response DTO.

## Authentication flow

1. Submit login credentials to a Server Action or auth Route Handler.
2. Call the backend auth endpoint server-to-server.
3. Store access and refresh tokens in separate HttpOnly cookies using backend expiry values.
4. Let `proxy.ts` perform only cheap presence/expiry routing. Do not trust it as the authorization boundary.
5. If access is expired and refresh exists during navigation, redirect to an auth refresh Route Handler with an allowlisted relative `returnTo`.
6. Rotate both cookies from the refresh response and redirect back.
7. In a Server Action, refresh and retry at most once after `401`.
8. Re-check authentication/role in the server-only DAL or action; rely on backend authorization for the final decision.
9. Clear both cookies when refresh fails or logout succeeds.

## State ownership

1. URL: filters, query, pagination, tab with shareable meaning.
2. Server/API: entities, lists, balances, lifecycle state.
3. Form-local: draft input, invoice lines, dialog state.
4. Context/store only when browser-owned state genuinely spans unrelated routes.

Start without Redux, Zustand, or TanStack Query. Add one only after a concrete requirement cannot be handled cleanly by Server Components, Server Actions, URL state, or local form state.

## Error boundaries

- Root `error.tsx`: unexpected application failure.
- Feature `error.tsx`: recoverable route-segment failure.
- `loading.tsx`: meaningful skeleton matching the final layout.
- `not-found.tsx`: missing public/detail resource.
- Inline form result: validation and business-rule errors.
- Toast: completed background mutation or non-blocking feedback.

## SEO placement

- Root defaults: `src/app/layout.tsx`.
- Dynamic page metadata: the public route's `page.tsx`.
- Robots: `src/app/robots.ts`.
- Sitemap: `src/app/sitemap.ts`.
- Manifest/icon/Open Graph files: `src/app` file conventions or `public` when static.
- Structured data: server-rendered in the owning public page.

The product API reads detail by public product ID while the DTO carries a URL slug. Use `/products/[productId]/[slug]`, fetch by ID, and redirect stale slugs to the canonical ID-plus-current-slug URL. Treat the API-provided slug as content data: preserve it rather than translating or inventing a second slug.
## Guest commerce sınırı

Guest cart, checkout ve order self-service için browser yalnız same-origin BFF Route Handler kullanır. Bu handler'lar auth token BFF'sinden ayrı route gruplarında tutulabilir ancak aynı server-only API istemcisini ve ProblemDetails mapper'ını paylaşır. Cookie/header allowlist kullanılır; guest sırları client component sınırını geçmez. Server-rendered page verisi Route Handler self-fetch yerine doğrudan server-only servisle alınır ve `no-store` işaretlenir.
