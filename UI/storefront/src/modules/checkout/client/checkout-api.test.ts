import { describe, expect, it, vi } from "vitest";

import {
  checkoutProblemMessage,
  isCheckoutChallengeRequired,
  loadGuestOrder,
} from "./checkout-api";

describe("checkout challenge recovery", () => {
  // Burada yalnız doğru status ve problem code birleşiminin güvenlik doğrulama akışını açtığını doğruluyorum.
  it("recognizes the documented Turnstile challenge", () => {
    expect(isCheckoutChallengeRequired({ status: 428, code: "guest_checkout_challenge_required" })).toBe(true);
    expect(isCheckoutChallengeRequired({ status: 428, code: "another_problem" })).toBe(false);
    expect(isCheckoutChallengeRequired({ status: 409, code: "guest_checkout_challenge_required" })).toBe(false);
  });

  // Burada kullanıcı mesajının ortam yapılandırması sızdırmadan gerçek kurtarma adımını anlattığını doğruluyorum.
  it("uses an actionable challenge message", () => {
    expect(checkoutProblemMessage({ status: 428, code: "guest_checkout_challenge_required" }))
      .toBe("Devam etmek için güvenlik doğrulamasını tamamlayın.");
  });

  // Burada guest sipariş detayının snapshot alanlarını tek order isteğinden aldığını ve ürün detayına N+1 çağrı üretmediğini doğruluyorum.
  it("loads guest order variant snapshots without product requests", async () => {
    const fetchMock = vi.fn<(input: RequestInfo | URL) => Promise<Response>>(async () => new Response(JSON.stringify({
      id: "bb49d4c3-9752-4116-9179-657c8d6259b0",
      items: [{ variantName: "Renk", variantValue: "Pudra" }],
    }), {
      status: 200,
      headers: { "Content-Type": "application/json" },
    }));
    vi.stubGlobal("fetch", fetchMock);

    try {
      const order = await loadGuestOrder("bb49d4c3-9752-4116-9179-657c8d6259b0");
      expect(order.items[0]?.variantValue).toBe("Pudra");
      expect(fetchMock).toHaveBeenCalledTimes(1);
      expect(fetchMock.mock.calls.some(([path]) => String(path).includes("/api/products/"))).toBe(false);
    } finally {
      vi.unstubAllGlobals();
    }
  });
});
