import { describe, expect, it } from "vitest";

import { parseAuthResult, parseRegisterResult } from "./contracts";

const user = {
  id: "user-1",
  email: "ada@example.com",
  firstName: "Ada",
  lastName: "Lovelace",
  phoneNumber: null,
  role: 1 as const,
  status: 1 as const,
  lastLoginAt: null,
  createdAt: "2026-08-13T10:00:00Z",
  updatedAt: null,
};

describe("auth runtime contracts", () => {
  // Burada OpenAPI ile uyumlu, geçerli tarihli login cevabının cookie katmanına kabul edildiğini doğruluyorum.
  it("accepts a complete auth result", () => {
    const value = {
      user,
      tokens: {
        accessToken: "access-token",
        accessTokenExpiresAt: "2026-08-13T10:15:00Z",
        refreshToken: "refresh-token",
        refreshTokenExpiresAt: "2026-08-20T10:00:00Z",
      },
    };
    expect(parseAuthResult(value)).toBe(value);
  });

  // Burada eksik refresh tokenı bulunan cevabın çerez yazılmadan reddedildiğini doğruluyorum.
  it("rejects an incomplete auth result", () => {
    expect(() => parseAuthResult({ user, tokens: { accessToken: "token" } })).toThrow(/token/);
  });

  // Burada kayıt cevabının zorunlu kullanıcı sözleşmesine uymasını şart koşuyorum.
  it("validates register results", () => {
    expect(parseRegisterResult({ user })).toEqual({ user });
    expect(() => parseRegisterResult({ user: { email: "missing@example.com" } })).toThrow(/kayıt/);
  });
});
