import { afterEach, describe, expect, it, vi } from "vitest";

vi.mock("server-only", () => ({}));

vi.mock("@/lib/site-config", () => ({
  siteConfig: { apiUrl: "https://api.test" },
}));

import { apiPost } from "@/lib/api/client";
import { ApiError } from "@/lib/api/problem";

describe("public API POST client", () => {
  afterEach(() => vi.unstubAllGlobals());

  // Burada forgot-password 202 boş cevabının JSON ayrıştırılmadan başarı sayıldığını doğruluyorum.
  it("accepts an empty 202 response", async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response(null, { status: 202 }));
    vi.stubGlobal("fetch", fetchMock);

    await expect(apiPost<void>("/api/auth/forgot-password", { email: "user@example.com" })).resolves.toBeUndefined();
    expect(fetchMock).toHaveBeenCalledOnce();
    expect(fetchMock.mock.calls[0]?.[1]).toMatchObject({ method: "POST", cache: "no-store" });
  });

  // Burada reset-password 204 boş cevabının gövde okumadan tamamlandığını doğruluyorum.
  it("accepts an empty 204 response", async () => {
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue(new Response(null, { status: 204 })));

    await expect(apiPost<void>("/api/auth/reset-password", { token: "fixture-token", newPassword: "secret7" })).resolves.toBeUndefined();
  });

  // Burada 429 ProblemDetails kodu ve Retry-After bilgisinin korunup isteğin otomatik tekrarlanmadığını doğruluyorum.
  it("preserves rate-limit details without retrying", async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response(JSON.stringify({
      title: "Too many requests",
      status: 429,
      code: "rate_limit_exceeded",
    }), {
      status: 429,
      headers: { "Content-Type": "application/problem+json", "Retry-After": "60" },
    }));
    vi.stubGlobal("fetch", fetchMock);

    const error = await apiPost<void>("/api/auth/forgot-password", { email: "user@example.com" }).catch((value) => value);

    expect(error).toBeInstanceOf(ApiError);
    expect(error.problem).toMatchObject({ code: "rate_limit_exceeded", retryAfter: "60" });
    expect(fetchMock).toHaveBeenCalledOnce();
  });
});
