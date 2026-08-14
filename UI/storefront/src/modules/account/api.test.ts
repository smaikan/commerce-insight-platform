import { beforeEach, describe, expect, it, vi } from "vitest";

const { requestMock } = vi.hoisted(() => ({ requestMock: vi.fn() }));

vi.mock("server-only", () => ({}));

vi.mock("@/lib/api/authenticated-client", () => ({
  authenticatedApiRequest: requestMock,
}));

import { getAccountOrder } from "@/modules/account/api";

describe("account API", () => {
  beforeEach(() => requestMock.mockReset());

  // Burada sipariş detayının snapshot alanları için yalnız tek order isteği ürettiğini ve ürün endpointine N+1 çağrı yapmadığını doğruluyorum.
  it("loads an order without product detail requests", async () => {
    requestMock.mockResolvedValue({ id: "order" });
    await getAccountOrder("bb49d4c3-9752-4116-9179-657c8d6259b0");
    expect(requestMock).toHaveBeenCalledTimes(1);
    expect(requestMock).toHaveBeenCalledWith("/api/orders/bb49d4c3-9752-4116-9179-657c8d6259b0");
    expect(requestMock.mock.calls.some(([path]) => String(path).includes("/api/products/"))).toBe(false);
  });
});
