import { describe, expect, it } from "vitest";
import { parseAllocationForm, parseGeneralExpenseForm, parsePurchaseInvoiceExpenseForm, parsePurchaseInvoiceForm } from "./form-data";

const supplierId = "11111111-1111-4111-8111-111111111111";
const variantId = "22222222-2222-4222-8222-222222222222";
const lineId = "33333333-3333-4333-8333-333333333333";
const movementId = "44444444-4444-4444-8444-444444444444";

function invoiceForm(linePatch: Record<string, unknown> = {}): FormData {
  const data = new FormData();
  data.set("currentAccountId", supplierId); data.set("invoiceNumber", "ALIŞ-001"); data.set("invoiceDate", "2026-08-24"); data.set("dueDate", "2026-09-24"); data.set("description", "Kontrollü taslak");
  data.set("linesJson", JSON.stringify([{ key: "new-1", lineNumber: "1", productVariantId: variantId, purchaseQuantity: "2", unitOfMeasure: "Koli", unitsPerPurchaseUnit: "5", priceEntryMode: "1", vatRate: "20", enteredUnitPrice: "100", isInvoiceDiscountEligible: true, hasAllocations: false, ...linePatch }]));
  return data;
}

describe("purchase invoice form contract", () => {
  it("maps a valid draft to TRY wire inputs without inventing discounts", () => {
    const result = parsePurchaseInvoiceForm(invoiceForm());
    expect(result.ok).toBe(true);
    if (!result.ok) return;
    expect(result.header).toEqual({ currentAccountId: supplierId, invoiceNumber: "ALIŞ-001", invoiceDate: "2026-08-24T00:00:00.000Z", dueDate: "2026-09-24T00:00:00.000Z", currencyCode: "TRY", exchangeRate: 1, description: "Kontrollü taslak" });
    expect(result.lines[0]).toEqual({ lineNumber: 1, productVariantId: variantId, purchaseQuantity: 2, unitOfMeasure: "Koli", unitsPerPurchaseUnit: 5, priceEntryMode: 1, vatRate: 20, enteredUnitPrice: 100, isInvoiceDiscountEligible: true });
  });

  it("rejects fractional stock quantity and VAT outside documented bounds while retaining the draft", () => {
    const result = parsePurchaseInvoiceForm(invoiceForm({ purchaseQuantity: "1.25", unitsPerPurchaseUnit: "1", vatRate: "101" }));
    expect(result.ok).toBe(false);
    if (result.ok) return;
    expect(result.state.fieldErrors).toMatchObject({ "lines.0.unitsPerPurchaseUnit": expect.any(Array), "lines.0.vatRate": expect.any(Array) });
    expect(result.state.draft?.lines[0]?.purchaseQuantity).toBe("1.25");
  });
});

describe("purchase allocation and expense contracts", () => {
  it("requires a unique positive allocation array", () => {
    const data = new FormData();
    data.set("allocationsJson", JSON.stringify([{ stockMovementId: movementId, quantity: "6" }]));
    expect(parseAllocationForm(data)).toMatchObject({ ok: true, allocations: [{ stockMovementId: movementId, quantity: 6 }] });
    data.set("allocationsJson", "[]");
    expect(parseAllocationForm(data)).toMatchObject({ ok: false, state: { fieldErrors: { allocations: expect.any(Array) } } });
  });

  it("requires manual expense allocations to cover every line and equal the expense", () => {
    const data = new FormData();
    data.set("categoryId", supplierId); data.set("allocationMethod", "3"); data.set("amountExcludingVat", "100"); data.set("vatRate", "20"); data.set("description", "Nakliye");
    data.set("manualAllocationsJson", JSON.stringify([{ purchaseInvoiceLineId: lineId, amountExcludingVat: "90" }]));
    const rejected = parsePurchaseInvoiceExpenseForm(data, [lineId]);
    expect(rejected).toMatchObject({ ok: false, state: { fieldErrors: { manualAllocations: expect.any(Array) } } });
    data.set("manualAllocationsJson", JSON.stringify([{ purchaseInvoiceLineId: lineId, amountExcludingVat: "100" }]));
    expect(parsePurchaseInvoiceExpenseForm(data, [lineId])).toMatchObject({ ok: true, input: { allocationMethod: 3, amountExcludingVat: 100, manualAllocations: [{ purchaseInvoiceLineId: lineId, amountExcludingVat: 100 }] } });
  });

  it("enforces documented VAT bounds for general expenses", () => {
    const data = new FormData();
    data.set("categoryId", supplierId); data.set("amountExcludingVat", "10"); data.set("vatRate", "120"); data.set("expenseDate", "2026-08-24"); data.set("description", "Test gideri");
    expect(parseGeneralExpenseForm(data)).toMatchObject({ ok: false, state: { fieldErrors: { vatRate: expect.any(Array) } } });
  });
});
