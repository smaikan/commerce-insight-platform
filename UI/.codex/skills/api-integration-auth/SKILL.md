---
name: api-integration-auth
description: Build, review, refactor, or diagnose a secure typed integration between a Next.js App Router frontend and an ASP.NET Core Web API. Use for OpenAPI-generated request/response models, server-only API clients, BFF Route Handlers and Server Actions, JWT login/logout/refresh flows, HttpOnly cookies, centralized ProblemDetails errors, authorization boundaries, retries, idempotency, and API contract drift.
---

# API Integration and Auth

Keep the ASP.NET API authoritative for identity, authorization, validation, and business rules. Keep bearer and refresh tokens outside browser JavaScript.

## Ground the integration

1. Locate the Next.js frontend root and ASP.NET/API documentation root.
2. Inspect the installed Next.js version, package manager, current API layer, environment variables, and existing auth code.
3. Read `docs/api/api-project-docs/openapi-controller-contract.json`, the general API rules, auth documentation, and relevant endpoint Markdown contracts.
4. Inspect the actual ASP.NET auth controller, auth DTOs, JWT configuration, policies, and ProblemDetails middleware when source is available.
5. Run the contract audit:

   ```powershell
   node .codex/skills/api-integration-auth/scripts/openapi-contract-audit.mjs ../docs/api/api-project-docs/openapi-controller-contract.json
   ```

6. Read [project-contract.md](references/project-contract.md) for known repository behavior.
7. Read [openapi-types.md](references/openapi-types.md) before generating models.
8. Read [bff-auth-flow.md](references/bff-auth-flow.md) for login, refresh, logout, cookies, and authorization.
9. Read [errors-and-tests.md](references/errors-and-tests.md) for centralized errors and acceptance tests.
10. Verify version-specific Next.js and ASP.NET Core behavior against current official documentation.

When OpenAPI, Markdown, and source disagree, report the contradiction. Use source/controller attributes for actual runtime behavior, OpenAPI for available wire schemas, and Markdown for documented workflow/UI rules until the contract is corrected.

## Use available MCP tools

- Use **Next DevTools MCP** (`next_devtools`) to inspect auth Route Handlers, Server Actions, proxy/runtime behavior, redirects, and Next.js diagnostics in the running application.
- Use **Chrome DevTools MCP** (`chrome_devtools`) to inspect status codes, redirect chains, request timing, cookie attributes, console failures, CSRF behavior, and refresh loops. Never print or persist cookie values, authorization headers, credentials, token bodies, or sensitive response data.
- Use **Playwright MCP** (`playwright`) to reproduce login, logout, protected navigation, refresh rotation, guest-cart merge, parallel request, expiry, and failure flows in an isolated test context. Store no reusable auth state outside an approved gitignored test location.
- MCP inspection supplements committed integration/E2E tests and server-side verification. If a tool is unavailable, use equivalent local tests/browser diagnostics and record the missing evidence.

## Keep a server-only boundary

- Put the backend origin in `API_BASE_URL`; never prefix it with `NEXT_PUBLIC_`.
- Mark token, cookie, and HTTP-client modules with `server-only`.
- Let Server Components call the server-only API client for initial reads.
- Use Server Actions for form mutations when no independent HTTP endpoint is needed.
- Use Route Handlers for browser-facing BFF endpoints, auth cookie writes, callbacks, downloads/uploads, and other real HTTP boundaries.
- Do not mirror every ASP.NET endpoint as a Next.js Route Handler.
- Do not call the application's own Route Handler from a Server Component/Action; call the shared server-only function directly.
- Build backend paths from owned constants/typed operations, not user-provided absolute URLs.
- Forward only allowlisted headers. Never forward browser-supplied authorization, host, or proxy headers blindly.

## Generate and use OpenAPI models

- Generate wire types from the repository's OpenAPI document with `openapi-typescript`.
- Keep generated output in `src/generated/api.ts` and never edit it manually.
- Add generation and `--check` scripts so CI fails on stale output.
- Access request/response types through generated `paths` or `components` instead of rewriting DTOs.
- Create small domain aliases/mappers only when they improve readability or adapt forms.
- Use numeric enum wire values exactly as documented.
- Use Zod or equivalent for user input and selected untrusted runtime boundaries; TypeScript generation does not validate runtime JSON.
- Define a local ProblemDetails type when the current OpenAPI document omits error schemas.
- Do not assume an undocumented error response, filter, status, or nullable field.

## Implement the BFF auth flow

- Store access and refresh tokens in separate `HttpOnly`, `SameSite=Lax`, `Secure` in production cookies with `path: "/"`, no `Domain`, and backend-provided expiry dates.
- Set, rotate, and delete cookies only in Server Actions, Route Handlers, or a carefully bounded proxy response.
- On login, call ASP.NET `/api/auth/login`, store both tokens, and return only safe user/session data to the browser.
- On refresh, send the refresh token in the ASP.NET request body and overwrite both cookies from the returned token pair.
- On logout, call ASP.NET `/api/auth/logout` with the refresh token, then clear both local cookies even if the upstream logout fails.
- Retry at most once after a successful refresh and never create a refresh loop.
- Do not retry a non-idempotent mutation automatically unless its endpoint documents idempotency and the same key is preserved.
- Use `proxy.ts` only for optimistic routing/proactive expiry handling; re-check authorization in every Server Action/data operation and rely on ASP.NET as the final authority.
- Never trust an unverified decoded JWT role for authorization. Decoding expiry may guide proactive refresh only.
- Validate `returnTo` as a relative same-origin path.
- Protect custom cookie-authenticated Route Handlers from CSRF with same-origin/Origin checks and POST semantics; authentication is not CSRF protection.
- Do not log tokens, cookie headers, credentials, or upstream auth bodies.

## Centralize HTTP and errors

Use one typed `apiRequest` implementation for:

- base URL and path joining;
- Bearer injection;
- JSON serialization/parsing;
- timeout/abort handling;
- explicit cache policy;
- idempotency and correlation headers;
- `application/problem+json` parsing;
- non-JSON/empty responses;
- safe retry rules.

Normalize upstream failures into a server-side `ApiError`, then expose a serializable `ActionResult<T>` or safe BFF response. Preserve `status`, `code`, `detail`, `errors`, `traceId`, `timestamp`, and `Retry-After` when present. Never expose stack traces, tokens, upstream URLs containing secrets, or development exception details.

Apply behavior by status:

- `400`: map validation errors to fields; preserve form input.
- `401`: refresh only when allowed, once; otherwise clear session and require login.
- `403`: do not refresh; show insufficient permission.
- `404`: render not-found or stale-resource behavior by route.
- `409`: refresh current data; never overwrite or blindly retry.
- `429`: preserve rate-limit guidance and use controlled backoff.
- `500`/network/timeout: show a generic message and retain `traceId` for support.

## Validate the completed flow

- Audit and regenerate OpenAPI types; run the stale-output check.
- Typecheck, lint, test, and build.
- Test cookie flags and exact backend expiries.
- Test login success/failure, refresh rotation, logout cleanup, and expired/invalid tokens.
- Test one refresh under parallel page requests and ensure no token-rotation race or retry loop.
- Test direct Server Action/Route Handler calls without UI authorization.
- Test 400/401/403/404/409/429/500, non-JSON upstream failures, empty `204`, timeout, and aborted requests.
- Test that tokens and `API_BASE_URL` are absent from client bundles, serialized props, HTML, logs, and analytics.
- Test idempotency keys survive safe retries.
- Test private/authenticated responses are never placed in a shared cache.

When asked only to review or diagnose, do not edit files. When asked to implement, make the smallest cohesive integration, preserve the existing architecture, and report contract gaps separately from frontend defects.
