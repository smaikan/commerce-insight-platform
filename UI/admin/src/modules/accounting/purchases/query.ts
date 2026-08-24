import type { ExpenseListQuery, PurchaseInvoiceListQuery } from "./types";

const PAGE_SIZE = 20;

// Burada alış faturası sicilinin yalnız belgelenmiş sayfalama parametrelerini kabul ediyorum.
export function parsePurchaseInvoiceListQuery(params: Record<string, string | string[] | undefined>): PurchaseInvoiceListQuery {
  return { pageNumber: positiveInteger(params.pageNumber, 1), pageSize: PAGE_SIZE };
}

export function buildPurchaseInvoiceListHref(query: PurchaseInvoiceListQuery, pageNumber = query.pageNumber): string {
  return pageNumber <= 1 ? "/accounting/purchase-invoices" : `/accounting/purchase-invoices?pageNumber=${pageNumber}`;
}

// Burada gider ve kategori sicillerinin bağımsız sayfalarını aynı URL üzerinde koruyorum.
export function parseExpenseListQuery(params: Record<string, string | string[] | undefined>): ExpenseListQuery {
  const rawView = Array.isArray(params.view) ? params.view[0] : params.view;
  return {
    view: rawView === "categories" ? "categories" : "general",
    expensePageNumber: positiveInteger(params.expensePageNumber, 1),
    categoryPageNumber: positiveInteger(params.categoryPageNumber, 1),
    pageSize: PAGE_SIZE,
  };
}

export function buildExpenseListHref(query: ExpenseListQuery, patch: Partial<ExpenseListQuery> = {}): string {
  const next = { ...query, ...patch };
  const params = new URLSearchParams();
  if (next.view === "categories") params.set("view", "categories");
  if (next.expensePageNumber > 1) params.set("expensePageNumber", String(next.expensePageNumber));
  if (next.categoryPageNumber > 1) params.set("categoryPageNumber", String(next.categoryPageNumber));
  const encoded = params.toString();
  return encoded ? `/accounting/expenses?${encoded}` : "/accounting/expenses";
}

export function canonicalPageNumber(requested: number, totalPages: number): number | null {
  const canonical = totalPages === 0 ? 1 : Math.min(requested, totalPages);
  return canonical === requested ? null : canonical;
}

function positiveInteger(value: string | string[] | undefined, fallback: number): number {
  const parsed = Number(Array.isArray(value) ? value[0] : value);
  return Number.isInteger(parsed) && parsed > 0 ? Math.min(parsed, 10_000) : fallback;
}
