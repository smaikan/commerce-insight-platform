# BFF Authentication Flow

## Suggested placement

```text
src/
├── app/api/auth/
│   ├── login/route.ts
│   ├── refresh/route.ts
│   └── logout/route.ts
├── lib/api/
│   ├── client.ts
│   └── problem.ts
├── lib/auth/
│   ├── cookies.ts
│   ├── session.ts
│   └── refresh.ts
├── modules/auth/
│   ├── actions.ts
│   └── schemas.ts
└── generated/api.ts
```

Create only boundaries needed by the chosen UI flow. A login form Server Action may call the same server-only auth function used by a Route Handler; it must not make an internal HTTP call to that handler.

## Cookie policy

- Separate access and refresh cookies.
- `httpOnly: true`.
- `secure: process.env.NODE_ENV === "production"`.
- `sameSite: "lax"`.
- `path: "/"`.
- No `domain` unless deployment architecture explicitly requires and secures it.
- Use backend expiry timestamps.
- Clear with matching path/options.

## Login

1. Validate form input.
2. POST credentials/deviceName server-to-server.
3. Parse `AuthResultDto`.
4. Set both cookies.
5. Return/redirect with safe user data only.
6. Never return tokens to a Client Component.

## Refresh

1. Read refresh cookie server-side.
2. POST `{ refreshToken, deviceName? }`.
3. Replace both cookies from the response.
4. Retry the original operation at most once when the boundary can safely write cookies.
5. Clear session on invalid/expired refresh.

Server Components can read but cannot set cookies during rendering. Prefer proactive refresh in a carefully scoped proxy/navigation boundary or a cookie-writing auth boundary. Do not implement hidden recursive refresh from arbitrary Server Component fetches.

## Logout

1. Read refresh cookie.
2. Attempt upstream logout.
3. Clear both local cookies in `finally`.
4. Return `204` or redirect.

## Security

- Re-authorize every Server Action/Route Handler.
- Treat proxy checks as optimistic UX only.
- Validate same-origin mutation requests and safe return URLs.
- Keep tokens out of errors/logs.
- Do not forward untrusted proxy/IP headers; original-client IP propagation requires trusted infrastructure configuration.
- Keep private responses `no-store`.
- Avoid simultaneous refresh attempts when refresh rotation invalidates previous tokens; test the real backend behavior.
## Guest cookie BFF topolojisi

Browser guest cart/order işlemlerini same-origin Route Handler'a yapar. Handler yalnız `ecommerce_guest_cart`, `ecommerce_guest_orders`, `ecommerce_guest_csrf` cookie'lerini ve açıkça allowlist edilmiş `Idempotency-Key`, `X-Turnstile-Token` gibi header'ları upstream API'ye taşır. Upstream `Set-Cookie`, storefront origin altında Secure/HttpOnly/SameSite=Lax olarak yeniden yazılır; değerler JS, localStorage, DOM, props, log ve analytics'e açılmaz.

Mutation öncesi browser `Origin` değeri BFF tarafından allowlist ile doğrulanır. BFF CSRF cookie'sini server-side okur ve `X-Guest-CSRF` header'ına koyar. Magic-link tokenı query yerine URL fragment'ında gelir; browser onu tek seferlik BFF exchange body alanına aktarır ve URL'yi hemen temizler. Server Component kendi Route Handler'ına HTTP self-fetch yapmaz; ortak server-only fonksiyonu çağırır. Guest cart/order/detail cevapları `no-store` kalır.
