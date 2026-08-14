import { describe, expect, it, vi } from "vitest";

const { apiPostMock } = vi.hoisted(() => ({ apiPostMock: vi.fn() }));

vi.mock("server-only", () => ({}));
vi.mock("@/lib/api/client", () => ({ apiPost: apiPostMock }));

import { claimGuestSession } from "@/modules/auth/api";

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
