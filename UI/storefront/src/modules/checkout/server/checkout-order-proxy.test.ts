import { beforeEach, describe, expect, it, vi } from "vitest";

const { readAccessTokenMock, readRefreshTokenMock, forwardGuestMock } = vi.hoisted(() => ({
  readAccessTokenMock: vi.fn(),
  readRefreshTokenMock: vi.fn(),
  forwardGuestMock: vi.fn(),
}));

vi.mock("server-only", () => ({}));
vi.mock("@/lib/auth/cookies", () => ({
  readAccessToken: readAccessTokenMock,
  readRefreshToken: readRefreshTokenMock,
}));
vi.mock("@/modules/checkout/server/guest-commerce-proxy", () => ({
  forwardGuestCommerceRequest: forwardGuestMock,
}));

import { forwardCheckoutOrderCancellation, forwardCheckoutOrderCancellationRead, forwardCheckoutOrderRead, forwardIyzicoCheckoutForm } from "./checkout-order-proxy";

const orderId = "bb49d4c3-9752-4116-9179-657c8d6259b0";
const idempotencyKey = "12345678-1234-1234-1234-123456789012";

describe("checkout order owner-aware proxy", () => {
  beforeEach(() => {
    vi.restoreAllMocks();
    readAccessTokenMock.mockReset().mockResolvedValue(null);
    readRefreshTokenMock.mockReset().mockResolvedValue(null);
    forwardGuestMock.mockReset().mockResolvedValue(new Response("{}", { status: 200 }));
  });

  // Burada üye initialize isteğinin doğru endpoint, Bearer ve değişmeyen idempotency anahtarıyla iletildiğini doğruluyorum.
  it("forwards member payment initialization with bearer ownership", async () => {
    readAccessTokenMock.mockResolvedValue("member-access-token");
    const fetchMock = vi.spyOn(globalThis, "fetch").mockResolvedValueOnce(new Response("{}", { status: 201, headers: { "Content-Type": "application/json" } }));

    const response = await forwardIyzicoCheckoutForm(new Request("http://localhost/api/checkout"), orderId, idempotencyKey);

    const [url, init] = fetchMock.mock.calls[0]!;
    const headers = init?.headers as Headers;
    expect(String(url)).toContain(`/api/orders/${orderId}/payments/iyzico/checkout-form`);
    expect(init?.method).toBe("POST");
    expect(headers.get("Authorization")).toBe("Bearer member-access-token");
    expect(headers.get("Idempotency-Key")).toBe(idempotencyKey);
    expect(response.status).toBe(201);
  });

  // Burada misafir initialize isteğinin guest order ve CSRF cookie sözleşmesini Origin doğrulaması yapan ortak proxy'ye aktardığını doğruluyorum.
  it("forwards guest payment initialization with session and csrf settings", async () => {
    const request = new Request("http://localhost/api/checkout", { headers: { Origin: "http://localhost" } });
    await forwardIyzicoCheckoutForm(request, orderId, idempotencyKey);

    expect(forwardGuestMock).toHaveBeenCalledWith(
      request,
      `/api/guest-orders/${orderId}/payments/iyzico/checkout-form`,
      {
        method: "POST",
        cookieNames: ["ecommerce_guest_orders", "ecommerce_guest_csrf"],
        idempotencyKey,
        csrf: true,
      },
    );
  });

  // Burada sipariş detayının da oturum türüne göre üye ve misafir sahiplik endpointlerinden okunduğunu doğruluyorum.
  it("selects the owner-scoped read endpoint", async () => {
    const request = new Request("http://localhost/api/checkout");
    await forwardCheckoutOrderRead(request, orderId);
    expect(forwardGuestMock).toHaveBeenCalledWith(request, `/api/guest-orders/${orderId}`, { method: "GET", cookieNames: ["ecommerce_guest_orders"] });

    readAccessTokenMock.mockResolvedValue("member-access-token");
    const fetchMock = vi.spyOn(globalThis, "fetch").mockResolvedValueOnce(new Response("{}", { status: 200 }));
    await forwardCheckoutOrderRead(request, orderId);
    expect(String(fetchMock.mock.calls[0]?.[0])).toContain(`/api/orders/${orderId}`);
  });

  // Burada iptal isteğinin üyede Bearer endpointine, misafirde session ve CSRF korumalı endpointine yönlendirildiğini doğruluyorum.
  it("forwards cancellation through the matching ownership channel", async () => {
    const request = new Request("http://localhost/api/checkout", { method: "POST", headers: { Origin: "http://localhost" } });
    await forwardCheckoutOrderCancellation(request, orderId);
    expect(forwardGuestMock).toHaveBeenCalledWith(
      request,
      `/api/guest-orders/${orderId}/cancel`,
      {
        method: "POST",
        cookieNames: ["ecommerce_guest_orders", "ecommerce_guest_csrf"],
        csrf: true,
      },
    );

    readAccessTokenMock.mockResolvedValue("member-access-token");
    const fetchMock = vi.spyOn(globalThis, "fetch").mockResolvedValueOnce(new Response("{}", { status: 200 }));
    await forwardCheckoutOrderCancellation(request, orderId);
    const [url, init] = fetchMock.mock.calls[0]!;
    expect(String(url)).toContain(`/api/orders/${orderId}/cancel`);
    expect(init?.method).toBe("POST");
  });

  // Burada polling okumasının üyede Bearer, misafirde yalnız guest order cookie'siyle doğru endpointten yapıldığını doğruluyorum.
  it("forwards cancellation polling through the matching ownership channel", async () => {
    const request = new Request("http://localhost/api/checkout");
    await forwardCheckoutOrderCancellationRead(request, orderId);
    expect(forwardGuestMock).toHaveBeenCalledWith(
      request,
      `/api/guest-orders/${orderId}/cancellation`,
      { method: "GET", cookieNames: ["ecommerce_guest_orders"] },
    );

    readAccessTokenMock.mockResolvedValue("member-access-token");
    const fetchMock = vi.spyOn(globalThis, "fetch").mockResolvedValueOnce(new Response("{}", { status: 200 }));
    await forwardCheckoutOrderCancellationRead(request, orderId);
    const [url, init] = fetchMock.mock.calls[0]!;
    expect(String(url)).toContain(`/api/orders/${orderId}/cancellation`);
    expect(init?.method).toBe("GET");
  });
});
