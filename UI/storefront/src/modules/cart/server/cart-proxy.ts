import "server-only";

import { NextResponse } from "next/server";

import { internalApiUrl } from "@/lib/api/client";
import { readAccessToken, readRefreshToken } from "@/lib/auth/cookies";
import { appendAllowedGuestSetCookies, guestCookieHeader } from "@/lib/security/guest-cookie";
export { hasTrustedStorefrontOrigin } from "@/lib/security/storefront-origin";

const GUEST_SESSION_COOKIE = "ecommerce_guest_cart";
const UPSTREAM_TIMEOUT_MS = 8_000;

type CartOwner =
  | { kind: "authenticated"; accessToken: string }
  | { kind: "refresh-required" }
  | { kind: "guest"; cookie?: string };

// Burada sepet isteğinin sahibini sunucu çerezlerinden belirleyip kullanıcı oturumunu guest session'dan kesin olarak öncelikli tutuyorum.
async function resolveCartOwner(request: Request): Promise<CartOwner> {
  const [accessToken, refreshToken] = await Promise.all([
    readAccessToken(),
    readRefreshToken(),
  ]);

  if (accessToken) return { kind: "authenticated", accessToken };
  if (refreshToken) return { kind: "refresh-required" };

  return {
    kind: "guest",
    cookie: guestCookieHeader(request.headers.get("cookie"), [GUEST_SESSION_COOKIE]),
  };
}

// Burada guest ve authenticated sepet çağrılarını aynı owner-aware BFF sınırından, birbirlerinin kimliğini karıştırmadan API'ye iletiyorum.
export async function forwardCartRequest(
  request: Request,
  path: "/api/cart" | "/api/cart/items" | `/api/cart/items/${string}` | `/api/cart?${string}` | `/api/cart/items/${string}?${string}`,
  init: { method: "GET" | "POST" | "PUT" | "DELETE"; body?: string },
): Promise<NextResponse> {
  const owner = await resolveCartOwner(request);
  if (owner.kind === "refresh-required") {
    return sessionRefreshResponse();
  }

  const headers = new Headers({ Accept: "application/json" });
  if (owner.kind === "authenticated") {
    headers.set("Authorization", `Bearer ${owner.accessToken}`);
  } else if (owner.cookie) {
    headers.set("Cookie", owner.cookie);
  }
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

  // Burada geçersiz kullanıcı access token'ında sessizce guest sepete düşmek yerine kontrollü refresh akışını başlatacak güvenli 401 cevabını üretiyorum.
  if (owner.kind === "authenticated" && upstream.status === 401) {
    const problem = await upstream.json().catch(() => null) as Record<string, unknown> | null;
    return sessionRefreshResponse(typeof problem?.traceId === "string" ? problem.traceId : undefined);
  }

  const responseHeaders = new Headers({
    "Cache-Control": "private, no-store",
    "Content-Type": upstream.headers.get("content-type") || "application/json",
    Vary: "Cookie",
  });
  const retryAfter = upstream.headers.get("retry-after");

  if (retryAfter) responseHeaders.set("Retry-After", retryAfter);
  if (owner.kind === "guest") {
    appendAllowedGuestSetCookies(upstream.headers, responseHeaders, [GUEST_SESSION_COOKIE]);
  }

  return new NextResponse(await upstream.arrayBuffer(), {
    status: upstream.status,
    headers: responseHeaders,
  });
}

// Burada kullanıcı sepetini yeni guest session oluşturmadan yeniden okuyabilmek için client'ı tek kontrollü refresh rotasına yönlendiren hata sözleşmesini kuruyorum.
function sessionRefreshResponse(traceId?: string): NextResponse {
  return NextResponse.json(
    {
      status: 401,
      title: "Oturum yenilenmeli",
      detail: "Sepetinizi güncellemek için oturumunuzun yenilenmesi gerekiyor.",
      code: "session_refresh_required",
      ...(traceId ? { traceId } : {}),
    },
    {
      status: 401,
      headers: {
        "Cache-Control": "private, no-store",
        "Content-Type": "application/problem+json",
        Vary: "Cookie",
      },
    },
  );
}

export function problemResponse(status: number, title: string, detail: string, code: string): NextResponse {
  return NextResponse.json(
    { status, title, detail, code },
    { status, headers: { "Cache-Control": "private, no-store", "Content-Type": "application/problem+json" } },
  );
}
