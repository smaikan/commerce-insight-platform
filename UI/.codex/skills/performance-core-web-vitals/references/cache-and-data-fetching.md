# Cache and Data Fetching

## Default data policy

| Data | Typical policy |
| --- | --- |
| Public product/category content | Cache with explicit revalidation and tags when freshness allows |
| Price/availability | Short revalidation or dynamic according to business contract |
| Session/profile | Private, no shared cache |
| Cart/checkout/orders/returns | Dynamic/no-store |
| Admin/accounting | Dynamic/no-store |
| Static navigation/config | Long cache with targeted invalidation |

Confirm policy with the API/business contract; this table is not authorization.

## Avoid waterfalls

- Initiate independent requests before awaiting.
- Use `Promise.all` for independent dependencies.
- Keep sequential fetching only when request B needs request A's result.
- Split independent slow sections into nested Server Components with Suspense.
- Do not hide essential page content behind a client effect.

## Avoid duplicate work

- Share one authoritative product fetch between metadata and page when possible.
- Use request-scoped memoization for repeated identical work.
- Use persistent cache only for data safe to share across requests/users.
- Include every identity/locale/currency dimension in cache keys.
- Never cache authenticated responses under a public key.

## Revalidation

- Tag by stable domain identity, not by UI component.
- Invalidate only affected product/category/list tags after mutation.
- Avoid broad `revalidatePath("/")` unless the whole site truly changed.
- Verify post-mutation freshness and stale-data tolerance.

## Diagnose server time

- Measure DNS/TLS, frontend TTFB, API latency, serialization, and dependent calls separately.
- Check payload size and over-fetching.
- Paginate large lists and select only data needed by the route when the API supports it.
- Do not add client caching to mask a slow or duplicated server request.
