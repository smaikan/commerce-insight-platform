import { describe, expect, it } from "vitest";
import { adminCookieNames } from "./constants";
import { isProtectedAdminPath, safeReturnTo, sessionCookiePolicy, validateLoginForm } from "./policy";

describe("auth policy", () => {
  // Burada yalnız bilinen admin route öneklerinin Proxy tarafından korumalı kabul edildiğini doğruluyorum.
  it("classifies protected admin paths", () => {
    expect(isProtectedAdminPath("/dashboard")).toBe(true);
    expect(isProtectedAdminPath("/products/P00001")).toBe(true);
    expect(isProtectedAdminPath("/contact-messages/11111111-1111-4111-8111-111111111111")).toBe(true);
    expect(isProtectedAdminPath("/login")).toBe(false);
    expect(isProtectedAdminPath("/productivity")).toBe(false);
  });

  // Burada dış origin, protokol göreli, ters slash ve auth döngüsü hedeflerinin dashboard'a kapatıldığını doğruluyorum.
  it("accepts only safe same-origin return targets", () => {
    expect(safeReturnTo("/products?pageNumber=2")).toBe("/products?pageNumber=2");
    expect(safeReturnTo("https://evil.example/steal")).toBe("/dashboard");
    expect(safeReturnTo("//evil.example/steal")).toBe("/dashboard");
    expect(safeReturnTo("/\\evil.example")).toBe("/dashboard");
    expect(safeReturnTo("/api/auth/refresh")).toBe("/dashboard");
    expect(safeReturnTo("/access-denied")).toBe("/dashboard");
  });

  // Burada login doğrulamasının e-posta ve parolayı kontrol ederken parola değerini hata sonucuna taşımadığını doğruluyorum.
  it("validates login input without returning the password on errors", () => {
    const form = new FormData();
    form.set("email", "invalid");
    form.set("password", "");
    const result = validateLoginForm(form);

    expect(result.ok).toBe(false);
    expect(result).not.toHaveProperty("password");
    if (!result.ok) {
      expect(result.fieldErrors.email).toBeDefined();
      expect(result.fieldErrors.password).toBeDefined();
    }
  });

  // Burada production cookie adları ve seçeneklerinin host-only HttpOnly güvenlik sözleşmesini koruduğunu doğruluyorum.
  it("uses hardened production cookie policy", () => {
    const names = adminCookieNames(true);
    const policy = sessionCookiePolicy(new Date("2026-09-01T00:00:00Z"), true);

    expect(names.access.startsWith("__Host-")).toBe(true);
    expect(names.refresh.startsWith("__Host-")).toBe(true);
    expect(policy).toMatchObject({ httpOnly: true, secure: true, sameSite: "lax", path: "/" });
    expect(policy).not.toHaveProperty("domain");
  });
});
