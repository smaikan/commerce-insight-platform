import { describe, expect, it } from "vitest";

import type { CheckoutOrder } from "@/modules/checkout/types";

import { authoritativePaymentState } from "./payment-state";

function order(overrides: Partial<CheckoutOrder> = {}): CheckoutOrder {
  return {
    id: "bb49d4c3-9752-4116-9179-657c8d6259b0",
    orderNumber: "ORD-1",
    status: 0,
    subTotal: 100,
    discountTotal: 0,
    shippingTotal: 0,
    taxTotal: 0,
    grandTotal: 100,
    items: [],
    payments: [],
    createdAt: "2026-08-16T10:00:00Z",
    ...overrides,
  };
}

describe("authoritative checkout payment state", () => {
  // Burada URL query değerlerinden bağımsız olarak Paid order veya ödeme kaydının sonucu kesinleştirdiğini doğruluyorum.
  it("recognizes paid orders and payments", () => {
    expect(authoritativePaymentState(order({ status: 2 }))).toBe("paid");
    expect(authoritativePaymentState(order({ payments: [{ id: "p1", provider: 1, status: 1, amount: 100, createdAt: "2026-08-16T10:01:00Z" }] }))).toBe("paid");
    expect(authoritativePaymentState(order({ grandTotal: 0 }))).toBe("paid");
  });

  // Burada en güncel iyzico denemesinin başarısız/iptal sonucunu, diğer sağlayıcı kayıtlarını karıştırmadan belirlediğini doğruluyorum.
  it("uses the latest iyzico attempt for failed and pending states", () => {
    expect(authoritativePaymentState(order({ payments: [{ id: "p1", provider: 1, status: 2, amount: 100, createdAt: "2026-08-16T10:01:00Z" }] }))).toBe("failed");
    expect(authoritativePaymentState(order({ payments: [
      { id: "p1", provider: 1, status: 2, amount: 100, createdAt: "2026-08-16T10:01:00Z" },
      { id: "p2", provider: 1, status: 0, amount: 100, createdAt: "2026-08-16T10:02:00Z" },
    ] }))).toBe("pending");
    expect(authoritativePaymentState(order({ payments: [{ id: "p1", provider: 2, status: 2, amount: 100, createdAt: "2026-08-16T10:01:00Z" }] }))).toBe("pending");
  });
});
