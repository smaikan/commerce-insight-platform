# Errors and Tests

## Shared models

```ts
export type ApiProblem = {
  type?: string;
  title: string;
  status: number;
  detail?: string;
  instance?: string;
  code?: string;
  traceId?: string;
  timestamp?: string;
  errors?: Record<string, string[]>;
};

export type ActionResult<T> =
  | { ok: true; data: T }
  | { ok: false; error: ApiProblem };
```

Keep a server-only `ApiError` class for control flow. Return only serializable safe fields to clients.

## Parsing

- Check status and content type.
- Handle `204` without JSON parsing.
- Parse `application/problem+json` and compatible JSON error bodies.
- Fall back safely for HTML/plain-text proxy failures.
- Preserve `traceId`, validation errors, and `Retry-After`.
- Use an abort timeout and distinguish timeout from user cancellation.
- Consume the response body once.

## Test matrix

### Contract

- Required auth paths/schemas exist.
- Generated types are current.
- Public auth security conflict is known or corrected.
- ProblemDetails omissions are explicit.

### Session

- Login sets two HttpOnly cookies with correct expiries.
- Refresh rotates both cookies.
- Logout clears cookies even when upstream fails.
- Invalid refresh clears session.
- Refresh occurs once; parallel requests do not loop/race.

### Authorization

- Unauthenticated and non-admin direct calls fail.
- Backend `401` and `403` remain distinct.
- Proxy bypass cannot bypass Server Action/DAL checks.

### Errors

- Validation fields reach the form.
- `409` preserves draft and avoids retry.
- `429` preserves retry guidance.
- Network, timeout, non-JSON, and `500` are safe.
- `traceId` remains visible for support.

### Leakage/cache

- Tokens do not appear in client JS, props, HTML, logs, analytics, or errors.
- `API_BASE_URL` is server-only.
- Authenticated responses are never shared-cached.
- Redirect targets cannot escape the origin.
