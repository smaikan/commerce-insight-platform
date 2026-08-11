import "server-only";

import { NextResponse } from "next/server";

import { internalApiUrl } from "@/lib/api/client";
export { hasTrustedStorefrontOrigin } from "@/lib/security/storefront-origin";

const GUEST_CART_COOKIE = "ecommerce_guest_cart";
const GUEST_CART_TOKEN_PATTERN = /^[0-9A-F]{64}$/;
const UPSTREAM_TIMEOUT_MS = 8_000;

// Burada yalnız backend'in ürettiği canonical guest cart cookie'sini upstream API'ye aktarıyorum.
function guestCartCookieHeader(cookieHeader: string | null): string | undefined {
  if (!cookieHeader) return undefined;

  const value = cookieHeader
    .split(";")
    .map((part) => part.trim())
    .find((part) => part.startsWith(`${GUEST_CART_COOKIE}=`))
    ?.slice(GUEST_CART_COOKIE.length + 1);

  return value && GUEST_CART_TOKEN_PATTERN.test(value)
    ? `${GUEST_CART_COOKIE}=${value}`
    : undefined;
}

// Burada guest cart çağrısını cookie ve response header allowlist'iyle aynı-origin BFF sınırından geçiriyorum.
export async function forwardGuestCartRequest(
  request: Request,
  path: "/api/cart" | "/api/cart/items" | `/api/cart/items/${string}` | `/api/cart?${string}` | `/api/cart/items/${string}?${string}`,
  init: { method: "GET" | "POST" | "PUT" | "DELETE"; body?: string },
): Promise<NextResponse> {
  const headers = new Headers({ Accept: "application/json" });
  const guestCookie = guestCartCookieHeader(request.headers.get("cookie"));

  if (guestCookie) headers.set("Cookie", guestCookie);
  if (init.body) headers.set("Content-Type", "application/json");

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
    return problemResponse(503, "Sepete şu anda ulaşılamıyor", "Lütfen kısa bir süre sonra tekrar deneyin.", "cart_unavailable");
  }

  const responseHeaders = new Headers({
    "Cache-Control": "private, no-store",
    "Content-Type": upstream.headers.get("content-type") || "application/json",
  });
  const retryAfter = upstream.headers.get("retry-after");
  const setCookie = upstream.headers.get("set-cookie");

  if (retryAfter) responseHeaders.set("Retry-After", retryAfter);
  if (setCookie) responseHeaders.append("Set-Cookie", setCookie);

  return new NextResponse(await upstream.arrayBuffer(), {
    status: upstream.status,
    headers: responseHeaders,
  });
}

export function problemResponse(status: number, title: string, detail: string, code: string): NextResponse {
  return NextResponse.json(
    { status, title, detail, code },
    { status, headers: { "Cache-Control": "private, no-store", "Content-Type": "application/problem+json" } },
  );
}
