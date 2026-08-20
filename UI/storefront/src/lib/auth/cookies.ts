import "server-only";

import { cookies } from "next/headers";

import type { AuthTokens } from "@/modules/auth/contracts";

const ACCESS_COOKIE = "ecommerce_storefront_access";
const REFRESH_COOKIE = "ecommerce_storefront_refresh";
const GUEST_SESSION_COOKIE = "ecommerce_guest_cart";

// Burada production ortamında __Host- önekiyle domain sabitlemesini engelleyip geliştirmede localhost uyumunu koruyorum.
export function authCookieNames(environment = process.env.NODE_ENV) {
  const prefix = environment === "production" ? "__Host-" : "";
  return {
    access: `${prefix}${ACCESS_COOKIE}`,
    refresh: `${prefix}${REFRESH_COOKIE}`,
  } as const;
}

// Burada erişim ve yenileme tokenlarını backend süreleriyle, browser JavaScript'ine kapalı ayrı Storefront çerezlerine yazıyorum.
export async function writeAuthCookies(tokens: AuthTokens): Promise<void> {
  const store = await cookies();
  const names = authCookieNames();
  const common = {
    httpOnly: true,
    secure: process.env.NODE_ENV === "production",
    sameSite: "lax" as const,
    path: "/",
  };

  store.set(names.access, tokens.accessToken, {
    ...common,
    expires: new Date(tokens.accessTokenExpiresAt),
  });
  store.set(names.refresh, tokens.refreshToken, {
    ...common,
    expires: new Date(tokens.refreshTokenExpiresAt),
  });
}

// Burada navbar ve hesap kabuğu için token değerini açmadan yalnız geçerli süreli oturum çerezi varlığını okuyorum.
export async function hasAuthSessionCookie(): Promise<boolean> {
  const store = await cookies();
  const names = authCookieNames();
  return Boolean(store.get(names.access)?.value || store.get(names.refresh)?.value);
}

// Burada authenticated API çağrılarında kullanılacak access tokenı yalnız sunucu tarafında okuyorum.
export async function readAccessToken(): Promise<string | null> {
  const store = await cookies();
  return store.get(authCookieNames().access)?.value || null;
}

// Burada backend logout isteğinde kullanılacak refresh tokenı yalnız sunucu sınırında okuyorum.
export async function readRefreshToken(): Promise<string | null> {
  const store = await cookies();
  return store.get(authCookieNames().refresh)?.value || null;
}

// Burada logout başarı veya upstream hata durumunda iki yerel oturum çerezini de kesin olarak temizliyorum.
export async function clearAuthCookies(): Promise<void> {
  const store = await cookies();
  const names = authCookieNames();
  const common = {
    httpOnly: true,
    secure: process.env.NODE_ENV === "production",
    sameSite: "lax" as const,
    path: "/",
  };
  store.set(names.access, "", { ...common, maxAge: 0 });
  store.set(names.refresh, "", { ...common, maxAge: 0 });
}

// Burada login öncesi cart ve favorites için oluşmuş canonical ortak guest session tokenını yalnız sunucuda okuyorum.
export async function readGuestSessionCookie(): Promise<string | null> {
  const value = (await cookies()).get(GUEST_SESSION_COOKIE)?.value;
  return value && /^[0-9A-F]{64}$/.test(value) ? value : null;
}

// Burada ortak guest session başarıyla hesaba claim edildiğinde Storefront kapsamındaki çerezi siliyorum.
export async function clearGuestSessionCookie(): Promise<void> {
  (await cookies()).delete(GUEST_SESSION_COOKIE);
}
