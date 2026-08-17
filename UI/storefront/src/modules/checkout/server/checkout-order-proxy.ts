import "server-only";

import { NextResponse } from "next/server";

import { internalApiUrl } from "@/lib/api/client";
import { readAccessToken, readRefreshToken } from "@/lib/auth/cookies";
import { forwardGuestCommerceRequest } from "@/modules/checkout/server/guest-commerce-proxy";

const UPSTREAM_TIMEOUT_MS = 15_000;

// Burada sipariş okumasında JWT sahibini guest grant'ten önceleyip iki sahiplik kanalını tek storefront rotasında birleştiriyorum.
export async function forwardCheckoutOrderRead(request: Request, orderId: string): Promise<NextResponse> {
  const [accessToken, refreshToken] = await Promise.all([readAccessToken(), readRefreshToken()]);
  if (accessToken) return forwardMemberRequest(`/api/orders/${orderId}`, accessToken, { method: "GET" });
  if (refreshToken) return refreshRequiredResponse();
  return forwardGuestCommerceRequest(request, `/api/guest-orders/${orderId}`, { method: "GET", cookieNames: ["ecommerce_guest_orders"] });
}

// Burada iyzico initialize isteğini üyede Bearer, misafirde session+CSRF ile doğru upstream endpointine iletiyorum.
export async function forwardIyzicoCheckoutForm(
  request: Request,
  orderId: string,
  idempotencyKey: string,
): Promise<NextResponse> {
  const [accessToken, refreshToken] = await Promise.all([readAccessToken(), readRefreshToken()]);
  if (accessToken) {
    return forwardMemberRequest(`/api/orders/${orderId}/payments/iyzico/checkout-form`, accessToken, {
      method: "POST",
      idempotencyKey,
    });
  }
  if (refreshToken) return refreshRequiredResponse();
  return forwardGuestCommerceRequest(request, `/api/guest-orders/${orderId}/payments/iyzico/checkout-form`, {
    method: "POST",
    cookieNames: ["ecommerce_guest_orders", "ecommerce_guest_csrf"],
    idempotencyKey,
    csrf: true,
  });
}

// Burada üye ödeme ve sipariş okumalarını hassas tokenı response'a taşımadan private/no-store olarak proxy'liyorum.
async function forwardMemberRequest(
  path: `/api/orders/${string}`,
  accessToken: string,
  init: { method: "GET" | "POST"; idempotencyKey?: string },
): Promise<NextResponse> {
  const headers = new Headers({ Accept: "application/json", Authorization: `Bearer ${accessToken}` });
  if (init.idempotencyKey) headers.set("Idempotency-Key", init.idempotencyKey);

  let upstream: Response;
  try {
    upstream = await fetch(internalApiUrl(path), {
      method: init.method,
      headers,
      cache: "no-store",
      signal: AbortSignal.timeout(UPSTREAM_TIMEOUT_MS),
    });
  } catch {
    return NextResponse.json(
      { status: 503, title: "Ödeme servisine ulaşılamıyor", detail: "Lütfen kısa bir süre sonra aynı ödeme denemesini tekrar deneyin.", code: "payment_unavailable" },
      { status: 503, headers: { "Cache-Control": "private, no-store", "Content-Type": "application/problem+json", Vary: "Cookie" } },
    );
  }

  return new NextResponse(await upstream.arrayBuffer(), {
    status: upstream.status,
    headers: {
      "Cache-Control": "private, no-store",
      "Content-Type": upstream.headers.get("content-type") || "application/json",
      Vary: "Cookie",
      ...(upstream.headers.get("retry-after") ? { "Retry-After": upstream.headers.get("retry-after")! } : {}),
    },
  });
}

function refreshRequiredResponse(): NextResponse {
  return NextResponse.json(
    { status: 401, title: "Oturum yenilenmeli", detail: "Ödeme işlemine devam etmek için oturumunuzu yenileyin.", code: "session_refresh_required" },
    { status: 401, headers: { "Cache-Control": "private, no-store", "Content-Type": "application/problem+json", Vary: "Cookie" } },
  );
}
