import type { components } from "@/generated/api";

export type LoginPayload = components["schemas"]["LoginRequest"];
export type RegisterPayload = components["schemas"]["RegisterUserCommand"];
export type AuthResult = components["schemas"]["AuthResultDto"];
export type AuthTokens = components["schemas"]["AuthTokensDto"];
export type RegisterResult = components["schemas"]["RegisterUserResultDto"];

// Burada token cevabını çerez yazmadan önce alan, tarih ve kullanıcı şekli bakımından çalışma zamanında doğruluyorum.
export function parseAuthResult(value: unknown): AuthResult {
  if (!isRecord(value) || !isUser(value.user) || !isRecord(value.tokens)) {
    throw new Error("API geçersiz bir oturum cevabı döndürdü.");
  }

  const tokens = value.tokens;
  if (
    !isNonEmptyString(tokens.accessToken) ||
    !isValidDate(tokens.accessTokenExpiresAt) ||
    !isNonEmptyString(tokens.refreshToken) ||
    !isValidDate(tokens.refreshTokenExpiresAt)
  ) {
    throw new Error("API geçersiz bir token cevabı döndürdü.");
  }

  return value as AuthResult;
}

// Burada kayıt cevabının OpenAPI'deki zorunlu kullanıcı alanlarını gerçekten taşıdığını doğruluyorum.
export function parseRegisterResult(value: unknown): RegisterResult {
  if (!isRecord(value) || !isUser(value.user)) {
    throw new Error("API geçersiz bir kayıt cevabı döndürdü.");
  }
  return value as RegisterResult;
}

function isUser(value: unknown): boolean {
  return Boolean(
    isRecord(value) &&
      isNonEmptyString(value.id) &&
      isNonEmptyString(value.email) &&
      isNonEmptyString(value.firstName) &&
      isNonEmptyString(value.lastName) &&
      (value.role === 1 || value.role === 2) &&
      (value.status === 1 || value.status === 2 || value.status === 3) &&
      isValidDate(value.createdAt),
  );
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return Boolean(value && typeof value === "object");
}

function isNonEmptyString(value: unknown): value is string {
  return typeof value === "string" && value.length > 0;
}

function isValidDate(value: unknown): value is string {
  return typeof value === "string" && !Number.isNaN(Date.parse(value));
}
