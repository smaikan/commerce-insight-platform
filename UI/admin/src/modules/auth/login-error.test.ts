import { describe, expect, it } from "vitest";
import { ApiError } from "../../lib/api/problem";
import { loginError } from "./login-error";

describe("login error", () => {
  // Burada paylaşılan BFF kotasının 429 cevabını kullanıcıyı yanlış biçimde parola denemesiyle suçlamadan gösterdiğimi doğruluyorum.
  it("uses neutral rate-limit language and Retry-After", () => {
    const state = loginError(new ApiError({
      title: "Too many requests",
      status: 429,
      retryAfter: "37",
    }), "admin@example.com");

    expect(state).toMatchObject({
      status: "error",
      email: "admin@example.com",
      message: "Giriş trafiği kısa süreli sınırlandı. Lütfen 37 saniye sonra tekrar deneyin.",
    });
    expect(state.message).not.toContain("giriş denemesi");
  });

  // Burada kimlik doğrulama mesajının hesap varlığını veya gönderilen parolayı sızdırmadığını doğruluyorum.
  it("keeps unauthorized errors generic", () => {
    const state = loginError(
      new ApiError({ title: "Unauthorized", status: 401, detail: "User exists but password secret-value failed" }),
      "admin@example.com",
    );

    expect(state.message).toBe("E-posta veya parola hatalı.");
    expect(JSON.stringify(state)).not.toContain("secret-value");
  });
});
