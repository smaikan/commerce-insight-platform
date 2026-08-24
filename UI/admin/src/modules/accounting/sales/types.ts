import type { components } from "@/generated/api";
import type { PagedResult } from "@/lib/api/pagination";

export type AccountingSalesOrder = components["schemas"]["AccountingSalesOrderDto"];
export type AccountingSalesOrderSummary = components["schemas"]["AccountingSalesOrderSummaryDto"];
export type AccountingSalesOrderPage = PagedResult<AccountingSalesOrderSummary>;
export type AccountingSalesOrderHeaderInput = components["schemas"]["AccountingSalesOrderHeaderInput"];
export type AccountingSalesOrderLineInput = components["schemas"]["AccountingSalesOrderLineInput"];
export type SalesInvoice = components["schemas"]["SalesInvoiceDto"];
export type SalesInvoiceSummary = components["schemas"]["SalesInvoiceSummaryDto"];
export type SalesInvoicePage = PagedResult<SalesInvoiceSummary>;
export type SalesInvoiceHeaderInput = components["schemas"]["SalesInvoiceHeaderInput"];
export type CurrentAccountOption = components["schemas"]["CurrentAccountDto"];

export type SalesVariantOption = {
  id: string;
  productId: string;
  productName: string;
  variantName: string;
  sku: string;
};

export type SalesListQuery = { pageNumber: number; pageSize: number };

export type SalesLineDraft = {
  key: string;
  lineNumber: string;
  productVariantId: string;
  quantity: string;
  unitOfMeasure: string;
  unitsPerSaleUnit: string;
  priceEntryMode: string;
  vatRate: string;
  enteredUnitPrice: string;
  lineDiscountType: string;
  lineDiscountValue: string;
  lineDiscountTaxBasis: string;
  lineDiscountUnitBasis: string;
  isInvoiceDiscountEligible: boolean;
};

export type SalesOrderFormDraft = {
  idempotencyKey: string;
  currentAccountId: string;
  orderNumber: string;
  orderDate: string;
  dueDate: string;
  shippingTotal: string;
  shippingPayer: string;
  description: string;
  invoiceDiscountType: string;
  invoiceDiscountValue: string;
  invoiceDiscountTaxBasis: string;
  createInvoice: boolean;
  invoiceNumber: string;
  invoiceDate: string;
  invoiceDueDate: string;
  invoiceDescription: string;
  lines: SalesLineDraft[];
};

export type SalesInvoiceEditDraft = {
  invoiceNumber: string;
  invoiceDate: string;
  dueDate: string;
  description: string;
  lines: SalesLineDraft[];
};

export type InvoiceFromOrderDraft = {
  invoiceNumber: string;
  invoiceDate: string;
  dueDate: string;
  description: string;
};

export type SalesFormState<TDraft = undefined> = {
  status: "idle" | "success" | "error";
  message?: string;
  code?: string;
  traceId?: string;
  retryAfter?: string;
  fieldErrors?: Record<string, string[]>;
  draft?: TDraft;
  redirectHref?: string;
  refresh?: boolean;
};

export const initialSalesFormState: SalesFormState = { status: "idle" };
