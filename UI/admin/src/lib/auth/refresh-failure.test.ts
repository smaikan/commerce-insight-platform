import { describe, expect, it } from "vitest";
import { ApiError } from "../api/problem";
import { refreshFailureDecision } from "./refresh-failure";

describe("refresh failure decision", () => {
  // Burada 429 durumunda kullanılabilir refresh cookie'sini koruyup kontrollü bekleme bilgisi taşıdığımı doğruluyorum.
  it("preserves the session on rate limiting", () => {
    const error = new ApiError({
      title: "Too many requests",
      status: 429,
      retryAfter: "60",
    });

    expect(refreshFailureDecision(error)).toEqual({
      reason: "refresh_rate_limited",
      clearCookies: false,
      retryAfter: 60,
    });
  });

  // Burada geçici upstream hatasında refresh cookie'sini yeniden deneme için koruduğumu doğruluyorum.
  it("preserves the session on transient upstream failures", () => {
    const error = new ApiError({ title: "Unavailable", status: 503 });

    expect(refreshFailureDecision(error)).toEqual({
      reason: "verification_failed",
      clearCookies: false,
    });
  });

  // Burada kesin geçersiz veya yetkisiz auth sonuçlarında yerel oturumun temizleneceğini doğruluyorum.
  it.each([
    new ApiError({ title: "Unauthorized", status: 401 }),
    new ApiError({ title: "Forbidden", status: 403 }),
    new ApiError({ title: "Invalid response", status: 502, code: "invalid_auth_response" }),
  ])("clears an invalid session for %#", (error) => {
    expect(refreshFailureDecision(error).clearCookies).toBe(true);
  });
});
