import "server-only";

import { NextResponse } from "next/server";

import { internalApiUrl } from "@/lib/api/client";
import { appendAllowedGuestSetCookies, guestCookieHeader } from "@/lib/security/guest-cookie";
import { siteConfig } from "@/lib/site-config";

const ALLOWED_COOKIE_NAMES = [
  "ecommerce_guest_cart",
  "ecommerce_guest_orders",
  "ecommerce_guest_csrf",
] as const;
const UPSTREAM_TIMEOUT_MS = 15_000;

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
  appendAllowedGuestSetCookies(upstream.headers, responseHeaders, ALLOWED_COOKIE_NAMES);

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
