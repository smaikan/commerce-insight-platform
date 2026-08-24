import type { PurchaseInvoiceFormDraft } from "./types";

export function invoiceStatusLabel(status: number): string {
  return status === 1 ? "Taslak" : status === 2 ? "Post edildi" : status === 3 ? "İptal edildi" : "Bilinmiyor";
}

export function invoiceStatusClass(status: number): string {
  return status === 1
    ? "border-amber-200 bg-amber-50 text-amber-900"
    : status === 2
      ? "border-emerald-200 bg-emerald-50 text-emerald-900"
      : status === 3
        ? "border-red-200 bg-red-50 text-red-900"
        : "border-border bg-surface-subtle text-muted";
}

export function expenseAllocationMethodLabel(method: number): string {
  return method === 1 ? "KDV hariç satır tutarı" : method === 2 ? "Stok miktarı" : method === 3 ? "Manuel" : "Bilinmiyor";
}

export function newPurchaseInvoiceDraft(): PurchaseInvoiceFormDraft {
  const today = new Date().toISOString().slice(0, 10);
  return {
    currentAccountId: "", invoiceNumber: "", invoiceDate: today, dueDate: "", description: "",
    lines: [{ key: "new-1", lineNumber: "1", productVariantId: "", purchaseQuantity: "1", unitOfMeasure: "Adet", unitsPerPurchaseUnit: "1", priceEntryMode: "1", vatRate: "20", enteredUnitPrice: "0", isInvoiceDiscountEligible: true, hasAllocations: false }],
  };
}
