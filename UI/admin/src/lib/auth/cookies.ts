import "server-only";

import { cookies } from "next/headers";
import { ADMIN_ACCESS_COOKIE, ADMIN_REFRESH_COOKIE } from "@/lib/auth/constants";
import type { AuthTokens } from "@/lib/auth/contracts";
import { sessionCookiePolicy } from "@/lib/auth/policy";

const secureCookies = process.env.NODE_ENV === "production";

// Burada token cookie'lerinin ortak güvenlik seçeneklerini host-only ve JavaScript erişimine kapalı biçimde tanımlıyorum.
export function sessionCookieOptions(expires: Date) {
  return sessionCookiePolicy(expires, secureCookies);
}

// Burada backend'in verdiği iki tokenı kendi son kullanma zamanlarıyla ayrı HttpOnly cookie'lere yazıyorum.
export async function setSessionCookies(tokens: AuthTokens): Promise<void> {
  const cookieStore = await cookies();
  cookieStore.set(
    ADMIN_ACCESS_COOKIE,
    tokens.accessToken,
    sessionCookieOptions(new Date(tokens.accessTokenExpiresAt)),
  );
  cookieStore.set(
    ADMIN_REFRESH_COOKIE,
    tokens.refreshToken,
    sessionCookieOptions(new Date(tokens.refreshTokenExpiresAt)),
  );
}

// Burada erişim tokenını yalnız server-side doğrulama ve API aktarımı için okuyorum.
export async function readAccessToken(): Promise<string | undefined> {
  return (await cookies()).get(ADMIN_ACCESS_COOKIE)?.value;
}

// Burada refresh tokenı yalnız yenileme veya upstream logout sınırında kullanmak üzere okuyorum.
export async function readRefreshToken(): Promise<string | undefined> {
  return (await cookies()).get(ADMIN_REFRESH_COOKIE)?.value;
}

// Burada iki oturum cookie'sini oluşturuldukları seçeneklerle eşleşen süresi geçmiş değerlerle temizliyorum.
export async function clearSessionCookies(): Promise<void> {
  const cookieStore = await cookies();
  const expiredOptions = { ...sessionCookieOptions(new Date(0)), maxAge: 0 };
  cookieStore.set(ADMIN_ACCESS_COOKIE, "", expiredOptions);
  cookieStore.set(ADMIN_REFRESH_COOKIE, "", expiredOptions);
}
