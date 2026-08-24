import type { AccountingSalesOrder, SalesInvoice, SalesInvoiceEditDraft, SalesLineDraft, SalesOrderFormDraft } from "./types";

export function documentStatusLabel(status: number): string { return status === 1 ? "Taslak" : status === 2 ? "Post edildi" : status === 3 ? "İptal edildi" : "Bilinmiyor"; }
export function documentStatusClass(status: number): string { return status === 1 ? "border-amber-200 bg-amber-50 text-amber-900" : status === 2 ? "border-emerald-200 bg-emerald-50 text-emerald-900" : status === 3 ? "border-red-200 bg-red-50 text-red-900" : "border-border bg-surface-subtle text-muted"; }
export function shippingPayerLabel(value: number): string { return value === 1 ? "Satıcı" : value === 2 ? "Müşteri" : "Yok"; }
export function discountTypeLabel(value?: number | null): string { return value === 1 ? "Yüzde" : value === 2 ? "Sabit birim" : value === 3 ? "Sabit satır" : value === 4 ? "Sabit fatura" : "Yok"; }

export function newSalesOrderDraft(idempotencyKey: string, directInvoice = false): SalesOrderFormDraft {
  const today = new Date().toISOString().slice(0, 10);
  return { idempotencyKey, currentAccountId: "", orderNumber: "", orderDate: today, dueDate: "", shippingTotal: "0", shippingPayer: "0", description: "", invoiceDiscountType: "", invoiceDiscountValue: "", invoiceDiscountTaxBasis: "", createInvoice: directInvoice, invoiceNumber: "", invoiceDate: today, invoiceDueDate: "", invoiceDescription: "", lines: [newSalesLine(1)] };
}

export function salesOrderToDraft(order: AccountingSalesOrder): SalesOrderFormDraft {
  return { idempotencyKey: "edit", currentAccountId: order.currentAccountId, orderNumber: order.orderNumber, orderDate: dateValue(order.orderDate), dueDate: dateValue(order.dueDate), shippingTotal: String(order.shippingTotal), shippingPayer: String(order.shippingPayer), description: order.description ?? "", invoiceDiscountType: value(order.invoiceDiscountType), invoiceDiscountValue: value(order.invoiceDiscountValue), invoiceDiscountTaxBasis: value(order.invoiceDiscountTaxBasis), createInvoice: false, invoiceNumber: "", invoiceDate: "", invoiceDueDate: "", invoiceDescription: "", lines: order.items.map(toLineDraft) };
}

export function salesInvoiceToDraft(invoice: SalesInvoice): SalesInvoiceEditDraft {
  return { invoiceNumber: invoice.invoiceNumber, invoiceDate: dateValue(invoice.invoiceDate), dueDate: dateValue(invoice.dueDate), description: invoice.description ?? "", lines: invoice.lines.map(toLineDraft) };
}

export function newSalesLine(lineNumber: number, key = `new-${lineNumber}`): SalesLineDraft {
  return { key, lineNumber: String(lineNumber), productVariantId: "", quantity: "1", unitOfMeasure: "Adet", unitsPerSaleUnit: "1", priceEntryMode: "1", vatRate: "20", enteredUnitPrice: "0", lineDiscountType: "", lineDiscountValue: "", lineDiscountTaxBasis: "", lineDiscountUnitBasis: "", isInvoiceDiscountEligible: true };
}

function toLineDraft(line: AccountingSalesOrder["items"][number] | SalesInvoice["lines"][number]): SalesLineDraft {
  return { key: line.id, lineNumber: String(line.lineNumber), productVariantId: line.productVariantId, quantity: String(line.quantity), unitOfMeasure: line.unitOfMeasure, unitsPerSaleUnit: String(line.unitsPerSaleUnit), priceEntryMode: String(line.priceEntryMode), vatRate: String(line.vatRate), enteredUnitPrice: String(line.enteredUnitPrice), lineDiscountType: value(line.lineDiscountType), lineDiscountValue: value(line.lineDiscountValue), lineDiscountTaxBasis: value(line.lineDiscountTaxBasis), lineDiscountUnitBasis: value(line.lineDiscountUnitBasis), isInvoiceDiscountEligible: line.isInvoiceDiscountEligible };
}
function dateValue(value?: string | null): string { return value ? value.slice(0, 10) : ""; }
function value(input?: number | null): string { return input === null || input === undefined ? "" : String(input); }
