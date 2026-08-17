import { beforeEach, describe, expect, it, vi } from "vitest";

const { requestMock } = vi.hoisted(() => ({ requestMock: vi.fn() }));

vi.mock("server-only", () => ({}));

vi.mock("@/lib/api/authenticated-client", () => ({
  authenticatedApiRequest: requestMock,
}));

import { changeAccountPassword, createAccountReturn, getAccountOrder, getAccountReturn, getAccountReturns, getAccountSessions, logoutAllAccountSessions, revokeAccountSession } from "@/modules/account/api";

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

  // Burada güvenlik isteklerinin yalnız belgeli owner-scoped yolları ve HTTP metotlarını kullandığını doğruluyorum.
  it("uses the documented password and session endpoints", async () => {
    requestMock.mockResolvedValue(undefined);
    await changeAccountPassword({ currentPassword: "old-password", newPassword: "new-password" });
    await getAccountSessions();
    await revokeAccountSession("bb49d4c3-9752-4116-9179-657c8d6259b0");
    await logoutAllAccountSessions();

    expect(requestMock).toHaveBeenNthCalledWith(1, "/api/users/me/password", { method: "PUT", body: { currentPassword: "old-password", newPassword: "new-password" } });
    expect(requestMock).toHaveBeenNthCalledWith(2, "/api/users/me/sessions");
    expect(requestMock).toHaveBeenNthCalledWith(3, "/api/users/me/sessions/bb49d4c3-9752-4116-9179-657c8d6259b0", { method: "DELETE" });
    expect(requestMock).toHaveBeenNthCalledWith(4, "/api/users/me/sessions", { method: "DELETE" });
  });

  // Burada üye iade liste, detay ve oluşturma çağrılarının belgeli owner-scoped sözleşmeden sapmadığını doğruluyorum.
  it("uses the documented member return endpoints", async () => {
    requestMock.mockResolvedValue({ id: "return" });
    const payload = { orderId: "bb49d4c3-9752-4116-9179-657c8d6259b0", type: 0 as const, items: [{ orderItemId: "2de3f02f-d20a-4e09-8fcb-290870de9ed3", quantity: 1 }] };
    await getAccountReturns(2, 10);
    await getAccountReturn("4c929556-5f03-45a7-b660-2a193136306c");
    await createAccountReturn(payload);
    expect(requestMock).toHaveBeenNthCalledWith(1, "/api/returns/mine?PageNumber=2&PageSize=10");
    expect(requestMock).toHaveBeenNthCalledWith(2, "/api/returns/4c929556-5f03-45a7-b660-2a193136306c");
    expect(requestMock).toHaveBeenNthCalledWith(3, "/api/returns", { method: "POST", body: payload });
  });
});
