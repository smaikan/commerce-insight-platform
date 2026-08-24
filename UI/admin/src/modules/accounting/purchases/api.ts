import "server-only";

import type { components } from "@/generated/api";
import { apiRequest } from "@/lib/api/client";
import type { PagedResult } from "@/lib/api/pagination";
import type { AdminSession } from "@/lib/auth/contracts";
import type {
  AvailableStockMovement,
  CurrentAccountOption,
  Expense,
  ExpenseCategory,
  ExpenseCategoryPage,
  ExpenseListQuery,
  ExpensePage,
  ProductVariantCostHistory,
  PurchaseInvoice,
  PurchaseInvoiceAllocationInput,
  PurchaseInvoiceExpense,
  PurchaseInvoiceExpenseInput,
  PurchaseInvoiceHeaderInput,
  PurchaseInvoiceLineInput,
  PurchaseInvoiceListQuery,
  PurchaseInvoicePage,
  PurchaseVariantOption,
} from "./types";

type Product = components["schemas"]["ProductDto"];

// Burada alış faturası sicilini yalnız API'nin desteklediği deterministik sayfalama ile okuyorum.
export function getPurchaseInvoices(query: PurchaseInvoiceListQuery, session: AdminSession): Promise<PurchaseInvoicePage> {
  const params = new URLSearchParams({ PageNumber: String(query.pageNumber), PageSize: String(query.pageSize) });
  return apiRequest(`/api/accounting/purchase-invoices?${params}`, { accessToken: session.accessToken });
}

export function getPurchaseInvoice(id: string, session: AdminSession): Promise<PurchaseInvoice> {
  return apiRequest(`/api/accounting/purchase-invoices/${encodeURIComponent(id)}`, { accessToken: session.accessToken });
}

export function createPurchaseInvoice(header: PurchaseInvoiceHeaderInput, lines: PurchaseInvoiceLineInput[], session: AdminSession): Promise<PurchaseInvoice> {
  return apiRequest("/api/accounting/purchase-invoices", { method: "POST", body: { header, lines }, accessToken: session.accessToken });
}

// Burada aynı varyanta ait mevcut pozitif Purchase hareketlerini fatura satırına tahsis ediyorum.
export function getAvailableStockMovements(productVariantId: string, session: AdminSession): Promise<AvailableStockMovement[]> {
  const params = new URLSearchParams({ productVariantId });
  return apiRequest(`/api/accounting/purchase-invoices/available-stock-movements?${params}`, { accessToken: session.accessToken });
}

export function setPurchaseInvoiceAllocations(invoiceId: string, lineId: string, allocations: PurchaseInvoiceAllocationInput[], session: AdminSession): Promise<PurchaseInvoice> {
  return apiRequest(`/api/accounting/purchase-invoices/${encodeURIComponent(invoiceId)}/lines/${encodeURIComponent(lineId)}/allocations`, { method: "PUT", body: allocations, accessToken: session.accessToken });
}

export function postPurchaseInvoice(id: string, session: AdminSession): Promise<PurchaseInvoice> {
  return apiRequest(`/api/accounting/purchase-invoices/${encodeURIComponent(id)}/post`, { method: "POST", accessToken: session.accessToken });
}

export function cancelPurchaseInvoice(id: string, reason: string, session: AdminSession): Promise<components["schemas"]["CancellationResultDto"]> {
  return apiRequest(`/api/accounting/purchase-invoices/${encodeURIComponent(id)}/cancel`, { method: "POST", body: { reason }, accessToken: session.accessToken });
}

export function getPurchaseInvoiceExpenses(id: string, session: AdminSession): Promise<PurchaseInvoiceExpense[]> {
  return apiRequest(`/api/accounting/purchase-invoices/${encodeURIComponent(id)}/expenses`, { accessToken: session.accessToken });
}

export function addPurchaseInvoiceExpense(id: string, input: PurchaseInvoiceExpenseInput, session: AdminSession): Promise<PurchaseInvoiceExpense> {
  return apiRequest(`/api/accounting/purchase-invoices/${encodeURIComponent(id)}/expenses`, { method: "POST", body: input, accessToken: session.accessToken });
}

// Burada fatura kaynaklı maliyet geçmişini varyant endpointinden okuyup kaynak faturaya göre ayırıyorum.
export async function getPurchaseInvoiceCostHistory(invoice: PurchaseInvoice, session: AdminSession): Promise<ProductVariantCostHistory[]> {
  const variantIds = [...new Set(invoice.lines.map((line) => line.productVariantId))];
  const histories = await Promise.all(variantIds.map((variantId) => apiRequest<ProductVariantCostHistory[]>(`/api/accounting/product-variants/${encodeURIComponent(variantId)}/cost-history`, { accessToken: session.accessToken })));
  return histories.flat().filter((item) => item.sourceType === 1 && item.sourceId === invoice.id);
}

// Burada accounting modülünün katalog ve cari seçimlerini kendi adapter sınırında hazırlıyorum.
export async function getPurchaseInvoiceLookups(session: AdminSession): Promise<{ suppliers: CurrentAccountOption[]; variants: PurchaseVariantOption[]; truncated: boolean }> {
  const [accounts, products] = await Promise.all([
    apiRequest<PagedResult<CurrentAccountOption>>("/api/accounting/current-accounts?PageNumber=1&PageSize=100", { accessToken: session.accessToken }),
    apiRequest<PagedResult<Product>>("/api/products?PageNumber=1&PageSize=100&SortBy=1&Descending=false", { accessToken: session.accessToken }),
  ]);
  const suppliers = accounts.items.filter((account) => account.isActive && (account.type === 2 || account.type === 3));
  const variants = products.items.flatMap((product) => product.variants.filter((variant) => product.isActive && variant.isActive).map((variant) => ({
    id: variant.id,
    productId: product.id,
    productName: product.title,
    variantName: `${variant.name}: ${variant.value}`,
    sku: variant.sku,
    isActive: variant.isActive,
  })));
  return { suppliers, variants, truncated: accounts.totalCount > 100 || products.totalCount > 100 };
}

export function getExpenseCategories(pageNumber: number, pageSize: number, session: AdminSession): Promise<ExpenseCategoryPage> {
  return apiRequest(`/api/accounting/expenses/categories?PageNumber=${pageNumber}&PageSize=${pageSize}`, { accessToken: session.accessToken });
}

export function createExpenseCategory(input: { code: string; name: string }, session: AdminSession): Promise<ExpenseCategory> {
  return apiRequest("/api/accounting/expenses/categories", { method: "POST", body: input, accessToken: session.accessToken });
}

export function getExpenses(query: ExpenseListQuery, session: AdminSession): Promise<ExpensePage> {
  return apiRequest(`/api/accounting/expenses?PageNumber=${query.expensePageNumber}&PageSize=${query.pageSize}`, { accessToken: session.accessToken });
}

export function createGeneralExpense(input: { categoryId: string; amountExcludingVat: number; vatRate: number; expenseDate: string; description: string }, session: AdminSession): Promise<Expense> {
  return apiRequest("/api/accounting/expenses", { method: "POST", body: input, accessToken: session.accessToken });
}
