import "server-only";

import { apiRequest } from "@/lib/api/client";
import { parseAuthResult, parseAuthUser, type AuthResult, type AuthUser } from "@/lib/auth/contracts";

const refreshRequests = new Map<string, Promise<AuthResult>>();

// Burada kullanıcı bilgisini browser'a token açmadan ASP.NET login ucundan doğruluyorum.
export async function loginWithPassword(email: string, password: string): Promise<AuthResult> {
  const payload = await apiRequest<unknown>("/api/auth/login", {
    method: "POST",
    body: { email, password, deviceName: "SERANTIS Admin" },
  });
  return parseAuthResult(payload);
}

// Burada aynı process içindeki eşzamanlı refresh taleplerini tek dönüşümlü token isteğinde birleştiriyorum.
export function refreshWithToken(refreshToken: string): Promise<AuthResult> {
  const existing = refreshRequests.get(refreshToken);
  if (existing) return existing;

  const request = apiRequest<unknown>("/api/auth/refresh-token", {
    method: "POST",
    body: { refreshToken, deviceName: "SERANTIS Admin" },
  })
    .then(parseAuthResult)
    .finally(() => {
      if (refreshRequests.get(refreshToken) === request) refreshRequests.delete(refreshToken);
    });
  refreshRequests.set(refreshToken, request);
  return request;
}

// Burada refresh token oturumunu backend'de iptal ediyorum; yerel cookie temizliği çağıran sınırın finally bloğunda kalıyor.
export function logoutWithToken(refreshToken: string): Promise<void> {
  return apiRequest<void>("/api/auth/logout", {
    method: "POST",
    body: { refreshToken },
  });
}

// Burada JWT içeriğini decode etmek yerine imza, süre, güvenlik sürümü ve session kontrolünü backend'e yaptırıyorum.
export async function getCurrentUser(accessToken: string): Promise<AuthUser> {
  const payload = await apiRequest<unknown>("/api/users/me", { accessToken });
  return parseAuthUser(payload);
}
