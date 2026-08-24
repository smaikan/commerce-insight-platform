import { beforeEach, describe, expect, it, vi } from "vitest";
import type { AdminSession } from "@/lib/auth/contracts";

const { apiRequestMock } = vi.hoisted(() => ({ apiRequestMock: vi.fn() }));
vi.mock("server-only", () => ({}));
vi.mock("@/lib/api/client", () => ({ apiRequest: apiRequestMock }));
import { cancelSalesOrder, createDirectSalesInvoice, createSalesInvoiceFromOrder, createSalesOrder, postSalesInvoice } from "./api";

const session = { accessToken: "token" } as AdminSession;
const header = { currentAccountId: "customer", orderNumber: "S-1", orderDate: "2026-08-24T00:00:00.000Z", currencyCode: "TRY", exchangeRate: 1, shippingTotal: 0, shippingPayer: 0 as const };
const invoice = { invoiceNumber: "F-1", invoiceDate: "2026-08-24T00:00:00.000Z" };
const lines = [{ lineNumber: 1, productVariantId: "variant", quantity: 1, unitOfMeasure: "Adet", unitsPerSaleUnit: 1, priceEntryMode: 1 as const, vatRate: 20, enteredUnitPrice: 10, isInvoiceDiscountEligible: true }];

describe("sales API adapter", () => {
  beforeEach(() => apiRequestMock.mockReset().mockResolvedValue({ id: "result" }));
  it("keeps required idempotency key on both direct create intents", async () => {
    await createSalesOrder(header, lines, true, invoice, "intent_A", session);
    await createDirectSalesInvoice(header, invoice, lines, "intent_B", session);
    expect(apiRequestMock).toHaveBeenNthCalledWith(1, "/api/accounting/sales-orders", { method: "POST", body: { header, lines, createInvoice: true, invoice }, headers: { "Idempotency-Key": "intent_A" }, accessToken: "token" });
    expect(apiRequestMock).toHaveBeenNthCalledWith(2, "/api/accounting/sales-invoices", { method: "POST", body: { orderHeader: header, invoiceHeader: invoice, lines }, headers: { "Idempotency-Key": "intent_B" }, accessToken: "token" });
  });
  it("does not invent a header for from-order and uses the linked invoice lifecycle", async () => {
    await createSalesInvoiceFromOrder("order/id", invoice, session); await postSalesInvoice("invoice/id", session); await cancelSalesOrder("order/id", "Hatalı kayıt", session);
    expect(apiRequestMock).toHaveBeenNthCalledWith(1, "/api/accounting/sales-invoices/from-order/order%2Fid", { method: "POST", body: { header: invoice }, accessToken: "token" });
    expect(apiRequestMock).toHaveBeenNthCalledWith(2, "/api/accounting/sales-invoices/invoice%2Fid/post", { method: "POST", accessToken: "token" });
    expect(apiRequestMock).toHaveBeenNthCalledWith(3, "/api/accounting/sales-orders/order%2Fid/cancel", { method: "POST", body: { reason: "Hatalı kayıt" }, accessToken: "token" });
  });
});
