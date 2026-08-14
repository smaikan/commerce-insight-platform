import "server-only";

import { NextResponse } from "next/server";

import { internalApiUrl } from "@/lib/api/client";
import { readAccessToken } from "@/lib/auth/cookies";
import {
  appendAllowedGuestSetCookies,
  guestCookieToken,
  guestTokenFromSetCookie,
} from "@/lib/security/guest-cookie";
import { siteConfig } from "@/lib/site-config";
import type { FavoriteProductPage, FavoriteState } from "@/modules/favorites/types";
import { favoriteProblemResponse } from "@/modules/favorites/server/route-response";

const GUEST_SESSION_COOKIE = "ecommerce_guest_cart";
const GUEST_CSRF_HEADER = "X-Guest-CSRF";
const FAVORITE_PAGE_SIZE = 100;
const UPSTREAM_TIMEOUT_MS = 8_000;

type FavoriteMethod = "GET" | "POST" | "DELETE";

// Burada guest ve authenticated favori isteklerini tek owner-aware BFF sınırında API'ye iletiyorum.
async function favoriteUpstreamRequest(
  request: Request,
  path: string,
  method: FavoriteMethod,
  accessToken: string | null,
  guestToken: string | null,
): Promise<Response> {
  const headers = new Headers({ Accept: "application/json" });

  if (accessToken) {
    headers.set("Authorization", `Bearer ${accessToken}`);
  } else if (guestToken) {
    headers.set("Cookie", `${GUEST_SESSION_COOKIE}=${guestToken}`);
    if (method !== "GET") {
      headers.set("Origin", new URL(siteConfig.url).origin);
      headers.set(GUEST_CSRF_HEADER, guestToken);
    }
  }

  return fetch(internalApiUrl(path), {
    method,
    headers,
    cache: "no-store",
    signal: AbortSignal.timeout(UPSTREAM_TIMEOUT_MS),
  });
}

// Burada upstream cevabını private/no-store tutup yalnızca izin verilen guest cookie'sini browser'a taşıyorum.
async function favoriteResponse(upstream: Response, fallbackCookieSource?: Headers): Promise<NextResponse> {
  const headers = new Headers({
    "Cache-Control": "private, no-store",
    "Content-Type": upstream.headers.get("content-type") || "application/json",
    Vary: "Cookie",
  });
  const retryAfter = upstream.headers.get("retry-after");
  if (retryAfter) headers.set("Retry-After", retryAfter);

  appendAllowedGuestSetCookies(upstream.headers, headers, [GUEST_SESSION_COOKIE]);
  if (!headers.has("set-cookie") && fallbackCookieSource) {
    appendAllowedGuestSetCookies(fallbackCookieSource, headers, [GUEST_SESSION_COOKIE]);
  }

  const body = upstream.status === 204 ? null : await upstream.arrayBuffer();
  return new NextResponse(body, { status: upstream.status, headers });
}

// Burada ilk guest favori mutasyonundan önce session yoksa yalnızca GET ile güvenli ortak session oluşturuyorum.
async function bootstrapGuestSession(request: Request): Promise<Response> {
  return favoriteUpstreamRequest(
    request,
    "/api/product-engagement/favorites?pageNumber=1&pageSize=1",
    "GET",
    null,
    null,
  );
}

// Burada favori listesinin tek sayfasını JWT önceliğini koruyarak aynı-origin response olarak döndürüyorum.
export async function forwardFavoriteProductsRequest(
  request: Request,
  pageNumber: number,
  pageSize: number,
): Promise<NextResponse> {
  try {
    const accessToken = await readAccessToken();
    const guestToken = accessToken
      ? null
      : guestCookieToken(request.headers.get("cookie"), GUEST_SESSION_COOKIE);
    const query = new URLSearchParams({ pageNumber: String(pageNumber), pageSize: String(pageSize) });
    const upstream = await favoriteUpstreamRequest(
      request,
      `/api/product-engagement/favorites?${query.toString()}`,
      "GET",
      accessToken,
      guestToken,
    );
    return favoriteResponse(upstream);
  } catch {
    return favoriteProblemResponse(
      503,
      "Favoriler şu anda kullanılamıyor",
      "Lütfen kısa bir süre sonra tekrar deneyin.",
      "favorites_unavailable",
    );
  }
}

// Burada katalog kalpleri için tüm favori kimliklerini aynı guest session'ı sayfalar arasında koruyarak topluyorum.
export async function forwardFavoriteStateRequest(request: Request): Promise<NextResponse> {
  try {
    const accessToken = await readAccessToken();
    let guestToken = accessToken
      ? null
      : guestCookieToken(request.headers.get("cookie"), GUEST_SESSION_COOKIE);
    let firstHeaders: Headers | undefined;
    const productIds: string[] = [];
    let totalCount = 0;
    let pageNumber = 1;

    while (true) {
      const upstream = await favoriteUpstreamRequest(
        request,
        `/api/product-engagement/favorites?pageNumber=${pageNumber}&pageSize=${FAVORITE_PAGE_SIZE}`,
        "GET",
        accessToken,
        guestToken,
      );
      if (!firstHeaders) firstHeaders = upstream.headers;
      if (!accessToken && !guestToken) {
        guestToken = guestTokenFromSetCookie(upstream.headers, GUEST_SESSION_COOKIE);
      }
      if (!upstream.ok) return favoriteResponse(upstream, firstHeaders);

      const page = await upstream.json() as FavoriteProductPage;
      productIds.push(...page.items.map((product) => product.id));
      totalCount = page.totalCount;
      if (!page.hasNextPage) break;
      pageNumber += 1;
    }

    const state: FavoriteState = { productIds, totalCount };
    const headers = new Headers({ "Cache-Control": "private, no-store", Vary: "Cookie" });
    if (firstHeaders) appendAllowedGuestSetCookies(firstHeaders, headers, [GUEST_SESSION_COOKIE]);
    return NextResponse.json(state, { headers });
  } catch {
    return favoriteProblemResponse(
      503,
      "Favoriler şu anda kullanılamıyor",
      "Lütfen kısa bir süre sonra tekrar deneyin.",
      "favorites_unavailable",
    );
  }
}

// Burada favori mutasyonunu JWT varsa yalnız bearer ile, guest ise cookie-Origin-CSRF üçlüsüyle tam bir kez gönderiyorum.
export async function forwardFavoriteMutationRequest(
  request: Request,
  productId: string,
  method: "POST" | "DELETE",
): Promise<NextResponse> {
  try {
    const accessToken = await readAccessToken();
    let guestToken = accessToken
      ? null
      : guestCookieToken(request.headers.get("cookie"), GUEST_SESSION_COOKIE);
    let bootstrapHeaders: Headers | undefined;

    if (!accessToken && !guestToken) {
      const bootstrap = await bootstrapGuestSession(request);
      bootstrapHeaders = bootstrap.headers;
      if (!bootstrap.ok) return favoriteResponse(bootstrap);
      guestToken = guestTokenFromSetCookie(bootstrap.headers, GUEST_SESSION_COOKIE);
      if (!guestToken) {
        return favoriteProblemResponse(
          503,
          "Favori oturumu kurulamadı",
          "Lütfen kısa bir süre sonra tekrar deneyin.",
          "guest_session_unavailable",
        );
      }
    }

    const upstream = await favoriteUpstreamRequest(
      request,
      `/api/product-engagement/products/${encodeURIComponent(productId)}/favorites`,
      method,
      accessToken,
      guestToken,
    );
    return favoriteResponse(upstream, bootstrapHeaders);
  } catch {
    return favoriteProblemResponse(
      503,
      "Favoriler şu anda kullanılamıyor",
      "Lütfen kısa bir süre sonra tekrar deneyin.",
      "favorites_unavailable",
    );
  }
}
