import { describe, expect, it, vi } from "vitest";

import {
  checkoutProblemMessage,
  initializeIyzicoCheckoutForm,
  isCheckoutChallengeRequired,
  loadCheckoutOrder,
  paymentIntentKey,
  redirectToPaymentPage,
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
      const order = await loadCheckoutOrder("bb49d4c3-9752-4116-9179-657c8d6259b0");
      expect(order.items[0]?.variantValue).toBe("Pudra");
      expect(fetchMock).toHaveBeenCalledTimes(1);
      expect(fetchMock).toHaveBeenCalledWith("/api/checkout/orders/bb49d4c3-9752-4116-9179-657c8d6259b0", expect.objectContaining({ method: "GET" }));
      expect(fetchMock.mock.calls.some(([path]) => String(path).includes("/api/products/"))).toBe(false);
    } finally {
      vi.unstubAllGlobals();
    }
  });

  // Burada aynı order ve idempotency anahtarıyla eşzamanlı başlayan isteklerin tek hosted ödeme oturumuna indirgendiğini doğruluyorum.
  it("deduplicates rapid payment initialization", async () => {
    let resolveResponse!: (response: Response) => void;
    const fetchMock = vi.fn(() => new Promise<Response>((resolve) => { resolveResponse = resolve; }));
    vi.stubGlobal("fetch", fetchMock);

    try {
      const first = initializeIyzicoCheckoutForm("bb49d4c3-9752-4116-9179-657c8d6259b0", "12345678-1234-1234-1234-123456789012");
      const second = initializeIyzicoCheckoutForm("bb49d4c3-9752-4116-9179-657c8d6259b0", "12345678-1234-1234-1234-123456789012");
      expect(fetchMock).toHaveBeenCalledTimes(1);
      expect(fetchMock).toHaveBeenCalledWith(
        "/api/checkout/orders/bb49d4c3-9752-4116-9179-657c8d6259b0/payments/iyzico/checkout-form",
        expect.objectContaining({ method: "POST", headers: { "Idempotency-Key": "12345678-1234-1234-1234-123456789012" } }),
      );
      resolveResponse(new Response(JSON.stringify({ paymentPageUrl: "https://sandbox-api.iyzipay.com/checkout" }), { status: 201, headers: { "Content-Type": "application/json" } }));
      await expect(Promise.all([first, second])).resolves.toHaveLength(2);
    } finally {
      vi.unstubAllGlobals();
    }
  });

  // Burada retry anahtarının sabit kaldığını, yalnız açık yeni denemenin farklı bir anahtar ürettiğini doğruluyorum.
  it("keeps a stable payment key until a new attempt", () => {
    const values = new Map<string, string>();
    const storage = {
      getItem: (key: string) => values.get(key) ?? null,
      setItem: (key: string, value: string) => { values.set(key, value); },
      removeItem: (key: string) => { values.delete(key); },
    };
    const first = paymentIntentKey("bb49d4c3-9752-4116-9179-657c8d6259b0", false, storage);
    expect(paymentIntentKey("bb49d4c3-9752-4116-9179-657c8d6259b0", false, storage)).toBe(first);
    expect(paymentIntentKey("bb49d4c3-9752-4116-9179-657c8d6259b0", true, storage)).not.toBe(first);
  });

  // Burada yalnız mutlak HTTPS hosted ödeme URL'sinin browser yönlendirmesine ulaşabildiğini doğruluyorum.
  it("redirects only to a valid HTTPS payment page", () => {
    const assign = vi.fn();
    redirectToPaymentPage({ paymentId: "a", orderId: "b", provider: 1, status: 0, amount: 10, paymentPageUrl: "https://sandbox-api.iyzipay.com/checkout", expiresAt: null }, assign);
    expect(assign).toHaveBeenCalledWith("https://sandbox-api.iyzipay.com/checkout");
    expect(() => redirectToPaymentPage({ paymentId: "a", orderId: "b", provider: 1, status: 0, amount: 10, paymentPageUrl: null, expiresAt: null }, assign)).toThrow();
    expect(() => redirectToPaymentPage({ paymentId: "a", orderId: "b", provider: 1, status: 0, amount: 10, paymentPageUrl: "http://unsafe.example", expiresAt: null }, assign)).toThrow();
  });
});
