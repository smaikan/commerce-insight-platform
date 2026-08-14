import { describe, expect, it } from "vitest";

import { validateForgotPassword, validateResetPassword } from "@/modules/auth/password-reset-validation";

describe("password reset validation", () => {
  // Burada e-posta değerinin kırpılıp normalize edilerek geçerli forgot-password gövdesine dönüştüğünü doğruluyorum.
  it("normalizes a valid email", () => {
    const formData = new FormData();
    formData.set("email", "  USER@Example.com ");

    expect(validateForgotPassword(formData)).toEqual({ success: true, data: { email: "user@example.com" } });
  });

  // Burada geçersiz e-postanın API çağrısından önce alan hatasına dönüştüğünü doğruluyorum.
  it("rejects an invalid email", () => {
    const formData = new FormData();
    formData.set("email", "invalid-email");

    expect(validateForgotPassword(formData)).toMatchObject({ success: false, errors: { email: expect.any(String) } });
  });

  // Burada eşleşmeyen yeni parolaların API isteğine dönüşmeden reddedildiğini doğruluyorum.
  it("rejects mismatched passwords", () => {
    const formData = new FormData();
    formData.set("newPassword", "secret7");
    formData.set("confirmPassword", "different7");

    expect(validateResetPassword(formData)).toMatchObject({ success: false, errors: { confirmPassword: expect.any(String) } });
  });

  // Burada API'nin 6–128 karakter aralığındaki eşleşen parolalarının kabul edildiğini doğruluyorum.
  it("accepts matching passwords within the API limits", () => {
    const formData = new FormData();
    formData.set("newPassword", "secret7");
    formData.set("confirmPassword", "secret7");

    expect(validateResetPassword(formData)).toEqual({ success: true, data: { newPassword: "secret7" } });
  });
});
