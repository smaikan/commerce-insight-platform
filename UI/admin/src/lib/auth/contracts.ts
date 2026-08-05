import type { components } from "../../generated/api";
import { ApiError } from "../api/problem";
import { ACTIVE_USER_STATUS, ADMIN_ROLE } from "./constants";

export type AuthResult = components["schemas"]["AuthResultDto"];
export type AuthTokens = components["schemas"]["AuthTokensDto"];
export type AuthUser = components["schemas"]["UserDto"];
export type AdminSession = {
  accessToken: string;
  user: AuthUser;
};

// Burada güvenlik açısından kritik auth cevabını TypeScript tipine güvenmeden çalışma zamanında doğruluyorum.
export function parseAuthResult(value: unknown, now = Date.now()): AuthResult {
  if (!isRecord(value) || !isAuthUser(value.user) || !isRecord(value.tokens)) throw invalidAuthResponse();

  const tokens = value.tokens;
  const accessToken = requiredString(tokens.accessToken);
  const refreshToken = requiredString(tokens.refreshToken);
  const accessTokenExpiresAt = validFutureDate(tokens.accessTokenExpiresAt, now);
  const refreshTokenExpiresAt = validFutureDate(tokens.refreshTokenExpiresAt, now);
  if (!accessToken || !refreshToken || !accessTokenExpiresAt || !refreshTokenExpiresAt) throw invalidAuthResponse();

  return {
    user: value.user,
    tokens: {
      accessToken,
      accessTokenExpiresAt,
      refreshToken,
      refreshTokenExpiresAt,
    },
  };
}

// Burada backend tarafından doğrulanan kullanıcının hem aktif hem de Admin rolünde olmasını zorunlu tutuyorum.
export function assertActiveAdmin(user: AuthUser): void {
  if (user.role !== ADMIN_ROLE || user.status !== ACTIVE_USER_STATUS) {
    throw new ApiError({
      title: "Yönetici yetkisi gerekli",
      status: 403,
      code: "admin_role_required",
      detail: "Bu panel yalnızca aktif yönetici hesaplarına açıktır.",
    });
  }
}

// Burada /users/me yanıtının rol kararında kullanılmadan önce beklenen güvenli kullanıcı biçiminde olduğunu doğruluyorum.
export function parseAuthUser(value: unknown): AuthUser {
  if (!isAuthUser(value)) throw invalidAuthResponse();
  return value;
}

// Burada auth kullanıcı DTO'sunun yetki için gerekli tüm alanlarını çalışma zamanında kontrol ediyorum.
function isAuthUser(value: unknown): value is AuthUser {
  if (!isRecord(value)) return false;
  return (
    Boolean(requiredString(value.id)) &&
    Boolean(requiredString(value.email)) &&
    Boolean(requiredString(value.firstName)) &&
    Boolean(requiredString(value.lastName)) &&
    (value.role === 1 || value.role === 2) &&
    (value.status === 1 || value.status === 2 || value.status === 3) &&
    Boolean(validDate(value.createdAt))
  );
}

// Burada auth servisindeki bozuk veya beklenmeyen cevabı güvenli bir upstream hatasına çeviriyorum.
function invalidAuthResponse(): ApiError {
  return new ApiError({
    title: "Geçersiz kimlik doğrulama yanıtı",
    status: 502,
    code: "invalid_auth_response",
    detail: "Kimlik doğrulama servisi beklenen yanıtı döndürmedi.",
  });
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return Boolean(value && typeof value === "object" && !Array.isArray(value));
}

function requiredString(value: unknown): string | undefined {
  return typeof value === "string" && value.trim() ? value : undefined;
}

function validDate(value: unknown): string | undefined {
  return typeof value === "string" && Number.isFinite(Date.parse(value)) ? value : undefined;
}

function validFutureDate(value: unknown, now: number): string | undefined {
  const parsed = validDate(value);
  return parsed && Date.parse(parsed) > now ? parsed : undefined;
}
