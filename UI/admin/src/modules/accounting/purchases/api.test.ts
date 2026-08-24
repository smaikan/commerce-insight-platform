import { beforeEach, describe, expect, it, vi } from "vitest";
import type { AdminSession } from "@/lib/auth/contracts";

const { apiRequestMock } = vi.hoisted(() => ({ apiRequestMock: vi.fn() }));
vi.mock("server-only", () => ({}));
vi.mock("@/lib/api/client", () => ({ apiRequest: apiRequestMock }));

import { createPurchaseInvoice, postPurchaseInvoice, setPurchaseInvoiceAllocations } from "./api";

const session = { accessToken: "test-token" } as AdminSession;

describe("purchase invoice API adapter", () => {
  beforeEach(() => apiRequestMock.mockReset().mockResolvedValue({ id: "invoice-id" }));

  it("sends the allocation body as the documented array", async () => {
    const allocations = [{ stockMovementId: "movement/id", quantity: 4 }];
    await setPurchaseInvoiceAllocations("invoice/id", "line/id", allocations, session);
    expect(apiRequestMock).toHaveBeenCalledWith("/api/accounting/purchase-invoices/invoice%2Fid/lines/line%2Fid/allocations", { method: "PUT", body: allocations, accessToken: "test-token" });
  });

  it("does not invent an Idempotency-Key for create or state-idempotent post", async () => {
    const header = { currentAccountId: "id", invoiceNumber: "A", invoiceDate: "2026-08-24T00:00:00Z", currencyCode: "TRY", exchangeRate: 1 };
    const lines = [{ lineNumber: 1, productVariantId: "id", purchaseQuantity: 1, unitOfMeasure: "Adet", unitsPerPurchaseUnit: 1, priceEntryMode: 1 as const, vatRate: 20, enteredUnitPrice: 10, isInvoiceDiscountEligible: true }];
    await createPurchaseInvoice(header, lines, session);
    await postPurchaseInvoice("invoice/id", session);
    expect(apiRequestMock).toHaveBeenNthCalledWith(1, "/api/accounting/purchase-invoices", { method: "POST", body: { header, lines }, accessToken: "test-token" });
    expect(apiRequestMock).toHaveBeenNthCalledWith(2, "/api/accounting/purchase-invoices/invoice%2Fid/post", { method: "POST", accessToken: "test-token" });
  });
});
