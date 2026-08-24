import { renderToStaticMarkup } from "react-dom/server";
import { describe, expect, it, vi } from "vitest";

vi.mock("next/navigation", () => ({ useRouter: () => ({ refresh: vi.fn() }) }));
vi.mock("@/lib/admin/components/confirm-dialog", () => ({ ConfirmDialog: () => null }));
vi.mock("@/modules/orders/actions", () => ({ manageReturnRequestAction: vi.fn() }));
vi.mock("@/modules/orders/return-presentation", () => import("../return-presentation"));
vi.mock("@/modules/orders/presentation", () => import("../presentation"));
vi.mock("@/modules/orders/return-action-state", () => ({ runReturnAction: vi.fn() }));
vi.mock("@/modules/orders/return-lifecycle", () => import("../return-lifecycle"));

import { OrderReturnManagement } from "./order-return-management";
import type { ReturnRequest } from "../types";

const orderId = "11111111-1111-4111-8111-111111111111";
const orderItemId = "22222222-2222-4222-8222-222222222222";

function returnRequest(overrides: Partial<ReturnRequest> = {}): ReturnRequest {
  return {
    id: "33333333-3333-4333-8333-333333333333",
    returnNumber: "RET-1001",
    orderId,
    type: 0,
    status: 0,
    refundTotal: 749.9,
    items: [{
      id: "44444444-4444-4444-8444-444444444444",
      orderItemId,
      productId: "P00042",
      productVariantId: "55555555-5555-4555-8555-555555555555",
      productTitle: "Keten Gömlek",
      variantSku: "KG-M-BEJ",
      unitPrice: 749.9,
      quantity: 1,
      lineTotal: 749.9,
      refundTotal: 749.9,
    }],
    createdAt: "2026-08-22T14:30:00Z",
    ...overrides,
  };
}

function renderReturn(request: ReturnRequest): string {
  return renderToStaticMarkup(
    <OrderReturnManagement
      orderId={orderId}
      orderItems={[{ id: orderItemId, quantity: 1 }]}
      returns={[request]}
    />,
  );
}

describe("OrderReturnManagement lifecycle controls", () => {
  it("shows only physical receipt for a requested return", () => {
    const html = renderReturn(returnRequest());

    expect(html).toContain("Ürünleri teslim aldım");
    expect(html).not.toContain("İadeyi onayla");
    expect(html).not.toContain("Talebi reddet");
    expect(html).not.toContain("Karar notu");
  });

  it("shows the decision controls only after a new return is received", () => {
    const html = renderReturn(returnRequest({
      status: 3,
      receivedAt: "2026-08-23T10:00:00Z",
    }));

    expect(html).toContain("İadeyi onayla");
    expect(html).toContain("Talebi reddet");
    expect(html).toContain("Karar notu");
    expect(html).not.toContain("Ürünleri teslim aldım");
    expect(html).not.toContain("Eski iade sürecini tamamla");
  });

  it("keeps legacy receipt and completion controls bounded by timestamps", () => {
    const approvedHtml = renderReturn(returnRequest({
      status: 1,
      approvedAt: "2026-08-20T09:00:00Z",
    }));
    const receivedHtml = renderReturn(returnRequest({
      status: 3,
      approvedAt: "2026-08-20T09:00:00Z",
      receivedAt: "2026-08-21T10:00:00Z",
    }));

    expect(approvedHtml).toContain("Ürünleri teslim aldım");
    expect(receivedHtml).toContain("Eski iade sürecini tamamla");
    expect(receivedHtml).not.toContain("İadeyi onayla");
  });
});
