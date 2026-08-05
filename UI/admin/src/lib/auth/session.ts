import "server-only";

import { cache } from "react";
import { redirect } from "next/navigation";
import { ApiError } from "@/lib/api/problem";
import { assertActiveAdmin, type AdminSession, type AuthResult } from "@/lib/auth/contracts";
import { clearSessionCookies, readAccessToken, readRefreshToken, setSessionCookies } from "@/lib/auth/cookies";
import { getCurrentUser, logoutWithToken, refreshWithToken } from "@/lib/auth/backend";
import { safeReturnTo } from "@/lib/auth/policy";

// Burada access tokenı backend /users/me ile doğrulayıp yalnız aktif Admin kullanıcısı için oturum DTO'su üretiyorum.
export async function verifyAdminAccessToken(accessToken: string): Promise<AdminSession> {
  const user = await getCurrentUser(accessToken);
  assertActiveAdmin(user);
  return { accessToken, user };
}

// Burada aynı Server Component render geçişindeki layout ve sayfa kontrollerini tek backend doğrulamasında birleştiriyorum.
export const getVerifiedAdminSession = cache(async (): Promise<AdminSession> => {
  const accessToken = await readAccessToken();
  if (!accessToken) throw authenticationRequired();
  return verifyAdminAccessToken(accessToken);
});

// Burada korumalı sayfayı 401, 403 ve refresh durumlarını birbirine karıştırmadan güvenli route'lara yönlendiriyorum.
export async function requireAdminPageSession(returnTo: string): Promise<AdminSession> {
  try {
    return await getVerifiedAdminSession();
  } catch (error) {
    if (!(error instanceof ApiError)) throw error;
    if (error.problem.status === 403) redirect("/access-denied");
    if (error.problem.status !== 401) throw error;

    const safeTarget = safeReturnTo(returnTo);
    if (await readRefreshToken()) {
      redirect(`/api/auth/refresh?returnTo=${encodeURIComponent(safeTarget)}`);
    }
    redirect(`/login?returnTo=${encodeURIComponent(safeTarget)}&reason=session_required`);
  }
}

// Burada login ve kök route için oturumu zorunlu kılmadan yalnız doğrulanmış Admin sonucunu döndürüyorum.
export async function getOptionalAdminSession(): Promise<AdminSession | null> {
  const accessToken = await readAccessToken();
  if (!accessToken) return null;
  try {
    return await verifyAdminAccessToken(accessToken);
  } catch (error) {
    if (error instanceof ApiError && (error.problem.status === 401 || error.problem.status === 403)) return null;
    throw error;
  }
}

// Burada Server Action başlamadan önce rolü doğruluyor, yalnız 401 durumunda bir kez token yenileyip tekrar doğruluyorum.
export async function requireAdminActionSession(): Promise<AdminSession> {
  const accessToken = await readAccessToken();
  if (accessToken) {
    try {
      return await verifyAdminAccessToken(accessToken);
    } catch (error) {
      if (!(error instanceof ApiError) || error.problem.status !== 401) throw error;
    }
  }

  return refreshAdminSession();
}

// Burada dönüşümlü refresh tokenı tek kez kullanıp yeni çifti yazmadan önce yeni kullanıcı rolünü backend ile tekrar doğruluyorum.
export async function refreshAdminSession(): Promise<AdminSession> {
  const currentRefreshToken = await readRefreshToken();
  if (!currentRefreshToken) throw authenticationRequired();

  let result: AuthResult | undefined;
  try {
    result = await refreshWithToken(currentRefreshToken);
    assertActiveAdmin(result.user);
    await setSessionCookies(result.tokens);
    return await verifyAdminAccessToken(result.tokens.accessToken);
  } catch (error) {
    if (result && error instanceof ApiError && error.problem.status === 403) {
      try {
        await logoutWithToken(result.tokens.refreshToken);
      } catch {
        // Burada yetkisiz yeni refresh token browser'a kalıcı dönmeyeceği için iptal hatasında da cookie temizliğini sürdürüyorum.
      }
    }
    if (shouldClearFailedSession(error)) await clearSessionCookies();
    throw error;
  }
}

// Burada geçersiz veya yetkisiz oturumları temizlerken kullanılabilir refresh tokenı backend'de de iptal etmeyi deniyorum.
export async function revokeAndClearSession(): Promise<void> {
  const refreshToken = await readRefreshToken();
  try {
    if (refreshToken) await logoutWithToken(refreshToken);
  } catch {
    // Burada upstream logout başarısız olsa bile yerel oturum sırlarının tarayıcıda kalmasına izin vermiyorum.
  } finally {
    await clearSessionCookies();
  }
}

// Burada yalnız kesin geçersiz, yasak veya bozuk auth cevaplarında cookie'leri kaldırıp geçici ağ hatasında güvenli yeniden denemeyi koruyorum.
function shouldClearFailedSession(error: unknown): boolean {
  return (
    error instanceof ApiError &&
    (error.problem.status === 401 || error.problem.status === 403 || error.problem.code === "invalid_auth_response")
  );
}

// Burada cookie bulunmadığında ortak ve seri hale getirilebilir 401 hatasını üretiyorum.
function authenticationRequired(): ApiError {
  return new ApiError({
    title: "Oturum gerekli",
    status: 401,
    code: "authentication_required",
    detail: "Bu işlem için yönetici oturumu gereklidir.",
  });
}
