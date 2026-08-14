import { describe, expect, it } from "vitest";

import { validateLogin, validateRegister } from "./validation";

describe("auth form validation", () => {
  // Burada login e-postasının normalize edildiğini ve şifrenin değiştirilmeden API girdisine taşındığını doğruluyorum.
  it("normalizes a valid login payload", () => {
    const data = new FormData();
    data.set("email", "  USER@Example.COM ");
    data.set("password", "secret7");

    expect(validateLogin(data)).toEqual({ success: true, data: { email: "user@example.com", password: "secret7" } });
  });

  // Burada kayıt sınırlarının eksik ad, kısa şifre, eşleşmeyen doğrulama ve yasal onayı ayrı alanlarda bildirdiğini doğruluyorum.
  it("returns field-level register errors without retaining passwords", () => {
    const data = new FormData();
    data.set("email", "invalid");
    data.set("password", "123");
    data.set("confirmPassword", "456");

    const result = validateRegister(data);
    expect(result.success).toBe(false);
    if (!result.success) {
      expect(result.errors).toMatchObject({ firstName: expect.any(String), lastName: expect.any(String), email: expect.any(String), password: expect.any(String), confirmPassword: expect.any(String), legalConsent: expect.any(String) });
      expect(result.values).not.toHaveProperty("password");
      expect(result.values).not.toHaveProperty("confirmPassword");
    }
  });

  // Burada opsiyonel telefonun boşken null, geçerli kayıt alanlarının ise OpenAPI gövdesi olarak üretildiğini doğruluyorum.
  it("builds a valid registration payload", () => {
    const data = new FormData();
    Object.entries({ firstName: "Ada", lastName: "Lovelace", email: "ada@example.com", password: "secret7", confirmPassword: "secret7", phoneNumber: "", legalConsent: "accepted" })
      .forEach(([key, value]) => data.set(key, value));

    expect(validateRegister(data)).toEqual({
      success: true,
      data: { firstName: "Ada", lastName: "Lovelace", email: "ada@example.com", password: "secret7", phoneNumber: null },
    });
  });

  // Burada form isteği elle gönderilse bile yasal onay bulunmadan API kayıt gövdesinin üretilmediğini doğruluyorum.
  it("rejects registration when legal consent is missing", () => {
    const data = new FormData();
    Object.entries({ firstName: "Ada", lastName: "Lovelace", email: "ada@example.com", password: "secret7", confirmPassword: "secret7", phoneNumber: "" })
      .forEach(([key, value]) => data.set(key, value));

    const result = validateRegister(data);

    expect(result.success).toBe(false);
    if (!result.success) expect(result.errors.legalConsent).toBe("Devam etmek için üyelik koşullarını onaylayın.");
  });
});
