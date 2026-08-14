import { beforeEach, describe, expect, it, vi } from "vitest";

const { apiPostMock } = vi.hoisted(() => ({ apiPostMock: vi.fn() }));

vi.mock("server-only", () => ({}));
vi.mock("@/lib/api/client", () => ({ apiPost: apiPostMock }));

import { claimGuestSession, requestPasswordReset, resetCustomerPassword } from "@/modules/auth/api";

beforeEach(() => apiPostMock.mockReset());

describe("password reset API contract", () => {
  // Burada forgot-password çağrısının yalnız normalize edilmiş e-posta gövdesini gönderdiğini doğruluyorum.
  it("posts the forgot-password payload", async () => {
    apiPostMock.mockResolvedValueOnce(undefined);

    await requestPasswordReset({ email: "user@example.com" });

    expect(apiPostMock).toHaveBeenCalledWith("/api/auth/forgot-password", { email: "user@example.com" });
  });

  // Burada reset-password çağrısının yalnız token ve yeni parola alanlarını gönderdiğini doğruluyorum.
  it("posts only the reset-password contract fields", async () => {
    apiPostMock.mockResolvedValueOnce(undefined);

    await resetCustomerPassword({ token: "fixture-token", newPassword: "secret7" });

    expect(apiPostMock).toHaveBeenCalledWith("/api/auth/reset-password", {
      token: "fixture-token",
      newPassword: "secret7",
    });
  });
});

describe("guest session claim contract", () => {
  // Burada login claim cevabının cart ve favoriteCount alanlarıyla authoritative biçimde korunduğunu doğruluyorum.
  it("returns the claimed cart and favorite count", async () => {
    apiPostMock.mockResolvedValueOnce({
      cart: {
        id: "6cf01506-270c-45a8-8d0c-e957a2ae873c",
        concurrencyToken: "b1113ea0-47a1-4fa2-9170-f97500d1fd15",
        items: [{ variantName: "Renk", variantValue: "Pudra" }],
        totalQuantity: 1,
        subTotal: 1200,
        hasUnavailableItems: false,
        hasPriceChanges: false,
      },
      favoriteCount: 3,
    });

    const result = await claimGuestSession("access-token", "A".repeat(64));

    expect(result?.cart.items[0]?.variantValue).toBe("Pudra");
    expect(result?.favoriteCount).toBe(3);
    expect(apiPostMock).toHaveBeenCalledWith("/api/guest-session/claim", undefined, {
      headers: {
        Authorization: "Bearer access-token",
        Cookie: `ecommerce_guest_cart=${"A".repeat(64)}`,
      },
    });
  });

  // Burada claim başarısızlığında login oturumunu bozmadan guest cookie'nin korunabilmesi için null döndürüyorum.
  it("keeps claim failure controlled", async () => {
    apiPostMock.mockRejectedValueOnce(new Error("unavailable"));
    await expect(claimGuestSession("access-token", "A".repeat(64))).resolves.toBeNull();
  });
});
