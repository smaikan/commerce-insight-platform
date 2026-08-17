import { beforeEach, describe, expect, it, vi } from "vitest";

const { forwardMock } = vi.hoisted(() => ({ forwardMock: vi.fn() }));
vi.mock("@/modules/checkout/server/guest-commerce-proxy", () => ({
  checkoutProblemResponse: vi.fn(),
  forwardGuestCommerceRequest: forwardMock,
}));

import { POST } from "./route";

describe("guest return BFF route", () => {
  beforeEach(() => forwardMock.mockReset());

  // Burada misafir iade mutasyonunun session ve CSRF cookie'lerini seçip header üretimini güvenli proxy katmanına zorunlu bıraktığını doğruluyorum.
  it("forwards return creation with CSRF enabled", async () => {
    const orderId = "bb49d4c3-9752-4116-9179-657c8d6259b0";
    const request = new Request(`http://localhost/api/guest-orders/${orderId}/returns`, { method: "POST", body: "{}" });
    forwardMock.mockResolvedValue(new Response(null, { status: 201 }));
    await POST(request, { params: Promise.resolve({ orderId }) });
    expect(forwardMock).toHaveBeenCalledWith(request, `/api/guest-orders/${orderId}/returns`, {
      method: "POST",
      body: "{}",
      cookieNames: ["ecommerce_guest_orders", "ecommerce_guest_csrf"],
      csrf: true,
    });
  });
});
