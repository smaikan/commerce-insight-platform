# Project Contract

## API surface

- Local API default: `http://localhost:5132`; production value must come from server-only configuration.
- OpenAPI: `docs/api/api-project-docs/openapi-controller-contract.json`.
- Current document: OpenAPI 3.0.4, 159 paths, 208 schemas, Bearer security scheme.
- IDs: users/products use public IDs; many order/variant/accounting resources use UUIDs.
- Enums are numeric on the wire.
- Lists commonly use `PagedResult<T>`.

## Auth endpoints

All are runtime-public via `[AllowAnonymous]` on `AuthController`:

- `POST /api/auth/register` -> `201 RegisterUserResultDto`.
- `POST /api/auth/login` -> `200 AuthResultDto`.
- `POST /api/auth/refresh-token` -> `200 AuthResultDto`.
- `POST /api/auth/logout` -> `204`.
- `POST /api/auth/forgot-password` -> `202`.
- `POST /api/auth/reset-password` -> `204`.

`LoginRequest`: email, password, optional deviceName.

`AuthResultDto`:

- `user: UserDto`.
- `tokens.accessToken`.
- `tokens.accessTokenExpiresAt`.
- `tokens.refreshToken`.
- `tokens.refreshTokenExpiresAt`.

Refresh may rotate the token pair. Always store both returned tokens and expiries. Logout sends `{ refreshToken }`.

## Authorization

- ASP.NET accepts access JWTs through `Authorization: Bearer`.
- Token validation checks signature, issuer, audience, expiry, security version, and session ID.
- Admin endpoints use the `AdminOnly` policy.
- Accounting endpoints are Admin-only.
- A hidden frontend control is not authorization.

## Errors

API responses use ProblemDetails with:

- `type`, `title`, `status`, `detail`, `instance`.
- extension `code`, `traceId`, `timestamp`.
- validation `errors: Record<string, string[]>`.

Known codes include validation, business rule, not found, conflict, concurrency conflict, unauthorized/authentication required/invalid access token, forbidden, rate limit, bad request, and internal error.

The current OpenAPI document mainly exposes success contracts and may omit ProblemDetails/error responses. Keep the frontend's shared error model aligned with API source/Markdown until the OpenAPI contract includes them.

## Known documentation conflict

The OpenAPI document applies a global Bearer security requirement, while `AuthController` is `[AllowAnonymous]`. Do not attach bearer requirements to login/register/refresh/logout based solely on the global OpenAPI security entry. Report/fix the contract separately.
