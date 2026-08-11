import "server-only";

import { NextResponse } from "next/server";

import { internalApiUrl } from "@/lib/api/client";
import { siteConfig } from "@/lib/site-config";

const CANONICAL_TOKEN_PATTERN = /^[0-9A-F]{64}$/;
const ALLOWED_COOKIE_NAMES = new Set([
  "ecommerce_guest_cart",
  "ecommerce_guest_orders",
  "ecommerce_guest_csrf",
]);
const UPSTREAM_TIMEOUT_MS = 15_000;

// Burada yalnız checkout ve guest order akışına ait canonical HttpOnly cookie'leri upstream isteğine taşıyorum.
function guestCookieHeader(cookieHeader: string | null, names: string[]): string | undefined {
  if (!cookieHeader) return undefined;

  const cookies = new Map(
    cookieHeader.split(";").map((part) => {
      const separator = part.indexOf("=");
      return separator < 0
        ? [part.trim(), ""]
        : [part.slice(0, separator).trim(), part.slice(separator + 1).trim()];
    }),
  );

  const forwarded = names.flatMap((name) => {
    const value = cookies.get(name);
    return value && CANONICAL_TOKEN_PATTERN.test(value) ? [`${name}=${value}`] : [];
  });

  return forwarded.length ? forwarded.join("; ") : undefined;
}

// Burada birden fazla upstream Set-Cookie değerini yalnız bilinen guest cookie adları ve canonical token biçimiyle geri iletiyorum.
function appendAllowedSetCookies(source: Headers, target: Headers) {
  const getSetCookie = (source as Headers & { getSetCookie?: () => string[] }).getSetCookie;
  const values = getSetCookie ? getSetCookie.call(source) : [source.get("set-cookie")].filter(Boolean) as string[];

  for (const value of values) {
    const match = /^([^=]+)=([^;]+)(?:;|$)/.exec(value);
    if (match && ALLOWED_COOKIE_NAMES.has(match[1]) && CANONICAL_TOKEN_PATTERN.test(match[2])) {
      target.append("Set-Cookie", value);
    }
  }
}

type GuestCommerceRequestInit = {
  method: "GET" | "POST";
  body?: string;
  cookieNames: string[];
  idempotencyKey?: string;
  turnstileToken?: string;
};

// Burada checkout ve sipariş okuma çağrılarını allowlist cookie/header sınırıyla API'ye iletiyorum.
export async function forwardGuestCommerceRequest(
  request: Request,
  path: "/api/cart/checkout/guest" | `/api/guest-orders/${string}`,
  init: GuestCommerceRequestInit,
): Promise<NextResponse> {
  const headers = new Headers({ Accept: "application/json" });
  const cookie = guestCookieHeader(request.headers.get("cookie"), init.cookieNames);

  if (cookie) headers.set("Cookie", cookie);
  if (init.body) headers.set("Content-Type", "application/json");
  if (init.idempotencyKey) headers.set("Idempotency-Key", init.idempotencyKey);
  if (init.turnstileToken) headers.set("X-Turnstile-Token", init.turnstileToken);
  if (init.method === "POST") headers.set("Origin", new URL(siteConfig.url).origin);

  let upstream: Response;
  try {
    upstream = await fetch(internalApiUrl(path), {
      method: init.method,
      headers,
      body: init.body,
      cache: "no-store",
      signal: AbortSignal.timeout(UPSTREAM_TIMEOUT_MS),
    });
  } catch {
    return checkoutProblemResponse(503, "Sipariş işlemi şu anda kullanılamıyor", "Lütfen kısa bir süre sonra tekrar deneyin.", "checkout_unavailable");
  }

  const responseHeaders = new Headers({
    "Cache-Control": "private, no-store",
    "Content-Type": upstream.headers.get("content-type") || "application/json",
  });
  const retryAfter = upstream.headers.get("retry-after");
  if (retryAfter) responseHeaders.set("Retry-After", retryAfter);
  appendAllowedSetCookies(upstream.headers, responseHeaders);

  return new NextResponse(await upstream.arrayBuffer(), {
    status: upstream.status,
    headers: responseHeaders,
  });
}

export function checkoutProblemResponse(status: number, title: string, detail: string, code: string): NextResponse {
  return NextResponse.json(
    { status, title, detail, code },
    { status, headers: { "Cache-Control": "private, no-store", "Content-Type": "application/problem+json" } },
  );
}
