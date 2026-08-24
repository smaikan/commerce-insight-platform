import type { components } from "@/generated/api";
import type { PagedResult } from "@/lib/api/pagination";

export type PurchaseInvoice = components["schemas"]["PurchaseInvoiceDto"];
export type PurchaseInvoiceSummary = components["schemas"]["PurchaseInvoiceSummaryDto"];
export type PurchaseInvoicePage = PagedResult<PurchaseInvoiceSummary>;
export type PurchaseInvoiceHeaderInput = components["schemas"]["PurchaseInvoiceHeaderInput"];
export type PurchaseInvoiceLineInput = components["schemas"]["PurchaseInvoiceLineInput"];
export type PurchaseInvoiceAllocationInput = components["schemas"]["PurchaseInvoiceAllocationInput"];
export type AvailableStockMovement = components["schemas"]["AvailableStockMovementDto"];
export type PurchaseInvoiceExpense = components["schemas"]["PurchaseInvoiceExpenseDto"];
export type PurchaseInvoiceExpenseInput = components["schemas"]["AddPurchaseInvoiceExpenseRequest"];
export type ExpenseCategory = components["schemas"]["ExpenseCategoryDto"];
export type Expense = components["schemas"]["ExpenseDto"];
export type ExpensePage = PagedResult<Expense>;
export type ExpenseCategoryPage = PagedResult<ExpenseCategory>;
export type ProductVariantCostHistory = components["schemas"]["ProductVariantCostHistoryDto"];
export type CurrentAccountOption = components["schemas"]["CurrentAccountDto"];

export type PurchaseVariantOption = {
  id: string;
  productId: string;
  productName: string;
  variantName: string;
  sku: string;
  isActive: boolean;
};

export type PurchaseInvoiceListQuery = { pageNumber: number; pageSize: number };
export type ExpenseListQuery = { view: "general" | "categories"; expensePageNumber: number; categoryPageNumber: number; pageSize: number };

export type PurchaseInvoiceLineDraft = {
  key: string;
  lineNumber: string;
  productVariantId: string;
  purchaseQuantity: string;
  unitOfMeasure: string;
  unitsPerPurchaseUnit: string;
  priceEntryMode: string;
  vatRate: string;
  enteredUnitPrice: string;
  isInvoiceDiscountEligible: boolean;
  hasAllocations: boolean;
};

export type PurchaseInvoiceFormDraft = {
  currentAccountId: string;
  invoiceNumber: string;
  invoiceDate: string;
  dueDate: string;
  description: string;
  lines: PurchaseInvoiceLineDraft[];
};

export type AccountingFormState<TDraft = undefined> = {
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

export type PurchaseInvoiceAllocationDraft = {
  allocations: Array<{ stockMovementId: string; quantity: string }>;
};

export type PurchaseInvoiceExpenseDraft = {
  categoryId: string;
  allocationMethod: string;
  amountExcludingVat: string;
  vatRate: string;
  description: string;
  manualAllocations: Array<{ purchaseInvoiceLineId: string; amountExcludingVat: string }>;
};

export type ExpenseCategoryDraft = { code: string; name: string };
export type GeneralExpenseDraft = { categoryId: string; amountExcludingVat: string; vatRate: string; expenseDate: string; description: string };

export const initialAccountingFormState: AccountingFormState = { status: "idle" };
