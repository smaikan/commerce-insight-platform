import { describe, expect, it } from "vitest";
import { ApiError } from "../api/problem";
import { assertActiveAdmin, parseAuthResult, parseAuthUser } from "./contracts";

const now = Date.parse("2026-08-03T10:00:00Z");

// Burada auth sözleşmesi testlerinde kullandığım güvenli ve sentetik kullanıcı/token cevabını üretiyorum.
function authPayload(role: 1 | 2 = 2) {
  return {
    user: {
      id: "U00001",
      email: "admin@example.test",
      firstName: "Test",
      lastName: "Admin",
      phoneNumber: null,
      role,
      status: 1,
      lastLoginAt: null,
      createdAt: "2026-08-01T10:00:00Z",
      updatedAt: null,
    },
    tokens: {
      accessToken: "synthetic-access-token",
      accessTokenExpiresAt: "2026-08-03T11:00:00Z",
      refreshToken: "synthetic-refresh-token",
      refreshTokenExpiresAt: "2026-09-03T10:00:00Z",
    },
  };
}

describe("auth contracts", () => {
  // Burada geçerli backend auth cevabının iki token ve Admin kullanıcıyla kabul edildiğini doğruluyorum.
  it("parses a valid auth response", () => {
    const result = parseAuthResult(authPayload(), now);
    expect(result.user.role).toBe(2);
    expect(result.tokens.accessTokenExpiresAt).toBe("2026-08-03T11:00:00Z");
  });

  // Burada süresi geçmiş veya eksik token cevabının cookie'ye yazılmadan önce reddedildiğini doğruluyorum.
  it("rejects expired and malformed token responses", () => {
    const expired = authPayload();
    expired.tokens.accessTokenExpiresAt = "2026-08-03T09:59:59Z";
    expect(() => parseAuthResult(expired, now)).toThrow(ApiError);
    expect(() => parseAuthResult({ user: expired.user, tokens: {} }, now)).toThrow(ApiError);
  });

  // Burada geçerli Customer kullanıcının auth cevabı olsa bile Admin yetki sınırından geçemediğini doğruluyorum.
  it("rejects a non-admin user", () => {
    const user = parseAuthUser(authPayload(1).user);
    expect(() => assertActiveAdmin(user)).toThrowError(
      expect.objectContaining({ problem: expect.objectContaining({ status: 403 }) }),
    );
  });
});
