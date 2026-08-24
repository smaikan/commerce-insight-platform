import { afterEach, describe, expect, it, vi } from "vitest";

import {
  cancelCustomerOrder,
  loadCustomerOrderCancellation,
  loadOrderAfterCancellation,
} from "@/modules/orders/client/cancellation-api";

const orderId = "bb49d4c3-9752-4116-9179-657c8d6259b0";
const operation = {
  operationId: "3470e031-3fc8-42af-9755-f0fcae2b06cb",
  orderId,
  status: 2,
  reversalType: 0,
  createdAt: "2026-08-24T07:19:00Z",
  updatedAt: "2026-08-24T07:19:03Z",
  nextAttemptAt: null,
  pollingUrl: `/api/orders/${orderId}/cancellation`,
};

afterEach(() => vi.unstubAllGlobals());

describe("customer order cancellation client", () => {
  // Burada 200 OrderDto cevabının tamamlanmış sonuç olarak ayrıştırıldığını doğruluyorum.
  it("returns a completed order only for 200", async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response(JSON.stringify({ id: orderId, orderNumber: "ORD-1", status: 6 }), {
      status: 200,
      headers: { "Content-Type": "application/json" },
    }));
    vi.stubGlobal("fetch", fetchMock);

    await expect(cancelCustomerOrder(orderId, "member")).resolves.toMatchObject({ kind: "completed", order: { status: 6 } });
    expect(fetchMock).toHaveBeenCalledWith(
      `/api/checkout/orders/${orderId}/cancel`,
      expect.objectContaining({ method: "POST", cache: "no-store", credentials: "same-origin" }),
    );
  });

  // Burada 202 operasyon gövdesinin OrderDto sanılmadan pending sonuç olarak korunduğunu doğruluyorum.
  it("keeps a 202 cancellation operation pending", async () => {
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue(new Response(JSON.stringify(operation), {
      status: 202,
      headers: { "Content-Type": "application/json" },
    })));

    await expect(cancelCustomerOrder(orderId, "member")).resolves.toEqual({ kind: "pending", operation });
  });

  // Burada magic-link guest işlemlerinin üye öncelikli route yerine guest grant BFF yollarını kullandığını doğruluyorum.
  it("uses dedicated guest cancellation and polling routes", async () => {
    const fetchMock = vi.fn()
      .mockResolvedValueOnce(new Response(JSON.stringify(operation), { status: 202, headers: { "Content-Type": "application/json" } }))
      .mockResolvedValueOnce(new Response(JSON.stringify(operation), { status: 200, headers: { "Content-Type": "application/json" } }))
      .mockResolvedValueOnce(new Response(JSON.stringify({ id: orderId, orderNumber: "ORD-1", status: 6 }), { status: 200, headers: { "Content-Type": "application/json" } }));
    vi.stubGlobal("fetch", fetchMock);

    await cancelCustomerOrder(orderId, "guest");
    await loadCustomerOrderCancellation(orderId, "guest");
    await loadOrderAfterCancellation(orderId, "guest");

    expect(fetchMock.mock.calls.map(([path]) => path)).toEqual([
      `/api/guest-orders/${orderId}/cancel`,
      `/api/guest-orders/${orderId}/cancellation`,
      `/api/guest-orders/${orderId}`,
    ]);
  });

  // Burada response içindeki pollingUrl'in browser fetch hedefi olarak güvenilmediğini doğruluyorum.
  it("does not follow a provider supplied polling URL", async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response(JSON.stringify({ ...operation, pollingUrl: "https://unsafe.example/operation" }), {
      status: 200,
      headers: { "Content-Type": "application/json" },
    }));
    vi.stubGlobal("fetch", fetchMock);

    await loadCustomerOrderCancellation(orderId, "member");
    expect(fetchMock).toHaveBeenCalledOnce();
    expect(fetchMock.mock.calls[0]?.[0]).toBe(`/api/checkout/orders/${orderId}/cancellation`);
  });

  // Burada ProblemDetails kodu ve traceId'nin müşteri mesajı için kaybolmadan taşındığını doğruluyorum.
  it("preserves documented ProblemDetails fields", async () => {
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue(new Response(JSON.stringify({
      title: "Conflict",
      status: 409,
      code: "payment_reversal_manual_review",
      traceId: "trace-safe",
    }), { status: 409, headers: { "Content-Type": "application/problem+json" } })));

    await expect(cancelCustomerOrder(orderId, "member")).rejects.toMatchObject({
      problem: { status: 409, code: "payment_reversal_manual_review", traceId: "trace-safe" },
    });
  });

  // Burada aynı siparişe ait olmayan veya biçimi eksik 202 gövdesinin polling'e alınmadığını doğruluyorum.
  it("rejects malformed or cross-order operation responses", async () => {
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue(new Response(JSON.stringify({ ...operation, orderId: crypto.randomUUID() }), {
      status: 202,
      headers: { "Content-Type": "application/json" },
    })));

    await expect(cancelCustomerOrder(orderId, "member")).rejects.toMatchObject({
      problem: { status: 502, code: "invalid_cancellation_response" },
    });
  });
});
