import { beforeEach, describe, expect, it, vi } from "vitest";

const mocks = vi.hoisted(() => ({
  requestPasswordReset: vi.fn(),
  resetCustomerPassword: vi.fn(),
  clearAuthCookies: vi.fn(),
}));

vi.mock("@/modules/auth/api", () => ({
  requestPasswordReset: mocks.requestPasswordReset,
  resetCustomerPassword: mocks.resetCustomerPassword,
}));
vi.mock("@/lib/auth/cookies", () => ({ clearAuthCookies: mocks.clearAuthCookies }));

import { ApiError } from "@/lib/api/problem";
import { forgotPasswordAction, resetPasswordAction } from "@/modules/auth/password-reset-actions";
import { initialForgotPasswordState, initialResetPasswordState } from "@/modules/auth/password-reset-state";

beforeEach(() => {
  vi.clearAllMocks();
  mocks.requestPasswordReset.mockResolvedValue(undefined);
  mocks.resetCustomerPassword.mockResolvedValue(undefined);
});

describe("forgot-password action", () => {
  // Burada farklı e-posta adresleri için aynı kullanıcı-varlığı-gizleyen başarı mesajının döndüğünü doğruluyorum.
  it("returns the same general result for every accepted email", async () => {
    const first = await forgotPasswordAction(initialForgotPasswordState, forgotForm("known@example.com"));
    const second = await forgotPasswordAction(initialForgotPasswordState, forgotForm("missing@example.com"));

    expect(first.status).toBe("success");
    expect(second.status).toBe("success");
    expect(first.message).toBe(second.message);
    expect(first.message).not.toMatch(/kayıtlı değil|kullanıcı bulunamadı/i);
  });

  // Burada geçersiz e-postanın API'ye hiç gönderilmeden alan hatasına dönüştüğünü doğruluyorum.
  it("blocks an invalid email before the API call", async () => {
    const result = await forgotPasswordAction(initialForgotPasswordState, forgotForm("invalid"));

    expect(result).toMatchObject({ status: "error", fieldErrors: { email: expect.any(String) } });
    expect(mocks.requestPasswordReset).not.toHaveBeenCalled();
  });

  // Burada 429 kodunun kontrollü bekleme mesajına dönüşüp otomatik tekrar üretmediğini doğruluyorum.
  it("maps rate limiting without retry", async () => {
    mocks.requestPasswordReset.mockRejectedValueOnce(new ApiError({
      title: "Too many requests",
      status: 429,
      code: "rate_limit_exceeded",
    }));

    const result = await forgotPasswordAction(initialForgotPasswordState, forgotForm("user@example.com"));

    expect(result.message).toMatch(/bekleyip yeniden deneyin/i);
    expect(mocks.requestPasswordReset).toHaveBeenCalledOnce();
  });
});

describe("reset-password action", () => {
  // Burada token yokken API isteği gönderilmediğini doğruluyorum.
  it("does not call the API without a token", async () => {
    const result = await resetPasswordAction("", initialResetPasswordState, resetForm("secret7", "secret7"));

    expect(result.status).toBe("invalid-link");
    expect(mocks.resetCustomerPassword).not.toHaveBeenCalled();
  });

  // Burada parolalar eşleşmediğinde hassas tokenın API'ye gönderilmediğini doğruluyorum.
  it("does not call the API when passwords do not match", async () => {
    const result = await resetPasswordAction("fixture-token", initialResetPasswordState, resetForm("secret7", "different7"));

    expect(result).toMatchObject({ status: "error", fieldErrors: { confirmPassword: expect.any(String) } });
    expect(mocks.resetCustomerPassword).not.toHaveBeenCalled();
  });

  // Burada 204 sonrasında yerel oturum çerezlerinin temizlenip tam sayfa login yönlendirmesine hazır başarı durumu döndüğünü doğruluyorum.
  it("clears the session and redirects after a successful reset", async () => {
    const result = await resetPasswordAction(
      "fixture-token",
      initialResetPasswordState,
      resetForm("secret7", "secret7"),
    );

    expect(mocks.resetCustomerPassword).toHaveBeenCalledWith({ token: "fixture-token", newPassword: "secret7" });
    expect(mocks.clearAuthCookies).toHaveBeenCalledOnce();
    expect(result).toMatchObject({ status: "success", message: expect.stringMatching(/değiştirildi/i) });
  });

  // Burada 401 ve 409 kodlarının aynı güvenli geçersiz bağlantı durumuna dönüştüğünü doğruluyorum.
  it.each([
    [401, "invalid_or_expired_reset_token"],
    [409, "concurrency_conflict"],
  ])("maps %s %s to the invalid-link state", async (status, code) => {
    mocks.resetCustomerPassword.mockRejectedValueOnce(new ApiError({ title: "Reset failed", status, code }));

    const result = await resetPasswordAction("fixture-token", initialResetPasswordState, resetForm("secret7", "secret7"));

    expect(result.status).toBe("invalid-link");
    expect(JSON.stringify(result)).not.toContain("fixture-token");
  });

  // Burada reset 429 cevabının otomatik tekrar yapılmadan kullanıcıya bekleme mesajı verdiğini doğruluyorum.
  it("does not retry a rate-limited reset", async () => {
    mocks.resetCustomerPassword.mockRejectedValueOnce(new ApiError({
      title: "Too many requests",
      status: 429,
      code: "rate_limit_exceeded",
    }));

    const result = await resetPasswordAction("fixture-token", initialResetPasswordState, resetForm("secret7", "secret7"));

    expect(result.message).toMatch(/bekleyip yeniden deneyin/i);
    expect(mocks.resetCustomerPassword).toHaveBeenCalledOnce();
  });
});

// Burada test e-posta değerini gerçek kullanıcı verisi olmadan FormData sınırında hazırlıyorum.
function forgotForm(email: string): FormData {
  const formData = new FormData();
  formData.set("email", email);
  return formData;
}

// Burada test parolalarını yalnız izole doğrulama isteği için FormData sınırında hazırlıyorum.
function resetForm(newPassword: string, confirmPassword: string): FormData {
  const formData = new FormData();
  formData.set("newPassword", newPassword);
  formData.set("confirmPassword", confirmPassword);
  return formData;
}
